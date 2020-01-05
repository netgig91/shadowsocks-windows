namespace Shadowsocks.Std.Sys
{
    public interface IGetResources
    {
        public byte[] GetLib(ref string name);

        public byte[] GetExec(ref string name);
    }
}