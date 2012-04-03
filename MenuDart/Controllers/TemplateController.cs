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
    [Authorize(Roles = "Administrator")]
    public class TemplateController : Controller
    {
        private MenuDartDBContext db = new MenuDartDBContext();

        //
        // GET: /Template/

        public ViewResult Index()
        {
            return View(db.Templates.ToList());
        }

        //
        // GET: /Template/Details/5

        public ActionResult Details(int id = 0)
        {
            Template template = db.Templates.Find(id);

            if (template == null)
            {
                return HttpNotFound();
            }

            return View(template);
        }

        //
        // GET: /Template/Create

        public ActionResult Create()
        {
            return View();
        } 

        //
        // POST: /Template/Create

        [HttpPost]
        public ActionResult Create(Template template)
        {
            if (ModelState.IsValid)
            {
                db.Templates.Add(template);
                db.SaveChanges();
                return RedirectToAction("Index");  
            }

            return View(template);
        }
        
        //
        // GET: /Template/Edit/5
 
        public ActionResult Edit(int id = 0)
        {
            Template template = db.Templates.Find(id);

            if (template == null)
            {
                return HttpNotFound();
            }

            return View(template);
        }

        //
        // POST: /Template/Edit/5

        [HttpPost]
        public ActionResult Edit(Template template)
        {
            if (ModelState.IsValid)
            {
                db.Entry(template).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(template);
        }

        //
        // GET: /Template/Delete/5
 
        public ActionResult Delete(int id = 0)
        {
            Template template = db.Templates.Find(id);

            if (template == null)
            {
                return HttpNotFound();
            }

            return View(template);
        }

        //
        // POST: /Template/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id = 0)
        {            
            Template template = db.Templates.Find(id);

            if (template == null)
            {
                return HttpNotFound();
            }

            db.Templates.Remove(template);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}