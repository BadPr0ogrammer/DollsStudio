#include "vtkitem.h"

#include <vtkPlaneSource.h>
#include <vtkPolyDataMapper.h>
#include <vtkActor.h>
#include <vtkTransform.h>
#include <vtkCommand.h>
#include <vtkNamedColors.h>
#include <vtkObjectFactory.h>
#include <vtkProperty.h>

vtkStandardNewMacro(VtkItem::Data)
vtkStandardNewMacro(VtkItem::TimerCallback)

namespace fs = std::filesystem;

VtkItem::vtkUserData VtkItem::initializeVTK(vtkRenderWindow* renderWindow)
{    
    vtkNew<Data> vtk;
    vtk->m_vtk = this;

    vtkNew<vtkPlaneSource> planeSource;
    planeSource->SetCenter(0.0, 0.0, 0.0);
    planeSource->SetNormal(0.0, 1.0, 0.0);
    planeSource->Update();
    vtkNew<vtkPolyDataMapper> mapper;
    mapper->SetInputData(planeSource->GetOutput());
    vtkNew<vtkActor> actor;
    actor->SetMapper(mapper);
    vtkNew<vtkTransform> transform;
    transform->Scale(200, 200, 200);
    actor->SetUserTransform(transform);
    vtkNew<vtkNamedColors> colors;
    actor->GetProperty()->SetColor(colors->GetColor3d("Gray").GetData());

    vtk->m_renderer->AddActor(actor);
    vtk->m_renderer->SetBackground(colors->GetColor3d("White").GetData());

    renderWindow->AddRenderer(vtk->m_renderer);

    vtkNew<TimerCallback> tcb;
    tcb->m_vtk = this;

    renderWindow->GetInteractor()->AddObserver(vtkCommand::TimerEvent, tcb);
    renderWindow->GetInteractor()->CreateRepeatingTimer(1000);

    return vtk;
}

void VtkItem::destroyingVTK(vtkRenderWindow* renderWindow, vtkUserData userData)
{
    auto* vtk = Data::SafeDownCast(userData);
}

bool VtkItem::openSource(const QUrl &url)
{
    m_fname = url.toLocalFile();
    dispatch_async([this](vtkRenderWindow* renderWindow, vtkUserData userData)
    {            
        Data* vtk = (Data*)userData.GetPointer();
        //m_engine.sceneClear();
        bool ret = false;
        vtk->m_assimpImporter->SetFileName(m_fname.toStdString());

        if (vtk->m_assimpImporter->ImportBegin()) {
            vtk->m_assimpImporter->ImportActors(vtk->m_renderer);
            ret = true;
        }
        vtk->m_renderer->ResetCamera();
        vtk->m_renderer->SetBackground(1,1,1);

        renderWindow->AddRenderer(vtk->m_renderer);
        return ret;
    });
    return false;
}

void VtkItem::TimerCallback::Execute(vtkObject* caller, unsigned long, void*data)
{
    if (m_vtk && m_vtk->m_play)
    {
        m_vtk->dispatch_async([this](vtkRenderWindow* renderWindow, vtkUserData userData)
        {
            Data* vtk = (Data*)userData.GetPointer();
            vtk->m_assimpImporter->EnableAnimation(0);
            vtk->m_assimpImporter->UpdateAtTimeValue(m_vtk->m_timeValue++);
            renderWindow->Render();
        });
    }
}

bool VtkItem::event(QEvent* ev)
{
    switch (ev->type())
    {
    case QEvent::MouseButtonPress:
    {
        auto e = static_cast<QMouseEvent*>(ev);
        _click.reset(e->clone());
        break;
    }
    case QEvent::MouseMove:
    {
        if (!_click)
            return QQuickVTKItem::event(ev);

        auto e = static_cast<QMouseEvent*>(ev);
        if ((_click->position() - e->position()).manhattanLength() > 5)
        {
            QQuickVTKItem::event(QScopedPointer<QMouseEvent>(_click.take()).get());
            return QQuickVTKItem::event(e);
        }
        break;
    }
    case QEvent::MouseButtonRelease:
    {
        if (!_click)
            return QQuickVTKItem::event(ev);
        else
            emit clicked();
        break;
    }
    default:
        break;
    }
    ev->accept();
    return true;
}

/* in async
void f3d_engine::sceneClear()
{
    // Clear the meta importer from all importers
    m_metaImporter->Clear();
    // Clear the window of all actors
    m_renderer->Initialize();
}
*/

//m_renderer->Initialize();
//m_renderer->SetImporter(m_metaImporter);
//m_metaImporter->SetRenderWindow(_renWin);
//m_metaImporter->AddImporter(m_assimpImporter);

//m_renderer->ResetCamera();
//m_renderer->ResetCameraScreenSpace(0.9);

//CreateProgressRepresentationAndCallback();
//m_vtk->setProperty("width", m_vtk->m_parent->width());
//m_vtk->setProperty("height", m_vtk->m_parent->height());

//m_timerCallback->m_vtk = m_vtk;
//m_timerInteractor->SetRenderWindow(renderWindow);
//renderWindow->GetInteractor()->AddObserver(vtkCommand::TimerEvent, m_timerCallback);
//renderWindow->GetInteractor()->CreateRepeatingTimer(1);
