using System.Windows;

namespace ReenLauncher.Models
{
    class BaseWindowModel
    {
        public Window obj { get; set; }
        public DelegateCommand moveObj { 
            get
            {
                return new DelegateCommand((o) =>
                {
                    obj.DragMove();
                });
            } 
        }
        public DelegateCommand closeObj
        {
            get
            {
                return new DelegateCommand((p) =>
                {
                    obj.Close();
                });
            }
        }
    }
}
