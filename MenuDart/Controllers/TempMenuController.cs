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
    //TODO: re-add admin when launched
    //[Authorize(Roles = "Administrator")]
    public class TempMenuController : Controller
    {
        private MenuDartDBContext db = new MenuDartDBContext();

        //
        // GET: /TempMenu/

        public ViewResult Index()
        {
            return View(db.TempMenus.ToList());
        }

        //
        // GET: /TempMenu/Details/5

        public ViewResult Details(int id)
        {
            TempMenu tempmenu = db.TempMenus.Find(id);
            return View(tempmenu);
        }

        //
        // GET: /TempMenu/Create

        public ActionResult Create()
        {
            return View();
        } 

        //
        // POST: /TempMenu/Create

        [HttpPost]
        public ActionResult Create(TempMenu tempmenu)
        {
            if (ModelState.IsValid)
            {
                db.TempMenus.Add(tempmenu);
                db.SaveChanges();
                return RedirectToAction("Index");  
            }

            return View(tempmenu);
        }
        
        //
        // GET: /TempMenu/Edit/5
 
        public ActionResult Edit(int id)
        {
            TempMenu tempmenu = db.TempMenus.Find(id);
            return View(tempmenu);
        }

        //
        // POST: /TempMenu/Edit/5

        [HttpPost]
        public ActionResult Edit(TempMenu tempmenu)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tempmenu).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tempmenu);
        }

        //
        // GET: /TempMenu/Delete/5
 
        public ActionResult Delete(int id)
        {
            TempMenu tempmenu = db.TempMenus.Find(id);
            return View(tempmenu);
        }

        //
        // POST: /TempMenu/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {            
            TempMenu tempmenu = db.TempMenus.Find(id);
            db.TempMenus.Remove(tempmenu);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        //
        // GET: /TempMenu/DeleteAll

        public ActionResult DeleteAll()
        {
            return View();
        }

        //
        // POST: /TempMenu/DeleteAll

        [HttpPost, ActionName("DeleteAll")]
        public ActionResult DeleteAllConfirmed()
        {
            //TODO: filter out all objects older than 48 hours
            //db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}