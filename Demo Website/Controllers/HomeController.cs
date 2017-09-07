using Demo.Common;
using PassData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using IOFile = System.IO.File;

namespace Demo_Website.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        public FileResult DynamicApplication()
        {
            var stampText = $"Server time is currently {DateTime.Now} at time of stamping";
            var stampData = Encoding.UTF8.GetBytes(stampText);
            var sourceFile = Server.MapPath("~/Content/Demo Application.exe");
            var signToolPath = Server.MapPath("~/App_Data/signtool.exe");
            var tempFile = Path.GetTempFileName();
            bool deleteStreamOpened = false;
            try
            {
                IOFile.Copy(sourceFile, tempFile, true);
                StampWriter.StampFile(tempFile, signToolPath, StampConstants.StampSubject, StampConstants.StampOid, stampData);

                var deleteOnClose = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 4096, FileOptions.DeleteOnClose);
                deleteStreamOpened = true;
                return File(deleteOnClose, "application/octet-stream", "Demo Application.exe");
            }
            finally
            {
                if (!deleteStreamOpened)
                {
                    try
                    {
                        IOFile.Delete(tempFile);
                    }
                    catch
                    {
                        // no-op, opportunistic cleanup
                        Debug.WriteLine("Failed to cleanup file");
                    }
                }
            }
        }
    }
}