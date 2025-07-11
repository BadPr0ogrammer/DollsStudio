#include "app.h"

#include <memory>

#include <QQmlContext>
#include <QQuickWindow>
#include <QQuickItem>
#include <QDebug>

#include "manager.h"
#include "vtkitem.h"

App::App(int &argc, char **argv)
{
    m_application = std::make_unique<QGuiApplication>(argc, argv);
    m_qmlEng = std::make_unique<QQmlApplicationEngine>();
    m_manager = std::make_unique<Manager>();

    qmlRegisterType<Manager>("Dolls.studio", 1, 0, "Manager");
    qmlRegisterType<VtkItem>("Dolls.studio", 1, 0, "VtkItem");

 #if defined(Q_OS_WIN) || defined(Q_OS_MACOS)
    QQmlFileSelector fileSelector(mEngine.data());
    fileSelector.setExtraSelectors(QStringList() << QLatin1String("nativemenubar"));
#endif
    m_qmlEng->setInitialProperties({
        { "projectManager", QVariant::fromValue(m_manager.get()) },
    });
    m_qmlEng->load(QUrl(QStringLiteral("qrc:/main.qml")));
    if (m_qmlEng->rootObjects().isEmpty()) {
        qDebug() << "... failed to load main.qml";
        exit(-1);
    }
    else {
        QObject *root = m_qmlEng->rootObjects().at(0);
        VtkItem* vtk = nullptr;
        if (root)
            vtk = reinterpret_cast<VtkItem*>(root->findChild<QObject*>("vtkItem"));
        if (vtk)
            m_manager->m_vtk = vtk;
        else {
            qDebug() << "... failed to get vtkItem";
            exit(-1);
        }
    }
}

App::~App()
{
    m_qmlEng.reset();
}
