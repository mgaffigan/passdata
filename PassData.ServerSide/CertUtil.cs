using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CERTENROLLLib;
using System.Diagnostics.Contracts;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using X509KeyUsageFlags = CERTENROLLLib.X509KeyUsageFlags;
using System.Diagnostics;

namespace PassData
{
    internal static class CertUtil
    {
        public sealed class EphemeralCertificateLifetimeManager : IDisposable
        {
            private readonly X509Certificate2 Certificate;
            private readonly StoreLocation StoreLocation;
            private readonly string StoreName;
            private bool isDisposed;

            public EphemeralCertificateLifetimeManager(X509Certificate2 cert, string storeName, StoreLocation location)
            {
                if (cert == null)
                {
                    throw new ArgumentNullException(nameof(cert));
                }
                if (string.IsNullOrWhiteSpace(storeName))
                {
                    throw new ArgumentNullException(nameof(storeName));
                }

                this.Certificate = cert;
                this.StoreName = storeName;
                this.StoreLocation = location;
            }

            public void Dispose()
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException(nameof(EphemeralCertificateLifetimeManager));
                }
                isDisposed = true;

                var store = new X509Store(this.StoreName, this.StoreLocation);
                store.Open(OpenFlags.ReadWrite);
                try
                {
                    var foundCert = store.Certificates.Find(X509FindType.FindByThumbprint, Certificate.Thumbprint, false);
                    store.Remove(foundCert[0]);
                }
                finally
                {
                    store.Close();
                }
            }
        }

        public static X509Certificate2 CreateCodeSigningCertificate(string subjectName, string oid, byte[] data)
        {
            var dn = new CX500DistinguishedName();
            dn.Encode(subjectName, X500NameFlags.XCN_CERT_NAME_STR_NONE);

            // create a new private key for the certificate
            CX509PrivateKey privateKey = new CX509PrivateKey();
            // http://blogs.technet.com/b/pki/archive/2009/08/05/how-to-create-a-web-server-ssl-certificate-manually.aspx
            privateKey.ProviderName = "Microsoft RSA SChannel Cryptographic Provider";
            privateKey.Length = 2048;
            privateKey.KeySpec = X509KeySpec.XCN_AT_KEYEXCHANGE;
            privateKey.MachineContext = false;
            privateKey.ExportPolicy = X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_PLAINTEXT_EXPORT_FLAG;
            privateKey.Create();

            // Use the stronger SHA512 hashing algorithm
            var hashobj = new CObjectId();
            hashobj.InitializeFromAlgorithmName(ObjectIdGroupId.XCN_CRYPT_HASH_ALG_OID_GROUP_ID,
                ObjectIdPublicKeyFlags.XCN_CRYPT_OID_INFO_PUBKEY_ANY,
                AlgorithmFlags.AlgorithmFlagsNone, "SHA512");

            // add code signing EKUs
            var oidCodeSigning = new CObjectId();
            oidCodeSigning.InitializeFromValue("1.3.6.1.5.5.7.3.3");
            var oidLifetimeSigning = new CObjectId();
            oidLifetimeSigning.InitializeFromValue("1.3.6.1.4.1.311.10.3.13");
            var oidlist = new CObjectIds();
            oidlist.Add(oidCodeSigning);
            oidlist.Add(oidLifetimeSigning);
            var eku = new CX509ExtensionEnhancedKeyUsage();
            eku.InitializeEncode(oidlist);

            var keyUsage = new CX509ExtensionKeyUsage();
            keyUsage.InitializeEncode(
                // Digital Signature, Key Encipherment (a0)
                X509KeyUsageFlags.XCN_CERT_DIGITAL_SIGNATURE_KEY_USAGE |
                X509KeyUsageFlags.XCN_CERT_KEY_ENCIPHERMENT_KEY_USAGE);

            // add CA Restriction (not a CA)
            var caRestriction = new CX509ExtensionBasicConstraints();
            caRestriction.InitializeEncode(false, -1);

            // add the arbitrary data
            var ourExtensionOid = new CObjectId();
            ourExtensionOid.InitializeFromValue(oid);
            var ourExtension = new CX509Extension();
            ourExtension.Initialize(ourExtensionOid, EncodingType.XCN_CRYPT_STRING_BASE64, Convert.ToBase64String(data));

            // Create the self signing request
            var cert = new CX509CertificateRequestCertificate();
            cert.InitializeFromPrivateKey(X509CertificateEnrollmentContext.ContextUser, privateKey, "");
            cert.Subject = dn;
            cert.Issuer = dn;
            cert.NotBefore = DateTime.Now.AddDays(-1);
            cert.NotAfter = DateTime.Now.AddYears(30);
            cert.X509Extensions.Add((CX509Extension)eku);
            cert.X509Extensions.Add((CX509Extension)caRestriction);
            cert.X509Extensions.Add((CX509Extension)keyUsage);
            cert.X509Extensions.Add((CX509Extension)ourExtension);
            cert.HashAlgorithm = hashobj;
            cert.Encode();

            var enroll = new CX509Enrollment();
            enroll.InitializeFromRequest(cert);
            var csr = enroll.CreateRequest(EncodingType.XCN_CRYPT_STRING_BASE64);
            enroll.InstallResponse(
                InstallResponseRestrictionFlags.AllowUntrustedCertificate,
                csr, EncodingType.XCN_CRYPT_STRING_BASE64, "");
            return new X509Certificate2(Convert.FromBase64String(enroll.Certificate[EncodingType.XCN_CRYPT_STRING_BASE64]));
        }
    }
}
