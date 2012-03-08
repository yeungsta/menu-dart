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

        public ActionResult Details(string parent, int idx, int id = 0)
        {
            //we need a parent ID in order to know what to display
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
                    ViewBag.UpLevel = RootLevel;

                    //if no specific index is specified, just display the whole level
                    if (idx == -1)
                    {
                        return View(currentMenuTree);
                    }
                    else
                    {
                        ViewBag.Parent = currentMenuTree[idx].Link;
                        ViewBag.Text = currentMenuTree[idx].Text;
                        return View(currentMenuTree[idx].Branches);
                    }
                }
                else
                {
                    //find the parent node
                    MenuNode parentNode = Composer.V1.FindMenuNode(currentMenuTree, parent);

                    //if no specific index is specified, just display the whole level
                    if (idx == -1)
                    {
                        string[] levels = parentNode.Link.Split('-');
                        if (levels.Count() > 2)
                        {
                            string upLevel = levels[0];

                            //construct one level up
                            for (int x = 1; x < levels.Count() - 1; x++)
                            {
                                upLevel += "-" + levels[x];
                            }

                            ViewBag.UpLevel = upLevel;
                        }
                        else
                        {
                            ViewBag.UpLevel = RootLevel;
                        }

                        ViewBag.Text = parentNode.Text;
                        return View(parentNode.Branches);
                    }
                    else
                    {
                        string[] levels = parentNode.Branches[idx].Link.Split('-');
                        if (levels.Count() > 2)
                        {
                            string upLevel = levels[0];

                            //construct one level up
                            for (int x = 1; x < levels.Count() - 1; x++)
                            {
                                upLevel += "-" + levels[x];
                            }

                            ViewBag.UpLevel = upLevel;
                        }
                        else
                        {
                            ViewBag.UpLevel = RootLevel;
                        }

                        ViewBag.Parent = parentNode.Branches[idx].Link;
                        ViewBag.Text = parentNode.Branches[idx].Text;
                        //return the branches of the parent node
                        return View(parentNode.Branches[idx].Branches);
                    }
                }
            }

            return HttpNotFound();
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
        public ActionResult Create(MenuNode newMenuNode, int id, string parent)
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
                        //if this is the root level
                        if (parent == RootLevel)
                        {
                            try
                            {
                                newMenuNode.Link = IncrementLink(currentMenuTree.Last(node => !(node is MenuLeaf)).Link);
                            }
                            catch //there is no other menuNode
                            {
                                newMenuNode.Link = "1-1";
                            }

                            //add node to the root level
                            currentMenuTree.Add(newMenuNode);
                        }
                        else
                        {
                            //find the parent node
                            MenuNode parentNode = Composer.V1.FindMenuNode(currentMenuTree, parent);

                            if (parentNode != null)
                            {
                                try
                                {
                                    newMenuNode.Link = IncrementLink(parentNode.Branches.Last(node => !(node is MenuLeaf)).Link);
                                }
                                catch //there is no other menuNode
                                {
                                    //create the first category link of this level
                                    newMenuNode.Link = parentNode.Link + "-1";
                                }

                                //add node to the parent's branches
                                parentNode.Branches.Add(newMenuNode);
                            }
                        }

                        //serialize back into the menu
                        menu.MenuTree = Composer.V1.SerializeMenuTree(currentMenuTree);
                    }
                    else
                    {
                        //this is the first and only node of the tree
                        newMenuNode.Link = "1-1";

                        List<MenuNode> newMenuNodeList = new List<MenuNode>();
                        newMenuNodeList.Add(newMenuNode);
                        //no current nodes, so just set serialized data directly into menu
                        menu.MenuTree = Composer.V1.SerializeMenuTree(newMenuNodeList);
                    }

                    //save menu
                    db.Entry(menu).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Details", new { id = id, parent = parent, idx = -1 });
                }

                return View(newMenuNode);
            }

            return HttpNotFound();
        }

        private string IncrementLink(string link)
        {
            string[] levels = link.Split('-');
            
            int lastNum = int.Parse(levels.Last());
            lastNum++;
            levels[levels.Count() - 1] = lastNum.ToString();

            string newLink = string.Empty;

            //construct the link
            newLink = levels[0];

            for (int x = 1; x < levels.Count(); x++)
            {
                newLink += "-" + levels[x];
            }

            return newLink;
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
        public ActionResult CreateItem(MenuLeaf newMenuLeaf, int id, string parent)
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
                            //add node to the root level
                            currentMenuTree.Add(newMenuLeaf);
                        }
                        else
                        {
                            //find the parent node
                            MenuNode parentNode = Composer.V1.FindMenuNode(currentMenuTree, parent);

                            if (parentNode != null)
                            {
                                //add node to the parent's branches
                                parentNode.Branches.Add(newMenuLeaf);
                            }
                        }

                        //serialize back into the menu
                        menu.MenuTree = Composer.V1.SerializeMenuTree(currentMenuTree);
                    }
                    else
                    {
                        //no current nodes, so just set serialized data directly into menu
                        List<MenuNode> newMenuNodes = new List<MenuNode>();
                        newMenuNodes.Add(newMenuLeaf as MenuNode);

                        menu.MenuTree = Composer.V1.SerializeMenuTree(newMenuNodes);
                    }

                    //save menu
                    db.Entry(menu).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Details", new { id = id, parent = parent, idx = -1 });
                }

                return View(newMenuLeaf);
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

                MenuNode currentNode;

                if (parent == RootLevel)
                {
                    currentNode = currentMenuTree[idx];
                }
                else
                {
                    //find the parent node based on link
                    MenuNode parentNode = Composer.V1.FindMenuNode(currentMenuTree, parent);

                    currentNode = parentNode.Branches[idx];
                }

                return View(currentNode);
            }

            return HttpNotFound();
        }

        //
        // POST: /MenuTree/Edit/5

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(MenuNode editedMenuNode, int id, string parent, int idx)
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

                    //deserialize current menu tree
                    List<MenuNode> currentMenuTree = Composer.V1.DeserializeMenuTree(menu.MenuTree);

                    //check if this is the root level
                    if (parent == RootLevel)
                    {
                        //update node: update all fields except branches
                        currentMenuTree[idx].Title = editedMenuNode.Title;
                        currentMenuTree[idx].Text = editedMenuNode.Text;
                        currentMenuTree[idx].Link = editedMenuNode.Link;
                    }
                    else
                    {
                        //find the parent node
                        MenuNode parentNode = Composer.V1.FindMenuNode(currentMenuTree, parent);

                        if (parentNode != null)
                        {
                            //update node: update all fields except branches
                            parentNode.Branches[idx].Title = editedMenuNode.Title;
                            parentNode.Branches[idx].Text = editedMenuNode.Text;
                            parentNode.Branches[idx].Link = editedMenuNode.Link;
                        }
                    }

                    //serialize back into the menu
                    menu.MenuTree = Composer.V1.SerializeMenuTree(currentMenuTree);

                    //save menu
                    db.Entry(menu).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Details", new { id = id, parent = parent, idx = -1 });
                }

                return View(editedMenuNode);
            }

            return HttpNotFound();
