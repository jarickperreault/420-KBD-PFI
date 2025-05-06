﻿using JSON_DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KBD_PFI.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int PhotoId { get; set; }
        public int ParentId { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }
        public int OwnerId { get; set; }
    }
}