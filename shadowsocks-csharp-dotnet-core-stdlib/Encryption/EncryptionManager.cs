-using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using static Shadowsocks.Std.Encryption.IEncryptor;

namespace Shadowsocks.Std.Encryption
{
    public class Encryptor
    {
        internal readonly IEncryptor encryptor;

        internal Encryptor(IEncryptor encryptor)
        {
            this.encryptor = encryptor;
        }

        public void EncryptTCP(byte[] inBuf, int inLength, byte[] outBuf, out int outLength) => encryptor.Encrypt(inBuf, inLength, outBuf, out outLength, Protocol.TCP);

        public void EncryptUDP(byte[] inBuf, int inLength, byte[] outBuf, out int outLength) => encryptor.Encrypt(inBuf, inLength, outBuf, out outLength, Protocol.UDP);

        public void DecryptTCP(byte[] inBuf, int inLength, byte[] outBuf, out int outLength) => encryptor.Decrypt(inBuf, inLength, outBuf, out outLength, Protocol.TCP);

        public void DecryptUDP(byte[] inBuf, int inLength, byte[] outBuf, out int outLength) => encryptor.Decrypt(inBuf, inLength, outBuf, out outLength, Protocol.UDP);
    }

    public static class EncryptionManager
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static readonly Type[] _instanceConstructor = { typeof(string), typeof(CipherParameters) };

        private static readonly Dictionary<string, EncryptionInfo> _encryptionInfos = new Dictionary<string, EncryptionInfo>();

        static EncryptionManager()
        {
            AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IEncryptor)))).ToList().ForEach(a => a.TypeInitializer.Invoke(Array.Empty<object>()));
        }

        public static Encryptor GetEncryptor(string method, string passwd)
        {
            if (method == null || passwd == null)
            {
                if (_encryptionInfos.TryGetValue(method.ToLower(), out EncryptionInfo info))
                {
                    var constructor = info.InstanceType.GetConstructor(_instanceConstructor) ?? throw new NotSupportedException("");

                    var encryptor = constructor.Invoke(new object[] { passwd, info.Parameters }) as IEncryptor;

                    return new Encryptor(encryptor);
                }
            }

            throw new ArgumentException("", nameof(method));
        }

        public static List<string> GetEncryptionNames() => _encryptionInfos.Keys.ToList();

        public static void RegisterEncryptionInfo(EncryptionInfo info)
        {


        }

        public static string DumpRegisteredEncryptor()
        {
            var sb = new StringBuilder();
            sb.Append(Environment.NewLine);
            sb.AppendLine("=========================");
            sb.AppendLine("Registered Encryptor Info");
            foreach (var e in _encryptionInfos)
            {
                sb.AppendLine($"{e.Key} => {e.Value.InstanceType.Name}");
            }

            sb.AppendLine("=========================");

            return sb.ToString();
        }
    }
}