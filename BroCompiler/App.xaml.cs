using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BroCompiler
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();

            foreach (String file in e.Args)
            {
                if (File.Exists(file) && file.EndsWith(".bro"))
                {
                    mainWindow.LoadCapture(file);
                    break;
                }
            }

            mainWindow.Show();
        }
    }
}
