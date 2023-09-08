using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ReenLauncher.Models;
using System.IO;
using System;

namespace ReenLauncher.Core
{
    class LauncherOptions
    {
        public OptionsModel get()
        {
            OptionsModel options = new OptionsModel()
            {
                Ram = 2048,
                username = ""
            };

            if (File.Exists(App.rootDirectory + "launcher_options.json"))
            {
                using (StreamReader file = File.OpenText(App.rootDirectory + "launcher_options.json"))
                {
                    using (JsonTextReader reader = new JsonTextReader(file))
                    {
                        JObject obj = (JObject)JToken.ReadFrom(reader);

                        options.Ram = Convert.ToInt32(obj["Ram"]);
                        options.username = (string) obj["username"];
                    }
                }
            } else
            {
                JObject writableOptions = new JObject(
                    new JProperty("Ram", options.Ram),
                    new JProperty("username", options.username));

                using (StreamWriter file = File.CreateText(App.rootDirectory + "launcher_options.json"))
                {
                    using (JsonTextWriter writer = new JsonTextWriter(file))
                    {
                        writableOptions.WriteTo(writer);
                    }
                }
            }

            return options;
        }

        public static void update()
        {
            OptionsModel options = App.options;

            JObject writableOptions = new JObject(
                    new JProperty("Ram", options.Ram),
                    new JProperty("username", options.username));

            using (StreamWriter file = File.CreateText(App.rootDirectory + "launcher_options.json"))
            {
                using (JsonTextWriter writer = new JsonTextWriter(file))
                {
                    writableOptions.WriteTo(writer);
                }
            }
        }
    }
}
