using System;

using Shadowsocks.Std.Sys;
using Shadowsocks.Std.Util;

namespace Shadowsocks.Std.Win.Util.Resource
{
    public class GetResource : IGetResources
    {
        public byte[] GetExec(ref string name)
        {
            var outName = name.Clone().ToString();

            name = $"{name}{(Environment.Is64BitOperatingSystem ? "64" : "")}.exe";

            return outName switch
            {
                Utils.sysproxy => Environment.Is64BitOperatingSystem ? Resources.sysproxy64_exe : Resources.sysproxy_exe,
                _ => null
            };
        }

        public byte[] GetLib(ref string name)
        {
            var outName = name.Clone().ToString();

            name = $"{name}.dll";

            return outName switch
            {
                Utils.libsscrypto => Resources.libsscrypto_dll,
                _ => null
            };
        }
    }
}