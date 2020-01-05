using System;
using System.Runtime.InteropServices;

using Shadowsocks.Std.Util;

namespace Shadowsocks.Std.Win.Encryption
{
    internal static class MbedTLS
    {
        private const string DLL_NAME = Utils.libsscrypto + ".dll";

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int md5_ret(byte[] input, uint ilen, byte[] output);

        /// <summary>
        /// Get cipher ctx size for unmanaged memory allocation
        /// </summary>
        /// <returns></returns>
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int cipher_get_size_ex();

        #region Cipher layer wrappers

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        internal static extern IntPtr cipher_info_from_string(string cipher_name);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void cipher_init(IntPtr ctx);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int cipher_setup(IntPtr ctx, IntPtr cipher_info);

        // XXX: Check operation before using it
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int cipher_setkey(IntPtr ctx, byte[] key, int key_bitlen, int operation);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int cipher_set_iv(IntPtr ctx, byte[] iv, int iv_len);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int cipher_reset(IntPtr ctx);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int cipher_update(IntPtr ctx, byte[] input, int ilen, byte[] output, ref int olen);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void cipher_free(IntPtr ctx);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int cipher_auth_encrypt(IntPtr ctx,
            byte[] iv, uint iv_len,
            IntPtr ad, uint ad_len,
            byte[] input, uint ilen,
            byte[] output, ref uint olen,
            byte[] tag, uint tag_len);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int cipher_auth_decrypt(IntPtr ctx,
            byte[] iv, uint iv_len,
            IntPtr ad, uint ad_len,
            byte[] input, uint ilen,
            byte[] output, ref uint olen,
            byte[] tag, uint tag_len);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int hkdf(byte[] salt,
            int salt_len, byte[] ikm, int ikm_len,
            byte[] info, int info_len, byte[] okm,
            int okm_len);

        #endregion Cipher layer wrappers
    }
}