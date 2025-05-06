using KBD_PFI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static KBD_PFI.Controllers.AccessControl;

namespace KBD_PFI.Controllers
{
    [UserAccess]
    public class PhotosController : Controller
    {
        const string IllegalAccessUrl = "/Accounts/Login?message=Tentative d'accès illégal!&success=false";

        public ActionResult SetPhotoOwnerSearchId(int id)
        {
            Session["photoOwnerSearchId"] = id;
            return RedirectToAction("List");
        }

        public ActionResult SetSearchKeywords(string keywords)
        {
            Session["searchKeywords"] = keywords;
            return RedirectToAction("List");
        }

        public ActionResult GetPhotos(bool forceRefresh = false)
        {
            if (forceRefresh || DB.Photos.HasChanged || DB.Likes.HasChanged || DB.Users.HasChanged)
            {
                return PartialView(DB.Photos.ToList().OrderByDescending(p => p.CreationDate).ToList());
            }
            return null;
        }

        public ActionResult List()
        {
            Session["id"] = null;
            Session["IsOwner"] = null;
            return View();
        }

        public ActionResult Create()
        {
            return View(new Photo());
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Create(Photo photo)
        {
            var connectedUser = (User)Session["ConnectedUser"];
            if (connectedUser == null)
            {
                return Json(new { success = false, error = "User not logged in" });
            }
            photo.OwnerId = connectedUser.Id;
            photo.CreationDate = DateTime.Now;
            DB.Photos.Add(photo);
            return RedirectToAction("List");
        }

        public ActionResult Comments(int photoId, int parentId = 0)
        {
            List<Comment> comments = DB.Comments.ToList().Where(c => c.PhotoId == photoId && c.ParentId == parentId).ToList();
            return PartialView("RenderComments", comments);
        }

        public ActionResult GetComments(bool forceRefresh = false)
        {
            if (Session["currentPhotoId"] == null)
            {
                return Content(""); // Return empty content if no photo is selected
            }
            int photoId = (int)Session["currentPhotoId"];
            if (forceRefresh || true) // always refresh
            {
                List<Comment> comments = DB.Comments.ToList().Where(c => c.PhotoId == photoId && c.ParentId == 0).ToList();
                return PartialView("RenderComments", comments);
            }
            return Content("");
        }

        [HttpPost]
        public ActionResult CreateComment(int photoId, int parentId, string text)
        {
            var connectedUser = (User)Session["ConnectedUser"];
            if (connectedUser == null)
            {
                return Json(new { success = false, error = "User not logged in" });
            }
            try
            {
                var comment = new Comment
                {
                    Id = DB.Comments.ToList().Any() ? DB.Comments.ToList().Max(c => c.Id) + 1 : 1,
                    PhotoId = photoId,
                    ParentId = parentId,
                    Text = text,
                    CreationDate = DateTime.Now,
                    OwnerId = connectedUser.Id
                };
                DB.Comments.Add(comment);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult UpdateComment(int commentId, string commentText)
        {
            var connectedUser = (User)Session["ConnectedUser"];
            if (connectedUser == null)
            {
                return Json(new { success = false, error = "User not logged in" });
            }
            try
            {
                Comment comment = DB.Comments.Get(commentId);
                if (comment != null && comment.OwnerId == connectedUser.Id)
                {
                    comment.Text = commentText;
                    DB.Comments.Update(comment);
                    return Json(new { success = true });
                }
                return Json(new { success = false, error = "Unauthorized or comment not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeleteComment(int id)
        {
            var connectedUser = (User)Session["ConnectedUser"];
            if (connectedUser == null)
            {
                return Json(new { success = false, error = "User not logged in" });
            }
            try
            {
                Comment comment = DB.Comments.Get(id);
                if (comment != null && comment.OwnerId == connectedUser.Id)
                {
                    DB.Comments.Delete(id);
                    return Json(new { success = true });
                }
                return Json(new { success = false, error = "Unauthorized or comment not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        public ActionResult Edit()
        {
            if (Session["id"] != null && Session["IsOwner"] != null && (bool)Session["IsOwner"])
            {
                int id = (int)Session["id"];
                Photo photo = DB.Photos.Get(id);
                var connectedUser = (User)Session["ConnectedUser"];
                if (connectedUser == null)
                {
                    return Redirect(IllegalAccessUrl);
                }
                if (photo != null)
                {
                    if (connectedUser.IsAdmin || photo.OwnerId == connectedUser.Id)
                    {
                        return View(photo);
                    }
                    return Redirect(IllegalAccessUrl);
                }
            }
            return Redirect(IllegalAccessUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Edit(Photo photo)
        {
            var connectedUser = (User)Session["ConnectedUser"];
            if (connectedUser == null)
            {
                return Redirect(IllegalAccessUrl);
            }
            if (Session["IsOwner"] != null ? (bool)Session["IsOwner"] : false)
            {
                Photo storedPhoto = DB.Photos.Get((int)Session["id"]);
                photo.Id = storedPhoto.Id;
                photo.OwnerId = storedPhoto.OwnerId;
                photo.CreationDate = storedPhoto.CreationDate;
                DB.Photos.Update(photo);
                return RedirectToAction("List");
            }
            return Redirect(IllegalAccessUrl);
        }

        public ActionResult Details(int id)
        {
            Photo photo = DB.Photos.Get(id);
            if (photo != null)
            {
                Session["id"] = id;
                Session["currentPhotoId"] = id;
                var connectedUser = (User)Session["ConnectedUser"];
                if (connectedUser == null)
                {
                    return Redirect(IllegalAccessUrl);
                }
                Session["IsOwner"] = connectedUser.IsAdmin || photo.OwnerId == connectedUser.Id;
                if ((bool)Session["IsOwner"] || photo.Shared)
                    return View(photo);
                else
                    return Redirect(IllegalAccessUrl);
            }
            return Redirect(IllegalAccessUrl);
        }

        public ActionResult Delete()
        {
            var connectedUser = (User)Session["ConnectedUser"];
            if (connectedUser == null)
            {
                return Redirect(IllegalAccessUrl);
            }
            if (Session["IsOwner"] != null ? (bool)Session["IsOwner"] : false)
            {
                int id = (int)Session["id"];
                Photo photo = DB.Photos.Get(id);
                if (photo != null)
                {
                    if (connectedUser.IsAdmin || photo.OwnerId == connectedUser.Id)
                    {
                        DB.Photos.Delete(id);
                        return RedirectToAction("List");
                    }
                    else
                        return Redirect(IllegalAccessUrl);
                }
            }
            return Redirect(IllegalAccessUrl);
        }

        public ActionResult TogglePhotoLike(int id)
        {
            var connectedUser = (User)Session["ConnectedUser"];
            if (connectedUser == null)
            {
                return Redirect(IllegalAccessUrl);
            }
            DB.Likes.ToggleLike(id, connectedUser.Id);
            return RedirectToAction("Details/" + id);
        }
    }
}