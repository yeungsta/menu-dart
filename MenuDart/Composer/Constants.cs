using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MenuDart.Composer
{
    public class Constants
    {
        public const string DocType = "<!DOCTYPE html>";
        public const string Jquery = "http://code.jquery.com/jquery-1.6.4.min.js";
        public const string JqueryMobile = "http://code.jquery.com/mobile/1.0/jquery.mobile-1.0.min.js";
        public const string JqueryMobileCss = "http://code.jquery.com/mobile/1.0/jquery.mobile-1.0.min.css";
        public const string LogoPath = "index_files/logo.png";
        public const string MenuDartUrl = "http://www.menudart.com";
        public const string RegularSiteRedirect = "/?menudart=regular";
        public const string OutputFile = "index.html";
        public const string BlankTarget = "_blank";
        public const string Break = "<br>";
        public const string Paragraph = "<p>";
        public const string NewLine = "\n";

        //jQuery Mobile
        public const string DataRole = "data-role";
        public const string PageRole = "page";
        public const string HeaderRole = "header";
        public const string ContentRole = "content";
        public const string NavbarRole = "navbar";
        public const string ButtonRole = "button";
        public const string DataInset = "data-inset";
        public const string DataIcon = "data-icon";
        public const string DataId = "data-id";
        public const string DataTransition = "data-transition";
        public const string DataRel = "data-rel";
        public const string DataPosition = "data-position";
        public const string DataTheme = "data-theme";
        public const string PositionFixed = "fixed";
        public const string SlideDown = "slidedown";
        public const string GoBack = "back";
        public const string CountClass = "ui-li-count";

        //jQuery icons
        public const string OrigSiteIcon = "monitor";
        public const string CallIcon = "phone";
        public const string MenuIcon = "grid";
        public const string AboutIcon = "star";
        public const string ContactIcon = "home";
        public const string BackIcon = "arrow-l";
        public const string EmailIcon = "email";
        public const string FacebookIcon = "facebook";
        public const string TwitterIcon = "twitter";
        public const string YelpIcon = "yelp";

        //default values
        public const string DefaultAddress = "(Address Here)";
        public const string DefaultHoursHtml = "(Example)<br>Monday - Friday:<br>10am - 9pm<p>Saturday - Sunday:<br>11am - 8pm<p>Happy Hour<br>Monday - Friday:<br>5pm - 7pm";
        public const string DefaultHours = "(Example)\nMonday - Friday:\n10am - 9pm\n\nSaturday - Sunday:\n11am - 8pm\n\nHappy Hour\nMonday - Friday:\n5pm - 7pm";
        public const string DefaultWebsite = "http://";
        public const string DefaultEmail = "your@email.here";
        public const string DefaultPhone = "111-1111";
        public const string DefaultAboutTitleFormat = "Welcome to\n{0}!";
        public const string DefaultAboutTextFormat = "A description and/or story about {0} here...";
    }
}