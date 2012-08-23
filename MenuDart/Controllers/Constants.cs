using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MenuDart.Controllers
{
    public class Constants
    {
        public const string Break = "<br>";
        public const string NewLine = "\n";
        public const string NewLine2 = "\r\n";
        public const string MenusDir = "/Content/menus/";
        public const string MenusPath = "~" + MenusDir;
        public const string PreviewMenusDir = "/Content/previews/";
        public const string PreviewMenusPath = "~" + PreviewMenusDir;
        public const string TemplatesPath = "~/Content/templates/themes/";
        public const string BaseTemplatesPath = "~/Content/templates/base/index_files/";
        public const int MaxLocations = 8;
        public const string RootLevel = "0";
        public const string RootTitle = "Main Level";
        public const string GoogleMapPrefix = @"http://maps.google.com/maps?q=";
        public const string GoogleMapImgPrefix = @"http://maps.googleapis.com/maps/api/staticmap?size=275x275&maptype=roadmap\&markers=size:mid%7Ccolor:red%7C";
        public const string GoogleMapImgSuffix = @"&sensor=false&zoom=14";
        public const string IndexFilesDir = "index_files";
        public const string LogoFileName = "logo.png";
        public const string LogoTmpFileName = "logo_tmp.png";
        public const string OutputFile = "index.html";
        public const string SupportEmailAddress = "support@menudart.com";
        public const string SessionPreviewKey = "PreviewId";
        //PayPal
        public const string PayPalApiUsername = "james_1339990989_biz_api1.menudart.com";
        public const string PayPalApiPassword = "1339991013";
        public const string PayPalApiSignature = "AxyiOd4RoQv.VfrED.7XMPHKc6NHAkJdAh6GWE6tIzsfagOaryGPvMPJ";
        public const string PayPalApiVersion = "89.0";
        public const double PayPalApiSubscriptionAmount = 7.00;
        public const string PayPalApiSubscriptionDescription = "MenuDart Subscription";
        public const int PayPalApiMaxFailedPayments = 1;
        public const string PayPalExpressCheckoutUrlSandbox = "https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_express-checkout&token=";
        //subscription actions
        public const int CostPerMenu = 7;
        public const string ActivateOne = "ActivateOne";
        public const string DeactivateOne = "DeactivateOne";
        public const string SubscribeAll = "SubscribeAll";
        //email
        public const string ReplyEmail = "no-reply@menudart.com";
        public const string SupportEmail = "support@menudart.com";
        //misc
        public const int TrialPeriodDays = 31; //30 days + 1 grace day
        public const int TrialExpWarningDays = 25; //warn 5 days ahead of expiration
    }
}