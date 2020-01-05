using System;
using System.Text;

using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Security;

namespace Shadowsocks.Std.Encryption
{
    public interface IEncryptor
    {
        public enum Protocol
        {
            TCP, UDP
        }

        abstract void Encrypt(byte[] inBuf, int inLength, byte[] outBuf, out int outLength, Protocol protocol);

        abstract void Decrypt(byte[] inBuf, int inLength, byte[] outBuf, out int outLength, Protocol protocol);
    }

    public abstract class EncryptorBase<P> : IEncryptor where P : CipherParameters
    {
        public const int MAX_INPUT_SIZE = 32768;

        public const int MAX_DOMAIN_LEN = 255;
        public const int ADDR_PORT_LEN = 2;
        public const int ADDR_ATYP_LEN = 1;

        public const int ATYP_IPv4 = 0x01;
        public const int ATYP_DOMAIN = 0x03;
        public const int ATYP_IPv6 = 0x04;

        public const int MD5_LEN = 16;

        protected readonly byte[] _key;

        protected readonly SecureRandom _random;

        protected readonly MD5Digest _md5Digest;

        protected P Parameters { get; private set; }

        protected EncryptorBase(string passwd, P parameters)
        {
            Parameters = parameters;

            _random = new SecureRandom();
            _md5Digest = new MD5Digest();

            InitKey(Encoding.UTF8.GetBytes(passwd));
        }

        protected virtual void InitKey(byte[] passwd)
        {
            var result = new byte[passwd.Length + MD5_LEN];
            var md5sum = new byte[_md5Digest.GetDigestSize()];

            int i = 0;
            while (i < Parameters.KeySize)
            {
                if (i == 0)
                {
                    _md5Digest.BlockUpdate(passwd, 0, passwd.Length);
                    _md5Digest.DoFinal(md5sum, 0);
                }
                else
                {
                    Array.Copy(md5sum, 0, result, 0, MD5_LEN);
                    Array.Copy(passwd, 0, result, MD5_LEN, passwd.Length);
                    _md5Digest.BlockUpdate(passwd, 0, passwd.Length);
                    _md5Digest.DoFinal(md5sum, 0);
                }
                Array.Copy(md5sum, 0, _key, i, Math.Min(MD5_LEN, Parameters.KeySize - i));
                i += MD5_LEN;

                _md5Digest.Reset();
            }
        }

        public abstract void Encrypt(byte[] inBuf, int inLength, byte[] outBuf, out int outLength, IEncryptor.Protocol protocol);

        public abstract void Decrypt(byte[] inBuf, int inLength, byte[] outBuf, out int outLength, IEncryptor.Protocol protocol);
    }
}