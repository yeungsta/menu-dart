using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using MenuDart.Models;
using MenuDart.Composer;
using MenuDart.PayPalSvc;

namespace MenuDart.Controllers
{
    public class SubscriptionController : Controller
    {
        private MenuDartDBContext db = new MenuDartDBContext();
        static private CustomSecurityHeaderType m_credentials;

        //
        // static constructor

        static SubscriptionController()
        {
            m_credentials = new CustomSecurityHeaderType
            {
                Credentials = new UserIdPasswordType
                {
                    Username = Constants.PayPalApiUsername,
                    Password = Constants.PayPalApiPassword,
                    Signature = Constants.PayPalApiSignature,
                }
            };
        }

        //
        // GET: /Subscription/

        public ActionResult Index()
        {
            return View();
        }

        //
        // GET: /Menu/Activate/5

        public ActionResult Activate(int id = 0)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            //find out how many active menus this owner already has
            IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(menu.Owner, db);
            if (allMenus == null) { return HttpNotFound(); }

            int activeCount = 0;

            foreach (Menu activeMenu in allMenus)
            {
                if (activeMenu.Active)
                {
                    activeCount++;
                }
            }

            //display to user what the new billing will be
            ViewBag.NumActiveMenus = activeCount + 1;
            ViewBag.NewTotal = (activeCount + 1) * 7;
            ViewBag.Email = menu.Owner;

            return View(menu);
        }

        //
        // POST: /Menu/Activate/5

        [HttpPost, ActionName("Activate")]
        public ActionResult ActivateConfirmed(int ActiveCount, string Email, int id = 0)
        {
            if (ActiveCount == 1)
            {
                //this is first active menu, so start subscription
                return RedirectToAction("Subscribe", "Subscription", new { id = id, subscribeAction = Constants.ActivateOne, email = Email, quantity = ActiveCount });
            }
            else
            {
                //there's already a previous active menu, so modify current subscription
                return RedirectToAction("ModifySubscription", "Subscription", new { id = id, subscribeAction = Constants.ActivateOne, email = Email, quantity = ActiveCount });
            }
        }

        //
        // GET: /Menu/ActivateAll/5

        public ActionResult ActivateAll(string email, int quantity)
        {
            //display to user what the billing will be
            ViewBag.NumActiveMenus = quantity;
            ViewBag.NewTotal = quantity * 7;
            ViewBag.Email = email;

            return View();
        }

        //
        // POST: /Menu/ActivateAll/5

        [HttpPost, ActionName("ActivateAll")]
        public ActionResult ActivateAllConfirmed(string Email, int Quantity)
        {
            return RedirectToAction("Subscribe", "Subscription", new { id = 0, subscribeAction = Constants.SubscribeAll, email = Email, quantity = Quantity });
        }

        //
        // GET: /Menu/Deactivate/5

        public ActionResult Deactivate(int id = 0)
        {
            Menu menu = db.Menus.Find(id);

            if (menu == null)
            {
                Utilities.LogAppError("Could not find menu.");
                return HttpNotFound();
            }

            //find out how many remaining active menus this owner has
            IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(menu.Owner, db);
            if (allMenus == null) { return HttpNotFound(); }

            int activeCount = 0;

            foreach (Menu activeMenu in allMenus)
            {
                if (activeMenu.Active)
                {
                    activeCount++;
                }
            }

            ViewBag.NumActiveMenus = activeCount - 1;
            ViewBag.NewTotal = (activeCount - 1) * 7;
            ViewBag.Email = menu.Owner;

            return View(menu);
        }

        //
        // POST: /Menu/Deactivate/5

        [HttpPost, ActionName("Deactivate")]
        public ActionResult DeactivateConfirmed(int ActiveCount, string Email, int id = 0)
        {
            //update PayPal subscription
            return RedirectToAction("ModifySubscription", "Subscription", new { id = id, subscribeAction = Constants.DeactivateOne, email = Email, quantity = ActiveCount });
        }

        //
        // GET: /Subscription/DeactivateAll
        //deactivate all menus and cancel billing agreement

        public ActionResult DeactivateAll(string email)
        {
            //find out how many active menus this owner has
            IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(email, db);
            if (allMenus == null) { return HttpNotFound(); }

            int activeCount = 0;

            foreach (Menu activeMenu in allMenus)
            {
                if (activeMenu.Active)
                {
                    activeCount++;
                }
            }

            ViewBag.NumActiveMenus = activeCount;
            ViewBag.Email = email;

            return View();
        }

