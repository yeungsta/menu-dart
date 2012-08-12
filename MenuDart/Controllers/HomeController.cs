using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using MenuDart.Models;

namespace MenuDart.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            //Utilities.LogAppError("Test exception.");

            try
            {
                //new MailController().SendPasswordResetEmail(User.Identity.Name, "SampleResetLink").Deliver();
                //new MailController().SendSignUpEmail(User.Identity.Name).Deliver();

                IList<MenuAndLink> menus = new List<MenuAndLink>();
                MenuAndLink item = new MenuAndLink();
                item.MenuName = "Bob's Cafe";
                item.MenuLink = "http://www.BobCafe.com";
                menus.Add(item);
                //MenuAndLink item2 = new MenuAndLink();
                //item2.MenuName = "Depot";
                //item2.MenuLink = "http://www.depot.com";
                //menus.Add(item2);
                IList<MenuAndLink> menus2 = new List<MenuAndLink>();
                new MailController().SendDeactivateEmail(User.Identity.Name, 0, menus2, menus).Deliver();
            }
            catch
            {
            }

            return View();
        }
    }
}
