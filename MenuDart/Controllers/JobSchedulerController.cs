using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Web.Security;
using System.IO;
using MenuDart.Models;
using Stripe;
using System.Net;

namespace MenuDart.Controllers
{
    [Serializable]
    public class StripeHookCustomerDiscount
    {
        public StripeCoupon coupon { get; set; }
        public string customer { get; set; }
    }

    public class JobSchedulerController : Controller
    {
        private MenuDartDBContext db = new MenuDartDBContext();

        //
        // GET: /JobScheduler/

        //TODO: add a password parameter so that not anyone w/ URL can kickoff
        public ActionResult KickOff()
        {
            WarnUpcomingExpiringTrials();
            CheckForExpiredTrials();

            Utilities.LogAppError("Job scheduler was kicked-off!");

            return View();
        }

        [HttpPost]
        public ActionResult StripeWebhook()
        {
            // MVC3/4: Since Content-Type is application/json in HTTP POST from Stripe
            // we need to pull POST body from request stream directly
            Stream req = Request.InputStream;
            req.Seek(0, System.IO.SeekOrigin.Begin);

            string json = new StreamReader(req).ReadToEnd();
            StripeEvent stripeEvent = null;
 
            try
            {
                // as in header, you need https://github.com/jaymedavis/stripe.net
                // it's a great library that should have been offered by Stripe directly
                stripeEvent = StripeEventUtility.ParseEvent(json);
            }
            catch (Exception)
            {
                return new HttpStatusCodeResult((int)HttpStatusCode.BadRequest, "Unable to parse incoming event");
            }

            if (stripeEvent == null)
                return new HttpStatusCodeResult((int)HttpStatusCode.BadRequest, "Incoming event empty");

            switch (stripeEvent.Type)
            {
                case "customer.discount.created":
                    StripeHookCustomerDiscount stripeHookCustomerInfo = Stripe.Mapper<StripeHookCustomerDiscount>.MapFromJson(stripeEvent.Data.Object.ToString());

                    if ((stripeHookCustomerInfo != null) && (!string.IsNullOrEmpty(stripeHookCustomerInfo.customer)))
                    {
                        //lookup customer email
                        var customerService = new StripeCustomerService();
                        StripeCustomer stripeCustomer = customerService.Get(stripeHookCustomerInfo.customer);

                        if ((stripeCustomer != null) && (!string.IsNullOrEmpty(stripeCustomer.Email)))
                        //send email to user
                        try //TODO: remove for Production SMTP
                        {
                            new MailController().SendCouponAppliedEmail(stripeCustomer.Email).Deliver();
                        }
                        catch
                        {
                        }
                    }
                    break;
                case "customer.subscription.updated":
                case "customer.subscription.deleted":
                case "customer.subscription.created":
                    // do work
                    break;
            }

            return new HttpStatusCodeResult((int)HttpStatusCode.OK);
        }

        private void WarnUpcomingExpiringTrials()
        {
            IList<UserInfo> userList = GetAllUsersOnTrial();

            if (userList != null)
            {
                foreach (UserInfo user in userList)
                {
                    MembershipUser currentUser = null;

                    try
                    {
                        currentUser = Membership.GetUser(user.Name);
                    }
                    catch (Exception e)
                    {
                        Utilities.LogAppError("Could not retrieve user account.", e);
                    }

                    if (currentUser != null)
                    {
                        //check if trial period is nearing expiration
                        TimeSpan diff = DateTime.Today - currentUser.CreationDate.Date;

                        if (diff.Days >= Constants.TrialExpWarningDays)
                        {
                            //send warning email if user hasn't subscribed yet
                            if (!user.Subscribed && !user.TrialExpWarningSent)
                            {
                                //list of menu names and links for confirmation email
                                IList<MenuAndLink> deactivatedMenusAndLinks = new List<MenuAndLink>();

                                //find all user's menus that are in danger of being deactivated
                                IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(user.Name, db);

                                if (allMenus != null)
                                {
                                    foreach (Menu singleMenu in allMenus)
                                    {
                                        //info for confirmation email
                                        MenuAndLink item = new MenuAndLink();
                                        item.MenuName = singleMenu.Name;
                                        item.MenuLink = Utilities.GetFullUrl(singleMenu.MenuDartUrl);
                                        deactivatedMenusAndLinks.Add(item);
                                    }
                                }

                                //send warning email to user
                                try //TODO: remove for Production SMTP
                                {
                                    new MailController().SendTrialWarningEmail(user.Name, deactivatedMenusAndLinks).Deliver();

                                    //flag that we've sent a warning email already
                                    user.TrialExpWarningSent = true;
                                    db.Entry(user).State = EntityState.Modified;
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }

                //save all changes to DB
                db.SaveChanges();
            }
        }

        private void CheckForExpiredTrials()
        {
            IList<UserInfo> userList = GetAllUsersOnTrial();

            if (userList != null)
            {
                foreach (UserInfo user in userList)
                {
                    MembershipUser currentUser = null;

                    try
                    {
                        currentUser = Membership.GetUser(user.Name);
                    }
                    catch (Exception e)
                    {
                        Utilities.LogAppError("Could not retrieve user account.", e);
                    }

                    if (currentUser != null)
                    {
                        //check if trial period has expired (30 days from user account creation)
                        TimeSpan diff = DateTime.Today - currentUser.CreationDate.Date;

                        if (diff.Days >= Constants.TrialPeriodDays)
                        {
                            user.TrialEnded = true;
                            db.Entry(user).State = EntityState.Modified;

                            //deactivate menus if not subscribed
                            if (!user.Subscribed)
                            {
                                //list of menu names and links for confirmation email
                                IList<MenuAndLink> deactivatedMenusAndLinks = new List<MenuAndLink>();

                                //set all menu(s) this owner has as inactive
                                IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(user.Name, db);

                                if (allMenus != null)
                                {
                                    foreach (Menu singleMenu in allMenus)
                                    {
                                        //set menu as deactivated
                                        singleMenu.Active = false;

                                        //deactivate the menu directory (delete menu but not logo file)
                                        Utilities.DeactivateDirectory(singleMenu.MenuDartUrl);

                                        db.Entry(singleMenu).State = EntityState.Modified;

                                        //info for confirmation email
                                        MenuAndLink item = new MenuAndLink();
                                        item.MenuName = singleMenu.Name;
                                        item.MenuLink = Utilities.GetFullUrl(singleMenu.MenuDartUrl);
                                        deactivatedMenusAndLinks.Add(item);
                                    }
                                }

                                //send confirmation email to user
                                try //TODO: remove for Production SMTP
                                {
                                    new MailController().SendTrialExpiredEmail(user.Name, deactivatedMenusAndLinks).Deliver();
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }

                //save all changes to DB
                db.SaveChanges();
            }
        }

        private IList<UserInfo> GetAllUsersOnTrial()
        {
            //retrieve user info entry
            IOrderedQueryable<UserInfo> userFound = from userInfo in db.UserInfo
                                                    where userInfo.TrialEnded == false
                                                    orderby userInfo.Name ascending
                                                    select userInfo;

            if ((userFound == null) || (userFound.Count() == 0))
            {
                return null;
            }

            return userFound.ToList();
        }
    }
}
