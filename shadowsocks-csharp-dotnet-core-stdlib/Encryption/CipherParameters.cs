namespace Shadowsocks.Std.Encryption
{
    public abstract class CipherParameters
    {
        public string Algorithm { get; private set; }

        public int KeySize { get; private set; }
    }
}