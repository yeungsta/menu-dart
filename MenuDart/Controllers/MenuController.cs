using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.IO;
using System.Web;
using System.Web.Mvc;
using MenuDart.Models;

namespace MenuDart.Controllers
{ 
    public class MenuController : Controller
    {
        private MenuDartDBContext db = new MenuDartDBContext();
        private const string RootLevel = "0";

        //max number of locations
        private const int MaxLocations = 8;
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
            //create initial empty menu tree
            menu.MenuTree = Composer.V1.SerializeMenuTree(new List<MenuNode>());

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
        // GET: /Menu/MenuBuilder
        // This is the start of the menu builder. A new menu is created here.
        public ActionResult MenuBuilder()
        {
            Menu newMenu = new Menu();

            ViewBag.MenuId = newMenu.ID;

            MenuBuilderViewModel menuBuilderViewData = new MenuBuilderViewModel();
            menuBuilderViewData.CurrentMenu = newMenu;

            return View(menuBuilderViewData);
        }

        //
        // POST: /Menu/MenuBuilder

        [HttpPost]
        public ActionResult MenuBuilder(MenuBuilderViewModel menuBuilderModel)
        {
            if (ModelState.IsValid)
            {
                //put some placeholder values for empty menu 
                menuBuilderModel.CurrentMenu.AboutTitle = "About Your Restaurant Here";
                menuBuilderModel.CurrentMenu.AboutText = "Description about your business here...";

                //create menudart URL: buffalo-fire-department-torrance
                //todo: check if there are duplicate URLs.
                menuBuilderModel.CurrentMenu.MenuDartUrl = 
                    (menuBuilderModel.CurrentMenu.Name.Replace(' ', '-') + 
                    "-" + menuBuilderModel.CurrentMenu.City).ToLower();

                //save to DB
                db.Menus.Add(menuBuilderModel.CurrentMenu);
                db.SaveChanges();

                Composer.V1 composer = new Composer.V1(menuBuilderModel.CurrentMenu);
                composer.CreateMenu();

                return RedirectToAction("MenuBuilder2", new { name = menuBuilderModel.CurrentMenu.Name, url = composer.Url, id = menuBuilderModel.CurrentMenu.ID });
            }

            return View(menuBuilderModel.CurrentMenu);
        }

        //
        // GET: /Menu/MenuBuilder2
        // 
        public ActionResult MenuBuilder2(string name, string url, int id = 0)
        {
            ViewBag.Name = name;
            ViewBag.Url = url;
            ViewBag.MenuId = id;

            return View();
        }

        //
        // GET: /Menu/MenuBuilder3
        // 
        public ActionResult MenuBuilder3(int id = 0)
        {
            ViewBag.MenuId = id;

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

            ViewData["templateList"] = new SelectList(templates);

            return View(menu);
        }

        //
        // POST: /Menu/MenuBuilder3

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult MenuBuilder3(Menu newMenu, int id)
        {
            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                //set template value
                menu.Template = newMenu.Template;

                //save menu
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                //Update index_html directory with new template CSS file
                CopyTemplate(menu.MenuDartUrl, menu.Template);

                return RedirectToAction("MenuBuilder4", new { id = id });
            }

            return View(newMenu);
        }

        //
        // GET: /Menu/MenuBuilder4
        // 
        public ActionResult MenuBuilder4(int id = 0)
        {
            ViewBag.MenuId = id;

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            return View(menu);
        }

        //
        // POST: /Menu/MenuBuilder4

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult MenuBuilder4(Menu newMenu, int id)
        {
            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                //set about values
                menu.AboutTitle = newMenu.AboutTitle;
                menu.AboutText = newMenu.AboutText;

                //save menu
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("MenuBuilder5", new { id = id });
            }

            return View(newMenu);
        }

        //
        // GET: /Menu/MenuBuilder5
        // 
        public ActionResult MenuBuilder5(int id = 0)
        {
            ViewBag.MenuId = id;

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            //compile list of number of locations
            var numLocations = new List<int>();

            for (int x = 1; x <= MaxLocations; x++)
            {
                numLocations.Add(x);
            }

            ViewData["numList"] = new SelectList(numLocations, 1);

            return View();
        }

        //
        // POST: /Menu/MenuBuilder5

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult MenuBuilder5(int numLocations, int id)
        {
            if (numLocations == 1)
            {
                return RedirectToAction("MenuBuilder6a", new { id = id });
            }
            else
            {
                return RedirectToAction("MenuBuilder6b", new { id = id });
            }
        }

        //
        // GET: /Menu/MenuBuilder6a
        // 
        public ActionResult MenuBuilder6a(int id = 0)
        {
            ViewBag.MenuId = id;

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            //populate the city since we already know it.
            Location newLocation = new Location();
            newLocation.City = menu.City;

            return View(newLocation);
        }

        //
        // POST: /Menu/MenuBuilder6a

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult MenuBuilder6a(Location newLocation, int id)
        {
            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                //add Google map link
                newLocation.MapLink = CreateMapLink(newLocation.Address, newLocation.City, newLocation.Zip);

                //add Google map image link

                List<Location> newLocationList = new List<Location>();
                newLocationList.Add(newLocation);

                //just set serialized locations directly into menu
                menu.Locations = Composer.V1.SerializeLocations(newLocationList);

                //save menu
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("MenuBuilder6a2", new { id = id });
            }

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        private string CreateMapLink(string address, string state, string zip)
        {
            return string.Empty;
        }

        private void CopyTemplate(string menuDartUrl, string templateName)
        {
            string indexFilesPath = HttpContext.Server.MapPath("~/Content/menus/" + menuDartUrl + "/index_files");
            string templatesPath = HttpContext.Server.MapPath("~/Content/templates/themes/" + templateName + "/");

            if (Directory.Exists(indexFilesPath))
            {
                if (Directory.Exists(templatesPath))
                {
                    string[] files = Directory.GetFiles(templatesPath);
                    string fileName;
                    string destFile;

                    // Copy the files and overwrite destination files if they already exist. Should be only one.
                    foreach (string s in files)
                    {
                        // Use static Path methods to extract only the file name from the path.
                        fileName = Path.GetFileName(s);
                        destFile = Path.Combine(indexFilesPath, fileName);
                        System.IO.File.Copy(s, destFile, true);
                    }
                }
            }
        }
    }
}