﻿using JSON_DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Web;

namespace KBD_PFI.Models
{
    public class Photo
    {
        const string PhotosFolder = @"/App_Assets/Photos/";
        const string DefaultPhoto = @"No_Image.png";

        public int Id { get; set; }
        public int OwnerId { get; set; }            // Id du propriétaire de la photo
        [Display(Name = "Titre"), Required(ErrorMessage = "Obligatoire")]
        public string Title { get; set; }           // Titre de la photo
        [Display(Name = "Description"), Required(ErrorMessage = "Obligatoire")]
        public string Description { get; set; }     // Description de la photo
        public DateTime CreationDate { get; set; }  // Date de création
        public bool Shared { get; set; }            // Indicateur de partage ("true" ou "false")

        [JsonIgnore]
        public List<Like> Likes => DB.Likes.ToList().Where(l => l.PhotoId == Id).ToList(); 

        [JsonIgnore]
        public int LikesCount => Likes.Count;

        [JsonIgnore]
        public List<Comment> Comments => DB.Comments.ToList().Where(c => c.PhotoId == Id).ToList();
        [JsonIgnore]
        public int CommentsCount => DB.Comments.ToList().Where(c => c.PhotoId == Id).Count();
        
        [JsonIgnore]
        public string UsersLikesList
        {
            get
            {
                string UsersLikesList = "";
                foreach (var like in Likes)
                {
                    UsersLikesList += DB.Users.Get(like.UserId).Name + "\n";
                }
                return UsersLikesList;
            }
        }

        [JsonIgnore] //Fonctionne pas, je ne sais pas pourquoi, mais le code devrait ressembler à ça
        public string UsersCommentsList
        {
            get
            {
                string UsersCommentsList = "";
                foreach (var comment in Comments)
                {
                    UsersCommentsList += DB.Users.Get(comment.OwnerId).Name + "\n";
                }
                return UsersCommentsList;
            }
        }

        [ImageAsset(PhotosFolder, DefaultPhoto)]
        public string Image { get; set; }           // Url relatif de l'image

        public Photo()
        {
            Id = 0;
            CreationDate = DateTime.Now;
            Shared = false;
            Image = PhotosFolder + DefaultPhoto;
        }
        [JsonIgnore]
        public User Owner => DB.Users.Get(OwnerId);
    }
}