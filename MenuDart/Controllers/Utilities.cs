using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Security.Principal;
using MenuDart.Models;
using Elmah;

namespace MenuDart.Controllers
{
    public static class Utilities
    {
        public static bool IsThisMyMenu(int questionedMenuId, MenuDartDBContext db, IPrincipal User)
        {
            //check if admin role
            if (User.IsInRole("Administrator"))
            {
                return true;
            }

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
        public static void CopyDirTo(string srcPatch, string destPath, bool overwrite)
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

                // Copy the files
                foreach (string s in files)
                {
                    // Use static Path methods to extract only the file name from the path.
                    fileName = Path.GetFileName(s);
                    destFile = Path.Combine(destPath, fileName);

                    try
                    {
                        System.IO.File.Copy(s, destFile, overwrite);
                    }
                    catch (Exception e)
                    {
                        //ignore exceptions about existing files
                        Utilities.LogAppError("Exception thrown while trying to copy files to destination folder.", e);
                    }
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
            return "http://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + Constants.MenusDir;
        }

        //constructs full URL of menu site
        public static string GetFullUrl(string menuDartUrl)
        {
            return "http://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + Constants.MenusDir + menuDartUrl + "/" + Constants.OutputFile;
        }

        //constructs full URL of preview menu site
        public static string GetFullUrlPreview(string menuDartUrl)
        {
            return "http://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + Constants.PreviewMenusDir + menuDartUrl + "/" + Constants.OutputFile;
        }

        //constructs full URL of menu site's logo
        public static string GetMenuLogoUrl(string menuDartUrl)
        {
            return "http://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + Constants.MenusDir + menuDartUrl + "/" + "index_files" + "/" + Constants.LogoFileName;
        }

        //constructs full URL of menu site's temp logo
        public static string GetMenuLogoTmpUrl(string menuDartUrl)
        {
            return "http://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + Constants.MenusDir + menuDartUrl + "/" + "index_files" + "/" + Constants.LogoTmpFileName;
        }

        public static void RemoveDirectory(string menuDartUrl)
        {
            string filepath = HttpContext.Current.Server.MapPath(Constants.MenusPath + menuDartUrl + "/");

            if (Directory.Exists(filepath))
            {
                try
                {
                    string[] files = Directory.GetFiles(filepath);
                    string[] dirs = Directory.GetDirectories(filepath);

                    // Remove all the files
                    foreach (string s in files)
                    {
                        System.IO.File.Delete(s);
                    }

                    // Remove all sub directories
                    foreach (string d in dirs)
                    {
                        Directory.Delete(d, true);
                    }

                    Directory.Delete(filepath, true);
                }
                catch (Exception e)
                {
                    //exception is sometimes thrown when dir is not empty. Remove directory again.
                    Utilities.LogAppError("Exception thrown while trying to remove a directory.", e);
                }
            }
        }

        public static void DeactivateDirectory(string menuDartUrl)
        {
            string filepath = HttpContext.Current.Server.MapPath(Constants.MenusPath + menuDartUrl + "/");

            if (Directory.Exists(filepath))
            {
                string[] files = Directory.GetFiles(filepath);

                // Remove all the files (menu file) but leave the index dir intact
                foreach (string s in files)
                {
                    System.IO.File.Delete(s);
                }
            }
        }

        //generates random 5-digit alphanumeric for temp directories
        public static string GetRandomId()
        {
            bool uniqueFound = false;
            string randomId = string.Empty;

            while (!uniqueFound)
            {
                randomId = Guid.NewGuid().ToString().Substring(0, 5);

                if (!Directory.Exists(HttpContext.Current.Server.MapPath(Controllers.Constants.PreviewMenusPath + randomId + "/")))
                {
                    uniqueFound = true;
                }
            }

            return randomId;
        }

        public static void LogAppError(string message)
        {
            ErrorSignal.FromCurrentContext().Raise(new Elmah.ApplicationException(message));
        }

        public static void LogAppError(string message, Exception e)
        {
            ErrorSignal.FromCurrentContext().Raise(new Elmah.ApplicationException(message, e));
        }
    }
}