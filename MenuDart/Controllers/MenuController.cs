using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.IO;
using System.Web;
using System.Web.Mvc;
using MenuDart.Models;
using MenuDart.Composer;

namespace MenuDart.Controllers
{ 
    public class MenuController : Controller
    {
        private MenuDartDBContext db = new MenuDartDBContext();

        //
        // GET: /Menu/

        public ViewResult Index()
        {
            ViewBag.UrlPath = Utilities.GetUrlPath();
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
            menu.MenuTree = V1.SerializeMenuTree(new List<MenuNode>());

            //create initial empty locations
            menu.Locations = V1.SerializeLocations(new List<Location>());

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

            //now delete the menu directory as well
            Utilities.RemoveDirectory(menu.MenuDartUrl);

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

            V1 composer = new V1(menu);

            ComposeViewModel composeViewData = new ComposeViewModel();
            composeViewData.Name = menu.Name;
            composeViewData.MenuString = composer.CreateMenu();
            composeViewData.Url = Utilities.GetFullUrl(menu.MenuDartUrl);

            return View(composeViewData);
        }

        //
        // GET: /Menu/MenuBuilder
        // This is the start of the main menu builder. A new menu is created here.
        public ActionResult MenuBuilder()
        {
            //create empty menu
            Menu newMenu = new Menu();

            //put the menu in a view model in case we need to 
            //transfer additional info in the future
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
/*
                //create initial default location
                List<Location> defaultLocationList = new List<Location>() {
                    new Location() { Address = "Address Here.",
                        City = menuBuilderModel.CurrentMenu.City,
                        Hours = "Hours of Operation Here.",
                        MapLink = Utilities.CreateMapLink(string.Empty, menuBuilderModel.CurrentMenu.City, string.Empty),
                        MapImgUrl = Utilities.CreateMapImgLink(string.Empty, menuBuilderModel.CurrentMenu.City, string.Empty) 
                    }
                };
*/
                menuBuilderModel.CurrentMenu.Locations = V1.SerializeLocations(new List<Location>());

                //create initial default menu tree
                List<MenuNode> defaultMenuNodeList = new List<MenuNode>(){
                    new MenuNode() { Title = "Breakfast (Example)", Link = "1-1", Text = "Breakfast menu items here. (Sample)" },
                    new MenuNode() { Title = "Lunch (Example)", Link = "1-2", Text = "Lunch menu items here. (Sample)" },
                    new MenuNode() { Title = "Dinner (Example)", Link = "1-3", Text = "Dinner menu items here. (Sample)" }
                };

                menuBuilderModel.CurrentMenu.MenuTree = V1.SerializeMenuTree(defaultMenuNodeList);

                //create default About page values
                menuBuilderModel.CurrentMenu.AboutTitle = string.Format(Constants.DefaultAboutTitleFormat, menuBuilderModel.CurrentMenu.Name);
                menuBuilderModel.CurrentMenu.AboutText = string.Format(Constants.DefaultAboutTextFormat, menuBuilderModel.CurrentMenu.Name);

                //Create unique menudart URL          
                string tempUrl = (menuBuilderModel.CurrentMenu.Name.Replace(' ', '-') + 
                    "-" + menuBuilderModel.CurrentMenu.City).ToLower();

                //remove unwanted chars
                tempUrl = tempUrl.Replace(",", "");
                tempUrl = tempUrl.Replace("'", "");

                //replace chars with text
                tempUrl = tempUrl.Replace("&", "and");

                //Check if there are duplicate URLs.
                int matches = db.Menus.Count(menu => menu.MenuDartUrl.Contains(tempUrl));

                if (matches > 0)
                {
                    tempUrl += "-" + (matches + 1).ToString();
                }
                
                menuBuilderModel.CurrentMenu.MenuDartUrl = tempUrl;

                if (Request.IsAuthenticated)
                {
                    //save current user as owner
                    menuBuilderModel.CurrentMenu.Owner = HttpContext.User.Identity.Name;
                }

                //save menu to DB, get an assigned menu ID
                db.Menus.Add(menuBuilderModel.CurrentMenu);
                db.SaveChanges();

                //save temp menu with session so anonymous user can retrieve later
                if (!Request.IsAuthenticated)
                {
                    var cart = SessionCart.GetCart(this.HttpContext);
                    cart.AddMenu(menuBuilderModel.CurrentMenu.ID);
                }

                //Compose the menu for the first time, so user can try it out
                V1 composer = new V1(menuBuilderModel.CurrentMenu);
                composer.CreateMenu();

                return RedirectToAction("MenuBuilder2", new { name = menuBuilderModel.CurrentMenu.Name, url = Utilities.GetFullUrl(menuBuilderModel.CurrentMenu.MenuDartUrl), id = menuBuilderModel.CurrentMenu.ID });
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
        // POST: /Menu/MenuBuilder3

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult MenuBuilder3(string template, int id)
        {
            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                //set template value
                menu.Template = template;

                //save menu to DB
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                //Update index_html directory with new CSS template file
                string indexFilesPath = HttpContext.Server.MapPath(Constants.MenusPath + menu.MenuDartUrl + "/" + Constants.IndexFilesDir);
                string templatesPath = HttpContext.Server.MapPath((Constants.TemplatesPath + menu.Template + "/"));

                Utilities.CopyDirTo(templatesPath, indexFilesPath);

                return RedirectToAction("MenuBuilder4", new { id = id });
            }

            return View();
        }

