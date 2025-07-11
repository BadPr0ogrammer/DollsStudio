#include "manager.h"
#include "vtkitem.h"

void Manager::openSource(const QUrl &url)
{
    m_vtk->openSource(url);
}

void Manager::playFlag()
{
    m_vtk->m_play = true;
}
