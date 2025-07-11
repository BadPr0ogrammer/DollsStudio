#ifndef APP_H
#define APP_H

#include <QGuiApplication>
#include <QQmlApplicationEngine>

#include <memory>

class Manager;

class App
{
public:
    App(int &argc, char **argv);
    ~App();

    std::unique_ptr<QGuiApplication> m_application;
    std::unique_ptr<QQmlApplicationEngine> m_qmlEng;
    std::unique_ptr<Manager> m_manager;
};

#endif
