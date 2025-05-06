using JSON_DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static System.Net.Mime.MediaTypeNames;

namespace KBD_PFI.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public int PhotoId { get; set; }
        public int ParentId { get; set; }
        public DateTime CreationDate { get; set; }
        public string Text { get; set; }

        public Comment(int photoId, int parentId, string commentText)
        {
            Id = 0;
            PhotoId = photoId;
            ParentId = parentId;
            Text = commentText;
            CreationDate = DateTime.Now;
        }
        public Comment(int id, string commentText, int OwnerId)
        {
            Id = id;
            Text = commentText;
            this.OwnerId = OwnerId;
            this.CreationDate = DateTime.Now;
        }

        [JsonIgnore]
        public User Owner => DB.Users.Get(OwnerId);

        [JsonIgnore]
        public Photo Photo => DB.Photos.Get(PhotoId);

        [JsonIgnore]
        public Comment Parent => DB.Comments.Get(ParentId);

        [JsonIgnore]
        public List<Like> Likes => DB.Likes.ToList().Where(l => l.CommentId == Id).ToList();

        [JsonIgnore]
        public List<Comment> Children => DB.Comments.ToList().Where(c => c.ParentId == Id).ToList();

        [JsonIgnore]
        public List<User> UsersLikesList => DB.Likes.ToList().Where(l => l.CommentId == Id).Select(l => DB.Users.Get(l.UserId)).ToList(); // l.UserId => UsersLikesList
        
        //public bool IsDeleted { get; set; } = false;
        //public bool IsModified { get; set; } = false;
        //public bool IsEdited { get; set; } = false;
        //public bool IsReported { get; set; } = false;
        //public bool IsHidden { get; set; } = false;

        //La plus part du jsonIgnore n'est pas necessaire, je l'ai laisser pour les tests seulement
    }
}