using Logazmic.Core.Log;
using ReenLauncher.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ReenLauncher.ViewModels
{
    class DebugViewModel : BaseViewModel
    {
        private BaseWindowModel _baseWindow;
        public BaseWindowModel baseWindow
        {
            get => _baseWindow;
            set
            {
                _baseWindow = value;
            }
        }

        private Process _minecraft;
        public Process minecraft
        {
            get => _minecraft;
            set
            {
                _minecraft = value;
            }
        }

        private string _debug;
        public string debug
        {
            get => _debug;
            set
            {
                _debug = value;
                onPropertyChanged("debug");
            }
        }

        private Window _parent;
        public Window parent
        {
            get => _parent;
            set
            {
                _parent = value;
                onPropertyChanged("parent");
            }
        }

        public DebugViewModel()
        {
            start();
        }

        private async void start()
        {
            while (minecraft == null)
            {
                await Task.Delay(1000);
            }
            minecraft.StartInfo.RedirectStandardError = true;
            minecraft.StartInfo.RedirectStandardOutput = true;
            minecraft.EnableRaisingEvents = true;
            minecraft.ErrorDataReceived += Minecraft_ErrorDataReceived;
            minecraft.OutputDataReceived += Minecraft_OutputDataReceived;
            minecraft.StartInfo.UseShellExecute = false;
            minecraft.Start();
            minecraft.BeginErrorReadLine();
            minecraft.BeginOutputReadLine();
        }

        String stateDataReceived = "";
        private void Minecraft_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                if (e.Data.Contains("<log4j:Event>"))
                {
                    stateDataReceived += e.Data;
                }
                else if (e.Data.Contains("</log4j:Event>"))
                {
                    stateDataReceived += e.Data;
                    // send log
                    debug += stateDataReceived + "\n\n";
                    stateDataReceived = "";
                }
                else
                {
                    stateDataReceived += e.Data;
                }
            }
        }

        String stateErrorReceived = "";
        private void Minecraft_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                if (e.Data.Contains("<log4j:Throwable>"))
                {
                    stateErrorReceived += e.Data;
                }
                else if (e.Data.Contains("</log4j:Throwable>"))
                {
                    stateErrorReceived += e.Data;
                    // send log
                    debug += stateErrorReceived + "\n\n";
                    stateErrorReceived = "";
                }
                else
                {
                    stateErrorReceived += e.Data;
                }
            }
        }

        private string getDateTime()
        {
            return $"{DateTime.Now.Day}.{DateTime.Now.Month}.{DateTime.Now.Year} at {DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}";
        }
    }
}
