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
    [Authorize(Roles = "Administrator")]    //because there is no external interface needed to create Locations. All serialized in the Composer.
    public class LocationsController : Controller
    {
        private MenuDartDBContext db = new MenuDartDBContext();

        //
        // GET: /Locations/Details/5

        public ViewResult Details(int id)
        {
            Location location = db.Locations.Find(id);
            return View(location);
        }

        //
        // GET: /Locations/Create

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
        // POST: /Locations/Create

        [HttpPost]
        public ActionResult Create(List<Location> locations, int id)
        {
            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                //first deserialize current locations
                List<Location> currentLocations = Composer.V1.DeserializeLocations(menu.Locations);

                if ((currentLocations != null) && (currentLocations.Count > 0))
                {
                    //add all locations to the current set
                    currentLocations.AddRange(locations);

                    //serialize back into the menu
                    menu.Locations = Composer.V1.SerializeLocations(currentLocations);
                }
                else
                {
                    //no current locations, so just set serialized data directly into menu
                    menu.Locations = Composer.V1.SerializeLocations(locations);
                }

                //save menu
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Details", "Menu", new { id = id });
            }

            return View(locations);
        }
        
        //
        // GET: /Locations/Edit/5

        public ActionResult Edit(int id = 0)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            ViewBag.Restaurant = menu.Name;
            ViewBag.MenuId = id;

            List<Location> locations = Composer.V1.DeserializeLocations(menu.Locations); 
            return View(locations);
        }

        //
        // POST: /Locations/Edit/5

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(List<Location> locations, int id)
        {
            if (ModelState.IsValid)
            {
                Menu menu = db.Menus.Find(id);

                if (menu == null)
                {
                    return HttpNotFound();
                }

                //set serialized data into menu
                menu.Locations = Composer.V1.SerializeLocations(locations);

                //save menu
                db.Entry(menu).State = EntityState.Modified;
                db.SaveChanges();

                //re-compose the menu
                V1 composer = new V1(menu);
                composer.CreateMenu();

                return RedirectToAction("Edit", "Menu", new { id = id });
            }

            return View(locations);
        }

        //
        // GET: /Locations/Delete/5

        public ActionResult Delete(int id, int locationIdx)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            List<Location> locations = Composer.V1.DeserializeLocations(menu.Locations);
            ViewBag.LocationIdx = locationIdx;
            ViewBag.MenuId = id;

            return View(locations[locationIdx]);
        }

        //
        // POST: /Locations/Delete/5

        public ActionResult DeleteConfirmed(int id, int locationIdx)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                return HttpNotFound();
            }

            //deserialize current locations
            List<Location> currentLocations = Composer.V1.DeserializeLocations(menu.Locations);

            //delete location
            currentLocations.RemoveAt(locationIdx);

            //serialize back into the menu
            menu.Locations = Composer.V1.SerializeLocations(currentLocations);

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