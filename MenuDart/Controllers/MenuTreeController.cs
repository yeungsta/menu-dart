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

        public ActionResult Create(string parent, int id = 0)
        {
            //Todo: may not need to do a DB find. Just pass in the restaurant name from link
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            ViewBag.Restaurant = menu.Name;
            ViewBag.MenuId = id;
            ViewBag.Parent = parent;

            return View();
        } 

        //
        // POST: /MenuTree/Create

        [HttpPost]
        public ActionResult Create(List<MenuNode> newMenuNodes, int id, string parent)
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
                    //if parent is empty, this is the root level
                    if (string.IsNullOrEmpty(parent))
                    {
                        //add all nodes to the root level
                        currentMenuTree.AddRange(newMenuNodes);
                    }
                    else
                    {
                        //find the parent node
                        MenuNode parentNode = currentMenuTree.Find(node => node.Link == parent);

                        if (parentNode != null)
                        {
                            //add all nodes to the parent's branches
                            parentNode.Branches.AddRange(newMenuNodes);
                        }
                    }

                    //serialize back into the menu
                    menu.MenuTree = Composer.V1.SerializeMenuTree(currentMenuTree);
                }
                else
                {
                    //no current nodes, so just set serialized data directly into menu
                    menu.MenuTree = Composer.V1.SerializeMenuTree(newMenuNodes);
                }

                //save menu
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Details", "Menu", new { id = id });
            }

            return View(newMenuNodes);
        }
        
        //
        // GET: /MenuTree/Edit/5

        public ActionResult Edit(string parent, int id = 0)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            ViewBag.Restaurant = menu.Name;
            ViewBag.MenuId = id;
            ViewBag.Parent = parent;

            List<MenuNode> menunodes = Composer.V1.DeserializeMenuTree(menu.MenuTree);
            return View(menunodes);
        }

        //
        // POST: /MenuTree/Edit/5

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(List<MenuNode> editedMenuNodes, int id, string parent)
        {
            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                //if parent is empty, this is the root level
                if (string.IsNullOrEmpty(parent))
                {
                    //serialize data directly into menu's root level
                    menu.MenuTree = Composer.V1.SerializeMenuTree(editedMenuNodes);
                }
                else
                {
                    //deserialize current menu tree
                    List<MenuNode> currentMenuTree = Composer.V1.DeserializeMenuTree(menu.MenuTree);

                    //find the parent node
                    MenuNode parentNode = currentMenuTree.Find(node => node.Link == parent);

                    if (parentNode != null)
                    {
                        //add all nodes to the parent's branches
                        parentNode.Branches.AddRange(editedMenuNodes);
                    }
                }

                //save menu
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Details", "Menu", new { id = id });
            }

            return View(editedMenuNodes);
        }

        //
        // GET: /MenuTree/Delete/5

        public ActionResult Delete(int id, int nodeIdx)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            List<MenuNode> menuNodes = Composer.V1.DeserializeMenuTree(menu.MenuTree);
            ViewBag.NodeIdx = nodeIdx;
            ViewBag.MenuId = id;

            return View(menuNodes[nodeIdx]);
        }

        //
        // POST: /MenuTree/Delete/5

        public ActionResult DeleteConfirmed(int id, int nodeIdx)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            //deserialize current menutree
            List<MenuNode> currentMenuTree = Composer.V1.DeserializeMenuTree(menu.MenuTree);

            //delete node
            currentMenuTree.RemoveAt(nodeIdx);

            //serialize back into the menu
            menu.MenuTree = Composer.V1.SerializeMenuTree(currentMenuTree);

            //save menu
            db.Entry(menu).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Edit", new { id = id });
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}