using Shadowsocks.Std.Encryption.Parameters;

namespace Shadowsocks.Std.Encryption.AEAD
{
    public abstract class AEADEncryptor : EncryptorBase<AEADEncryptionParameters>
    {
        protected byte[] _sessionKey;

        protected AEADEncryptor(string passwd, AEADEncryptionParameters parameters) : base(passwd, parameters)
        {

        }

        protected virtual void InitSessionKey(ref byte[] iv)
        {

        }
    }
}