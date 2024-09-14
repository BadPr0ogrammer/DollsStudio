using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Linq;

using Microsoft.Win32;
using HelixToolkit.Wpf.SharpDX.Assimp;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Animations;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Model;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using SharpDX;
using BoundingBox = SharpDX.BoundingBox;
using OrthographicCamera = HelixToolkit.Wpf.SharpDX.OrthographicCamera;
using MeshGeometry3D = HelixToolkit.Wpf.SharpDX.MeshGeometry3D;


namespace DollsStudio
{
    public class MainViewModel : BaseViewModel
    {
        private string OpenFileFilter = $"{HelixToolkit.Wpf.SharpDX.Assimp.Importer.SupportedFormatsString}";
        private string ExportFileFilter = $"{HelixToolkit.Wpf.SharpDX.Assimp.Exporter.SupportedFormatsString}";

        private bool showWireframe = false;
        public bool ShowWireframe
        {
            set
            {
                if (SetValue(ref showWireframe, value))
                {
                    ShowWireframeFunct(value);
                }
            }
            get
            {
                return showWireframe;
            }
        }

        private bool renderFlat = false;
        public bool RenderFlat
        {
            set
            {
                if (SetValue(ref renderFlat, value))
                {
                    RenderFlatFunct(value);
                }
            }
            get
            {
                return renderFlat;
            }
        }

        private bool renderEnvironmentMap = true;
        public bool RenderEnvironmentMap
        {
            set
            {
                if (SetValue(ref renderEnvironmentMap, value) && _scene != null && _scene.Root != null)
                {
                    foreach (var node in _scene.Root.Traverse())
                    {
                        if (node is MaterialGeometryNode m && m.Material is PBRMaterialCore material)
                        {
                            material.RenderEnvironmentMap = value;
                        }
                    }
                }
            }
            get => renderEnvironmentMap;
        }

        public ICommand OpenFileCommand
        {
            get; set;
        }

        public ICommand ResetCameraCommand
        {
            set; get;
        }

        public ICommand ExportCommand { private set; get; }

        public ICommand CopyAsBitmapCommand { private set; get; }

        public ICommand CopyAsHiresBitmapCommand { private set; get; }

        private bool isLoading = false;
        public bool IsLoading
        {
            private set => SetValue(ref isLoading, value);
            get => isLoading;
        }

        private bool isPlaying = false;
        public bool IsPlaying
        {
            private set => SetValue(ref isPlaying, value);
            get => isPlaying;
        }

        private float startTime;
        public float StartTime
        {
            private set => SetValue(ref startTime, value);
            get => startTime;
        }

        private float endTime;
        public float EndTime
        {
            private set => SetValue(ref endTime, value);
            get => endTime;
        }

        private float currAnimationTime = 0;
        public float CurrAnimationTime
        {
            set
            {
                if (EndTime == 0)
                { return; }
                if (SetValue(ref currAnimationTime, value % EndTime + StartTime))
                {
                    _animationUpdater?.Update(value, 1);
                }
            }
            get => currAnimationTime;
        }

        public ObservableCollection<IAnimationUpdater> Animations { get; } = new ObservableCollection<IAnimationUpdater>();

        public SceneNodeGroupModel3D GroupModel { get; } = new SceneNodeGroupModel3D();

        private IAnimationUpdater selectedAnimation = null;
        public IAnimationUpdater SelectedAnimation
        {
            set
            {
                if (SetValue(ref selectedAnimation, value))
                {
                    StopAnimation();
                    CurrAnimationTime = 0;
                    if (value != null)
                    {
                        _animationUpdater = value;
                        _animationUpdater.Reset();
                        _animationUpdater.RepeatMode = AnimationRepeatMode.Loop;
                        StartTime = value.StartTime;
                        EndTime = value.EndTime;
                    }
                    else
                    {
                        _animationUpdater = null;
                        StartTime = EndTime = 0;
                    }
                }
            }
            get
            {
                return selectedAnimation;
            }
        }

        private float speed = 1.0f;
        public float Speed
        {
            set
            {
                SetValue(ref speed, value);
            }
            get => speed;
        }

        private Point3D modelCentroid = default;
        public Point3D ModelCentroid
        {
            private set => SetValue(ref modelCentroid, value);
            get => modelCentroid;
        }
        private BoundingBox modelBound = new BoundingBox();
        public BoundingBox ModelBound
        {
            private set => SetValue(ref modelBound, value);
            get => modelBound;
        }
        public TextureModel EnvironmentMap { get; }

        public ICommand PlayCommand { get; }

        private SynchronizationContext _context = SynchronizationContext.Current;
        private HelixToolkitScene _scene;
        private IAnimationUpdater _animationUpdater;
        private List<BoneSkinMeshNode> _boneSkinNodes = new List<BoneSkinMeshNode>();
        private List<BoneSkinMeshNode> _skeletonNodes = new List<BoneSkinMeshNode>();
        private CompositionTargetEx _compositeHelper = new CompositionTargetEx();
        private long _initTimeStamp = 0;