        //
        // GET: /Menu/MenuBuilder4
        // 
        public ActionResult MenuBuilder4(int id = 0)
        {
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

                //save menu to DB
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
            //compile list of number of locations for drop-down control
            var numLocations = new List<int>();

            for (int x = 1; x <= Constants.MaxLocations; x++)
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
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            //Starting with a new location. Populate the city since we already know it.
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
                newLocation.MapLink = Utilities.CreateMapLink(newLocation.Address, newLocation.City, newLocation.Zip);

                //add Google map image link
                newLocation.MapImgUrl = Utilities.CreateMapImgLink(newLocation.Address, newLocation.City, newLocation.Zip);

                //create just one location and add to new list
                List<Location> newLocationList = new List<Location>();
                newLocationList.Add(newLocation);

                //set serialized locations directly into menu
                menu.Locations = V1.SerializeLocations(newLocationList);

                //save menu to DB
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("MenuBuilder6a2", new { id = id });
            }

            return View();
        }

        //
        // GET: /Menu/MenuBuilder6a2
        // 
        public ActionResult MenuBuilder6a2(int id = 0)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            //a location already exists, but let's just create an empty location
            //as a placeholder for the next view's data.
            Location newLocation = new Location();

            //pre-populate the phone number if it's already been filled at the menu-level.
            if (!string.IsNullOrEmpty(menu.Phone))
            {
                newLocation.Phone = menu.Phone;
            }

            return View(newLocation);
        }

        //
        // POST: /Menu/MenuBuilder6a2

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult MenuBuilder6a2(Location newLocation, int id)
        {
            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                //get current locations (should be only one on this path)
                List<Location> currentLocations = V1.DeserializeLocations(menu.Locations);

                //write new data
                currentLocations[0].Hours = newLocation.Hours;
                currentLocations[0].Phone = newLocation.Phone;
                currentLocations[0].Email = newLocation.Email;

                //set serialized locations back into menu
                menu.Locations = V1.SerializeLocations(currentLocations);

                //save menu to DB
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("MenuBuilder6a3", new { id = id });
            }

            return View();
        }

        //
        // GET: /Menu/MenuBuilder6a3
        // 
        public ActionResult MenuBuilder6a3(int id = 0)
        {
            //a location already exists, but let's just create an empty location
            //as a placeholder for the next view's data.
            Location newLocation = new Location();

            return View(newLocation);
        }

        //
        // POST: /Menu/MenuBuilder6a3

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult MenuBuilder6a3(Location newLocation, int id)
        {
            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                //get current locations (should be only one on this path)
                List<Location> currentLocations = V1.DeserializeLocations(menu.Locations);

                //write new data
                currentLocations[0].Facebook = newLocation.Facebook;
                currentLocations[0].Twitter = newLocation.Twitter;
                currentLocations[0].Yelp = newLocation.Yelp;

                //set serialized locations back into menu
                menu.Locations = V1.SerializeLocations(currentLocations);

                //save menu to DB
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                //send to MenuTree builder (root level)
                return RedirectToAction("Details", "MenuTree", new { id = id, parent = 0, idx = -1 });
            }

            return View();
        }

        //
        // GET: /Menu/MenuBuilder7
        // 
        public ActionResult MenuBuilder7(int id = 0)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            ViewBag.Name = menu.Name;
            ViewBag.Url = Utilities.GetFullUrl(menu.MenuDartUrl);

            //Compose the menu
            V1 composer = new V1(menu);
            composer.CreateMenu();

            return View();
        }

        [HttpPost]
        public ActionResult LogoUpload(string qqfile, string menuDartUrl)
        {
            if (!string.IsNullOrEmpty(menuDartUrl))
            {
                string indexFilesPath = HttpContext.Server.MapPath(Constants.MenusPath + menuDartUrl + "/" + Constants.IndexFilesDir);
                var file = string.Empty;

                try
                {
                    var stream = Request.InputStream;
                    if (String.IsNullOrEmpty(Request["qqfile"]))
                    {
                        // IE
                        HttpPostedFileBase postedFile = Request.Files[0];
                        stream = postedFile.InputStream;
                        file = Path.Combine(indexFilesPath, Constants.LogoFileName);
                    }
                    else
                    {
                        //Webkit, Mozilla
                        file = Path.Combine(indexFilesPath, Constants.LogoFileName);
                    }

                    //TODO: check if there exists a logo.png file; if it does prompt
                    //TODO: user to confirm that they want to overwrite.

                    var buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    System.IO.File.WriteAllBytes(file, buffer);
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message }, "application/json");
                }

                return Json(new { success = true }, "text/html");
            }

            return Json(new { success = false, message = "No URL path." }, "application/json");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}