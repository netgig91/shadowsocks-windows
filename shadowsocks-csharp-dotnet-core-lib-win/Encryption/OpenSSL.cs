using System;
using System.Runtime.InteropServices;
using System.Security;

using Shadowsocks.Std.Util;

namespace Shadowsocks.Std.Win.Encryption
{
    internal static class OpenSSL
    {
        private const string DLL_NAME = Utils.libsscrypto + ".dll";

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr EVP_CIPHER_CTX_new();

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EVP_CIPHER_CTX_free(IntPtr ctx);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EVP_CIPHER_CTX_reset(IntPtr ctx);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EVP_CipherInit_ex(IntPtr ctx, IntPtr type, IntPtr impl, byte[] key, byte[] iv, int enc);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EVP_CipherUpdate(IntPtr ctx, byte[] outb, out int outl, byte[] inb, int inl);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EVP_CipherFinal_ex(IntPtr ctx, byte[] outm, ref int outl);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EVP_CIPHER_CTX_set_padding(IntPtr x, int padding);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EVP_CIPHER_CTX_set_key_length(IntPtr x, int keylen);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EVP_CIPHER_CTX_ctrl(IntPtr ctx, int type, int arg, IntPtr ptr);

        /// <summary>
        /// simulate NUL-terminated string
        /// </summary>
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr EVP_get_cipherbyname(byte[] name);
    }
}