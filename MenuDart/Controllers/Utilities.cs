using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Security.Principal;
using MenuDart.Models;

namespace MenuDart.Controllers
{
    public static class Utilities
    {
        public static bool IsThisMyMenu(int questionedMenuId, MenuDartDBContext db, IPrincipal User)
        {
            IOrderedQueryable<Menu> myMenus = from menu in db.Menus
                                              where menu.Owner == User.Identity.Name
                                              orderby menu.ID ascending
                                              select menu;

            if (myMenus == null)
            {
                return false;
            }

            //check if we own this menu
            foreach (Menu menu in myMenus.ToList())
            {
                if (menu.ID == questionedMenuId)
                {
                    return true;
                }
            }

            return false;
        }

        //Copies source directory + files to a destination directory.
        //If destination directory doesn't exist, it will be created.
        public static void CopyDirTo(string srcPatch, string destPath)
        {
            if (Directory.Exists(srcPatch))
            {
                if (!Directory.Exists(destPath))
                {
                    Directory.CreateDirectory(destPath);
                }

                string[] files = Directory.GetFiles(srcPatch);
                string fileName;
                string destFile;

                // Copy the files and overwrite destination files if they already exist.
                foreach (string s in files)
                {
                    // Use static Path methods to extract only the file name from the path.
                    fileName = Path.GetFileName(s);
                    destFile = Path.Combine(destPath, fileName);
                    System.IO.File.Copy(s, destFile, true);
                }
            }
        }

        //Creates Google map link
        public static string CreateMapLink(string address, string state, string zip)
        {
            string noSpacesAddress = string.Empty;

            if (!string.IsNullOrEmpty(address))
            {
                noSpacesAddress = address.Replace(' ', '+');
            }

            return (Constants.GoogleMapPrefix + noSpacesAddress + "+" + state + "+" + zip);
        }

        //Creates Google map image link
        public static string CreateMapImgLink(string address, string state, string zip)
        {
            string noSpacesAddress = string.Empty;

            if (!string.IsNullOrEmpty(address))
            {
                noSpacesAddress = address.Replace(' ', '+');
            }

            return (Constants.GoogleMapImgPrefix + noSpacesAddress + "+" + state + "+" + zip + Constants.GoogleMapImgSuffix);
        }

        //constructs full URL of site
        public static string PrependUrl(string url)
        {
            return "http://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + url;
        }

        //constructs URL path of menu repository
        public static string GetUrlPath()
        {
            return "http://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/Content/menus/";
        }

        //constructs full URL of menu site
        public static string GetFullUrl(string menuDartUrl)
        {
            return "http://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/Content/menus/" + menuDartUrl + "/" + Constants.OutputFile;
        }

        //constructs full URL of menu site's logo
        public static string GetMenuLogoUrl(string menuDartUrl)
        {
            return "http://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/Content/menus/" + menuDartUrl + "/" + "index_files" + "/" + Constants.LogoFileName;
        }

        //constructs full URL of menu site's temp logo
        public static string GetMenuLogoTmpUrl(string menuDartUrl)
        {
            return "http://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + "/Content/menus/" + menuDartUrl + "/" + "index_files" + "/" + Constants.LogoTmpFileName;
        }

        public static void RemoveDirectory(string menuDartUrl)
        {
            string filepath = HttpContext.Current.Server.MapPath("~/Content/menus/" + menuDartUrl + "/");

            if (Directory.Exists(filepath))
            {
                try
                {
                    Directory.Delete(filepath, true);
                }
                catch
                {
                    //exception is thrown when dir is not empty, so ignore
                }
            }
        }
    }
}