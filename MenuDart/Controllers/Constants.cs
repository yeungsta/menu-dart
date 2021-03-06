﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MenuDart.Controllers
{
    public class Constants
    {
        //current deployment version/timestamp
        public static readonly string DeploymentVersion = "0.3";

        public const string Break = "<br>";
        public const string NewLine = "\n";
        public const string NewLine2 = "\r\n";
        public const string MenusDir = "/Content/menus/";
        public const string MenusPath = "~" + MenusDir;
        public const string PreviewMenusDir = "/content/previews/";
        public const string PreviewMenusStagingDir = "/staging/content/previews/";
        public const string PreviewMenusPath = "~" + PreviewMenusDir;
        public const string SampleLogoPath = "~/Content/menus/shared/images/logo.png";
        public const int MaxLocations = 8;
        public const string RootLevel = "0";
        public const string RootTitle = "Main Level";
        public const string UrlPrefix = @"http://";
        public const string UrlSecurePrefix = @"https://";
        public const string TwitterPrefix = @"https://www.twitter.com/";
        public const string GoogleMapPrefix = @"http://maps.google.com/maps?q=";
        public const string GoogleMapImgPrefix = @"http://maps.googleapis.com/maps/api/staticmap?size=275x275&maptype=roadmap\&markers=size:mid%7Ccolor:red%7C";
        public const string GoogleMapImgSuffix = @"&sensor=false&zoom=14";
        public const string LogoFileName = "logo.png";
        public const string OutputFile = "index.html";
        public const string SupportEmailAddress = "support@menudart.com";
        public const string SessionPreviewKey = "PreviewId";
        //Stripe
        public const string StripeSubscriptionPlan = "1";
        //subscription actions
        public const int CostPerMenu = 7;
        public const string ActivateOne = "ActivateOne";
        public const string DeactivateOne = "DeactivateOne";
        public const string ActivateAll = "ActivateAll";
        public const string DeactivateAll = "DeactivateAll";
        //email
        public const string ReplyEmail = "no-reply@menudart.com";
        public const string SupportEmail = "support@menudart.com";
        //misc
        public const int TrialPeriodDays = 30;
        public const int TrialExpWarningDays = 25; //warn 5 days ahead of expiration
        //Amazon S3
        public const string AmazonS3BucketName = "biz.menudart.com";
        public const string AmazonS3HtmlObjectType = "text/html";
        //Stripe
        //test key
        //public const string StripePublishableKey = "pk_0AQ27F6Upi3VpRUBHAPDlR9o4GnM6";
        //live key
        public const string StripePublishableKey = "pk_0AQ2fsho4juqMLqP9wNX33seuq1eI";
    }
}