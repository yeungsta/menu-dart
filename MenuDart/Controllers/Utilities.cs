using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;

namespace MenuDart.Controllers
{
    public static class Utilities
    {
        //Copies source directory + files to a destination directory
        public static void CopyDirTo(string srcPatch, string destPath)
        {
            if (Directory.Exists(destPath))
            {
                if (Directory.Exists(srcPatch))
                {
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

        public static string PrependUrl(string url)
        {
            return "http://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + url;
        }
    }
}