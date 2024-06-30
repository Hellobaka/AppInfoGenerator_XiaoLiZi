using System;

namespace XiaoLiZi_AppInfoGenerator
{
    public class PluginInfo
    {
        public string PluginType { get; set; }

        public string FilePath { get; set; }

        public string FileName { get; set; }

        public IntPtr Handle { get; set; } = IntPtr.Zero;
    }
}
