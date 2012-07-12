using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Net.Mail;
using MenuDart.Models;
using MenuDart.PayPalSvc;

namespace MenuDart.Controllers
{
    public class AccountController : Controller
    {
        private MenuDartDBContext db = new MenuDartDBContext();

        //
        // GET: /Account/LogOn

        public ActionResult LogOn(string ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;

            return View();
        }

        //
        // POST: /Account/LogOn

        [HttpPost]
        public ActionResult LogOn(LogOnModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                if (Membership.ValidateUser(model.Email, model.Password))
                {
                    //set temp menu to owner
                    MigrateSessionCart(model.Email);

                    FormsAuthentication.SetAuthCookie(model.Email, model.RememberMe);
                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        //send to Dashboard by default
                        return RedirectToAction("Index", "Dashboard");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "The email address or password provided is incorrect.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/LogOff

        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/Register

        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register

        [HttpPost]
        public ActionResult Register(RegisterModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                // Attempt to create the user account
                MembershipCreateStatus createStatus;
                Membership.CreateUser(model.Email, model.Password, model.Email, null, null, true, null, out createStatus);

                if (createStatus == MembershipCreateStatus.Success)
                {
                    //create user info entry
                    IOrderedQueryable<UserInfo> userFound = from userInfo in db.UserInfo
                                                            where userInfo.Name == model.Email
                                                            orderby userInfo.Name ascending
                                                            select userInfo;

                    if ((userFound == null) || (userFound.Count() == 0))
                    {
                        UserInfo newUserInfo = new UserInfo();
                        newUserInfo.Name = model.Email;
                        newUserInfo.Subscribed = false;
                        newUserInfo.TrialEnded = false;
                        newUserInfo.PayPalProfileId = string.Empty;
                        newUserInfo.PayPalProfileStatus = string.Empty;

                        db.UserInfo.Add(newUserInfo);
                        db.SaveChanges();
                    }

                    //set temp menu to owner
                    MigrateSessionCart(model.Email);

                    FormsAuthentication.SetAuthCookie(model.Email, false /* createPersistentCookie */);

                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError("", ErrorCodeToString(createStatus));
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ChangePassword

        [Authorize]
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Account/ChangePassword

        [Authorize]
        [HttpPost]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {

                // ChangePassword will throw an exception rather
                // than return false in certain failure scenarios.
                bool changePasswordSucceeded;
                try
                {
                    MembershipUser currentUser = Membership.GetUser(User.Identity.Name, true /* userIsOnline */);
                    changePasswordSucceeded = currentUser.ChangePassword(model.OldPassword, model.NewPassword);
                }
                catch (Exception)
                {
                    changePasswordSucceeded = false;
                }

                if (changePasswordSucceeded)
                {
                    return RedirectToAction("ChangePasswordSuccess", new { email = User.Identity.Name });
                }
                else
                {
                    ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ChangePasswordSuccess

        public ActionResult ChangePasswordSuccess(string email)
        {
            ViewBag.Email = email;

            return View();
        }

        //
        // GET: /Account/StartReset

        public ActionResult StartReset()
        {
            return View();
        }

        //
        // POST: /Account/StartReset

        [HttpPost]
        public ActionResult StartReset(StartResetModel model)
        {
            if (ModelState.IsValid)
            {
                MembershipUser currentUser = Membership.GetUser(model.Email, true /* userIsOnline */);

                if (currentUser != null)
                {
                    try
                    {
                        SendResetEmail(currentUser);
                        return RedirectToAction("StartResetSuccess");
                    }
                    catch (Exception e)
                    {
                        string hi = e.Message;
                        ModelState.AddModelError("", "The system could not email you your password reset. Please try again later or contact Support.");

                        //something failed, redisplay form
                        return View(model);
                    }
                }

                ModelState.AddModelError("", "The email address provided is not found. Please check again.");
            }

            //something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/StartResetSuccess

        public ActionResult StartResetSuccess()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword

        public ActionResult ResetPassword(string reset, string username)
        {
            if ((reset != null) && (username != null))
            {
                MembershipUser currentUser = Membership.GetUser(username);
                if (HashResetParams(currentUser.Email, currentUser.ProviderUserKey.ToString()) == reset)
                {
                    ResetPasswordModel model = new ResetPasswordModel();
                    model.TempPassword = currentUser.ResetPassword();
                    model.Email = currentUser.Email;
                    //save reset string in case user needs to do this over
                    model.Reset = reset;
                    return View(model);
                }
            }

            return RedirectToAction("ResetPasswordFailed");
        }

        //
        // POST: /Account/ResetPassword

        [HttpPost]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                bool resetPasswordSucceeded = false;

                try
                {
                    MembershipUser currentUser = Membership.GetUser(model.Email, true /* userIsOnline */);

                    //if temp password passed in is correct, then we will be able to update it:
                    resetPasswordSucceeded = currentUser.ChangePassword(model.TempPassword, model.NewPassword);
                }
                catch (Exception)
                {
                    resetPasswordSucceeded = false;
                }

                if (resetPasswordSucceeded)
                {
                    return RedirectToAction("ResetPasswordSuccess");
                }
                else
                {
                    ModelState.AddModelError("", "We could not reset your password. Please contact support.");
                }
            }

            // something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ResetPasswordSuccess

        public ActionResult ResetPasswordSuccess()
        {
            return View();
        }

        //
        // GET: /Account/ResetPasswordFailed

        public ActionResult ResetPasswordFailed()
        {
            return View();
        }

        public static void SendResetEmail(MembershipUser user)
        {
            MailMessage email = new MailMessage();

            email.From = new MailAddress("noreply@menudart.com");
            email.To.Add(new MailAddress(user.Email));

            email.Subject = "MenuDart Password Reset";
            email.IsBodyHtml = true;
            string link = Utilities.PrependUrl("/Account/ResetPassword/?username=" + user.Email + "&reset=" + HashResetParams(user.Email, user.ProviderUserKey.ToString()));
            email.Body = "<p>" + user.Email + ", please click the following link to reset your password at MenuDart.com:<p><a href='" + link + "'>" + link + "</a></p>";
            email.Body += "<p>If you did not request a password reset, you do not need to take any action.</p>";
            
            //TODO: configure SMTP host
            //SmtpClient smtpClient = new SmtpClient();          
            //smtpClient.Send(email);
            string tempFile = @"c:\\temp\\menudart_password_reset_email.htm";
            System.IO.File.WriteAllText(tempFile, email.Body);
        }

        //Method to hash parameters to generate the Reset URL
        public static string HashResetParams(string username, string guid)
        {
            byte[] bytesofLink = System.Text.Encoding.UTF8.GetBytes(username + guid);
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            string HashParams = BitConverter.ToString(md5.ComputeHash(bytesofLink)).Replace("-", "");

            return HashParams;
        }

        #region Status Codes
        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "Email address already exists. Please enter a different email address.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }
        #endregion

        private void MigrateSessionCart(string UserName)
        {
            // Associate shopping cart items with logged-in user
            var cart = SessionCart.GetCart(this.HttpContext);

            cart.MigrateMenu(UserName);
            Session[SessionCart.CartSessionKey] = UserName;
        }
    }
}
