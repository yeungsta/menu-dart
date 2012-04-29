using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MenuDart.Models
{
    public class MenuEditorBasicViewModel
    {
        public int MenuId { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string Website { get; set; }
    }
}