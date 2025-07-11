cmake_minimum_required(VERSION 3.16)

set(MYNAME dolls_studio)

project(${MYNAME} VERSION 0.1 LANGUAGES CXX)

set(CMAKE_INCLUDE_CURRENT_DIR ON)
set(CMAKE_AUTORCC ON)
set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTOUIC OFF)

set(CMAKE_GLOBAL_AUTOGEN_TARGET OFF)
set(AUTOGEN_BUILD_DIR "${CMAKE_CURRENT_BINARY_DIR}/autogen")

set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_STANDARD 17)

add_compile_definitions(VTK_USE_X)

find_package(Qt6 REQUIRED COMPONENTS Core Gui Qml Quick)
find_package(VTK REQUIRED)
find_package(assimp REQUIRED)

qt_standard_project_setup(REQUIRES 6.8)

qt_add_executable(${MYNAME}
    MANUAL_FINALIZATION
    main.cpp
    app.cpp app.h
    vtkitem.cpp vtkitem.h
    f3d_assimp_importer.cpp f3d_assimp_importer.h
    manager.cpp manager.h
    qml.qrc
)

qt_add_qml_module(${MYNAME}
    URI Module_${MYNAME}
    VERSION 1.0
    QML_FILES
    main.qml
)

target_link_libraries(${MYNAME}
    PRIVATE
    Qt6::Quick Qt6::Qml Qt6::Gui Qt6::Core
    ${VTK_LIBRARIES}
    assimp
)

vtk_module_autoinit(TARGETS ${MYNAME} MODULES ${VTK_LIBRARIES})

include(GNUInstallDirs)
install(TARGETS ${MYNAME}
    BUNDLE DESTINATION .
    LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
    RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR}
)
