namespace Shadowsocks.Std.Sys
{
    public interface IAutoStartup
    {
        public bool Set(bool enabled);

        public bool Check();
    }
}