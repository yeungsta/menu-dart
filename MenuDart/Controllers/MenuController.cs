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
    public class MenuController : Controller
    {
        private MenuDartDBContext db = new MenuDartDBContext();
        private const string RootLevel = "0";

        //
        // GET: /Menu/

        public ViewResult Index()
        {
            return View(db.Menus.ToList());
        }

        //
        // GET: /Menu/Details/5

        public ActionResult Details(int id = 0)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            return View(menu);
        }

        //
        // GET: /Menu/Create

        public ActionResult Create()
        {
            //compile list of Templates
            var templates = new List<string>();

            var templateQuery = from t in db.Templates
                                orderby t.Name
                                select t.Name;
            templates.AddRange(templateQuery.Distinct());

            ViewData["templateList"] = new SelectList(templates);

            return View();
        } 

        //
        // POST: /Menu/Create

        [HttpPost]
        public ActionResult Create(Menu menu)
        {
            if (ModelState.IsValid)
            {
                db.Menus.Add(menu);
                db.SaveChanges();
                return RedirectToAction("Index");  
            }

            return View(menu);
        }
        
        //
        // GET: /Menu/Edit/5

        public ActionResult Edit(int id = 0)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            //compile list of Templates
            var templates = new List<string>();

            var templateQuery = from t in db.Templates
                           orderby t.Name
                           select t.Name;
            templates.AddRange(templateQuery.Distinct());

            ViewData["templateList"] = new SelectList(templates, menu.Template);

            return View(menu);
        }

        //
        // POST: /Menu/Edit/5

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(Menu menu)
        {
            if (ModelState.IsValid)
            {
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(menu);
        }

        //
        // GET: /Menu/Delete/5
 
        public ActionResult Delete(int id = 0)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            return View(menu);
        }

        //
        // POST: /Menu/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id = 0)
        {            
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            db.Menus.Remove(menu);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        //
        // GET: /Menu/Compose/5

        public ActionResult Compose(int id = 0)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            Composer.V1 composer = new Composer.V1(menu);

            ComposeViewModel composeViewData = new ComposeViewModel();
            composeViewData.Name = menu.Name;
            composeViewData.MenuString = composer.CreateMenu();
            composeViewData.Url = composer.Url;

            return View(composeViewData);
        }

        //
        // GET: /Menu/MenuBuilder/5

        public ActionResult MenuBuilder(string parent, int id = 0)
        {
            //we need a parent in order to know what to edit
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

                MenuBuilderViewModel menuBuilderViewData = new MenuBuilderViewModel();
                menuBuilderViewData.MenuTree = Composer.V1.DeserializeMenuTree(menu.MenuTree);

                //if we're at the root level, just return the whole tree. Else find the right
                //parent node and send its branches.
                if (parent != RootLevel)
                {
                    //find the right node
                    MenuNode parentNode = menuBuilderViewData.MenuTree.Find(node => node.Link == parent);

                    //return the branches of the parent node
                    menuBuilderViewData.MenuTree = parentNode.Branches;
                }

                return View(menuBuilderViewData);
            }

            return HttpNotFound();
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}