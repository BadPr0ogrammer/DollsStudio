#include <QQuickVTKItem.h>

#include "app.h"

int main(int argc, char *argv[])
{
    QQuickVTKItem::setGraphicsApi();

    App app(argc, argv);
    if (app.m_qmlEng->rootObjects().isEmpty())
        return 1;
    return app.m_application.get()->exec();
}
