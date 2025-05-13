using PhotosManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PhotosManager.Controllers
{
    public class NotificationsController : Controller
    {
        public JsonResult Pop()
        {
            var user = Session["ConnectedUser"] as User;

            string message = DB.Notifications.Pop();

            return Json(message, JsonRequestBehavior.AllowGet);
        }

        public static void AddNotification(int userId, string message)
        {
            var user = DB.Users.Get(userId);
            if (user != null && user.Notify)
            {
                DB.Notifications.Add(userId, message);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationsController] User {userId} not found or Notify is false");
            }
        }
    }
}