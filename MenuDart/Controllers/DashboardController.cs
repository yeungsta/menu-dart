﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Net.Mail;
using MenuDart.Models;

namespace MenuDart.Controllers
{
    public class DashboardController : Controller
    {
        private MenuDartDBContext db = new MenuDartDBContext();

        //
        // GET: /Dashboard

        [Authorize]
        public ActionResult Index()
        {
            MembershipUser currentUser = null;

            try
            {
                currentUser = Membership.GetUser(User.Identity.Name, true /* userIsOnline */);
            }
            catch (Exception)
            {
                return HttpNotFound();
            }

            if (currentUser != null)
            {
                IOrderedQueryable<Menu> menus = from menu in db.Menus
                                                where menu.Owner == currentUser.Email
                                                orderby menu.Name ascending
                                                select menu;

                if (menus == null)
                {
                    return HttpNotFound();
                }

                ViewBag.UrlPath = Utilities.GetUrlPath();

                DashboardModel model = new DashboardModel();
                model.Menus = menus.ToList();
                model.Email = currentUser.Email;

                return View(model);
            }

            return HttpNotFound();
        }

        //
        // POST: /Dashboard

        [HttpPost]
        public ActionResult Index(DashboardModel model)
        {
            if (ModelState.IsValid)
            {
                SendFeedbackEmail(model.Email, model.Feedback);
                return RedirectToAction("SendFeedbackSuccess");
            }

            // something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Dashboard/SendFeedbackSuccess

        public ActionResult SendFeedbackSuccess()
        {
            return View();
        }

        private static void SendFeedbackEmail(string userEmail, string feedback)
        {
            string htmlFeedback = feedback.Replace(Constants.NewLine2, Constants.Break);

            MailMessage email = new MailMessage();

            email.From = new MailAddress(userEmail);
            email.To.Add(new MailAddress(Constants.SupportEmailAddress));
            email.Subject = htmlFeedback.Substring(0, 15);
            email.IsBodyHtml = true;
            email.Body = userEmail + " says:<br><br>" + htmlFeedback;

            //TODO: configure SMTP host
            //SmtpClient smtpClient = new SmtpClient();          
            //smtpClient.Send(email);
            string tempFile = @"c:\\temp\\menudart_customer_feedback.htm";
            System.IO.File.WriteAllText(tempFile, email.Body);
        }
    }
}