/*
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
 */ 
        }

        //
        // GET: /MenuTree/EditItem/5

        public ActionResult EditItem(string parent, int idx, int id = 0)
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

                MenuLeaf currentLeaf;

                if (parent == RootLevel)
                {
                    currentLeaf = currentMenuTree[idx] as MenuLeaf; 
                }
                else
                {
                    //find the parent node based on link
                    MenuNode parentNode = Composer.V1.FindMenuNode(currentMenuTree, parent);

                    currentLeaf = parentNode.Branches[idx] as MenuLeaf;
                }

                return View(currentLeaf);
            }

            return HttpNotFound();
        }

        //
        // POST: /MenuTree/EditItem/5

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditItem(MenuLeaf newMenuLeaf, int id, string parent, int idx)
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

                    //deserialize current menu tree
                    List<MenuNode> currentMenuTree = Composer.V1.DeserializeMenuTree(menu.MenuTree);

                    //check if this is the root level
                    if (parent == RootLevel)
                    {
                        //update item
                        currentMenuTree[idx] = newMenuLeaf;
                    }
                    else
                    {
                        //find the parent node
                        MenuNode parentNode = Composer.V1.FindMenuNode(currentMenuTree, parent);

                        if (parentNode != null)
                        {
                            //update item
                            parentNode.Branches[idx] = newMenuLeaf;
                        }
                    }

                    //serialize back into the menu
                    menu.MenuTree = Composer.V1.SerializeMenuTree(currentMenuTree);

                    //save menu
                    db.Entry(menu).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Details", new { id = id, parent = parent, idx = -1 });
                }

                return View(newMenuLeaf);
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

            return RedirectToAction("Details", new { id = id, parent = parent, idx = -1 });
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}