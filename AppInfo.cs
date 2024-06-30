using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoLiZi_AppInfoGenerator
{
    public class AppInfo
    {
        public int ret { get; set; }

        public int apiver { get; set; }

        public string name { get; set; }

        public string version { get; set; }

        public int version_id { get; set; }

        public string author { get; set; }

        public string description { get; set; }

        [JsonProperty("event")]
        public Event[] _event { get; set; } = [];

        public Menu[] menu { get; set; } = [];

        public object[] status { get; set; } = [];

        public int[] auth { get; set; } = [];

        public class Event
        {
            public int id { get; set; }

            public int type { get; set; }

            public string name { get; set; }

            public string function { get; set; }

            public int priority { get; set; }

            public int address { get; set; }
        }

        public class Menu
        {
            public string name { get; set; }

            public string function { get; set; }

            public int address { get; set; }
        }
    }

}
