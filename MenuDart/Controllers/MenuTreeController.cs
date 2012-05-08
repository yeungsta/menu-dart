using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MenuDart.Models;
using MenuDart.Composer;

namespace MenuDart.Controllers
{ 
    public class MenuTreeController : Controller
    {
        private MenuDartDBContext db = new MenuDartDBContext();
        private const string FirstLink = "1-1";
        private const string FirstLinkSuffix = "-1";
        //
        // GET: /MenuTree/

        public ViewResult Index()
        {
            return View(db.MenuTree.ToList());
        }

        //
        // GET: /MenuTree/Details/5
        [Authorize]
        public ActionResult Details(string parent, int idx, string source, int id = 0)
        {
            //we need a parent ID in order to know what to display
            if (!string.IsNullOrEmpty(parent))
            {
                if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
                {
                    return RedirectToAction("MenuBuilderAccessViolation", "Menu");
                }

                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                ViewBag.MenuId = id;
                ViewBag.Parent = parent;
                ViewBag.Source = source;

                List<MenuNode> currentMenuTree = V1.DeserializeMenuTree(menu.MenuTree);

                if (parent == Constants.RootLevel)
                {
                    ViewBag.UpLevel = Constants.RootLevel;

                    //if no specific index is specified, just display the whole level
                    if (idx == -1)
                    {
                        return View(currentMenuTree);
                    }
                    else
                    {
                        ViewBag.Parent = currentMenuTree[idx].Link;

                        if (!string.IsNullOrEmpty(currentMenuTree[idx].Text))
                        {
                            ViewBag.Text = currentMenuTree[idx].Text.Replace(Constants.NewLine, Constants.Break);
                        }

                        if (!string.IsNullOrEmpty(currentMenuTree[idx].Title))
                        {
                            ViewBag.LevelTitle = currentMenuTree[idx].Title;
                        }

                        return View(currentMenuTree[idx].Branches);
                    }
                }
                else //we're not at the root level
                {
                    //find the parent node
                    MenuNode parentNode = V1.FindMenuNode(currentMenuTree, parent);

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
                            ViewBag.UpLevel = Constants.RootLevel;
                        }

                        if (!string.IsNullOrEmpty(parentNode.Text))
                        {
                            ViewBag.Text = parentNode.Text.Replace(Constants.NewLine, Constants.Break);
                        }

                        if (!string.IsNullOrEmpty(parentNode.Title))
                        {
                            ViewBag.LevelTitle = parentNode.Title;
                        }

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
                            ViewBag.UpLevel = Constants.RootLevel;
                        }

                        ViewBag.Parent = parentNode.Branches[idx].Link;

                        if (!string.IsNullOrEmpty(parentNode.Branches[idx].Text))
                        {
                            ViewBag.Text = parentNode.Branches[idx].Text.Replace(Constants.NewLine, Constants.Break);
                        }

                        if (!string.IsNullOrEmpty(parentNode.Branches[idx].Title))
                        {
                            ViewBag.LevelTitle = parentNode.Branches[idx].Title;
                        }

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
                    List<MenuNode> currentMenuTree = V1.DeserializeMenuTree(menu.MenuTree);

                    if ((currentMenuTree != null) && (currentMenuTree.Count > 0))
                    {
                        //if this is the root level
                        if (parent == Constants.RootLevel)
                        {
                            try
                            {
                                newMenuNode.Link = IncrementLink(currentMenuTree.Last(node => !(node is MenuLeaf)).Link);
                            }
                            catch //there is no other menuNode
                            {
                                newMenuNode.Link = FirstLink;
                            }

                            //add node to the root level
                            currentMenuTree.Add(newMenuNode);
                        }
                        else
                        {
                            //find the parent node
                            MenuNode parentNode = V1.FindMenuNode(currentMenuTree, parent);

                            if (parentNode != null)
                            {
                                try
                                {
                                    newMenuNode.Link = IncrementLink(parentNode.Branches.Last(node => !(node is MenuLeaf)).Link);
                                }
                                catch //there is no other menuNode
                                {
                                    //create the first category link of this level
                                    newMenuNode.Link = parentNode.Link + FirstLinkSuffix;
                                }

                                //add node to the parent's branches
                                parentNode.Branches.Add(newMenuNode);
                            }
                        }

                        //serialize back into the menu
                        menu.MenuTree = V1.SerializeMenuTree(currentMenuTree);
                    }
                    else
                    {
                        //this is the first and only node of the tree
                        newMenuNode.Link = FirstLink;

                        List<MenuNode> newMenuNodeList = new List<MenuNode>();
                        newMenuNodeList.Add(newMenuNode);

                        //no current nodes, so just set serialized data directly into menu
                        menu.MenuTree = V1.SerializeMenuTree(newMenuNodeList);
                    }

                    //save menu in DB
                    db.Entry(menu).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Details", new { id = id, parent = parent, idx = -1 });
                }

