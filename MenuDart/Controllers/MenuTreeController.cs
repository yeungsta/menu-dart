using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MenuDart.Models;

namespace MenuDart.Controllers
{ 
    public class MenuTreeController : Controller
    {
        private MenuDartDBContext db = new MenuDartDBContext();

        //
        // GET: /MenuTree/

        public ViewResult Index()
        {
            return View(db.MenuTree.ToList());
        }

        //
        // GET: /MenuTree/Details/5

        public ViewResult Details(int id)
        {
            MenuNode menunode = db.MenuTree.Find(id);
            return View(menunode);
        }

        //
        // GET: /MenuTree/Create

        public ActionResult Create(int id = 0)
        {
            //Todo: may not need to do a DB find. Just pass in the restaurant name from link
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            ViewBag.Restaurant = menu.Name;
            ViewBag.MenuId = id;

            return View();
        } 

        //
        // POST: /MenuTree/Create

        [HttpPost]
        public ActionResult Create(List<MenuNode> menunodes, int id)
        {
            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                //first deserialize current menu tree
                List<MenuNode> currentMenuTree = Composer.V1.DeserializeMenuTree(menu.MenuTree);

                if ((currentMenuTree != null) && (currentMenuTree.Count > 0))
                {
                    //add all nodes to the current set
                    currentMenuTree.AddRange(menunodes);

                    //serialize back into the menu
                    menu.MenuTree = Composer.V1.SerializeMenuTree(currentMenuTree);
                }
                else
                {
                    //no current nodes, so just set serialized data directly into menu
                    menu.MenuTree = Composer.V1.SerializeMenuTree(menunodes);
                }

                //save menu
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Details", "Menu", new { id = id });
            }

            return View(menunodes);
        }
        
        //
        // GET: /MenuTree/Edit/5

        public ActionResult Edit(int id = 0)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            ViewBag.Restaurant = menu.Name;
            ViewBag.MenuId = id;

            List<MenuNode> menunodes = Composer.V1.DeserializeMenuTree(menu.MenuTree);
            return View(menunodes);
        }

        //
        // POST: /MenuTree/Edit/5

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(List<MenuNode> menunodes, int id)
        {
            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                //set serialized data into menu
                menu.MenuTree = Composer.V1.SerializeMenuTree(menunodes);

                //save menu
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Details", "Menu", new { id = id });
            }

            return View(menunodes);
        }

        //
        // GET: /MenuTree/Delete/5
 
        public ActionResult Delete(int id)
        {
            MenuNode menunode = db.MenuTree.Find(id);
            return View(menunode);
        }

        //
        // POST: /MenuTree/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {            
            MenuNode menunode = db.MenuTree.Find(id);
            db.MenuTree.Remove(menunode);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}