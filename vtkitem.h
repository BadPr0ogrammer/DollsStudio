#ifndef VTKITEM_H
#define VTKITEM_H

#include <QQuickVTKItem.h>

#include <QString>
#include <QMouseEvent>
#include <QScopedPointer>

#include <vtkNew.h>
#include <vtkObject.h>
#include <vtkObjectFactory.h>
#include <vtkRenderer.h>
#include <vtkRenderWindow.h>
#include <vtkRenderWindowInteractor.h>
#include <vtkCommand.h>

#include "f3d_assimp_importer.h"

struct VtkItem : QQuickVTKItem
{
    Q_OBJECT
public:
    struct Data : vtkObject
    {
        static Data* New();
        vtkTypeMacro(Data, vtkObject);
        vtkNew<vtkRenderer> m_renderer;
        vtkNew<f3d_assimp_importer> m_assimpImporter;

        struct VtkItem* m_vtk = nullptr;
    };
    struct TimerCallback : vtkCommand
    {
        static TimerCallback* New();
        vtkTypeMacro(TimerCallback, vtkCommand);
        void Execute(vtkObject* caller, unsigned long evId, void* data) override;

        struct VtkItem* m_vtk = nullptr;
    };

    QString m_fname;
    bool m_play = false;
    int m_timeValue = 0;

    vtkUserData initializeVTK(vtkRenderWindow* renderWindow) override;
    void destroyingVTK(vtkRenderWindow* renderWindow, vtkUserData userData) override;

    bool openSource(const QUrl &url);

    bool event(QEvent* ev) override;
    QScopedPointer<QMouseEvent> _click;
    Q_SIGNAL void clicked();
};

#endif
