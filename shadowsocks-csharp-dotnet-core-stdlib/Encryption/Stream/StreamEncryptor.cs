using System;
using System.Collections.Generic;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

using Shadowsocks.Std.Encryption.Parameters;
using static Shadowsocks.Std.Encryption.IEncryptor;

namespace Shadowsocks.Std.Encryption.Stream
{
    public abstract class StreamEncryptor : EncryptorBase<StreamEncryptionParameters>
    {
        // for UDP only
        protected byte[] _udpTmpBuf = new byte[65536];

        protected byte[] _encryptIV;
        protected byte[] _decryptIV;

        protected IBufferedCipher cipher;

        static StreamEncryptor()
        {
        }

        protected StreamEncryptor(string passwd, StreamEncryptionParameters parameters) : base(passwd, parameters)
        {
            cipher = CipherUtilities.GetCipher(parameters.Algorithm);

            InitIv(out _encryptIV, Parameters.IvLength);
            InitIv(out _decryptIV, Parameters.IvLength);
        }

        public override void Encrypt(byte[] inBuf, int inLength, byte[] outBuf, out int outLength, Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.TCP:


                    outLength = Parameters.IvLength;
                    break;
                case Protocol.UDP:
                    InitIv(out _encryptIV, Parameters.IvLength);

                    Array.Copy(_encryptIV, outBuf, Parameters.IvLength);
                    lock (_udpTmpBuf)
                    {
                        cipher.Init(true, new ParametersWithIV(ParameterUtilities.CreateKeyParameter("AES", _key), _encryptIV));
                        var len = cipher.ProcessBytes(inBuf, 0, inLength, outBuf, 0);
                        cipher.DoFinal(outBuf, len);

                        outLength = inLength + Parameters.IvLength;
                    }
                    break;
                default:
                    throw new Exception();
            }

            cipher.Reset();
        }

        public override void Decrypt(byte[] inBuf, int inLength, byte[] outBuf, out int outLength, Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.TCP:
                    outLength = inLength + Parameters.IvLength;




                    break;
                case Protocol.UDP:
                    Array.Copy(inBuf, _decryptIV, Parameters.IvLength);

                    lock (_udpTmpBuf)
                    {
                        outLength = inLength - Parameters.IvLength;

                        // C# could be multi-threaded
                        Buffer.BlockCopy(inBuf, Parameters.IvLength, _udpTmpBuf, 0, outLength);

                        cipher.Init(false, new ParametersWithIV(ParameterUtilities.CreateKeyParameter("AES", _key), _decryptIV));
                        cipher.DoFinal(_udpTmpBuf, outBuf, outLength);
                    }
                    break;
                default:
                    throw new Exception();
            }

            throw new System.NotImplementedException();
        }

        protected virtual void InitIv(out byte[] iv, int len)
        {
            iv = new byte[len];

            _random.NextBytes(iv);
        }
    }
}