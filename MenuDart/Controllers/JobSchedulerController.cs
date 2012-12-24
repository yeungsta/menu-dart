using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Web.Security;
using MenuDart.Models;

namespace MenuDart.Controllers
{
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
