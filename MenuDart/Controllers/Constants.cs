﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MenuDart.Controllers
{
    public class Constants
    {
        public const string DefaultAboutTitleFormat = "Welcome to {0}!";
        public const string DefaultAboutTextFormat = "A description and/or story about {0} here...";
        public const string Break = "<br>";
        public const string NewLine = "\n";
        public const string MenusPath = "~/Content/menus/";
        public const string TemplatesPath = "~/Content/templates/themes/";
        public const string IndexFilesDir = "index_files";
        public const int MaxLocations = 8;
        public const string RootLevel = "0";
        public const string RootTitle = "Main Level";
        public const string GoogleMapPrefix = @"http://maps.google.com/maps?q=";
        public const string GoogleMapImgPrefix = @"http://maps.googleapis.com/maps/api/staticmap?size=275x275&maptype=roadmap\&markers=size:mid%7Ccolor:red%7C";
        public const string GoogleMapImgSuffix = @"&sensor=false&zoom=14";
    }
}