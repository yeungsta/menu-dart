using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MenuDart.Models
{
    public class MenuAndLink
    {
        public string MenuName { get; set; }
        public string MenuLink { get; set; }
    }

    public class SendActivateEmailViewModel
    {
        public string Email { get; set; }
        public int MonthlyBill { get; set; }

        //List of menu names and their links
        public IList<MenuAndLink> MenusJustActivated { get; set; }
        public IList<MenuAndLink> AllActivatedMenus { get; set; }
    }

    public class SendDeactivateEmailViewModel
    {
        public string Email { get; set; }
        public int MonthlyBill { get; set; }

        //List of menu names and their links
        public IList<MenuAndLink> RemainingActiveMenus { get; set; }
        public IList<MenuAndLink> DeactivatedMenus { get; set; }
    }

    public class SendTrialExpiredEmailViewModel
    {
        public string Email { get; set; }

        //List of menu names and their links
        public IList<MenuAndLink> DeactivatedMenus { get; set; }
    }
}