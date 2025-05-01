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
        }
    }
}
