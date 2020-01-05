namespace Shadowsocks.Std.Encryption.Parameters
{
    public class StreamEncryptionParameters : CipherParameters
    {
        public int IvLength { get; private set; }
    }
}