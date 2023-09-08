using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.FabricMC;
using CmlLib.Core.Version;
using CmlLib.Core.VersionMetadata;
using ReenLauncher.Core;
using ReenLauncher.Models;
using ReenLauncher.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;

namespace ReenLauncher.ViewModels
{
    class MainViewModel : BaseViewModel
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

        private string _userName;
        public string userName
        {
            get => _userName;
            set
            {
                _userName = value;
                App.options.username = value;
                LauncherOptions.update();
                onPropertyChanged("userName");
            }
        }

        private string _ram;
        public string ram
        {
            get => _ram;
            set
            {
                if (value.Any(char.IsDigit) && value.Length != 0)
                {
                    if (Convert.ToInt16(value) <= 8192 && Convert.ToInt16(value) > 0)
                    {
                        _ram = value;
                        App.options.Ram = Convert.ToInt16(value);
                        LauncherOptions.update();
                    } else
                    {
                        _ram = _ram;
                    }
                } else
                {
                    _ram = _ram;
                }
                onPropertyChanged("ram");
            }
        }

        private bool _isEnabledGame = true;
        public bool isEnabledGame
        {
            get => _isEnabledGame;
            set
            {
                _isEnabledGame = value;
                onPropertyChanged("isEnabledGame");
            }
        }