        private MeshSimplification _simHelper;

        public ICommand SimplifyCommand { private set; get; }
        public ICommand ResetCommand { private set; get; }

        public int NumberOfTriangles { set; get; } = 0;
        public int NumberOfVertices { set; get; } = 0;

        private MeshGeometry3D simpleModel;
        public MeshGeometry3D SimpleModel
        {
            get { return simpleModel; }
            private set
            {
                if (SetValue(ref simpleModel, value))
                {
                    NumberOfTriangles = simpleModel.Indices.Count / 3;
                    NumberOfVertices = simpleModel.Positions.Count;
                }
            }
        }

        private MeshGeometry3D _orgMesh;

        private MainWindow _mainWindow = null;

        public MainViewModel(MainWindow window)
        {
            _mainWindow = window;

            this.OpenFileCommand = new DelegateCommand(this.OpenFile);
            EffectsManager = new DefaultEffectsManager();
            Camera = new OrthographicCamera()
            {
                LookDirection = new System.Windows.Media.Media3D.Vector3D(0, -10, -10),
                Position = new System.Windows.Media.Media3D.Point3D(0, 10, 10),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                FarPlaneDistance = 5000,
                NearPlaneDistance = 0.1f
            };
            ResetCameraCommand = new DelegateCommand(() =>
            {
                (Camera as OrthographicCamera).Reset();
                (Camera as OrthographicCamera).FarPlaneDistance = 5000;
                (Camera as OrthographicCamera).NearPlaneDistance = 0.1f;
            });
            ExportCommand = new DelegateCommand(() => { ExportFile(); });

            CopyAsBitmapCommand = new DelegateCommand(() => { CopyAsBitmapToClipBoard(_mainWindow.view); });
            CopyAsHiresBitmapCommand = new DelegateCommand(() => { CopyAsHiResBitmapToClipBoard(_mainWindow.view); });

            EnvironmentMap = TextureModel.Create("Cubemap_Grandcanyon.dds");

            PlayCommand = new DelegateCommand(() =>
            {
                if (!IsPlaying && SelectedAnimation != null)
                {
                    StartAnimation();
                }
                else
                {
                    StopAnimation();
                }
            });

            SimplifyCommand = new RelayCommand(Simplify, CanSimplify);
            ResetCommand = new RelayCommand((o) =>
            {
                simpleModel = _orgMesh;
                _simHelper = new MeshSimplification(simpleModel);
            }, CanSimplify);
        }

