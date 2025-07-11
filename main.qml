import QtQuick
import QtQuick.Controls

import Qt.labs.platform as Platform

import Dolls.studio 1.0

ApplicationWindow
{
    width: 640
    height: 480
    visible: true
    title: "Dolls.studio"

    required property Manager projectManager
    property alias openProjectDialog: openProjectDialog

    StackView {
        anchors.fill: parent
        VtkItem {
            objectName: "vtkItem"
            anchors.fill: parent
        }
    }
    menuBar: MenuBar {
        Menu {
            title: qsTr("&File")
            Action {
                text: qsTr("&Open...")
                onTriggered: openProjectDialog.open()
            }
            MenuSeparator { }
            Action {
                text: qsTr("&Quit")
                onTriggered: Qt.quit()
            }
        }
        Menu {
            title: qsTr("&Animation")
            Action {
                onTriggered: projectManager.playFlag();
            }
        }
        Menu {
            title: qsTr("&Help")
            Action { text: qsTr("&About") }
        }
    }
    Platform.FileDialog {
        id: openProjectDialog
        objectName: "openProjectDialog"        
        nameFilters: ["FBX files (*.fbx)","All files (*)"]
        onAccepted: projectManager.openSource(file);
    } 
}