        //
        // POST: /Menu/DeactivateAll/5

        [HttpPost, ActionName("DeactivateAll")]
        public ActionResult DeactivateAllConfirmed(string Email)
        {
            UserInfo userInfo = GetUserInfo(Email);

            //there must be a user info entry found
            if (userInfo == null)
            {
                Utilities.LogAppError("Can't retrieve user info.");
                return RedirectToAction("Failed");
            }

            ManageRecurringPaymentsProfileStatusResponseDetailsType profile = CancelRecurringPaymentsProfile(userInfo.PayPalProfileId);

            if ((profile != null) && (profile.ProfileID == userInfo.PayPalProfileId))
            {
                //profile status with user info
                userInfo.Subscribed = false;
                userInfo.PayPalProfileStatus = RecurringPaymentsProfileStatusType.CancelledProfile.ToString();

                db.Entry(userInfo).State = EntityState.Modified;

                //set all menu(s) this owner has as inactive
                IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(Email, db);
                if (allMenus == null) { return HttpNotFound(); }

                //list of menu names and links for confirmation email
                //remaining active menus will be empty since we're deactivating all
                IList<MenuAndLink> remainingActiveMenusAndLinks = new List<MenuAndLink>();
                IList<MenuAndLink> deactivatedMenusAndLinks = new List<MenuAndLink>();

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

                //save changes to DB
                db.SaveChanges();

                //send confirmation email to user
                try //TODO: remove for Production SMTP
                {
                    new MailController().SendDeactivateEmail(Email, 0, remainingActiveMenusAndLinks, deactivatedMenusAndLinks).Deliver();
                }
                catch
                {
                }

                return RedirectToAction("DeactivateAllCompleted");
            }

            Utilities.LogAppError("Canceling payment profile from PayPal failed.");
            return RedirectToAction("Failed");
        }

        //
        // GET: /Menu/Suspend
        //suspend subscription to all menu(s)

        public ActionResult Suspend(string email)
        {
            UserInfo userInfo = GetUserInfo(email);

            //there must be a user info entry found
            if (userInfo == null)
            {
                Utilities.LogAppError("Can't retrieve user info.");	
                return RedirectToAction("Failed");
            }

            ManageRecurringPaymentsProfileStatusResponseDetailsType profile = SuspendRecurringPaymentsProfile(userInfo.PayPalProfileId);

            if ((profile != null) && (profile.ProfileID == userInfo.PayPalProfileId))
            {
                //true because menus are still active even though the billing is suspended.
                userInfo.Subscribed = true; 
                userInfo.PayPalProfileStatus = RecurringPaymentsProfileStatusType.SuspendedProfile.ToString();

                db.Entry(userInfo).State = EntityState.Modified;
                //save changes to DB
                db.SaveChanges();

                return RedirectToAction("SuspendCompleted");
            }

            Utilities.LogAppError("Suspending payment profile from PayPal failed.");
            return RedirectToAction("Failed");
        }

        //
        // GET: /Menu/Unsuspend
        //un-suspend subscription to all menu(s)

        public ActionResult Unsuspend(string email)
        {
            UserInfo userInfo = GetUserInfo(email);

            //there must be a user info entry found
            if (userInfo == null)
            {
                Utilities.LogAppError("Can't retrieve user info.");	
                return RedirectToAction("Failed");
            }

            ManageRecurringPaymentsProfileStatusResponseDetailsType profile = UnsuspendRecurringPaymentsProfile(userInfo.PayPalProfileId);

            if ((profile != null) && (profile.ProfileID == userInfo.PayPalProfileId))
            {
                userInfo.Subscribed = true;
                userInfo.PayPalProfileStatus = RecurringPaymentsProfileStatusType.ActiveProfile.ToString();

                db.Entry(userInfo).State = EntityState.Modified;
                //save changes to DB
                db.SaveChanges();

                return RedirectToAction("UnsuspendCompleted");
            }

            Utilities.LogAppError("Unsuspending payment profile from PayPal failed.");
            return RedirectToAction("Failed");
        }

        //
        // GET: /Subscription/Subscribe

