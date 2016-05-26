using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace Demo.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Header(string p1)
        {
            Thread.Sleep(3000);
            return View("~/Views/Shared/Header.cshtml", "This is a Default action." as object);
        }

        [HttpGet]
        public ActionResult HeaderGet(string p1, string p2, int p3)
        {
            Thread.Sleep(7000);
            return View("~/Views/Shared/Header.cshtml", "This is a GET action." as object);
        }

        [HttpPost]
        public ActionResult HeaderPost(string p1)
        {
            Thread.Sleep(5000);
            return View("~/Views/Shared/Header.cshtml", "This is a POST action." as object);
        }
    }
}
