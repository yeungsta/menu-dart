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
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            return View(menu);
        }

        //
        // GET: /Menu/Create

        public ActionResult Create()
        {
            ViewData["templateList"] = new SelectList(GetTemplates());

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
        [Authorize]
        public ActionResult Edit(int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            MenuEditorBasicViewModel basicViewData = new MenuEditorBasicViewModel();
            basicViewData.MenuId = menu.ID;
            basicViewData.Name = menu.Name;
            basicViewData.City = menu.City;
            basicViewData.Phone = menu.Phone;
            basicViewData.Website = menu.Website;

            return View(basicViewData);
        }

        //
        // POST: /Menu/Edit/5

        [HttpPost]
        [ValidateInput(false)]
        [Authorize]
        public ActionResult Edit(MenuEditorBasicViewModel basicInfo, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                //if name or city is updated, need to update new URL
                if ((menu.Name != basicInfo.Name) || (menu.City != basicInfo.City))
                {
                    menu.Name = basicInfo.Name;
                    menu.City = basicInfo.City;
                    menu.MenuDartUrl = CreateMenuDartUrl(basicInfo.Name, basicInfo.City);

                    //TODO: notify user of new URL when saving
                }
                
                //update basic info fields
                menu.Phone = basicInfo.Phone;
                menu.Website = basicInfo.Website;

                //save changes
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();
            }

            //create view model
            basicInfo.MenuId = id;
            return View(basicInfo);
        }

        //
        // GET: /Menu/Edit2/5
        [Authorize]
        public ActionResult Edit2(int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            //pass template list to view
            ViewData["templateList"] = new SelectList(GetTemplates(), menu.Template);

            //create view model
            MenuEditorThemeViewModel themeViewData = new MenuEditorThemeViewModel();
            themeViewData.MenuId = menu.ID;
            themeViewData.Name = menu.Name;

            return View(themeViewData);
        }

        //
        // POST: /Menu/Edit2/5
        [HttpPost]
        [ValidateInput(false)]
        [Authorize]
        public ActionResult Edit2(string template, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                //set template value
                menu.Template = template;

                //save menu to DB
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                //Update index_html directory with new CSS template file
                string indexFilesPath = HttpContext.Server.MapPath(Constants.MenusPath + menu.MenuDartUrl + "/" + Constants.IndexFilesDir);
                string templatesPath = HttpContext.Server.MapPath((Constants.TemplatesPath + menu.Template + "/"));

                Utilities.CopyDirTo(templatesPath, indexFilesPath, true);
            }

            //pass template list to view
            ViewData["templateList"] = new SelectList(GetTemplates(), menu.Template);

            //create view model
            MenuEditorThemeViewModel themeViewData = new MenuEditorThemeViewModel();
            themeViewData.MenuId = menu.ID;
            themeViewData.Name = menu.Name;

            return View(themeViewData);
        }

        //
        // GET: /Menu/Edit3/5
        [Authorize]
        public ActionResult Edit3(int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            //create view model
            MenuEditorAboutViewModel aboutViewData = new MenuEditorAboutViewModel();
            aboutViewData.MenuId = menu.ID;
            aboutViewData.Name = menu.Name;
            aboutViewData.AboutTitle = menu.AboutTitle;
            aboutViewData.AboutText = menu.AboutText;

            return View(aboutViewData);
        }

        //
        // POST: /Menu/Edit3/5
        [HttpPost]
        [ValidateInput(false)]
        [Authorize]
        public ActionResult Edit3(MenuEditorAboutViewModel aboutInfo, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    Utilities.LogAppError("Could not find menu.");
                    return HttpNotFound();
                }

                //update about info fields
                menu.AboutTitle = aboutInfo.AboutTitle;
                menu.AboutText = aboutInfo.AboutText;

                //save menu to DB
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();
            }

            return View(aboutInfo);
        }

        //
        // GET: /Menu/Edit4/5
        [Authorize]
        public ActionResult Edit4(int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            //create view model
            MenuEditorLogoViewModel logoViewData = new MenuEditorLogoViewModel();
            logoViewData.MenuId = menu.ID;
            logoViewData.Name = menu.Name;
            logoViewData.MenuDartUrl = menu.MenuDartUrl;

            //check if logo exists for display
            string logoPath = HttpContext.Server.MapPath(Constants.MenusPath + menu.MenuDartUrl + "/" + Constants.IndexFilesDir + "/" + Constants.LogoFileName);

            if (System.IO.File.Exists(logoPath))
            {
                logoViewData.LogoUrl = Utilities.GetMenuLogoUrl(menu.MenuDartUrl);
            }

            //check if temp logo exists for display
            string logoTmpPath = HttpContext.Server.MapPath(Constants.MenusPath + menu.MenuDartUrl + "/" + Constants.IndexFilesDir + "/" + Constants.LogoTmpFileName);

            if (System.IO.File.Exists(logoTmpPath))
            {
                logoViewData.LogoTmpUrl = Utilities.GetMenuLogoTmpUrl(menu.MenuDartUrl);
            }

            return View(logoViewData);
        }

        //
        // GET: /Menu/Edit5/5
        [Authorize]
        public ActionResult Edit5(int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            //create view model
            MenuEditorLocationViewModel locationViewData = new MenuEditorLocationViewModel();
            locationViewData.MenuId = menu.ID;
            locationViewData.Name = menu.Name;
            locationViewData.Locations = V1.DeserializeLocations(menu.Locations);

            return View(locationViewData);
        }

        //
        // POST: /Menu/Edit5/5
        [HttpPost]
        [ValidateInput(false)]
        [Authorize]
        public ActionResult Edit5(MenuEditorLocationViewModel locationInfo, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    Utilities.LogAppError("Could not find menu.");
                    return HttpNotFound();
                }

                //set serialized locations back into menu
                menu.Locations = V1.SerializeLocations(locationInfo.Locations);

                //save menu to DB
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();
            }

            return View(locationInfo);
        }

        //
        // GET: /Menu/Edit6/5
        [Authorize]
        public ActionResult Edit6(int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            //create view model
            MenuEditorLocationViewModel locationViewData = new MenuEditorLocationViewModel();
            locationViewData.MenuId = menu.ID;
            locationViewData.Name = menu.Name;
            locationViewData.Locations = V1.DeserializeLocations(menu.Locations);

            return View(locationViewData);
        }

        //
        // POST: /Menu/Edit6/5
        [HttpPost]
        [ValidateInput(false)]
        [Authorize]
        public ActionResult Edit6(MenuEditorLocationViewModel locationInfo, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    Utilities.LogAppError("Could not find menu.");
                    return HttpNotFound();
                }

                //set serialized locations back into menu
                menu.Locations = V1.SerializeLocations(locationInfo.Locations);

                //save menu to DB
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();
            }

            return View(locationInfo);
        }

        //
        // GET: /Menu/Edit7/5
        [Authorize]
        public ActionResult Edit7(int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            //create view model
            MenuEditorLocationViewModel locationViewData = new MenuEditorLocationViewModel();
            locationViewData.MenuId = menu.ID;
            locationViewData.Name = menu.Name;
            locationViewData.Locations = V1.DeserializeLocations(menu.Locations);

            return View(locationViewData);
        }

        //
        // POST: /Menu/Edit7/5
        [HttpPost]
        [ValidateInput(false)]
        [Authorize]
        public ActionResult Edit7(MenuEditorLocationViewModel locationInfo, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    Utilities.LogAppError("Could not find menu.");
                    return HttpNotFound();
                }

                //set serialized locations back into menu
                menu.Locations = V1.SerializeLocations(locationInfo.Locations);

                //save menu to DB
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();
            }

            return View(locationInfo);
        }

        //
        // GET: /Menu/Delete/5
 
        public ActionResult Delete(int id = 0)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
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
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            db.Menus.Remove(menu);
            db.SaveChanges();

            //now delete the menu directory as well
            Utilities.RemoveDirectory(menu.MenuDartUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        //
        // GET: /Menu/Publish/5

        public ActionResult Publish(string ReturnUrl, int id = 0)
        {
            try
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    Utilities.LogAppError("Could not find menu.");
                    return HttpNotFound();
                }

                if (menu.Active)
                {
                    V1 composer = new V1(menu);
                    // re-compose the menu
                    composer.CreateMenu();

                    ActivateViewModel activateViewData = new ActivateViewModel();
                    activateViewData.Name = menu.Name;
                    activateViewData.MenuId = id;
                    activateViewData.Url = Utilities.GetFullUrl(menu.MenuDartUrl);
                    activateViewData.ReturnUrl = ReturnUrl;

                    return View(activateViewData);
                }
                else
                {
                    //menu not activated; cannot publish
                    return RedirectToAction("PublishNotActive", new { ReturnUrl = ReturnUrl, menuName = menu.Name, id = id });
                }
            }
            catch (Exception e)
            {
                Utilities.LogAppError("Could not publish menu", e);
                return View();
            }
        }

        //
        // GET: /Menu/PublishNotActive/5

        public ActionResult PublishNotActive(string ReturnUrl, string menuName, int id = 0)
        {
            ActivateViewModel activateViewData = new ActivateViewModel();
            activateViewData.Name = menuName;
            activateViewData.MenuId = id;
            activateViewData.ReturnUrl = ReturnUrl;

            return View(activateViewData);
        }

        //
        // GET: /Menu/ViewActiveMenu/5
        //
        public ActionResult ViewActiveMenu(int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            //menu should already exist
            ViewBag.Url = Utilities.GetFullUrl(menu.MenuDartUrl);

            return View();
        }

        //
        // GET: /Menu/PreviewMenu/5
        //
        //create temporary menu for previewing
        public ActionResult PreviewMenu(int id = 0)
        {
            bool createNewTempDir = false;

            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            V1 composer = new V1(menu);
            string randomId = string.Empty;

            //check if a preview key/URL exists for this session
            if (this.HttpContext.Session[Constants.SessionPreviewKey] != null)
            {
                randomId = this.HttpContext.Session[Constants.SessionPreviewKey].ToString();
                string directoryPath = HttpContext.Server.MapPath(Controllers.Constants.PreviewMenusPath + randomId + "/");

                //check if preview location still exists
                if (Directory.Exists(directoryPath))
                {
                    composer.UpdateTempMenu(randomId);
                }
                else
                {
                    createNewTempDir = true;
                }
            }
            else
            {
                createNewTempDir = true;
            }

            if (createNewTempDir)
            {
                randomId = composer.CreateTempMenu();

                //Save preview random key to session cookie so that we'll use
                //the same temp folder for all previews
                this.HttpContext.Session[Constants.SessionPreviewKey] = randomId;
            }

            ViewBag.Url = Utilities.GetFullUrlPreview(randomId);

            return View();
        }

        //
        // GET: /Menu/MenuBuilder
        // This is the start of the main menu builder. A new menu is created here.
        // *Access is open to all.*

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
        // *Access is open to all.*

        [HttpPost]
        public ActionResult MenuBuilder(MenuBuilderViewModel menuBuilderModel)
        {
            if (ModelState.IsValid)
            {
                //menu is inactive by default at initial creation;
                //but will be set to active if user decides to publish
                //at the end of the builder process.
                menuBuilderModel.CurrentMenu.Active = false;

                //create initial default location
                List<Location> defaultLocationList = new List<Location>() {
                    new Location() { Address = Composer.Constants.DefaultAddress,
                        City = menuBuilderModel.CurrentMenu.City,
                        Hours = Composer.Constants.DefaultHoursHtml,
                        MapLink = Utilities.CreateMapLink(string.Empty, menuBuilderModel.CurrentMenu.City, string.Empty),
                        MapImgUrl = Utilities.CreateMapImgLink(string.Empty, menuBuilderModel.CurrentMenu.City, string.Empty),
                        Twitter = Composer.Constants.DefaultWebsite,
                        Yelp = Composer.Constants.DefaultWebsite,
                        Facebook = Composer.Constants.DefaultWebsite,
                        Email = Composer.Constants.DefaultEmail
                    }
                };

                menuBuilderModel.CurrentMenu.Locations = V1.SerializeLocations(defaultLocationList);

                //set default if not supplied (so sample will look good)
                if (string.IsNullOrEmpty(menuBuilderModel.CurrentMenu.Website))
                {
                    menuBuilderModel.CurrentMenu.Website = Composer.Constants.DefaultWebsite;
                }
                if (string.IsNullOrEmpty(menuBuilderModel.CurrentMenu.Phone))
                {
                    menuBuilderModel.CurrentMenu.Phone = Composer.Constants.DefaultPhone;
                }

                //create initial default menu tree
                //default breakfast
                List<MenuNode> defaultBreakfastMenuLeafList = new List<MenuNode>(){
                    new MenuLeaf() { Title = "House Omelet (example)", Description = "Three eggs, cheese, sausage, onions", Price = 7 },
                    new MenuLeaf() { Title = "Pancakes (example)", Description = "Four flapjacks piled high, your choice of blueberry or banana", Price = 5 },
                    new MenuLeaf() { Title = "Latte (example)", Description = "Organic espresso, soy available", Price = 4.50m }
                };

                //default lunch
                List<MenuNode> defaultLunchMenuLeafList = new List<MenuNode>(){
                    new MenuLeaf() { Title = "Pizza (example)", Description = "Pepperoni, cheese, mushrooms", Price = 10 },
                    new MenuLeaf() { Title = "Bacon Cheeseburger (example)", Description = "Angus beef, made to order", Price = 7 },
                    new MenuLeaf() { Title = "Smoothie (example)", Description = "Strawberries, bananas, pineapples, protein powder", Price = 5 }
                };

                //default dinner
                List<MenuNode> defaultDinnerMenuLeafList = new List<MenuNode>(){
                    new MenuLeaf() { Title = "BBQ Ribs (example)", Description = "Slow cooked, house bbq sauce", Price = 14 },
                    new MenuLeaf() { Title = "Enchiladas (example)", Description = "Three cheese, chicken, or steak ", Price = 9.50m },
                    new MenuLeaf() { Title = "Magarita (example)", Description = "Fresh limes, triple sec, tequila", Price = 7 }
                };

                //default root
                List<MenuNode> defaultMenuNodeList = new List<MenuNode>(){
                    new MenuNode() { Title = "Breakfast (example)", Link = "1-1", Text = "Breakfast menu items (example)", Branches = defaultBreakfastMenuLeafList },
                    new MenuNode() { Title = "Lunch (example)", Link = "1-2", Text = "Lunch menu items (example)", Branches = defaultLunchMenuLeafList },
                    new MenuNode() { Title = "Dinner (example)", Link = "1-3", Text = "Dinner menu items (example)", Branches = defaultDinnerMenuLeafList }
                };

                menuBuilderModel.CurrentMenu.MenuTree = V1.SerializeMenuTree(defaultMenuNodeList);

                //create default About page values
                menuBuilderModel.CurrentMenu.AboutTitle = string.Format(Composer.Constants.DefaultAboutTitleFormat, menuBuilderModel.CurrentMenu.Name);
                menuBuilderModel.CurrentMenu.AboutText = string.Format(Composer.Constants.DefaultAboutTextFormat, menuBuilderModel.CurrentMenu.Name);

                //create menudart URL
                menuBuilderModel.CurrentMenu.MenuDartUrl = CreateMenuDartUrl(menuBuilderModel.CurrentMenu.Name, menuBuilderModel.CurrentMenu.City);

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

                //Reserve an empty, permanent location/URL
                V1 composer = new V1(menuBuilderModel.CurrentMenu);
                composer.CreateMenuDir();

                return RedirectToAction("MenuBuilder2", new { name = menuBuilderModel.CurrentMenu.Name, id = menuBuilderModel.CurrentMenu.ID });
            }

            return View(menuBuilderModel.CurrentMenu);
        }

        //
        // GET: /Menu/MenuBuilder2
        // 
        // Access is open to all, but jumping out of order and calling this
        // method doesn't create any menu.

        public ActionResult MenuBuilder2(string name, int id)
        {
            if (string.IsNullOrEmpty(name))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }
            else
            {
                //TODO: may want to validate name/url so that we they are from us and not
                //spoofed.
                ViewBag.Name = name;
                ViewBag.MenuId = id;

                return View();
            }
        }

        //
        // GET: /Menu/MenuBuilder3
        // 
        //Starting at this step, only authorized users can edit their menu

        [Authorize]
        public ActionResult MenuBuilder3(int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            ViewData["templateList"] = new SelectList(GetTemplates());

            return View();
        }

        //
        // POST: /Menu/MenuBuilder3

        [HttpPost]
        [ValidateInput(false)]
        [Authorize]
        public ActionResult MenuBuilder3(string template, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    Utilities.LogAppError("Could not find menu.");
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

                Utilities.CopyDirTo(templatesPath, indexFilesPath, true);

                return RedirectToAction("MenuBuilder4", new { id = id });
            }

            return View();
        }

        //
        // GET: /Menu/MenuBuilder4
        // 
        [Authorize]
        public ActionResult MenuBuilder4(int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            //check if logo exists for display
            string logoPath = HttpContext.Server.MapPath(Constants.MenusPath + menu.MenuDartUrl + "/" + Constants.IndexFilesDir + "/" + Constants.LogoFileName);

            if (System.IO.File.Exists(logoPath))
            {
                ViewBag.LogoUrl = Utilities.GetMenuLogoUrl(menu.MenuDartUrl);
            }

            return View(menu);
        }

        //
        // POST: /Menu/MenuBuilder4

        [HttpPost]
        [ValidateInput(false)]
        [Authorize]
        public ActionResult MenuBuilder4(Menu newMenu, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    Utilities.LogAppError("Could not find menu.");
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
        [Authorize]
        public ActionResult MenuBuilder5(int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

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
        [Authorize]
        public ActionResult MenuBuilder5(int numLocations, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            return RedirectToAction("MenuBuilder6", new { id = id, numLocations = numLocations });
        }

        //
        // GET: /Menu/MenuBuilder6
        // 
        [Authorize]
        public ActionResult MenuBuilder6(int numLocations, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            //Create empty location(s). Pre-populate the city since we already know it.
            List<Location> locations = new List<Location>();

            for (int x = 0; x < numLocations; x++)
            {
                Location newLocation = new Location();
                newLocation.City = menu.City;
                locations.Add(newLocation);
            }

            return View(locations);
        }

        //
        // POST: /Menu/MenuBuilder6

        [HttpPost]
        [ValidateInput(false)]
        [Authorize]
        public ActionResult MenuBuilder6(List<Location> locations, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    Utilities.LogAppError("Could not find menu.");
                    return HttpNotFound();
                }

                //create Google map links for each location
                foreach (Location location in locations)
                {
                    //add Google map link
                    location.MapLink = Utilities.CreateMapLink(location.Address, location.City, location.Zip);

                    //add Google map image link
                    location.MapImgUrl = Utilities.CreateMapImgLink(location.Address, location.City, location.Zip);
                }

                //set serialized locations directly into menu
                menu.Locations = V1.SerializeLocations(locations);

                //save menu to DB
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("MenuBuilder7", new { id = id, numLocations = locations.Count });
            }

            return View();
        }

        //
        // GET: /Menu/MenuBuilder7
        // 
        [Authorize]
        public ActionResult MenuBuilder7(int numLocations, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            //a location already exists, but let's just create an empty location
            //as a placeholder for the next view's data.
            List<Location> locations = new List<Location>();

            for (int x = 0; x < numLocations; x++)
            {
                Location newLocation = new Location();

                //pre-populate a sample "hours" text
                newLocation.Hours = Composer.Constants.DefaultHours;

                locations.Add(newLocation);
            }

            return View(locations);
        }

        //
        // POST: /Menu/MenuBuilder7

        [HttpPost]
        [ValidateInput(false)]
        [Authorize]
        public ActionResult MenuBuilder7(List<Location> locations, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    Utilities.LogAppError("Could not find menu.");
                    return HttpNotFound();
                }

                //get current locations
                List<Location> currentLocations = V1.DeserializeLocations(menu.Locations);

                for (int i = 0; i < currentLocations.Count; i++ )
                {
                    //write new data
                    currentLocations[i].Hours = locations[i].Hours;
                }

                //set serialized locations back into menu
                menu.Locations = V1.SerializeLocations(currentLocations);

                //save menu to DB
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("MenuBuilder8", new { id = id, numLocations = locations.Count });
            }

            return View();
        }

        //
        // GET: /Menu/MenuBuilder8
        // 
        [Authorize]
        public ActionResult MenuBuilder8(int numLocations, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            //a location already exists, but let's just create an empty location
            //as a placeholder for the next view's data.
            List<Location> locations = new List<Location>();

            for (int x = 0; x < numLocations; x++)
            {
                Location newLocation = new Location();
                locations.Add(newLocation);

                //pre-populate the phone number if it's already been filled at the menu-level.
                if (!string.IsNullOrEmpty(menu.Phone))
                {
                    newLocation.Phone = menu.Phone;
                }
            }

            return View(locations);
        }

        //
        // POST: /Menu/MenuBuilder8

        [HttpPost]
        [ValidateInput(false)]
        [Authorize]
        public ActionResult MenuBuilder8(List<Location> locations, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    Utilities.LogAppError("Could not find menu.");
                    return HttpNotFound();
                }

                //get current locations
                List<Location> currentLocations = V1.DeserializeLocations(menu.Locations);

                for (int i = 0; i < currentLocations.Count; i++)
                {
                    //write new data
                    currentLocations[i].Phone = locations[i].Phone;
                    currentLocations[i].Email = locations[i].Email;	
                    currentLocations[i].Facebook = locations[i].Facebook;
                    currentLocations[i].Twitter = locations[i].Twitter;
                    currentLocations[i].Yelp = locations[i].Yelp;
                }

                //set serialized locations back into menu
                menu.Locations = V1.SerializeLocations(currentLocations);

                //save menu to DB
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                //send to MenuTree builder (root level)
                return RedirectToAction("Details", "MenuTree", new { id = id, parent = 0, idx = -1, source = "MenuBuilder"});
            }

            return View();
        }

        //
        // GET: /Menu/MenuBuilder9
        // 
        [Authorize]
        public ActionResult MenuBuilder9(int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            ViewBag.MenuId = id;
            ViewBag.Name = menu.Name;

            return View();
        }

        //
        // GET: /Menu/MenuBuilderAccessViolation

        public ActionResult MenuBuilderAccessViolation()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public ActionResult LogoUpload(string qqfile, string menuDartUrl, bool replaceCurrent)
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
                    }
                    else
                    {
                        //Webkit, Mozilla
                    }

                    if (replaceCurrent)
                    {
                        file = Path.Combine(indexFilesPath, Constants.LogoFileName);
                    }
                    else
                    {
                        //save logo temporarily (until directed to replace)
                        file = Path.Combine(indexFilesPath, Constants.LogoTmpFileName);        
                    }

                    var buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    System.IO.File.WriteAllBytes(file, buffer);
                }
                catch (Exception ex)
                {
                    Utilities.LogAppError("File upload failed.", ex);
                    return Json(new { success = false, message = ex.Message }, "application/json");
                }

                return Json(new { success = true }, "text/html");
            }

            return Json(new { success = false, message = "No URL path." }, "application/json");
        }

        //
        // GET: /Menu/LogoReplace/5
        // replace current logo with temp one
        [Authorize]
        public ActionResult LogoReplace(int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            string indexFilesPath = HttpContext.Server.MapPath(Constants.MenusPath + menu.MenuDartUrl + "/" + Constants.IndexFilesDir);
            string currentLogo = Path.Combine(indexFilesPath, Constants.LogoFileName);
            string tempLogo = Path.Combine(indexFilesPath, Constants.LogoTmpFileName);

            if (System.IO.File.Exists(tempLogo))
            {
                System.IO.File.Copy(tempLogo, currentLogo, true);
                System.IO.File.Delete(tempLogo);
            }

            return RedirectToAction("Edit4", new { id = id });
        }

        //
        // GET: /Menu/LogoRemove/5
        // delete logo
        [Authorize]
        public ActionResult LogoRemove(string ReturnUrl, int id = 0)
        {
            if ((id == 0) || !Utilities.IsThisMyMenu(id, db, User))
            {
                return RedirectToAction("MenuBuilderAccessViolation");
            }

            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            string indexFilesPath = HttpContext.Server.MapPath(Constants.MenusPath + menu.MenuDartUrl + "/" + Constants.IndexFilesDir);
            string currentLogo = Path.Combine(indexFilesPath, Constants.LogoFileName);
            string tempLogo = Path.Combine(indexFilesPath, Constants.LogoTmpFileName);

            if (System.IO.File.Exists(tempLogo))
            {
                System.IO.File.Delete(tempLogo);
            }

            if (System.IO.File.Exists(currentLogo))
            {
                System.IO.File.Delete(currentLogo);
            }

            return RedirectToAction(ReturnUrl, new { id = id });
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        private string CreateMenuDartUrl(string name, string city)
        {
            //Create unique menudart URL          
            string tempUrl = (name.Replace(' ', '-') +
                "-" + city).ToLower();

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

            return tempUrl;
        }

        private List<string> GetTemplates()
        {
            //compile list of templates
            var templates = new List<string>();

            var templateQuery = from t in db.Templates
                                orderby t.Name
                                select t.Name;
            templates.AddRange(templateQuery.Distinct());

            return templates;
        }
    }
}