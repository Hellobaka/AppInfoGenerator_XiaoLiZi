using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace XiaoLiZi_AppInfoGenerator
{
    public static class Native
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr lib, string funcName);

        [DllImport("kernel32.dll")]
        public static extern int GetLastError();

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string path);

        public static T? CreateDelegateFromUnmanaged<T>(IntPtr handle, string apiName) where T : Delegate
        {
            IntPtr api = GetProcAddress(handle, apiName);
            return api == IntPtr.Zero ? null : (T?)Marshal.GetDelegateForFunctionPointer(api, typeof(T));
        }
    }
}
