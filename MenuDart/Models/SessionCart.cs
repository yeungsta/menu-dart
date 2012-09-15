using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MenuDart.Composer;

namespace MenuDart.Models
{
    public partial class SessionCart
    {
        MenuDartDBContext storeDB = new MenuDartDBContext();
        string SessionCartId { get; set; }

        public const string CartSessionKey = "SessionId";

        public static SessionCart GetCart(HttpContextBase context)
        {
            var cart = new SessionCart();
            cart.SessionCartId = cart.GetCartId(context);

            return cart;
        }

        // Helper method to simplify session cart calls
        private static SessionCart GetCart(Controller controller)
        {
            return GetCart(controller.HttpContext);
        }

        // We're using HttpContextBase to allow access to cookies.
        private string GetCartId(HttpContextBase context)
        {
            if (context.Session[CartSessionKey] == null)
            {
                if (!string.IsNullOrWhiteSpace(context.User.Identity.Name))
                {
                    context.Session[CartSessionKey] =
                        context.User.Identity.Name;
                }
                else
                {
                    // Generate a new random GUID using System.Guid class
                    Guid tempCartId = Guid.NewGuid();
                    // Send tempCartId back to client as a cookie
                    context.Session[CartSessionKey] = tempCartId.ToString();
                }
            }
            return context.Session[CartSessionKey].ToString();
        }

        public void AddMenu(int menuId)
        {
            // Create a new temp menu
            TempMenu tempMenu = new TempMenu
            {
                MenuId = menuId,
                SessionId = SessionCartId,
                DateCreated = DateTime.Now
            };

            storeDB.TempMenus.Add(tempMenu);

            // Save changes
            storeDB.SaveChanges();
        }

        // When a user has logged in, migrate their session cart's menu to
        // be associated with their username
        public void MigrateMenu(string userName)
        {
            var tempMenus = storeDB.TempMenus.Where(
                c => c.SessionId == SessionCartId);

            //in case there are more than one
            foreach (TempMenu tempMenu in tempMenus)
            {
                //todo: don't need to keep temp menu?
                //item.SessionId = userName;

                //set owner-less menu in DB to owner
                MenuDartDBContext db = new MenuDartDBContext();
                Menu menu = db.Menus.Find(tempMenu.MenuId);
                menu.Owner = userName;
                db.SaveChanges();

                //remove temp menu
                storeDB.TempMenus.Remove(tempMenu);

                //Reserve an empty, permanent location/URL
                V1 composer = new V1(menu);
                composer.CreateMenuDir();
            }

            if (tempMenus.Count() > 0)
            {
                storeDB.SaveChanges();
            }
        }
    }
}