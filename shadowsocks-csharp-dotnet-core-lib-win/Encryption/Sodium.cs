using System.Runtime.InteropServices;

using Shadowsocks.Std.Util;

namespace Shadowsocks.Std.Win.Encryption
{
    public static class Sodium
    {
        private const string DLL_NAME = Utils.libsscrypto + ".dll";

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sodium_init();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int crypto_aead_aes256gcm_is_available();

        #region AEAD

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sodium_increment(byte[] n, int nlen);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int crypto_aead_chacha20poly1305_ietf_encrypt(byte[] c, ref ulong clen_p, byte[] m, ulong mlen, byte[] ad, ulong adlen, byte[] nsec, byte[] npub, byte[] k);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int crypto_aead_chacha20poly1305_ietf_decrypt(byte[] m, ref ulong mlen_p, byte[] nsec, byte[] c, ulong clen, byte[] ad, ulong adlen, byte[] npub, byte[] k);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int crypto_aead_xchacha20poly1305_ietf_encrypt(byte[] c, ref ulong clen_p, byte[] m, ulong mlen, byte[] ad, ulong adlen, byte[] nsec, byte[] npub, byte[] k);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int crypto_aead_xchacha20poly1305_ietf_decrypt(byte[] m, ref ulong mlen_p, byte[] nsec, byte[] c, ulong clen, byte[] ad, ulong adlen, byte[] npub, byte[] k);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int crypto_aead_aes256gcm_encrypt(byte[] c, ref ulong clen_p, byte[] m, ulong mlen, byte[] ad, ulong adlen, byte[] nsec, byte[] npub, byte[] k);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int crypto_aead_aes256gcm_decrypt(byte[] m, ref ulong mlen_p, byte[] nsec, byte[] c, ulong clen, byte[] ad, ulong adlen, byte[] npub, byte[] k);

        #endregion AEAD

        #region Stream

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int crypto_stream_salsa20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic, byte[] k);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int crypto_stream_chacha20_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, ulong ic, byte[] k);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int crypto_stream_chacha20_ietf_xor_ic(byte[] c, byte[] m, ulong mlen, byte[] n, uint ic, byte[] k);

        #endregion Stream
    }
}