        public ActionResult Subscribe(int id, string subscribeAction, string email, int quantity)
        {
            string returnURL = Utilities.PrependUrl("/Subscription/Completed?subscribeAction=" + subscribeAction + "&id=" + id + "&email=" + email + "&quantity=" + quantity);
            string cancelURL = Utilities.PrependUrl("/Subscription/Cancelled");

            string token = ECSetExpressCheckout(email, returnURL, cancelURL, quantity);

            if (!string.IsNullOrEmpty(token))
            {
                return Redirect(Constants.PayPalExpressCheckoutUrlSandbox + token);
            }

            Utilities.LogAppError("Subscribe failed: Could not start Express Checkout on PayPal.");
            return RedirectToAction("Failed");
        }

        //
        // GET: /Subscription/ModifySubscription

        public ActionResult ModifySubscription(int id, string subscribeAction, string email, int quantity)
        {
            UserInfo userInfo = GetUserInfo(email);

            //there must be a user info entry found
            if (userInfo == null)
            {
                Utilities.LogAppError("Can't retrieve user info.");	
                return RedirectToAction("Failed");
            }

            //cancel current payment profile
            ManageRecurringPaymentsProfileStatusResponseDetailsType profile = CancelRecurringPaymentsProfile(userInfo.PayPalProfileId);

            if ((profile != null) && (profile.ProfileID == userInfo.PayPalProfileId))
            {
                //if there are no more active menus, just mark this account as unsubscribed.
                if (quantity == 0)
                {
                    //profile status with user info
                    userInfo.Subscribed = false;
                    userInfo.PayPalProfileStatus = RecurringPaymentsProfileStatusType.CancelledProfile.ToString();

                    db.Entry(userInfo).State = EntityState.Modified;

                    //get menu attributes
                    Menu menu = db.Menus.Find(id);

                    if (menu == null)
                    {
                        return HttpNotFound();
                    }

                    //if deactivating a menu, set as inactive in menu DB
                    if (subscribeAction == Constants.DeactivateOne)
                    {
                        //set menu as deactivated
                        menu.Active = false;
                        db.Entry(menu).State = EntityState.Modified;

                        //deactivate the menu directory (delete menu but not logo file)
                        Utilities.DeactivateDirectory(menu.MenuDartUrl);
                    }

                    //save changes to DB
                    db.SaveChanges();

                    //gather info for confirmation email
                    IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(email, db);
                    if (allMenus == null) { return HttpNotFound(); }

                    //list of menu names and links for confirmation email
                    //remaining active menus will be empty since we've deactivated the last one
                    IList<MenuAndLink> remainingActiveMenusAndLinks = new List<MenuAndLink>();
                    IList<MenuAndLink> deactivatedMenusAndLinks = new List<MenuAndLink>();

                    foreach (Menu singleMenu in allMenus)
                    {
                        if (!singleMenu.Active)
                        {
                            MenuAndLink item = new MenuAndLink();
                            item.MenuName = singleMenu.Name;
                            item.MenuLink = Utilities.GetFullUrl(singleMenu.MenuDartUrl);
                            deactivatedMenusAndLinks.Add(item);
                        }
                    }

                    //send confirmation email to user
                    try //TODO: remove for Production SMTP
                    {
                        new MailController().SendDeactivateEmail(email, quantity, remainingActiveMenusAndLinks, deactivatedMenusAndLinks).Deliver();
                    }
                    catch
                    {
                    }

                    //for view display
                    ViewBag.Name = menu.Name;

                    return View();
                }
                else
                {
                    //create a new, updated payment profile
                    string returnURL = Utilities.PrependUrl("/Subscription/Completed?subscribeAction=" + subscribeAction + "&id=" + id + "&email=" + email + "&quantity=" + quantity);
                    string cancelURL = Utilities.PrependUrl("/Subscription/Cancelled");

                    string token = ECSetExpressCheckout(email, returnURL, cancelURL, quantity);

                    if (!string.IsNullOrEmpty(token))
                    {
                        return Redirect(Constants.PayPalExpressCheckoutUrlSandbox + token);
                    }

                    Utilities.LogAppError("Modify subscription failed: Could not start Express Checkout on PayPal.");
                    return RedirectToAction("Failed");
                }
            }

            Utilities.LogAppError("Canceling payment profile from PayPal failed.");
            return RedirectToAction("Failed");
        }

        //
        // GET: /Subscription/Completed
        // called by PayPal after a successful subscription signup
        // Handles single activates/deactivates, and multi-activate

