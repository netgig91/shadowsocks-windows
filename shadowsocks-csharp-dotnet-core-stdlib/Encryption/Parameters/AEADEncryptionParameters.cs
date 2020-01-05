namespace Shadowsocks.Std.Encryption.Parameters
{
    public class AEADEncryptionParameters : CipherParameters
    {
        public int SaltSize { get; private set; }

        public int NonceSize { get; private set; }

        public int TagSize { get; private set; }
    }
}