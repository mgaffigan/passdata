using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PassData
{
    using static NativeMethods;

    public static class StampReader
    {
        public static byte[] ReadStampFromFile(string path, string certSubjectName, string extensionOid)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (string.IsNullOrWhiteSpace(certSubjectName))
            {
                throw new ArgumentNullException(nameof(certSubjectName));
            }
            if (string.IsNullOrWhiteSpace(extensionOid))
            {
                throw new ArgumentNullException(nameof(extensionOid));
            }

            try
            {

                var cert = FindSigningCertificateFromAuthenticodeExe(path, certSubjectName);
                var extensionData = cert.Extensions.Cast<X509Extension>()
                    .FirstOrDefault(s => s.Oid.Value == extensionOid);
                if (extensionData == null)
                {
                    throw new StampNotFoundException($"Stamp certificate was found, but no extension with OID {extensionOid} was found");
                }

                return extensionData.RawData;
            }
            catch (Exception ex)
            {
                throw new StampNotFoundException("An exception occurred while reading the stamp data", ex);
            }
        }

        private static X509Certificate2 FindSigningCertificateFromAuthenticodeExe(string thisPath, string subjectName)
        {
            var pkcs7data = GetPkcs7FromAuthenticodeExe(thisPath);

            var cms = new SignedCms();
            cms.Decode(pkcs7data);

            foreach (var nested in cms.SignerInfos[0].UnsignedAttributes
                .Cast<CryptographicAttributeObject>()
                .Where(ca => ca.Oid.Value == szOID_NESTED_SIGNATURE)
                .SelectMany(ca => ca.Values.Cast<AsnEncodedData>())
                .Where(ca => ca.Oid.Value == szOID_NESTED_SIGNATURE))
            {
                var cms2 = new SignedCms();
                cms2.Decode(nested.RawData);

                var subsigner = cms2.SignerInfos[0].Certificate;
                if (subsigner.Subject == subjectName)
                {
                    return subsigner;
                }
            }

            throw new IndexOutOfRangeException();
        }

        private static byte[] GetPkcs7FromAuthenticodeExe(string thisPath)
        {
            uint dwEncoding, dwContentType, dwFormatType;
            SafeCertStoreHandle hStore;
            SafeCryptMsgHandle hMsg;
            if (!CryptQueryObject(CERT_QUERY_OBJECT.FILE, thisPath, CERT_QUERY_CONTENT.FLAG_PKCS7_SIGNED_EMBED,
                CERT_QUERY_FORMAT.FLAG_BINARY, 0, out dwEncoding, out dwContentType, out dwFormatType,
                out hStore, out hMsg, IntPtr.Zero))
            {
                throw new Win32Exception();
            }
            using (hMsg)
            using (hStore)
            {
                return ReadCryptMsgAttribute(hMsg, CMSG_ENCODED_MESSAGE, 0);
            }
        }

        private static byte[] ReadCryptMsgAttribute(SafeCryptMsgHandle hMsg, uint attribute, uint index)
        {
            int cbData = 0;
            // we cannot check the result because it seems to violate the docs
            CryptMsgGetParam(hMsg, attribute, index, IntPtr.Zero, ref cbData);
            if (Marshal.GetLastWin32Error() != ERROR_SUCCESS
                && Marshal.GetLastWin32Error() != ERROR_MORE_DATA)
            {
                throw new Win32Exception();
            }

            var bResult = new byte[cbData];
            IntPtr pResult = Marshal.AllocHGlobal(cbData);
            try
            {
                if (!CryptMsgGetParam(hMsg, attribute, index, pResult, ref cbData))
                {
                    throw new Win32Exception();
                }
                Marshal.Copy(pResult, bResult, 0, bResult.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(pResult);
            }

            return bResult;
        }
    }
}
