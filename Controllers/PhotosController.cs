﻿using PhotosManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Razor.Tokenizer.Symbols;
using static PhotosManager.Controllers.AccessControl;

namespace PhotosManager.Controllers
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
            if (forceRefresh || DB.Photos.HasChanged || DB.Likes.HasChanged || DB.Users.HasChanged || DB.Comments.HasChanged)
            {
                if (DB.Likes.HasChanged || DB.Comments.HasChanged) DB.Photos.ResetLikesCount();

                string sortType = Session["sortType"]?.ToString() ?? "dates"; //C'est le type de tri choisi
                bool descending = Session["sortDescending"] as bool? ?? true; //ascendant ou descendant
                string keywords = Session["searchKeywords"]?.ToString()?.ToLower() ?? ""; //mots clé de search
                int? ownerId = Session["photoOwnerSearchId"] as int?; //permet de donner l'id de l'utilisateur choisi

                User connectedUser = (User)Session["ConnectedUser"];

                IEnumerable<Photo> photos = DB.Photos.ToList();

                if(!string.IsNullOrWhiteSpace(keywords))
                {
                    photos = photos.Where(p => p.Title.ToLower().Contains(keywords) || p.Description.ToLower().Contains(keywords));
                }
                if(ownerId.HasValue && ownerId.Value != 0)
                {
                    photos = photos.Where(p => p.OwnerId == ownerId.Value);
                }

                switch(sortType)
                {
                    case "likes":
                        photos = descending ? photos.OrderByDescending(p => p.LikesCount) : photos.OrderBy(p => p.LikesCount);
                        break;
                    case "comments":
                        photos = descending ? photos.OrderByDescending(p => p.CommentsCount) : photos.OrderBy(p => p.CommentsCount);
                        break;
                    case "dates":
                    default:
                        photos = descending ? photos.OrderByDescending(p => p.CreationDate) : photos.OrderBy(p => p.CreationDate);
                        break;
                }

                return PartialView("GetPhotos", photos.ToList());
            }
            return null;
        }
        public ActionResult List(string sortType = "dates", bool? descending = null)
        {
            Session["id"] = null;
            Session["IsOwner"] = null;
            DB.Photos.ResetLikesCount();

            if(descending == null) 
                descending = Session["sortDescending"] as bool? ?? true; 

            Session["sortType"] = sortType;
            Session["sortDescending"] = descending;

            ViewBag.SortType = sortType; //donne le type de tri choisi
            ViewBag.SortDescending = descending; //donne si le tri est ascendant ou descendant

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
            photo.OwnerId = ((User)Session["ConnectedUser"]).Id;
            photo.CreationDate = DateTime.Now;
            DB.Photos.Add(photo);
            return RedirectToAction("List");
        }
        public ActionResult Edit()
        {
            if (Session["id"] != null && Session["IsOwner"] != null && (bool)Session["IsOwner"])
            {
                int id = (int)Session["id"];
                Photo photo = DB.Photos.Get(id);
                User connectedUser = (User)Session["ConnectedUser"];
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
            User connectedUser = ((User)Session["ConnectedUser"]);
            if (Session["IsOwner"] != null? (bool)Session["IsOwner"] : false)
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
        public ActionResult GetDetails(bool forceRefresh = false)
        {
            if (forceRefresh || DB.Photos.HasChanged || DB.Users.HasChanged || DB.Comments.HasChanged || DB.Likes.HasChanged)
            {
                int photoId = Session["currentPhotoId"] != null ? (int)Session["currentPhotoId"] : 0;
                DB.Photos.ResetLikesCount();
                Photo photo = DB.Photos.Get(photoId);
                if (photo != null)
                    return PartialView(photo);
            }
            return null;
        }
        public ActionResult Details(int id)
        {
            Photo photo = DB.Photos.Get(id);
            if (photo != null)
            {
                Session["currentPhotoId"] = id;
                User connectedUser = ((User)Session["ConnectedUser"]);
                Session["IsOwner"] = connectedUser.IsAdmin || photo.OwnerId == connectedUser.Id;
                if ((bool)Session["IsOwner"] || photo.Shared)
                    return View();
                else
                    return Redirect(IllegalAccessUrl);
            }
            return Redirect(IllegalAccessUrl);
        }
        public ActionResult Delete()
        {
            if (Session["IsOwner"] != null ? (bool)Session["IsOwner"] : false)
            {
                int id = (int)Session["id"];
                Photo photo = DB.Photos.Get(id);
                if (photo != null)
                {
                    User connectedUser = (User)Session["ConnectedUser"];
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
            User connectedUser = (User)Session["ConnectedUser"];
            DB.Likes.ToggleLike(id, connectedUser.Id);

            var photo = DB.Photos.Get(id);
            if (photo != null && photo.OwnerId != connectedUser.Id)
            {
                NotificationsController.AddNotification(photo.OwnerId, $"{connectedUser.Name} a aimé votre photo!");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[PhotosController] Notification not triggered: photo null or same owner");
            }
            return null;
        }
        public ActionResult Comments(int photoId, int parentId = 0)
        {
            List<Comment> comments = DB.Comments.ToList().Where(c => c.PhotoId == photoId && c.ParentId == parentId).ToList();
            return PartialView("RenderComments", comments);
        }
        public ActionResult GetComments(bool forceRefresh = false)
        {
            if (Session["currentPhotoId"] != null)
            {
                int photoId = (int)Session["currentPhotoId"];
                if (forceRefresh || true)
                {
                    List<Comment> comments = DB.Comments.ToList().Where(c => c.PhotoId == photoId && c.ParentId == 0).ToList();
                    return PartialView("RenderComments", comments);
                }
            }
            return null;
        }
        [HttpPost]
        public ActionResult CreateComment(int parentId, string commentText)
        {
            User connectedUser = ((User)Session["ConnectedUser"]);
            Comment comment = new Comment();
            comment.ParentId = parentId;
            comment.PhotoId = (int)Session["currentPhotoId"];
            comment.OwnerId = connectedUser.Id;
            comment.Text = commentText;
            comment.CreationDate = DateTime.Now;
            DB.Comments.Add(comment);

            var photo = DB.Photos.Get(comment.PhotoId);
            if (photo != null && photo.OwnerId != connectedUser.Id)
            {
                NotificationsController.AddNotification(photo.OwnerId, $"{connectedUser.Name} a commenté votre photo!");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[PhotosController] Notification not triggered: photo null or same owner");
            }

            return null;
        }
        [HttpPost]
        public ActionResult UpdateComment(int commentId, string commentText)
        {
            User connectedUser = ((User)Session["ConnectedUser"]);
            Comment comment = DB.Comments.Get(commentId);
            if (comment != null && comment.Owner.Id == connectedUser.Id)
            {
                comment.Text = commentText;
                DB.Comments.Update(comment);
            }
            return null;
        }
        public ActionResult ToggleCommentLike(int id)
        {
            User connectedUser = (User)Session["ConnectedUser"];
            DB.Likes.ToggleCommentLike(id, connectedUser.Id);

            var comment = DB.Comments.Get(id); // Fetch the comment
            if (comment != null)
            {
                var photo = DB.Photos.Get(comment.PhotoId); // Fetch the photo via comment's PhotoId
                if (photo != null && photo.OwnerId != connectedUser.Id)
                {
                    NotificationsController.AddNotification(photo.OwnerId, $"{connectedUser.Name} a aimé votre commentaire!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ToggleCommentLike: No notification added - photo null or same owner");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ToggleCommentLike: No notification added - comment not found");
            }

            return null;
        }
        public ActionResult DeleteComment(int id)
        {
            User connectedUser = ((User)Session["ConnectedUser"]);
            Comment comment = DB.Comments.Get(id);
            if (comment != null && comment.Owner.Id == connectedUser.Id)
            {
                DB.Comments.BeginTransaction();
                DB.Comments.Delete(comment.Id);
                DB.Comments.EndTransaction();
            }
            return null;
        }

        //En gros c'est ce qui fait le sens du tri et fait juste changer la valeur dans la session pour l'autre
        public JsonResult ToggleSortDirection()
        {
            Session["sortDescending"] = !(Session["sortDescending"] as bool? ?? true);

            string sortType = Session["sortType"]?.ToString() ?? "dates"; // Garde le tri actuel
            bool descending = (bool)Session["sortDescending"]; // Garde le sens actuel


            // Retourne les nouvelles valeurs et permet que ça soit pas dans l'url et aussi pas besoin d'un fichier json aussi
            return Json(new
            {
                success = true,
                descending,
                sortType
            }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ToggleSearch()
        {
            bool current = Session["ShowSearch"] as bool? ?? false;
            Session["ShowSearch"] = !current;
            return RedirectToAction("List");
        }
    }
}