        public string version
        {
            get
            {
                return "Версия " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        private bool _isProcess = false;
        public bool isProcess
        {
            get => _isProcess;
            set
            {
                _isProcess = value;
                onPropertyChanged("isProcess");
            }
        }

        private int _process;
        public int process
        {
            get => _process;
            set
            {
                _process = value;
                onPropertyChanged("process");
            }
        }

        public DelegateCommand toPlay
        {
            get
            {
                return new DelegateCommand((o) =>
                {
                    startGame();
                });
            }
        }

        public DelegateCommand toggleDebug
        {
            get
            {
                return new DelegateCommand((o) =>
                {
                    isDebug = !isDebug;
                });
            }
        }

        private bool _isDebug = false;
        public bool isDebug
        {
            get => _isDebug;
            set
            {
                _isDebug = value;
                onPropertyChanged("isDebug");
            }
        }

        private async void startGame()
        {
            state = "Проверка игры...";
            stateColor = "#1e81b0";

            isProcess = true;
            isEnabledGame = false;

            int ram = App.options.Ram;
            string username = App.options.username;

            // options 
            MinecraftPath path = new MinecraftPath(App.rootDirectory + "\\game");
            CMLauncher launcher = new CMLauncher(path);
            var fabricVersions = await new FabricVersionLoader().GetVersionMetadatasAsync();

            launcher.ProgressChanged += Launcher_ProgressChanged;
            launcher.FileChanged += Launcher_FileChanged;

            // instal fabric
            var fabric = fabricVersions.GetVersionMetadata("fabric-loader-0.14.22-1.20.1");
            await fabric.SaveAsync(path);

            // update version list
            await launcher.GetAllVersionsAsync();

            var processMinecraft = await launcher.CreateProcessAsync("fabric-loader-0.14.22-1.20.1", new MLaunchOption()
            {
                MaximumRamMb = ram,
                Session = MSession.GetOfflineSession(username),
                ScreenHeight = 530,
                Path = path,
                JVMArguments = new string[] { },
                ScreenWidth = 925,
                VersionType = "Reen",
                GameLauncherName = "Reen Launcher",
            });

            await updateMods(processMinecraft);
        }

        private void Launcher_FileChanged(CmlLib.Core.Downloader.DownloadFileChangedEventArgs e)
        {
            state = e.FileName;
            stateFiles = $"{e.ProgressedFileCount}/{e.TotalFileCount}";
        }

        private void Launcher_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            process = e.ProgressPercentage;
        }

        private async Task updateMods(Process process)
        {
            if (!Directory.Exists(App.rootDirectory + "game\\mods"))
            {
                Directory.CreateDirectory(App.rootDirectory + "game\\mods");
                // download
                state = "Скачивание модов...";
                stateColor = "#1e81b0";
                await downloadMods();
            } else
            {
                // update
                state = "Проверка модов...";
                stateColor = "#1e81b0";
                await downloadMods();
            }

            state = "Запуск игры...";
            if (isDebug)
            {
                Window window = new DebugWindow();
                window.DataContext = new DebugViewModel() 
                {  
                    baseWindow = new BaseWindowModel()
                    {
                        obj = window
                    },
                    minecraft = process
                };
                window.Show();
                baseWindow.obj.Hide();
                window.Closed += (s, e) =>
                {
                    baseWindow.obj.Show();
                    this.isEnabledGame = true;
                    state = ""; this.process = 0;
                };
            } else
            {
                baseWindow.obj.Hide();
                process.Start();
                process.WaitForExit();
                baseWindow.obj.Show();
                this.isEnabledGame = true;
                state = ""; this.process = 0;
            }
        }

        private async Task downloadMods()
        {
            GameSourceModel gameSources = await getGameSources();
            isProcess = true; Dictionary<string, bool> modsDownloaded = new Dictionary<string, bool>();

            foreach (String file in Directory.GetFiles(App.rootDirectory + "game\\mods"))
            {
                foreach (Mod mod in gameSources.mods)
                {
                    if (file.ToLower().Contains(mod.name.ToLower()))
                    {
                        if (!mod.sha.Contains(SHA256CheckSum(file)))
                        {
                            // download
                            File.Delete(file); state = "Обновление " + mod.name;
                            using (WebClient clientMod = new WebClient())
                            {
                                clientMod.DownloadProgressChanged += (s, e) =>
                                {
                                    process = e.ProgressPercentage;
                                };
                                clientMod.DownloadFileCompleted += (s, e) =>
                                {
                                    modsDownloaded.Add(mod.sha, true);
                                    clientMod.Dispose();
                                };
                                await clientMod.DownloadFileTaskAsync(mod.url, App.rootDirectory + "game\\mods\\" + mod.name);
                            }
                        } else
                        {
                            modsDownloaded.Add(mod.sha, true);
                        }
                    }
                }
            }

            foreach (Mod mod in gameSources.mods)
            {
                if (!modsDownloaded.ContainsKey(mod.sha))
                {
                    state = "Скачивание " + mod.name;
                    using (WebClient clientMod = new WebClient())
                    {
                        clientMod.DownloadProgressChanged += (s, e) =>
                        {
                            process = e.ProgressPercentage;
                        };
                        clientMod.DownloadFileCompleted += (s, e) =>
                        {
                            modsDownloaded.Add(mod.sha, true);
                            clientMod.Dispose();
                        };
                        await clientMod.DownloadFileTaskAsync(mod.url, App.rootDirectory + "game\\mods\\" + mod.name);
                    }
                }
            }
        }

        private string url = "https://pastebin.com/raw/2NNiZrdm";
        private async Task<GameSourceModel> getGameSources()
        {
            GameSourceModel gameSource = null;
            using (HttpClient client = new HttpClient())
            {
                string output= "";
                try
                {
                    output = await client.GetStringAsync(url);
                } catch 
                {
                    MessageBox.Show("Internal error: bad request");
                    Application.Current.Shutdown();
                }
                gameSource = Newtonsoft.Json.JsonConvert.DeserializeObject<GameSourceModel>(output);
            }
            return gameSource;
        }
             
        public DelegateCommand modsOpen
        {
            get
            {
                return new DelegateCommand((o) =>
                {
                    if (Directory.Exists(App.rootDirectory + "game\\mods"))
                    {
                        Process.Start(new ProcessStartInfo(App.rootDirectory + "game\\mods"));
                    } else
                    {
                        Directory.CreateDirectory(App.rootDirectory + "game\\mods");
                        Process.Start(new ProcessStartInfo(App.rootDirectory + "game\\mods"));
                    }
                });
            }
        }

        public string SHA256CheckSum(string filePath)
        {
            using (SHA256 SHA256 = SHA256Managed.Create())
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                    return Convert.ToBase64String(SHA256.ComputeHash(fileStream));
            }
        }

        private String _state;
        public String state {
            get => _state;
            set
            {
                _state = value;
                onPropertyChanged("state");
            }
        }

        private String _stateFiles;
        public String stateFiles
        {
            get => _stateFiles;
            set
            {
                _stateFiles = value;
                onPropertyChanged("stateFiles");
            }
        }

        private String _stateColor;
        public String stateColor
        {
            get => _stateColor;
            set
            {
                _stateColor = value;
                onPropertyChanged("stateColor");
            }
        }

        public MainViewModel()
        {
            
        }
    }
}
