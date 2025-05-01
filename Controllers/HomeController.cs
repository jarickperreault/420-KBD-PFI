using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace KBD_PFI.Controllers
{
    public class HomeController : Controller
    {
        
        public ActionResult About()
        {
            ViewBag.Message = "À propos";

            return View();
        }

    }
}