using ReenLauncher.Core;
using ReenLauncher.Models;
using ReenLauncher.ViewModels;
using ReenLauncher.Views;
using System;
using System.Threading;
using System.Windows;

namespace ReenLauncher
{
    public partial class App : Application
    {
        private static OptionsModel _options;
        public static OptionsModel options
        {
            get => _options;
            set
            {
                _options = value;
            }
        }

        public static Thread[] threads = new Thread[] { };

        public static string rootDirectory = $@"C:\Users\{Environment.UserName}\AppData\Roaming\.reenLauncher\";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // restore file system
            new SourceLauncher().restore();

            // load options
            options = new LauncherOptions().get();

            MainWindow mainWindow = new MainWindow()
            {
                Opacity = 0.0f
            };
            mainWindow.DataContext = new MainViewModel()
            {
                baseWindow = new Models.BaseWindowModel()
                {
                    obj = mainWindow
                },
                userName = options.username,
                ram = options.Ram.ToString()
            };
            mainWindow.Closed += MainWindow_Closed;
            mainWindow.Show();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            foreach(Thread thread in threads)
            {
                if (thread.IsAlive)
                {
                    thread.Abort();
                }
            }

            Application.Current.Shutdown();
        }
    }
}
