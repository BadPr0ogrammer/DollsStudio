/****************************************************************************
** Generated QML type registration code
**
** WARNING! All changes made in this file will be lost!
*****************************************************************************/

#include <QtQml/qqml.h>
#include <QtQml/qqmlmoduleregistration.h>

#if __has_include(<manager.h>)
#  include <manager.h>
#endif


#if !defined(QT_STATIC)
#define Q_QMLTYPE_EXPORT Q_DECL_EXPORT
#else
#define Q_QMLTYPE_EXPORT
#endif
Q_QMLTYPE_EXPORT void qml_register_types_Module_dolls_studio()
{
    QT_WARNING_PUSH QT_WARNING_DISABLE_DEPRECATED
    qmlRegisterTypesAndRevisions<Manager>("Module_dolls_studio", 1);
    QT_WARNING_POP
    qmlRegisterModule("Module_dolls_studio", 1, 0);
}

static const QQmlModuleRegistration moduledollsstudioRegistration("Module_dolls_studio", qml_register_types_Module_dolls_studio);