                return View(newMenuNode);
            }

            return HttpNotFound();
        }

        public ActionResult CreateItem(string parent, int id = 0)
        {
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
                    List<MenuNode> currentMenuTree = V1.DeserializeMenuTree(menu.MenuTree);

                    if ((currentMenuTree != null) && (currentMenuTree.Count > 0))
                    {
                        //if parent is empty, this is the root level
                        if (parent == Constants.RootLevel)
                        {
                            //add node to the root level
                            currentMenuTree.Add(newMenuLeaf);
                        }
                        else
                        {
                            //find the parent node
                            MenuNode parentNode = V1.FindMenuNode(currentMenuTree, parent);

                            if (parentNode != null)
                            {
                                //add node to the parent's branches
                                parentNode.Branches.Add(newMenuLeaf);
                            }
                        }

                        //serialize back into the menu
                        menu.MenuTree = V1.SerializeMenuTree(currentMenuTree);
                    }
                    else
                    {
                        //no current nodes, so just set serialized data directly into menu
                        List<MenuNode> newMenuNodes = new List<MenuNode>();
                        newMenuNodes.Add(newMenuLeaf as MenuNode);

                        menu.MenuTree = V1.SerializeMenuTree(newMenuNodes);
                    }

                    //save menu to DB
                    db.Entry(menu).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Details", new { id = id, parent = parent, idx = -1 });
                }

                return View(newMenuLeaf);
            }

            return HttpNotFound();
        }

        public ActionResult CreateText(string parent, int id = 0)
        {
            ViewBag.MenuId = id;
            ViewBag.Parent = parent;

            return View();
        }

        //
        // POST: /MenuTree/CreateText

        [HttpPost]
        public ActionResult CreateText(MenuNode newNode, int id, string parent)
        {
            //we need a parent in order to know where to add the text
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
                    List<MenuNode> currentMenuTree = V1.DeserializeMenuTree(menu.MenuTree);

                    if ((currentMenuTree != null) && (currentMenuTree.Count > 0))
                    {
                        //if parent is empty, this is the root level
                        if (parent == Constants.RootLevel)
                        {
                            //don't allow page text on root level?
                        }
                        else
                        {
                            //find the parent node
                            MenuNode parentNode = V1.FindMenuNode(currentMenuTree, parent);

                            if (parentNode != null)
                            {
                                //add node to the parent's branches
                                parentNode.Text = newNode.Text;
                            }
                        }

                        //serialize back into the menu
                        menu.MenuTree = V1.SerializeMenuTree(currentMenuTree);
                    }

                    //save menu to DB
                    db.Entry(menu).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Details", new { id = id, parent = parent, idx = -1 });
                }

                return View(newNode);
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

                ViewBag.MenuId = id;
                ViewBag.Parent = parent;

                List<MenuNode> currentMenuTree = V1.DeserializeMenuTree(menu.MenuTree);

                MenuNode currentNode;

                if (parent == Constants.RootLevel)
                {
                    currentNode = currentMenuTree[idx];
                }
                else
                {
                    //find the parent node based on link
                    MenuNode parentNode = V1.FindMenuNode(currentMenuTree, parent);

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
                    List<MenuNode> currentMenuTree = V1.DeserializeMenuTree(menu.MenuTree);

                    //check if this is the root level
                    if (parent == Constants.RootLevel)
                    {
                        //update node: update fields except branches
                        currentMenuTree[idx].Title = editedMenuNode.Title;
                    }
                    else
                    {
                        //find the parent node
                        MenuNode parentNode = V1.FindMenuNode(currentMenuTree, parent);

                        if (parentNode != null)
                        {
                            //update node: update fields except branches
                            parentNode.Branches[idx].Title = editedMenuNode.Title;
                        }
                    }

                    //serialize back into the menu
                    menu.MenuTree = V1.SerializeMenuTree(currentMenuTree);

                    //save menu in DB
                    db.Entry(menu).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Details", new { id = id, parent = parent, idx = -1 });
                }

                return View(editedMenuNode);
            }

            return HttpNotFound(); 
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

                ViewBag.MenuId = id;
                ViewBag.Parent = parent;

                List<MenuNode> currentMenuTree = V1.DeserializeMenuTree(menu.MenuTree);

                MenuLeaf currentLeaf;

                if (parent == Constants.RootLevel)
                {
                    currentLeaf = currentMenuTree[idx] as MenuLeaf; 
                }
                else
                {
                    //find the parent node based on link
                    MenuNode parentNode = V1.FindMenuNode(currentMenuTree, parent);

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
                    List<MenuNode> currentMenuTree = V1.DeserializeMenuTree(menu.MenuTree);

                    //check if this is the root level
                    if (parent == Constants.RootLevel)
                    {
                        //update item
                        currentMenuTree[idx] = newMenuLeaf;
                    }
                    else
                    {
                        //find the parent node
                        MenuNode parentNode = V1.FindMenuNode(currentMenuTree, parent);

                        if (parentNode != null)
                        {
                            //update item
                            parentNode.Branches[idx] = newMenuLeaf;
                        }
                    }

                    //serialize back into the menu
                    menu.MenuTree = V1.SerializeMenuTree(currentMenuTree);

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
        // GET: /MenuTree/EditText/5

        public ActionResult EditText(string parent, int id = 0)
        {
            //we need a parent ID in order to know where to edit
            if (!string.IsNullOrEmpty(parent))
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                ViewBag.MenuId = id;
                ViewBag.Parent = parent;

                List<MenuNode> currentMenuTree = V1.DeserializeMenuTree(menu.MenuTree);

                if (parent == Constants.RootLevel)
                {
                    //no root
                }
                else
                {
                    //find the parent node
                    return View(V1.FindMenuNode(currentMenuTree, parent));
                }
            }

            return HttpNotFound();
        }

        //
        // POST: /MenuTree/EditText/5

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditText(MenuNode editedMenuNode, int id, string parent)
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
                    List<MenuNode> currentMenuTree = V1.DeserializeMenuTree(menu.MenuTree);

                    //check if this is the root level
                    if (parent == Constants.RootLevel)
                    {
                        //no root level?
                    }
                    else
                    {
                        //find the parent node
                        MenuNode parentNode = V1.FindMenuNode(currentMenuTree, parent);

                        if (parentNode != null)
                        {
                            //update text field
                            parentNode.Text = editedMenuNode.Text;
                        }
                    }

                    //serialize back into the menu
                    menu.MenuTree = V1.SerializeMenuTree(currentMenuTree);

                    //save menu to DB
                    db.Entry(menu).State = EntityState.Modified;
                    db.SaveChanges();

                    return RedirectToAction("Details", new { id = id, parent = parent, idx = -1 });
                }

                return View(editedMenuNode);
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

                List<MenuNode> currentMenuTree = V1.DeserializeMenuTree(menu.MenuTree);

                //check if this is the root level
                if (parent == Constants.RootLevel)
                {
                    return View(currentMenuTree[idx]);
                }
                else
                {
                    //find the parent node based on link, then index
                    return View(V1.FindMenuNode(currentMenuTree, parent).Branches[idx]);
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
            List<MenuNode> currentMenuTree = V1.DeserializeMenuTree(menu.MenuTree);

            //check if this is the root level
            if (parent == Constants.RootLevel)
            {
                //remove at the index
                currentMenuTree.RemoveAt(idx);
            }
            else
            {
                //remove at the index
                V1.FindMenuNode(currentMenuTree, parent).Branches.RemoveAt(idx);
            }

            //serialize back into the menu
            menu.MenuTree = V1.SerializeMenuTree(currentMenuTree);

            //save menu to DB
            db.Entry(menu).State = EntityState.Modified;
            db.SaveChanges();

            return RedirectToAction("Details", new { id = id, parent = parent, idx = -1 });
        }

        //
        // GET: /MenuTree/DeleteText/5

        public ActionResult DeleteText(string parent, int id)
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

                List<MenuNode> currentMenuTree = V1.DeserializeMenuTree(menu.MenuTree);

                //check if this is the root level
                if (parent == Constants.RootLevel)
                {
                    //no root level?
                }
                else
                {
                    //find the parent node
                    return View(V1.FindMenuNode(currentMenuTree, parent));
                }
            }

            return HttpNotFound();
        }

        //
        // POST: /MenuTree/DeleteText/5

        public ActionResult DeleteTextConfirmed(int id, string parent)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            //deserialize current menutree
            List<MenuNode> currentMenuTree = V1.DeserializeMenuTree(menu.MenuTree);

            //check if this is the root level
            if (parent == Constants.RootLevel)
            {
                //none at root level?
            }
            else
            {
                //clear out text
                V1.FindMenuNode(currentMenuTree, parent).Text = string.Empty;
            }

            //serialize back into the menu
            menu.MenuTree = V1.SerializeMenuTree(currentMenuTree);

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

        //Increments link
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
    }
}