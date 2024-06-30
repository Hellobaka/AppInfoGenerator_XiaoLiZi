using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoLiZi_AppInfoGenerator
{
    public class EventInfo : INotifyPropertyChanged
    {
        public bool Checked { get; set; }

        public int ID { get; set; }

        public int Type { get; set; }

        public string Name { get; set; }

        public string Function { get; set; }

        public int Priority { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
        }
    }
}
