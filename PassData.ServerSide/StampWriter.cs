using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace PassData
{
    public static class StampWriter
    {
        public static void StampFile(string targetPath, string signToolPath, string certSubjectName, string extensionOid, byte[] stampData)
        {
            var data = CertUtil.CreateCodeSigningCertificate(certSubjectName, extensionOid, stampData);
            using (new CertUtil.EphemeralCertificateLifetimeManager(data, "My", StoreLocation.CurrentUser))
            {
                var thumbprint = data.Thumbprint;
                var signToolArgs = $"sign /fd sha256 /sha1 {thumbprint} /as /v \"{targetPath}\"";
                var startInfo = new ProcessStartInfo(signToolPath, signToolArgs);
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;

                var signToolProcess = Process.Start(startInfo);
                signToolProcess.WaitForExit();

                try
                {
                    if (signToolProcess.ExitCode != 0)
                    {
                        throw new ApplicationException(signToolProcess.StandardError.ReadToEnd());
                    }
                }
                finally
                {
                    signToolProcess.Close();
                }
            }
        }
    }
}