        private void CopyAsBitmapToClipBoard(Viewport3DX viewport)
        {
            var bitmap = ViewportExtensions.RenderBitmap(viewport);
            try
            {
                Clipboard.Clear();
                Clipboard.SetImage(bitmap);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private void CopyAsHiResBitmapToClipBoard(Viewport3DX viewport)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var bitmap = ViewportExtensions.RenderBitmap(viewport, 1920, 1080);
            try
            {
                Clipboard.Clear();
                Clipboard.SetImage(bitmap);
                stopwatch.Stop();
                Debug.WriteLine($"creating bitmap needs {stopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public void OpenTheFile(string path)
        {
            StopAnimation();
            var syncContext = SynchronizationContext.Current;
            IsLoading = true;
            Task.Run(() =>
            {
                var loader = new Importer();
                var scene = loader.Load(path);
                scene.Root.Attach(EffectsManager); // Pre attach scene graph
                scene.Root.UpdateAllTransformMatrix();
                if (scene.Root.TryGetBound(out var bound))
                {
                    /// Must use UI thread to set value back.
                    syncContext.Post((o) => { ModelBound = bound; }, null);
                }
                if (scene.Root.TryGetCentroid(out var centroid))
                {
                    /// Must use UI thread to set value back.
                    syncContext.Post((o) => { ModelCentroid = centroid.ToPoint3D(); }, null);
                }
                return scene;
            }).ContinueWith((result) =>
               {
                   IsLoading = false;
                   if (result.IsCompleted)
                   {
                       _scene = result.Result;

                       //.Select(x => x.Geometry as MeshGeometry3D).ToArray()[0];
                       var root = _scene.Root;
                       //simpleModel = _scene.Root as Model3D;
                       _orgMesh = simpleModel;
                       _simHelper = new MeshSimplification(simpleModel);


                       Animations.Clear();
                       var oldNode = GroupModel.SceneNode.Items.ToArray();
                       GroupModel.Clear(false);
                       Task.Run(() =>
                       {
                           foreach (var node in oldNode)
                           { node.Dispose(); }
                       });
                       if (_scene != null)
                       {
                           if (_scene.Root != null)
                           {
                               foreach (var node in _scene.Root.Traverse())
                               {
                                   if (node is MaterialGeometryNode m)
                                   {
                                       //m.Geometry.SetAsTransient();
                                       if (m.Material is PBRMaterialCore pbr)
                                       {
                                           pbr.RenderEnvironmentMap = RenderEnvironmentMap;
                                       }
                                       else if (m.Material is PhongMaterialCore phong)
                                       {
                                           phong.RenderEnvironmentMap = RenderEnvironmentMap;
                                       }
                                   }
                               }
                           }
                           GroupModel.AddNode(_scene.Root);
                           if (_scene.HasAnimation)
                           {
                               var dict = _scene.Animations.CreateAnimationUpdaters();
                               foreach (var ani in dict.Values)
                               {
                                   Animations.Add(ani);
                               }
                           }
                           foreach (var n in _scene.Root.Traverse())
                           {
                               n.Tag = new AttachedNodeViewModel(n);
                           }

                           FocusCameraToScene();
                       }
                   }
                   else if (result.IsFaulted && result.Exception != null)
                   {
                       MessageBox.Show(result.Exception.Message);
                   }
               }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void OpenFile()
        {
            if (isLoading)
            {
                return;
            }
            string path = OpenFileDialog(OpenFileFilter);
            if (path == null)
            {
                return;
            }
            OpenTheFile(path);
        }

        public void StartAnimation()
        {
            _initTimeStamp = Stopwatch.GetTimestamp();
            _compositeHelper.Rendering += CompositeHelper_Rendering;
            IsPlaying = true;
        }

        public void StopAnimation()
        {
            IsPlaying = false;
            _compositeHelper.Rendering -= CompositeHelper_Rendering;
        }

        private void CompositeHelper_Rendering(object sender, System.Windows.Media.RenderingEventArgs e)
        {
            if (_animationUpdater != null)
            {
                var elapsed = (Stopwatch.GetTimestamp() - _initTimeStamp) * speed;
                CurrAnimationTime = elapsed / Stopwatch.Frequency;
            }
        }

        private void FocusCameraToScene()
        {
            var maxWidth = Math.Max(Math.Max(modelBound.Width, modelBound.Height), modelBound.Depth);
            var pos = modelBound.Center + new Vector3(0, 0, maxWidth);
            Camera.Position = pos.ToPoint3D();
            Camera.LookDirection = (modelBound.Center - pos).ToVector3D();
            Camera.UpDirection = Vector3.UnitY.ToVector3D();
            if (Camera is OrthographicCamera orthCam)
            {
                orthCam.Width = maxWidth;
            }
        }

        private void ExportFile()
        {
            var index = SaveFileDialog(ExportFileFilter, out var path);
            if (!string.IsNullOrEmpty(path) && index >= 0)
            {
                string ext = HelixToolkit.Wpf.SharpDX.Assimp.Exporter.SupportedFormatsString;
                var exporter = new HelixToolkit.Wpf.SharpDX.Assimp.Exporter();
                ErrorCode errorCode = exporter.ExportToFile(path, _scene, ext);
                return;
            }
        }

        private string OpenFileDialog(string filter)
        {
            var d = new OpenFileDialog();
            d.CustomPlaces.Clear();

            d.Filter = filter;

            if (!d.ShowDialog().Value)
            {
                return null;
            }

            return d.FileName;
        }

        private int SaveFileDialog(string filter, out string path)
        {
            var d = new SaveFileDialog();
            d.Filter = filter;
            if (d.ShowDialog() == true)
            {
                path = d.FileName;
                return d.FilterIndex - 1;//This is tarting from 1. So must minus 1
            }
            else
            {
                path = "";
                return -1;
            }
        }

        private void ShowWireframeFunct(bool show)
        {
            foreach (var node in GroupModel.GroupNode.Items.PreorderDFT((node) =>
            {
                return node.IsRenderable;
            }))
            {
                if (node is MeshNode m)
                {
                    m.RenderWireframe = show;
                }
            }
        }

        private void RenderFlatFunct(bool show)
        {
            foreach (var node in GroupModel.GroupNode.Items.PreorderDFT((node) =>
            {
                return node.IsRenderable;
            }))
            {
                if (node is MeshNode m)
                {
                    if (m.Material is PhongMaterialCore phong)
                    {
                        phong.EnableFlatShading = show;
                    }
                    else if (m.Material is PBRMaterialCore pbr)
                    {
                        pbr.EnableFlatShading = show;
                    }
                }
            }
        }


        public long CalculationTime { set; get; } = 0;

        public bool Busy { set; get; } = false;

        public bool Lossless { set; get; } = false;

        private bool CanSimplify(object obj) { return !Busy; }
        private void Simplify(object obj)
        {
            if (!CanSimplify(null)) { return; }
            Busy = true;
            int size = simpleModel.Indices.Count / 3 / 2;
            CalculationTime = 0;
            Task.Factory.StartNew(() =>
            {
                var sw = Stopwatch.StartNew();
                var model = _simHelper.Simplify(size, 7, true, Lossless);
                sw.Stop();
                CalculationTime = sw.ElapsedMilliseconds;
                model.Normals = model.CalculateNormals();
                return model;
            }).ContinueWith(x =>
            {
                Busy = false;
                simpleModel = x.Result;
                CommandManager.InvalidateRequerySuggested();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

    }
}
