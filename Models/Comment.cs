using JSON_DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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

        public Comment()
        {
            Id = 0;
            CreationDate = DateTime.Now;
        }
        
        //public bool IsDeleted { get; set; } = false;
        //public bool IsModified { get; set; } = false;
        //public bool IsEdited { get; set; } = false;
        //public bool IsReported { get; set; } = false;
        //public bool IsHidden { get; set; } = false;
    }
}