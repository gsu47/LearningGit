using EICRead.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EICRead.Controllers
{
    public static class Blah
    {
        public static bool HasFile(this HttpPostedFileBase file)
        {
            return (file != null && file.ContentLength > 0) ? true : false;
        }
    }

    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        
        public ActionResult EIC()
        {
            ViewBag.Message = "Load and Read EIC As-Applied File";

            // Upload the files
            foreach (string upload in Request.Files)
            //if (Request.Files.Count > 0)
            {
                //string upload = Request.Files[0].ToString();
                if (!Request.Files[upload].HasFile()) continue;
                //if (Request.Files[upload].HasFile())
                //{
                    string path = AppDomain.CurrentDomain.BaseDirectory + "uploads";
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    string filename = Path.GetFileName(Request.Files[upload].FileName);

                    string filepath = Path.Combine(path, filename);
                    Request.Files[upload].SaveAs(filepath);

                    string dirpath = Path.Combine(path, filename.Substring(0, filename.LastIndexOf('.')));
                    if (Directory.Exists(dirpath)) Directory.Delete(dirpath, true);
                    ZipFile.ExtractToDirectory(filepath, dirpath);

                    // Store file information to display on page
                    var model = new AsApplied(dirpath);
                    return View(model);
                //}
            }
            
            return View();
        }

        public ActionResult EIC2()
        {
            ViewBag.Message = "Load and Read EIC Yield/AsApplied File";

            // Upload the files
            foreach (string upload in Request.Files)
            //if (Request.Files.Count > 0)
            {
                //string upload = Request.Files[0].ToString();
                if (!Request.Files[upload].HasFile()) continue;
                //if (Request.Files[upload].HasFile())
                //{
                string path = AppDomain.CurrentDomain.BaseDirectory + "uploads";
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                string filename = Path.GetFileName(Request.Files[upload].FileName);

                string filepath = Path.Combine(path, filename);
                Request.Files[upload].SaveAs(filepath);

                string dirpath = Path.Combine(path, filename.Substring(0, filename.LastIndexOf('.')));
                if (Directory.Exists(dirpath)) Directory.Delete(dirpath, true);
                ZipFile.ExtractToDirectory(filepath, dirpath);

                // Store file information to display on page
                var model = new Yield(dirpath);
                return View(model);
                //}
            }

            return View();
        }    
    }
}
