using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MenuDart.Models
{
    public class MenuBuilderViewModel
    {
        public Menu CurrentMenu { get; set; }
    }

    public class MenuBuilderThemeViewModel
    {
        public List<string> Themes { get; set; }
    }
}