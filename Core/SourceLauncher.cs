using System.IO;

namespace ReenLauncher.Core
{
    class SourceLauncher
    {
        public void restore()
        {
            if (!Directory.Exists(App.rootDirectory))
            {
                Directory.CreateDirectory(App.rootDirectory);
            }
        }
    }
}
