using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Security.Principal;
using System.Configuration;
using MenuDart.Models;
using Elmah;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

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

        public static IOrderedQueryable<Menu> GetAllMenus(string owner, MenuDartDBContext db)
        {
            IOrderedQueryable<Menu> allMenus = from allMenu in db.Menus
                                               where allMenu.Owner == owner
                                               orderby allMenu.Name ascending
                                               select allMenu;

            if (allMenus == null)
            {
                Utilities.LogAppError("Could not retrieve all menus for owner: " + owner);
            }

            return allMenus;
        }

/*
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
*/

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

        /* The following methods are for local filesystem */

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
#if UseAmazonS3
            return "http://" + Controllers.Constants.AmazonS3BucketName + "/" + menuDartUrl;          
#else
            return "http://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + Constants.MenusDir + menuDartUrl + "/" + Constants.OutputFile;
#endif
        }

        //constructs full URL of preview menu site
        public static string GetFullUrlPreview(string menuDartUrl)
        {
            return "http://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + Constants.PreviewMenusDir + menuDartUrl + "/" + Constants.OutputFile;
        }

        //constructs full URL of menu site's logo
        public static string GetMenuLogoUrl(string menuDartUrl)
        {
            return "http://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + Constants.MenusDir + menuDartUrl + "/" + Constants.LogoFileName;
        }

        //constructs full URL of menu site's temp logo
        public static string GetMenuLogoTmpUrl(string menuDartUrl)
        {
            return "http://" + HttpContext.Current.Request.Url.Host + ":" + HttpContext.Current.Request.Url.Port + Constants.MenusDir + menuDartUrl + "/" + Constants.LogoTmpFileName;
        }

        //delete entire directory and contents
        public static void RemoveDirectory(string menuDartUrl)
        {
#if UseAmazonS3
            RemoveAllObjectsWithPrefixS3(menuDartUrl);
#else
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
#endif
        }

        //delete menu file but not logo
        public static void DeactivateDirectory(string menuDartUrl)
        {
#if UseAmazonS3
            RemoveObjectFromS3(menuDartUrl + "/index.html");
#else
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
#endif
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

        /* The following methods are for Amazon S3 filesystem */

        //writes plaintext data to S3 cloud
        public static void WritePlainTextObjectToS3(string data, string filePath)
        {
            AmazonS3 client;

            if (CheckS3Credentials())
            {
                NameValueCollection appConfig =
                    ConfigurationManager.AppSettings;

                string accessKeyID = appConfig["AWSAccessKey"];
                string secretAccessKeyID = appConfig["AWSSecretKey"];

                using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(
                    accessKeyID, secretAccessKeyID, RegionEndpoint.USWest1))
                {
                    try
                    {
                        PutObjectRequest request = new PutObjectRequest();
                        request.WithContentBody(data)
                            .WithBucketName(Constants.AmazonS3BucketName)
                            .WithKey(filePath)
                            .WithContentType(Controllers.Constants.AmazonS3HtmlObjectType)
                            .WithCannedACL(S3CannedACL.PublicRead);

                        S3Response response3 = client.PutObject(request);
                        response3.Dispose();
                    }
                    catch (AmazonS3Exception amazonS3Exception)
                    {
                        if (amazonS3Exception.ErrorCode != null &&
                            (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                            ||
                            amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                        {
                            Console.WriteLine("Check the provided AWS Credentials.");
                            Console.WriteLine(
                                "For service sign up go to http://aws.amazon.com/s3");
                        }
                        else
                        {
                            Console.WriteLine(
                                "Error occurred. Message:'{0}' when writing an object"
                                , amazonS3Exception.Message);
                        }
                    }
                }
            }
        }

        //writes data stream to S3 cloud
        public static void WriteStreamToS3(Stream data, string filePath)
        {
            AmazonS3 client;

            if (CheckS3Credentials())
            {
                NameValueCollection appConfig =
                    ConfigurationManager.AppSettings;

                string accessKeyID = appConfig["AWSAccessKey"];
                string secretAccessKeyID = appConfig["AWSSecretKey"];

                using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(
                    accessKeyID, secretAccessKeyID, RegionEndpoint.USWest1))
                {
                    try
                    {
                        PutObjectRequest request = new PutObjectRequest();
                        request.WithBucketName(Constants.AmazonS3BucketName)
                            .WithKey(filePath)
                            .WithCannedACL(S3CannedACL.PublicRead)
                            .WithInputStream(data);

                        S3Response response3 = client.PutObject(request);
                        response3.Dispose();
                    }
                    catch (AmazonS3Exception amazonS3Exception)
                    {
                        if (amazonS3Exception.ErrorCode != null &&
                            (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                            ||
                            amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                        {
                            Console.WriteLine("Check the provided AWS Credentials.");
                            Console.WriteLine(
                                "For service sign up go to http://aws.amazon.com/s3");
                        }
                        else
                        {
                            Console.WriteLine(
                                "Error occurred. Message:'{0}' when writing an object"
                                , amazonS3Exception.Message);
                        }
                    }
                }
            }
        }

        //removes a file in S3 cloud
        public static void RemoveObjectFromS3(string filePath)
        {
            AmazonS3 client;

            if (CheckS3Credentials())
            {
                NameValueCollection appConfig =
                    ConfigurationManager.AppSettings;

                string accessKeyID = appConfig["AWSAccessKey"];
                string secretAccessKeyID = appConfig["AWSSecretKey"];

                using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(
                    accessKeyID, secretAccessKeyID, RegionEndpoint.USWest1))
                {
                    try
                    {
                        DeleteObjectRequest request = new DeleteObjectRequest();
                        request.WithBucketName(Constants.AmazonS3BucketName)
                            .WithKey(filePath);

                        S3Response response = client.DeleteObject(request);
                        response.Dispose();
                    }
                    catch (AmazonS3Exception amazonS3Exception)
                    {
                        if (amazonS3Exception.ErrorCode != null &&
                            (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                            ||
                            amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                        {
                            Console.WriteLine("Check the provided AWS Credentials.");
                            Console.WriteLine(
                                "For service sign up go to http://aws.amazon.com/s3");
                        }
                        else
                        {
                            Console.WriteLine(
                                "Error occurred. Message:'{0}' when deleting an object"
                                , amazonS3Exception.Message);
                        }
                    }
                }
            }
        }

        //removes all files with same prefix in S3 cloud
        public static void RemoveAllObjectsWithPrefixS3(string filePath)
        {
            AmazonS3 client;

            if (CheckS3Credentials())
            {
                NameValueCollection appConfig =
                    ConfigurationManager.AppSettings;

                string accessKeyID = appConfig["AWSAccessKey"];
                string secretAccessKeyID = appConfig["AWSSecretKey"];

                using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(
                    accessKeyID, secretAccessKeyID, RegionEndpoint.USWest1))
                {
                    try
                    {
                        //first get all objects with the same prefix (in the same "folder")
                        ListObjectsRequest listRequest = new ListObjectsRequest();
                        listRequest.WithBucketName(Constants.AmazonS3BucketName)
                            .WithPrefix(filePath);

                        ListObjectsResponse listResponse = client.ListObjects(listRequest);

                        //delete all objects
                        DeleteObjectsRequest deleteRequest = new DeleteObjectsRequest();
                        deleteRequest.WithBucketName(Constants.AmazonS3BucketName);

                        foreach (S3Object obj in listResponse.S3Objects)
                        {
                            deleteRequest.AddKey(obj.Key);
                        }

                        //if there are any files to delete
                        if (deleteRequest.Keys.Count > 0)
                        {
                            S3Response response = client.DeleteObjects(deleteRequest);
                            response.Dispose();
                        } 
                    }
                    catch (AmazonS3Exception amazonS3Exception)
                    {
                        if (amazonS3Exception.ErrorCode != null &&
                            (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                            ||
                            amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                        {
                            Console.WriteLine("Check the provided AWS Credentials.");
                            Console.WriteLine(
                                "For service sign up go to http://aws.amazon.com/s3");
                        }
                        else
                        {
                            Console.WriteLine(
                                "Error occurred. Message:'{0}' when deleting an object"
                                , amazonS3Exception.Message);
                        }
                    }
                }
            }
        }

        //gets file from S3
        public static void GetObjectFromS3(string filePathS3, string destFilePath)
        {
            AmazonS3 client;

            if (CheckS3Credentials())
            {
                NameValueCollection appConfig =
                    ConfigurationManager.AppSettings;

                string accessKeyID = appConfig["AWSAccessKey"];
                string secretAccessKeyID = appConfig["AWSSecretKey"];

                using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(
                    accessKeyID, secretAccessKeyID, RegionEndpoint.USWest1))
                {
                    try
                    {
                        GetObjectRequest request = new GetObjectRequest();
                        request.WithBucketName(Constants.AmazonS3BucketName)
                            .WithKey(filePathS3);

                        using (GetObjectResponse response = client.GetObject(request))
                        {
                            response.WriteResponseStreamToFile(destFilePath);
                        }
                    }
                    catch (AmazonS3Exception amazonS3Exception)
                    {
                        if (amazonS3Exception.ErrorCode != null &&
                            (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                            ||
                            amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                        {
                            Console.WriteLine("Check the provided AWS Credentials.");
                            Console.WriteLine(
                                "For service sign up go to http://aws.amazon.com/s3");
                        }
                        else
                        {
                            Console.WriteLine(
                                "Error occurred. Message:'{0}' when writing an object"
                                , amazonS3Exception.Message);
                        }
                    }
                }
            }
        }

        //copies source file with a new filename, then deletes original
        public static void RenameObjectInS3(string sourceFilePath, string destFilePath)
        {
            AmazonS3 client;

            if (CheckS3Credentials())
            {
                NameValueCollection appConfig =
                    ConfigurationManager.AppSettings;

                string accessKeyID = appConfig["AWSAccessKey"];
                string secretAccessKeyID = appConfig["AWSSecretKey"];

                using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(
                    accessKeyID, secretAccessKeyID, RegionEndpoint.USWest1))
                {
                    try
                    {
                        //copy to new file
                        CopyObjectRequest copyRequest = new CopyObjectRequest()
                              .WithSourceBucket(Constants.AmazonS3BucketName)
                              .WithSourceKey(sourceFilePath)
                              .WithDestinationBucket(Constants.AmazonS3BucketName)
                              .WithDestinationKey(destFilePath)
                              .WithCannedACL(S3CannedACL.PublicRead);

                        S3Response response = client.CopyObject(copyRequest);
                        response.Dispose();

                        //delete the original
                        DeleteObjectRequest deleteRequest = new DeleteObjectRequest()
                               .WithBucketName(Constants.AmazonS3BucketName)
                               .WithKey(sourceFilePath);
                        response = client.DeleteObject(deleteRequest);
                        response.Dispose();
                    }
                    catch (AmazonS3Exception amazonS3Exception)
                    {
                        if (amazonS3Exception.ErrorCode != null &&
                            (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                            ||
                            amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                        {
                            Console.WriteLine("Check the provided AWS Credentials.");
                            Console.WriteLine(
                                "For service sign up go to http://aws.amazon.com/s3");
                        }
                        else
                        {
                            Console.WriteLine(
                                "Error occurred. Message:'{0}' when writing an object"
                                , amazonS3Exception.Message);
                        }
                    }
                }
            }
        }

        //checks if file exists S3 cloud
        public static bool IsObjectExistS3(string filePath)
        {
            AmazonS3 client;

            if (CheckS3Credentials())
            {
                NameValueCollection appConfig =
                    ConfigurationManager.AppSettings;

                string accessKeyID = appConfig["AWSAccessKey"];
                string secretAccessKeyID = appConfig["AWSSecretKey"];

                using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(
                    accessKeyID, secretAccessKeyID, RegionEndpoint.USWest1))
                {
                    try
                    {
                        S3Response response = client.GetObjectMetadata(new GetObjectMetadataRequest()
                           .WithBucketName(Constants.AmazonS3BucketName)
                           .WithKey(filePath));

                        return true;
                    }
                    catch (Amazon.S3.AmazonS3Exception ex)
                    {
                        if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)

                            return false;
                    }
                }
            }

            return false;
        }

        static bool CheckS3Credentials()
        {
            NameValueCollection appConfig = ConfigurationManager.AppSettings;

            if (string.IsNullOrEmpty(appConfig["AWSAccessKey"]))
            {
                Console.WriteLine(
                    "AWSAccessKey was not set in the App.config file.");
                return false;
            }
            if (string.IsNullOrEmpty(appConfig["AWSSecretKey"]))
            {
                Console.WriteLine(
                    "AWSSecretKey was not set in the App.config file.");
                return false;
            }
            if (string.IsNullOrEmpty(Constants.AmazonS3BucketName))
            {
                Console.WriteLine("The variable bucketName is not set.");
                return false;
            }

            return true;
        }

        //ELMAH logging routines
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