        public ActionResult Completed(string subscribeAction, int id, string email, int quantity, string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                CreateRecurringPaymentsProfileResponseDetailsType profile =
                    ECCreateRecurringPaymentsProfile(email, token, quantity);

                if (profile != null)
                {
                    UserInfo userInfo = GetUserInfo(email);

                    //there must be a user info entry found
                    if (userInfo == null)
                    {
                        Utilities.LogAppError("Can't retrieve user info.");	
                        return RedirectToAction("Failed");
                    }

                    //save profile ID and profile status with user info
                    userInfo.Subscribed = true;
                    userInfo.PayPalProfileId = profile.ProfileID;
                    userInfo.PayPalProfileStatus = profile.ProfileStatus.ToString();
   
                    db.Entry(userInfo).State = EntityState.Modified;

                    if (subscribeAction == Constants.SubscribeAll)
                    {
                        //set all menu(s) this owner has as active
                        IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(email, db);
                        if (allMenus == null) { return HttpNotFound(); }

                        //list of menu names and links for confirmation email
                        IList<MenuAndLink> justActivatedMenusAndLinks = new List<MenuAndLink>();
                        //not used by view when all menus are activated, so just keep empty
                        IList<MenuAndLink> totalActivatedMenusAndLinks = new List<MenuAndLink>();

                        foreach (Menu singleMenu in allMenus)
                        {
                            //set menu as active
                            singleMenu.Active = true;

                            V1 composer = new V1(singleMenu);
                            // re-compose the menu
                            string fullURL = composer.CreateMenu();

                            db.Entry(singleMenu).State = EntityState.Modified;

                            //info for confirmation email
                            MenuAndLink item = new MenuAndLink();
                            item.MenuName = singleMenu.Name;
                            item.MenuLink = fullURL;
                            justActivatedMenusAndLinks.Add(item);
                        }

                        //send confirmation email to user
                        try //TODO: remove for Production SMTP
                        {
                            new MailController().SendActivateEmail(email, quantity * Constants.CostPerMenu, justActivatedMenusAndLinks, totalActivatedMenusAndLinks).Deliver();
                        }
                        catch
                        {
                        }

                        //For default View() only
                        ViewBag.ProfileId = profile.ProfileID;
                        ViewBag.ProfileStatus = profile.ProfileStatus.ToString();
                    }
                    else 
                    {
                        Menu menu = db.Menus.Find(id);

                        if (menu == null)
                        {
                            Utilities.LogAppError("Could not retrieve menu.");
                            return HttpNotFound();
                        }

                        //if deactivating a menu, set as inactive in menu DB
                        if (subscribeAction == Constants.DeactivateOne)
                        {
                            //set menu as deactivated
                            menu.Active = false;

                            //deactivate the menu directory (delete menu but not logo file)
                            Utilities.DeactivateDirectory(menu.MenuDartUrl);

                            db.Entry(menu).State = EntityState.Modified;

                            //get status of all menus this owner has
                            IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(email, db);
                            if (allMenus == null) { return HttpNotFound(); }

                            //list of menu names and links for confirmation email
                            IList<MenuAndLink> remainingActiveMenusAndLinks = new List<MenuAndLink>();
                            IList<MenuAndLink> deactivatedMenusAndLinks = new List<MenuAndLink>();

                            foreach (Menu singleMenu in allMenus)
                            {
                                MenuAndLink item = new MenuAndLink();
                                item.MenuName = singleMenu.Name;
                                item.MenuLink = Utilities.GetFullUrl(singleMenu.MenuDartUrl);

                                if (singleMenu.Active)
                                {
                                    remainingActiveMenusAndLinks.Add(item);
                                }
                                else
                                {
                                    deactivatedMenusAndLinks.Add(item);
                                }
                            }

                            //send confirmation email to user
                            try //TODO: remove for Production SMTP
                            {
                                new MailController().SendDeactivateEmail(email, quantity * Constants.CostPerMenu, remainingActiveMenusAndLinks, deactivatedMenusAndLinks).Deliver();
                            }
                            catch
                            {
                            }
                        }
                        else if (subscribeAction == Constants.ActivateOne)
                        {
                            //set menu as active
                            menu.Active = true;

                            V1 composer = new V1(menu);
                            // re-compose the menu
                            string fullURL = composer.CreateMenu();

                            db.Entry(menu).State = EntityState.Modified;

                            //for confirmation email
                            IList<MenuAndLink> justActivatedMenusAndLinks = new List<MenuAndLink>();
                            IList<MenuAndLink> totalActivatedMenusAndLinks = new List<MenuAndLink>();

                            MenuAndLink item = new MenuAndLink();
                            item.MenuName = menu.Name;
                            item.MenuLink = fullURL;
                            justActivatedMenusAndLinks.Add(item);

                            //get total list of activated menus
                            IOrderedQueryable<Menu> allMenus = Utilities.GetAllMenus(email, db);
                            if (allMenus == null) { return HttpNotFound(); }

                            foreach (Menu singleMenu in allMenus)
                            {
                                item = new MenuAndLink();
                                item.MenuName = singleMenu.Name;
                                item.MenuLink = Utilities.GetFullUrl(singleMenu.MenuDartUrl);

                                if (singleMenu.Active)
                                {
                                    totalActivatedMenusAndLinks.Add(item);
                                }
                            }

                            //send confirmation email to user
                            try //TODO: remove for Production SMTP
                            {
                                new MailController().SendActivateEmail(email, quantity * Constants.CostPerMenu, justActivatedMenusAndLinks, totalActivatedMenusAndLinks).Deliver();
                            }
                            catch
                            {
                            }
                        }

                        //for view display. Use TempData since we're
                        //using RedirectToAction() as opposed to View()
                        TempData["Id"] = id;
                        TempData["Name"] = menu.Name;
                        TempData["Url"] = Utilities.GetFullUrl(menu.MenuDartUrl);
                    }

                    //save all changes to DB
                    db.SaveChanges();

                    //send to appropriate view
                    switch (subscribeAction)
                    {
                        case Constants.ActivateOne:
                            return RedirectToAction("ActivateCompleted");
                        case Constants.DeactivateOne:
                            return RedirectToAction("DeactivateCompleted");
                        default:
                            return View();
                    }
                }
            }

