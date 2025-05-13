using JSON_DAL;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Web;

namespace PhotosManager.Models
{
    public class NotificationsRepository : Repository<Notification>
    {
        public string Pop()
        {
            try
            {
                var user = HttpContext.Current.Session["ConnectedUser"] as User;
                if (user == null || !user.Notify)
                {
                    return null;
                }

                BeginTransaction();
                var notification = ToList()
                    .Where(n => n.UserId == user.Id)
                    .OrderBy(n => n.CreatedDate)
                    .FirstOrDefault();

                if (notification != null)
                {
                    base.Delete(notification.Id);
                    EndTransaction();
                    return notification.Message;
                }
                EndTransaction();
                return null;
            }
            catch (Exception ex)
            {
                EndTransaction();
                return null;
            }
        }

        public void Add(int userId, string message)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Message = message,
                    CreatedDate = DateTime.Now
                };
                BeginTransaction();
                base.Add(notification);
                System.Diagnostics.Debug.WriteLine($"[NotificationsRepository] Notification added, committing transaction");
                EndTransaction();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationsRepository] Error adding notification: {ex.Message}");
                EndTransaction();
            }
        }
    }
}