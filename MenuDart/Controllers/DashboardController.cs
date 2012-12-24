using System;
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
            catch (Exception e)
            {
                Utilities.LogAppError("Could not retrieve user account.", e);
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
                    Utilities.LogAppError("Could not retrieve menu.");
                    return HttpNotFound();
                }

                //retrieve user info entry
                IOrderedQueryable<UserInfo> userFound = from userInfo in db.UserInfo
                                                        where userInfo.Name == currentUser.Email
                                                        orderby userInfo.Name ascending
                                                        select userInfo;

                //there must be a user info entry found
                if ((userFound == null) || (userFound.Count() == 0))
                {
                    Utilities.LogAppError("Could not retrieve user info.");
                    return HttpNotFound();
                }

                //should only be one in the list
                IList<UserInfo> userInfoList = userFound.ToList();

                DashboardModel model = new DashboardModel();
                model.Menus = menus.ToList();
                model.Email = currentUser.Email;
                model.ID = userInfoList[0].ID;
                model.TrialEnded = userInfoList[0].TrialEnded;
                model.Subscribed = userInfoList[0].Subscribed;

                //TODO: coupon is active if ?
                model.CouponActive = false;

                //convert creation date to Unix Time
                TimeSpan ts = (currentUser.CreationDate - new DateTime(1970, 1, 1, 0, 0, 0));
                model.SignUpDate = ts.TotalSeconds;

                //provide trial end date if applies
                if (!userInfoList[0].TrialEnded)
                {
                    TimeSpan trialDuration = new TimeSpan(Constants.TrialPeriodDays, 0, 0, 0);
                    DateTime? endDate = currentUser.CreationDate.Date + trialDuration;
                    model.TrialEndDate = endDate.Value.Date.ToShortDateString();
                }
                else
                {
                    model.TrialEndDate = null;
                }

                return View(model);
            }

            return HttpNotFound();
        }

        //
        // POST: /Dashboard

        [Authorize]
        [HttpPost]
        public ActionResult Index(DashboardModel model)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(model.Feedback))
                {
                    SendFeedbackEmail(model.Email, model.Feedback);
                    return RedirectToAction("SendFeedbackSuccess");
                }
            }

            // something failed or feedback was empty
            return RedirectToAction("SendFeedbackFail");
        }

        //
        // GET: /Dashboard/SendFeedbackSuccess

        [Authorize]
        public ActionResult SendFeedbackSuccess()
        {
            return View();
        }

        //
        // GET: /Dashboard/SendFeedbackFail

        [Authorize]
        public ActionResult SendFeedbackFail()
        {
            return View();
        }

        private static void SendFeedbackEmail(string userEmail, string feedback)
        {
            if (!string.IsNullOrEmpty(feedback))
            {
                string htmlFeedback = userEmail + " says:<br><br>" + feedback.Replace(Constants.NewLine2, Constants.Break);

                try //TODO: remove for Production SMTP
                {
                    new MailController().SendFeedbackEmail(userEmail, htmlFeedback).Deliver();
                }
                catch
                {
                }
            }
        }
    }
}