            Utilities.LogAppError("Could not complete transaction on PayPal.");
            return RedirectToAction("Failed");
        }

        //
        // GET: /Subscription/Cancelled

        public ActionResult Cancelled()
        {
            return View();
        }

        //
        // GET: /Subscription/Failed

        public ActionResult Failed()
        {
            return View();
        }

        //
        // GET: /Subscription/ActivateCompleted

        public ActionResult ActivateCompleted()
        {
            return View();
        }

        //
        // GET: /Subscription/DeactivateCompleted

        public ActionResult DeactivateCompleted()
        {
            return View();
        }

        //
        // GET: /Subscription/DeactivateAllCompleted

        public ActionResult DeactivateAllCompleted()
        {
            return View();
        }

        //
        // GET: /Subscription/SuspendCompleted

        public ActionResult SuspendCompleted()
        {
            return View();
        }

        //
        // GET: /Subscription/UnsuspendCompleted

        public ActionResult UnsuspendCompleted()
        {
            return View();
        }

        //
        // POST: /Subscription/PayPalIpnListener

        public ActionResult PayPalIpnListener()
        {
            //TODO: create IPN Listener

            return View();
        }

        //
        // GET: /Subscription/SyncAccounts
        //Gives a real-time comparison of active subscriptions with PayPal's database
        //to see if there are any discrepancies.

        public ActionResult SyncAccounts()
        {
            IEnumerable<UserInfo> userInfoList = db.UserInfo.AsEnumerable<UserInfo>();
            IList<SyncAccountsViewModel> userCancelList = new List<SyncAccountsViewModel>();

            foreach (UserInfo user in userInfoList)
            {
                if (!string.IsNullOrEmpty(user.PayPalProfileId))
                {
                    GetRecurringPaymentsProfileDetailsResponseDetailsType details = 
                        GetRecurringPaymentsProfileDetails(user.PayPalProfileId);

                    if (details != null)
                    {
                        //Generally the case we're looking for are subscribed users in 
                        //MenuDart that somehow actually have their PayPal subscription
                        //cancelled/expired/suspended.
                        if ((user.PayPalProfileStatus == RecurringPaymentsProfileStatusType.ActiveProfile.ToString()) &&
                            (user.PayPalProfileStatus != details.ProfileStatus.ToString()))
                        {
                            switch (details.ProfileStatus)
                            {
                                case RecurringPaymentsProfileStatusType.CancelledProfile:
                                    //user probably cancelled profile from PayPal.com
                                    //TODO: may want to send email to user to fix?
                                case RecurringPaymentsProfileStatusType.ExpiredProfile:
                                    //user's profile expired (shouldn't happen with our recurring profiles)
                                case RecurringPaymentsProfileStatusType.SuspendedProfile:
                                    //user's profile got suspended, maybe because of an invalid credit card, etc.?
                                    userCancelList.Add(new SyncAccountsViewModel { UserInfo = user, PayPalProfileStatus = details.ProfileStatus.ToString() });
                                    //TODO: may want to send email to user to ask if they really intended to cancel MenuDart?
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        //lookup to PayPal didn't work. Possible invalid profile ID.
                        Utilities.LogAppError("Could not find payment profile on PayPal.");
                        userCancelList.Add(new SyncAccountsViewModel { UserInfo = user, PayPalProfileStatus = details.ProfileStatus.ToString() });
                    }
                }
            }

            return View(userCancelList);
        }

        private CustomSecurityHeaderType GetCredentials()
        {
            if (m_credentials != null)
            {
                return m_credentials;
            }

            return null;
        }

        private string ECSetExpressCheckout(string email, string returnURL, string cancelURL, int quantity)
        {
            using (PayPalAPIAAInterfaceClient client = new PayPalAPIAAInterfaceClient())
            {
                CustomSecurityHeaderType credentials = GetCredentials();

                // Create the payment details.
                BasicAmountType basicAmount = new BasicAmountType();
                basicAmount.Value = (Constants.PayPalApiSubscriptionAmount).ToString();
                basicAmount.currencyID = CurrencyCodeType.USD;

                BasicAmountType orderAmount = new BasicAmountType();
                orderAmount.Value = (Constants.PayPalApiSubscriptionAmount * quantity).ToString();
                orderAmount.currencyID = CurrencyCodeType.USD;

                PaymentDetailsItemType paymentDetailsItem = new PaymentDetailsItemType();
                paymentDetailsItem.Amount = basicAmount;
                paymentDetailsItem.Description = Constants.PayPalApiSubscriptionDescription;
                paymentDetailsItem.ItemCategory = ItemCategoryType.Digital;
                paymentDetailsItem.Quantity = quantity.ToString();

                PaymentDetailsItemType[] paymentDetailsItemArray = new PaymentDetailsItemType[1];
                paymentDetailsItemArray[0] = paymentDetailsItem;

                PaymentDetailsType paymentDetails = new PaymentDetailsType();
                paymentDetails.OrderTotal = orderAmount;
                paymentDetails.ItemTotal = orderAmount;
                paymentDetails.OrderDescription = Constants.PayPalApiSubscriptionDescription;
                //paymentDetails.Recurring = RecurringFlagType.Y;
                //paymentDetails.PaymentAction = PaymentActionCodeType.Sale;
                paymentDetails.PaymentDetailsItem = paymentDetailsItemArray;

                PaymentDetailsType[] paymentDetailsArray = new PaymentDetailsType[1];
                paymentDetailsArray[0] = paymentDetails;

                BillingAgreementDetailsType billingAgreementDetails = new BillingAgreementDetailsType();
                billingAgreementDetails.BillingType = BillingCodeType.RecurringPayments;
                billingAgreementDetails.BillingAgreementDescription = Constants.PayPalApiSubscriptionDescription;

                BillingAgreementDetailsType[] billingAgreementDetailsArray = new BillingAgreementDetailsType[1];
                billingAgreementDetailsArray[0] = billingAgreementDetails;

                SetExpressCheckoutRequestDetailsType details = new SetExpressCheckoutRequestDetailsType();
                details.PaymentDetails = paymentDetailsArray;
                details.ReturnURL = returnURL;
                details.CancelURL = cancelURL;
                details.BuyerEmail = email;
                details.BillingAgreementDetails = billingAgreementDetailsArray;
                //details.PaymentAction = PaymentActionCodeType.Sale;
                //No shipping required
                details.ReqConfirmShipping = "0";
                //Buyer does not need to create a PayPal account to check out. This is referred to as PayPal Account Optional.
                details.SolutionType = SolutionTypeType.Sole;
                details.LandingPage = LandingPageType.Billing;
                //Determines where or not PayPal displays shipping address fields on the PayPal pages. For digital goods, this field is required, and you must set it to 1.
                details.NoShipping = "1";

                SetExpressCheckoutRequestType reqType = new SetExpressCheckoutRequestType();
                reqType.SetExpressCheckoutRequestDetails = details;
                reqType.Version = Constants.PayPalApiVersion;

                // Create the request object.
                SetExpressCheckoutReq request = new SetExpressCheckoutReq();
                request.SetExpressCheckoutRequest = reqType;

                SetExpressCheckoutResponseType response = client.SetExpressCheckout(ref credentials, request);

                if (response.Ack == AckCodeType.Success)
                {
                    return response.Token;
                }
            }

            return string.Empty;
        }

        private CreateRecurringPaymentsProfileResponseDetailsType ECCreateRecurringPaymentsProfile(string email, string token, int quantity)
        {
            using (PayPalAPIAAInterfaceClient client = new PayPalAPIAAInterfaceClient())
            {
                CustomSecurityHeaderType credentials = GetCredentials();

                // Create the payment details.
                BasicAmountType basicAmount = new BasicAmountType();
                basicAmount.Value = (Constants.PayPalApiSubscriptionAmount * quantity).ToString();
                basicAmount.currencyID = CurrencyCodeType.USD;

                PaymentDetailsItemType paymentDetailsItem = new PaymentDetailsItemType();
                paymentDetailsItem.Amount = basicAmount;
                paymentDetailsItem.Quantity = quantity.ToString();
                paymentDetailsItem.ItemCategory = ItemCategoryType.Digital;
                paymentDetailsItem.Description = Constants.PayPalApiSubscriptionDescription;

                PaymentDetailsItemType[] paymentDetailsItemArray = new PaymentDetailsItemType[1];
                paymentDetailsItemArray[0] = paymentDetailsItem;

                RecurringPaymentsProfileDetailsType profileDetails = new RecurringPaymentsProfileDetailsType();
                profileDetails.BillingStartDate = DateTime.UtcNow;
                profileDetails.SubscriberName = email;

                BillingPeriodDetailsType billingPeriod = new BillingPeriodDetailsType();
                billingPeriod.Amount = basicAmount;
                billingPeriod.BillingPeriod = BillingPeriodType.Month;
                billingPeriod.BillingFrequency = 1;

                ScheduleDetailsType scheduleDetails = new ScheduleDetailsType();
                scheduleDetails.PaymentPeriod = billingPeriod;
                scheduleDetails.Description = Constants.PayPalApiSubscriptionDescription;
                scheduleDetails.MaxFailedPayments = Constants.PayPalApiMaxFailedPayments;
                scheduleDetails.AutoBillOutstandingAmount = AutoBillType.NoAutoBill;

                CreateRecurringPaymentsProfileRequestDetailsType details = new CreateRecurringPaymentsProfileRequestDetailsType();
                details.RecurringPaymentsProfileDetails = profileDetails;
                details.ScheduleDetails = scheduleDetails;
                details.Token = token;
                details.PaymentDetailsItem = paymentDetailsItemArray;

                CreateRecurringPaymentsProfileRequestType reqType = new CreateRecurringPaymentsProfileRequestType();
                reqType.CreateRecurringPaymentsProfileRequestDetails = details;
                reqType.Version = Constants.PayPalApiVersion;

                // Create the request object.
                CreateRecurringPaymentsProfileReq request = new CreateRecurringPaymentsProfileReq();
                request.CreateRecurringPaymentsProfileRequest = reqType;

                CreateRecurringPaymentsProfileResponseType response = client.CreateRecurringPaymentsProfile(ref credentials, request);

                if (response.Ack == AckCodeType.Success)
                {
                    return response.CreateRecurringPaymentsProfileResponseDetails;
                }
            }

            return null;
        }

        private ManageRecurringPaymentsProfileStatusResponseDetailsType ECManageRecurringPaymentsProfileStatus(StatusChangeActionType statusAction, string note, string profileID)
        {
            using (PayPalAPIAAInterfaceClient client = new PayPalAPIAAInterfaceClient())
            {
                CustomSecurityHeaderType credentials = GetCredentials();

                ManageRecurringPaymentsProfileStatusRequestDetailsType details = new ManageRecurringPaymentsProfileStatusRequestDetailsType();
                details.Action = statusAction;
                details.ProfileID = profileID;
                details.Note = note;

                ManageRecurringPaymentsProfileStatusRequestType reqType = new ManageRecurringPaymentsProfileStatusRequestType();
                reqType.ManageRecurringPaymentsProfileStatusRequestDetails = details;
                reqType.Version = Constants.PayPalApiVersion;

                // Create the request object.
                ManageRecurringPaymentsProfileStatusReq request = new ManageRecurringPaymentsProfileStatusReq();
                request.ManageRecurringPaymentsProfileStatusRequest = reqType;

                ManageRecurringPaymentsProfileStatusResponseType response = client.ManageRecurringPaymentsProfileStatus(ref credentials, request);

                if (response.Ack == AckCodeType.Success)
                {
                    return response.ManageRecurringPaymentsProfileStatusResponseDetails;
                }
            }

            return null;
        }

        private ManageRecurringPaymentsProfileStatusResponseDetailsType CancelRecurringPaymentsProfile(string profileID)
        {
            return ECManageRecurringPaymentsProfileStatus(
                StatusChangeActionType.Cancel,
                "MenuDart subscription cancellation via MenuDart.com",
                profileID);
        }

        private ManageRecurringPaymentsProfileStatusResponseDetailsType SuspendRecurringPaymentsProfile(string profileID)
        {
            return ECManageRecurringPaymentsProfileStatus(
                StatusChangeActionType.Suspend,
                "MenuDart subscription suspended via MenuDart.com: " + "Courtesy 1-month coupon applied.",
                profileID);
        }

        private ManageRecurringPaymentsProfileStatusResponseDetailsType UnsuspendRecurringPaymentsProfile(string profileID)
        {
            return ECManageRecurringPaymentsProfileStatus(
                StatusChangeActionType.Reactivate,
                "MenuDart subscription un-suspended via MenuDart.com: " + "Courtesy 1-month coupon expired.",
                profileID);
        }

        private UpdateRecurringPaymentsProfileResponseDetailsType ECUpdateRecurringPaymentsProfile(string profileID, int quantity)
        {
            using (PayPalAPIAAInterfaceClient client = new PayPalAPIAAInterfaceClient())
            {
                CustomSecurityHeaderType credentials = GetCredentials();

                // Create the payment details.
                BasicAmountType basicAmount = new BasicAmountType();
                basicAmount.Value = (Constants.PayPalApiSubscriptionAmount * quantity).ToString();
                basicAmount.currencyID = CurrencyCodeType.USD;

                UpdateRecurringPaymentsProfileRequestDetailsType details = new UpdateRecurringPaymentsProfileRequestDetailsType();
                details.ProfileID = profileID;
                details.Note = "MenuDart subscription change via MenuDart.com";
                details.Amount = basicAmount;

                UpdateRecurringPaymentsProfileRequestType reqType = new UpdateRecurringPaymentsProfileRequestType();
                reqType.UpdateRecurringPaymentsProfileRequestDetails = details;
                reqType.Version = Constants.PayPalApiVersion;

                // Create the request object.
                UpdateRecurringPaymentsProfileReq request = new UpdateRecurringPaymentsProfileReq();
                request.UpdateRecurringPaymentsProfileRequest = reqType;

                UpdateRecurringPaymentsProfileResponseType response = client.UpdateRecurringPaymentsProfile(ref credentials, request);

                if (response.Ack == AckCodeType.Success)
                {
                    return response.UpdateRecurringPaymentsProfileResponseDetails;
                }
            }

            return null;
        }

        private GetRecurringPaymentsProfileDetailsResponseDetailsType GetRecurringPaymentsProfileDetails(string profileID)
        {
            using (PayPalAPIAAInterfaceClient client = new PayPalAPIAAInterfaceClient())
            {
                CustomSecurityHeaderType credentials = GetCredentials();

                GetRecurringPaymentsProfileDetailsRequestType reqType = new GetRecurringPaymentsProfileDetailsRequestType();
                reqType.ProfileID = profileID;
                reqType.Version = Constants.PayPalApiVersion;

                // Create the request object.
                GetRecurringPaymentsProfileDetailsReq request = new GetRecurringPaymentsProfileDetailsReq();
                request.GetRecurringPaymentsProfileDetailsRequest = reqType;

                GetRecurringPaymentsProfileDetailsResponseType response = client.GetRecurringPaymentsProfileDetails(ref credentials, request);

                if (response.Ack == AckCodeType.Success)
                {
                    return response.GetRecurringPaymentsProfileDetailsResponseDetails;
                }
            }

            return null;
        }

        private UserInfo GetUserInfo(string email)
        {
            //retrieve user info entry
            IOrderedQueryable<UserInfo> userFound = from userInfo in db.UserInfo
                                                    where userInfo.Name == email
                                                    orderby userInfo.Name ascending
                                                    select userInfo;

            //there must be a user info entry found
            if ((userFound == null) || (userFound.Count() == 0))
            {
                return null;
            }

            //should only be one in the list
            IList<UserInfo> userInfoList = userFound.ToList();

            return userInfoList[0];
        }
    }
}
