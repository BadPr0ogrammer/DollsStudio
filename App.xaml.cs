using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;

namespace DollsStudio
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            if (e.Args.Length == 1)
            {
                var model = mainWindow.DataContext as MainViewModel;
                model.OpenTheFile(e.Args[0]);
            }
        }
    }
}