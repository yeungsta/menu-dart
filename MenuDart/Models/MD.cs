using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace MenuDart.Models
{
    public class Menu
    {
        public int ID { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string City { get; set; }
        public string Website { get; set; }
        public string AboutTitle { get; set; }
        public string AboutText { get; set; }
        public string Template { get; set; }
        public string Owner { get; set; }
        public string MenuDartUrl { get; set; }
        //populated only if one location, or shared across all locations
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Facebook { get; set; }
        public string Twitter { get; set; }
        public string Yelp { get; set; }
        [Column(TypeName = "xml")]
        public string Locations { get; set; }
        [Column(TypeName = "xml")]
        public string MenuTree { get; set; }
    }

    public class Location
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string MapImgUrl { get; set; }
        public string MapLink { get; set; }
        public string Hours { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Facebook { get; set; }
        public string Twitter { get; set; }
        public string Yelp { get; set; }
    }

    [XmlInclude(typeof(MenuLeaf))]
    public class MenuNode
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Text { get; set; }
        public List<MenuNode> Branches { get; set; }
    }

    public class MenuLeaf : MenuNode
    {
        public string Description { get; set; }
        public decimal? Price { get; set; }
    }

    public class Template
    {
        public int ID { get; set; }
        [Required]
        public string Name { get; set; }
        public string HdrTxtClr { get; set; }
        public string HdrTxtFnt { get; set; }
        public string HdrClrGrdntTop { get; set; }
        public string HdrClrGrdntBottom { get; set; }
        public string BkgndClr { get; set; }
        public string Stripes { get; set; }
        public string FtrTxtClr { get; set; }
        public string FtrLnkClr { get; set; }
        public string temp { get; set; }
    }

    public class TempMenu
    {
        public int ID { get; set; }
        public string SessionId { get; set; }
        public int MenuId { get; set; }
        public System.DateTime DateCreated { get; set; }
    }

    public class MenuDartDBContext : DbContext
    {
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Template> Templates { get; set; }

        //we won't be storing Locations in it's SQL table, however. Just as XML
        //in the Menus table.
        public DbSet<Location> Locations { get; set; }

        //we won't be storing MenuNodes in it's SQL table, however. Just as XML
        //in the Menus table.
        public DbSet<MenuNode> MenuTree { get; set; }

        public DbSet<TempMenu> TempMenus { get; set; }
    }
}