using System;

namespace Shadowsocks.Std
{
    namespace Encryption
    {
        public static class DelegateSodium
        {
            public delegate int Sodium_init();

            public delegate int Crypto_aead_aes256gcm_is_available();

            #region AEAD

            public delegate int Sodium_increment(byte[] n, int nlen);

            public delegate int Crypto_aead_chacha20poly1305_ietf_encrypt(byte[] c, ref ulong clen_p, byte[] m, ulong mlen, byte[] ad, ulong adlen, byte[] nsec, byte[] npub, byte[] k);

            public delegate int Crypto_aead_chacha20poly1305_ietf_decrypt(byte[] m, ref ulong mlen_p, byte[] nsec, byte[] c, ulong clen, byte[] ad, ulong adlen, byte[] npub, byte[] k);

            public delegate int Crypto_aead_xchacha20poly1305_ietf_encrypt(byte[] c, ref ulong clen_p, byte[] m, ulong mlen, byte[] ad, ulong adlen, byte[] nsec, byte[] npub, byte[] k);

            public delegate int Crypto_aead_xchacha20poly1305_ietf_decrypt(byte[] m, ref ulong mlen_p, byte[] nsec, byte[] c, ulong clen, byte[] ad, ulong adlen, byte[] npub, byte[] k);

            public delegate int Crypto_aead_aes256gcm_encrypt(byte[] c, ref ulong clen_p, byte[] m, ulong mlen, byte[] ad, ulong adlen, byte[] nsec, byte[] npub, byte[] k);

            public delegate int Crypto_aead_aes256gcm_decrypt(byte[] m, ref ulong mlen_p, byte[] nsec, byte[] c, ulong clen, byte[] ad, ulong adlen, byte[] npub, byte[] k);

            #endregion AEAD

            #region Stream

            public delegate int Crypto_stream_salsa20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic, byte[] k);

            public delegate int Crypto_stream_chacha20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic, byte[] k);

            public delegate int Crypto_stream_chacha20_ietf_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, uint ic, byte[] k);

            #endregion Stream
        }

        public static class DelegateOpenSSL
        {
            public delegate IntPtr EVP_CIPHER_CTX_new();

            public delegate void EVP_CIPHER_CTX_free(IntPtr ctx);

            public delegate int EVP_CIPHER_CTX_reset(IntPtr ctx);

            public delegate int EVP_CipherInit_ex(IntPtr ctx, IntPtr type, IntPtr impl, byte[] key, byte[] iv, int enc);

            public delegate int EVP_CipherUpdate(IntPtr ctx, byte[] outb, out int outl, byte[] inb, int inl);

            public delegate int EVP_CipherFinal_ex(IntPtr ctx, byte[] outm, ref int outl);

            public delegate int EVP_CIPHER_CTX_set_padding(IntPtr x, int padding);

            public delegate int EVP_CIPHER_CTX_set_key_length(IntPtr x, int keylen);

            public delegate int EVP_CIPHER_CTX_ctrl(IntPtr ctx, int type, int arg, IntPtr ptr);

            /// <summary>
            /// simulate NUL-terminated string
            /// </summary>
            public delegate IntPtr EVP_get_cipherbyname(byte[] name);
        }

        public static class DelegateMbedTLS
        {
            public delegate int md5_ret(byte[] input, uint ilen, byte[] output);

            /// <summary>
            /// Get cipher ctx size for unmanaged memory allocation
            /// </summary>
            /// <returns></returns>
            public delegate int cipher_get_size_ex();

            #region Cipher layer wrappers

            public delegate IntPtr cipher_info_from_string(string cipher_name);

            public delegate void cipher_init(IntPtr ctx);

            public delegate int cipher_setup(IntPtr ctx, IntPtr cipher_info);

            // XXX: Check operation before using it
            public delegate int cipher_setkey(IntPtr ctx, byte[] key, int key_bitlen, int operation);

            public delegate int cipher_set_iv(IntPtr ctx, byte[] iv, int iv_len);

            public delegate int cipher_reset(IntPtr ctx);

            public delegate int cipher_update(IntPtr ctx, byte[] input, int ilen, byte[] output, ref int olen);

            public delegate void cipher_free(IntPtr ctx);

            public delegate int cipher_auth_encrypt(IntPtr ctx,
                byte[] iv, uint iv_len,
                IntPtr ad, uint ad_len,
                byte[] input, uint ilen,
                byte[] output, ref uint olen,
                byte[] tag, uint tag_len);

            public delegate int cipher_auth_decrypt(IntPtr ctx,
                byte[] iv, uint iv_len,
                IntPtr ad, uint ad_len,
                byte[] input, uint ilen,
                byte[] output, ref uint olen,
                byte[] tag, uint tag_len);

            public delegate int hkdf(byte[] salt,
                int salt_len, byte[] ikm, int ikm_len,
                byte[] info, int info_len, byte[] okm,
                int okm_len);

            #endregion Cipher layer wrappers
        }
    }

    namespace Util
    {
        public static class DelegateUtil
        {
            public delegate IntPtr LoadLibrary(string path);

            public delegate bool SetProcessWorkingSetSize(IntPtr process, UIntPtr minimumWorkingSetSize, UIntPtr maximumWorkingSetSize);
        }
    }

    namespace Model
    {
        public interface IDelegatesInit
        {
            public void Init();
        }
    }
}