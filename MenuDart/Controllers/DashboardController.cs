using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MenuDart.Models;

namespace MenuDart.Controllers
{
    public class DashboardController : Controller
    {
        private MenuDartDBContext db = new MenuDartDBContext();

        //
        // GET: /Dashboard/user@email.com

        public ActionResult Index(string email)
        {
            IOrderedQueryable<Menu> menus = from menu in db.Menus
                                            where menu.Owner == email
                                            orderby menu.Name ascending
                                            select menu;

            if (menus == null)
            {
                return HttpNotFound();
            }

            ViewBag.Email = email;
            ViewBag.UrlPath = Utilities.GetUrlPath();

            return View(menus.ToList());
        }

    }
}
