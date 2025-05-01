using JSON_DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KBD_PFI.Models
{
    public class Comment
    {
        public int ID { get; set; }
        public int PhotoId { get; set; }
        public int ParentId { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }
        public int OwnerId { get; set; }
        //public bool IsDeleted { get; set; } = false;
        //public bool IsModified { get; set; } = false;
        //public bool IsEdited { get; set; } = false;
        //public bool IsReported { get; set; } = false;
        //public bool IsHidden { get; set; } = false;
        //public List<Comment> Replies { get; set; } = new List<Comment>();
    }
}