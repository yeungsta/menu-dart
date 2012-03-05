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
        private const string RootLevel = "0";

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
            //we need a parent in order to know where to create these menu nodes
            if (!string.IsNullOrEmpty(parent))
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
                        if (parent == RootLevel)
                        {
                            //add all nodes to the root level
                            currentMenuTree.AddRange(newMenuNodes);
                        }
                        else
                        {
                            //find the parent node
                            MenuNode parentNode = Composer.V1.FindMenuNode(currentMenuTree, parent);

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

                    return RedirectToAction("Edit", new { id = id, parent = parent, idx = -1 });
                }

                return View(newMenuNodes);
            }

            return HttpNotFound();
        }

        public ActionResult CreateItem(string parent, int id = 0)
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
        // POST: /MenuTree/CreateItem

        [HttpPost]
        public ActionResult CreateItem(List<MenuLeaf> newMenuLeaves, int id, string parent)
        {
            //we need a parent in order to know where to create these menu nodes
            if (!string.IsNullOrEmpty(parent))
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
                        if (parent == RootLevel)
                        {
                            //add all nodes to the root level
                            currentMenuTree.AddRange(newMenuLeaves);
                        }
                        else
                        {
                            //find the parent node
                            MenuNode parentNode = Composer.V1.FindMenuNode(currentMenuTree, parent);

                            if (parentNode != null)
                            {
                                //add all nodes to the parent's branches
                                parentNode.Branches.AddRange(newMenuLeaves);
                            }
                        }

                        //serialize back into the menu
                        menu.MenuTree = Composer.V1.SerializeMenuTree(currentMenuTree);
                    }
                    else
                    {
                        //no current nodes, so just set serialized data directly into menu
                        List<MenuNode> newMenuNodes = new List<MenuNode>();
                        newMenuNodes.Add(newMenuLeaves[0] as MenuNode);

                        menu.MenuTree = Composer.V1.SerializeMenuTree(newMenuNodes);
                    }

                    //save menu
                    db.Entry(menu).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Edit", new { id = id, link = parent });
                }

                return View(newMenuLeaves);
            }

            return HttpNotFound();
        }

        //
        // GET: /MenuTree/Edit/5

        public ActionResult Edit(string parent, int idx, int id = 0)
        {
            //we need a parent ID in order to know where to edit
            if (!string.IsNullOrEmpty(parent))
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                ViewBag.Restaurant = menu.Name;
                ViewBag.MenuId = id;
                ViewBag.Parent = parent;

                List<MenuNode> currentMenuTree = Composer.V1.DeserializeMenuTree(menu.MenuTree);

                if (parent == RootLevel)
                {
                    //if no specific index is specified, just return the whole level to edit
                    if (idx == -1)
                    {
                        return View(currentMenuTree);
                    }
                    else
                    {
                        ViewBag.Parent = currentMenuTree[idx].Link;
                        return View(currentMenuTree[idx].Branches);
                    }
                }
                else
                {
                    //find the parent node based on link
                    MenuNode parentNode = Composer.V1.FindMenuNode(currentMenuTree, parent);

                    //if no specific index is specified, just return the whole level to edit
                    if (idx == -1)
                    {
                        return View(parentNode.Branches);
                    }
                    else
                    {
                        ViewBag.Parent = parentNode.Branches[idx].Link;
                        //return the branches of the parent node
                        return View(parentNode.Branches[idx].Branches);
                    }
                }
            }

            return HttpNotFound();
        }

        //
        // POST: /MenuTree/Edit/5

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(List<MenuNode> editedMenuNodes, int id, string parent)
        {
            //we need a parent ID in order to know where to edit
            if (!string.IsNullOrEmpty(parent))
            {
                if (ModelState.IsValid)
                {
                    Menu menu = db.Menus.Find(id);

                    if (menu == null)
                    {
                        return HttpNotFound();
                    }

                    //check if this is the root level
                    if (parent == RootLevel)
                    {
                        //serialize data directly into menu's root level
                        menu.MenuTree = Composer.V1.SerializeMenuTree(editedMenuNodes);
                    }
                    else
                    {
                        //deserialize current menu tree
                        List<MenuNode> currentMenuTree = Composer.V1.DeserializeMenuTree(menu.MenuTree);

                        //find the parent node based on link
                        MenuNode parentNode = Composer.V1.FindMenuNode(currentMenuTree, parent);

                        if (parentNode != null)
                        {
                            //add all nodes to the parent node's branches
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

            return HttpNotFound();
        }

        //
        // GET: /MenuTree/EditItem/5

        public ActionResult EditItem(string link, int id = 0)
        {
            //we need a link ID in order to know what to item edit
            if (!string.IsNullOrEmpty(link))
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                ViewBag.Restaurant = menu.Name;
                ViewBag.MenuId = id;
                ViewBag.Link = link;

                List<MenuNode> currentMenuTree = Composer.V1.DeserializeMenuTree(menu.MenuTree);

                //find the right node
                MenuNode currentNode = currentMenuTree.Find(node => node.Link == link);

                //view as leaf
                MenuLeaf currentLeaf = currentNode as MenuLeaf;

                //return the node as a leaf
                return View(currentLeaf);
            }

            return HttpNotFound();
        }

        //
        // POST: /MenuTree/EditItem/5

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditItem(List<MenuNode> editedMenuNodes, int id, string link)
        {
            //we need a link ID in order to know what to edit
            if (!string.IsNullOrEmpty(link))
            {
                if (ModelState.IsValid)
                {
                    Menu menu = db.Menus.Find(id);

                    if (menu == null)
                    {
                        return HttpNotFound();
                    }

                    //check if this is the root level
                    if (link == RootLevel)
                    {
                        //serialize data directly into menu's root level
                        menu.MenuTree = Composer.V1.SerializeMenuTree(editedMenuNodes);
                    }
                    else
                    {
                        //deserialize current menu tree
                        List<MenuNode> currentMenuTree = Composer.V1.DeserializeMenuTree(menu.MenuTree);

                        //find the right node
                        MenuNode currentNode = currentMenuTree.Find(node => node.Link == link);

                        if (currentNode != null)
                        {
                            //add all nodes to the right node's branches
                            currentNode.Branches.AddRange(editedMenuNodes);
                        }
                    }

                    //save menu
                    db.Entry(menu).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Details", "Menu", new { id = id });
                }

                return View(editedMenuNodes);
            }

            return HttpNotFound();
        }

        //
        // GET: /MenuTree/Delete/5

        public ActionResult Delete(string parent, int idx, int id)
        {
            //we need a parent ID in order to know where to delete
            if (!string.IsNullOrEmpty(parent))
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                ViewBag.MenuId = id;
                ViewBag.Parent = parent;
                ViewBag.Index = idx;

                List<MenuNode> currentMenuTree = Composer.V1.DeserializeMenuTree(menu.MenuTree);

                //check if this is the root level
                if (parent == RootLevel)
                {
                    return View(currentMenuTree[idx]);
                }
                else
                {
                    //find the parent node based on link, then index
                    return View(Composer.V1.FindMenuNode(currentMenuTree, parent).Branches[idx]);
                }
            }

            return HttpNotFound();  
        }

        //
        // POST: /MenuTree/Delete/5

        public ActionResult DeleteConfirmed(int id, string parent, int idx)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            //deserialize current menutree
            List<MenuNode> currentMenuTree = Composer.V1.DeserializeMenuTree(menu.MenuTree);

            //check if this is the root level
            if (parent == RootLevel)
            {
                //remove at the index
                currentMenuTree.RemoveAt(idx);
            }
            else
            {
                //remove at the index
                Composer.V1.FindMenuNode(currentMenuTree, parent).Branches.RemoveAt(idx);
            }

            //serialize back into the menu
            menu.MenuTree = Composer.V1.SerializeMenuTree(currentMenuTree);

            //save menu
            db.Entry(menu).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Edit", new { id = id, parent = parent, idx = -1 });
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}