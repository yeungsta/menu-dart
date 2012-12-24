using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;

namespace MenuDart.Models
{
    public class MenuEditorBasicViewModel
    {
        public int MenuId { get; set; }
        [Required(ErrorMessage = "You must enter a restaurant name.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "You must enter the city of your restaurant.")]
        public string City { get; set; }
        public string Phone { get; set; }
        public string Website { get; set; }
        public bool ChangesUnpublished { get; set; }
    }

    public class MenuEditorThemeViewModel
    {
        public int MenuId { get; set; }
        public string Name { get; set; }
        public bool ChangesUnpublished { get; set; }
        public List<string> Themes { get; set; }
        public string CurrentTheme { get; set; }
    }

    public class MenuEditorAboutViewModel
    {
        public int MenuId { get; set; }
        public string Name { get; set; }
        public string AboutTitle { get; set; }
        public string AboutText { get; set; }
        public bool ChangesUnpublished { get; set; }
    }

    public class MenuEditorLogoViewModel
    {
        public int MenuId { get; set; }
        public string Name { get; set; }
        public string MenuDartUrl { get; set; }
        public string LogoUrl { get; set; }
        public bool ChangesUnpublished { get; set; }
        public string Owner { get; set; }
    }

    public class MenuEditorLocationViewModel
    {
        public int MenuId { get; set; }
        public string Name { get; set; }
        public List<Location> Locations { get; set; }
        public bool ChangesUnpublished { get; set; }
    }
}