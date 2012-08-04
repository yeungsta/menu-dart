using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Net.Mail;
using MenuDart.Models;
using MenuDart.PayPalSvc;

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
                model.TrialEnded = userInfoList[0].TrialEnded;
                model.Subscribed = userInfoList[0].Subscribed;
                //coupon is active if the profile status is suspended while user is subscribed
                model.CouponActive = ((userInfoList[0].PayPalProfileStatus == RecurringPaymentsProfileStatusType.SuspendedProfile.ToString())
                    && (userInfoList[0].Subscribed));

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

            Utilities.LogAppError("Could not send out feedback from user dashbaord.");
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
