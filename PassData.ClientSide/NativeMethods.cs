using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PassData
{
    internal static class NativeMethods
    {
        internal const string Crypt32 = "crypt32.dll";

        public const int ERROR_SUCCESS = 0;
        public const int ERROR_MORE_DATA = 234;
        public const uint CMSG_ENCODED_MESSAGE = 29;
        public const string szOID_NESTED_SIGNATURE = "1.3.6.1.4.1.311.2.4.1";
        public enum CERT_QUERY_OBJECT : uint
        {
            FILE = 0x00000001
        }

        public enum CERT_QUERY_CONTENT : uint
        {
            FLAG_PKCS7_SIGNED_EMBED = 1 << 10
        }

        public enum CERT_QUERY_FORMAT : uint
        {
            FLAG_BINARY = 1 << 1
        }

        [DllImport(Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CryptQueryObject(
            CERT_QUERY_OBJECT dwObjectType,
            [MarshalAs(UnmanagedType.LPWStr)] string pvObject,
            CERT_QUERY_CONTENT dwExpectedContentTypeFlags,
            CERT_QUERY_FORMAT dwExpectedFormatTypeFlags,
            [In]     uint dwFlags,
            out uint pdwMsgAndCertEncodingType,
            out uint pdwContentType,
            out uint pdwFormatType,
            out SafeCertStoreHandle phCertStore,
            out SafeCryptMsgHandle phMsg,
            [In, Out] IntPtr ppvContext);

        [DllImport(Crypt32, SetLastError = true)]
        private static extern bool CertCloseStore(IntPtr hCertStore, uint dwFlags);

        [DllImport(Crypt32, SetLastError = true)]
        private static extern bool CryptMsgClose(IntPtr handle);

        [DllImport(Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public extern static bool CryptMsgGetParam(
            [In]      SafeCryptMsgHandle hCryptMsg,
            [In]      uint dwParamType,
            [In]      uint dwIndex,
            [In, Out] IntPtr pvData,
            [In, Out] ref int pcbData);

        public sealed class SafeCertStoreHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeCertStoreHandle()
                : base(true)
            {
            }

            override protected bool ReleaseHandle()
            {
                return CertCloseStore(handle, 0);
            }
        }

        public sealed class SafeCryptMsgHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeCryptMsgHandle()
                : base(true)
            {
            }

            override protected bool ReleaseHandle()
            {
                return CryptMsgClose(handle);
            }
        }
    }
}
