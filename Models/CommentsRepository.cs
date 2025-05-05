using JSON_DAL;
using KBD_PFI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PhotosManager.Models
{
    public class CommentsRepository : Repository<Comment>
    {
        public void DeleteByPhoto(int photoId)
        {
            List<Comment> list = ToList().Where(c => c.PhotoId == photoId).ToList();
            list.ForEach(c => base.Delete(c.Id));
        }
        public override bool Delete(int commentId)
        {
            try
            {
                Comment commentToDelete = DB.Comments.Get(commentId);
                if (commentToDelete != null)
                {
                    BeginTransaction();
                    base.Delete(commentId);
                    EndTransaction();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete Comment failed : Message - {ex.Message}");
                EndTransaction();
                return false;
            }
        }
    }
}
