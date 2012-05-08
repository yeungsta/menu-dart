using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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

    public class MenuEditorThemeViewModel
    {
        public int MenuId { get; set; }
        public string Name { get; set; }
    }

    public class MenuEditorAboutViewModel
    {
        public int MenuId { get; set; }
        public string Name { get; set; }
        public string AboutTitle { get; set; }
        public string AboutText { get; set; }
    }

    public class MenuEditorLogoViewModel
    {
        public int MenuId { get; set; }
        public string Name { get; set; }
        public string MenuDartUrl { get; set; }
        public string LogoUrl { get; set; }
    }

    public class MenuEditorLocationViewModel
    {
        public int MenuId { get; set; }
        public string Name { get; set; }
        public List<Location> Locations { get; set; }
    }
}