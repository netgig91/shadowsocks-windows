using System;

namespace Shadowsocks.Std.Encryption
{
    public class EncryptionInfo
    {
        internal readonly Type InstanceType;

        public readonly string Name;

        public readonly CipherParameters Parameters;

        public EncryptionInfo(Type instanceType, string name, CipherParameters parameters)
        {
            InstanceType = instanceType;
            Name = name;
            Parameters = parameters;
        }
    }
}