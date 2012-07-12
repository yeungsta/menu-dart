using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using MenuDart.Models;
using MenuDart.PayPalSvc;

namespace MenuDart.Controllers
{
    public class SubscriptionController : Controller
    {
        private MenuDartDBContext db = new MenuDartDBContext();

        //
        // GET: /Subscription/

        public ActionResult Index()
        {
            return View();
        }

        //
        // GET: /Subscription/Subscribe

        public ActionResult Subscribe(string email, int quantity)
        {
            string returnURL = Utilities.PrependUrl("/Subscription/Completed?subscribeAction=" + Constants.SubscribeAll + "&email=" + email + "&quantity=" + quantity);
            string cancelURL = Utilities.PrependUrl("/Subscription/Cancelled"); ;

            string token = ECSetExpressCheckout(email, returnURL, cancelURL, quantity);
            
            if (!string.IsNullOrEmpty(token))
            {
                return Redirect(Constants.PayPalExpressCheckoutUrlSandbox + token);
            }

            return RedirectToAction("Failed");
        }

        //
        // GET: /Subscription/Unsubscribe

        public ActionResult Unsubscribe(string email, int quantity)
        {
            UserInfo userInfo = GetUserInfo(email);

            //there must be a user info entry found
            if (userInfo == null)
            {
                return RedirectToAction("Failed");
            }

            ManageRecurringPaymentsProfileStatusResponseDetailsType profile = CancelRecurringPaymentsProfile(userInfo.PayPalProfileId);

            if ((profile != null) && (profile.ProfileID == userInfo.PayPalProfileId))
            {
                //profile status with user info
                userInfo.Subscribed = false;
                userInfo.PayPalProfileStatus = RecurringPaymentsProfileStatusType.CancelledProfile.ToString();

                //save changes to DB
                db.Entry(userInfo).State = EntityState.Modified;
                db.SaveChanges(); 

                return View();
            }

            return RedirectToAction("Failed");
        }

        //
        // GET: /Subscription/ModifySubscription

        public ActionResult ModifySubscription(string subscribeAction, string email, int quantity)
        {
            UserInfo userInfo = GetUserInfo(email);

            //there must be a user info entry found
            if (userInfo == null)
            {
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

                    //save changes to DB
                    db.Entry(userInfo).State = EntityState.Modified;
                    db.SaveChanges();

                    return View();
                }
                else
                {
                    //create a new, updated payment profile
                    string returnURL = Utilities.PrependUrl("/Subscription/Completed?subscribeAction=" + subscribeAction + "&email=" + email + "&quantity=" + quantity);
                    string cancelURL = Utilities.PrependUrl("/Subscription/Cancelled"); ;

                    string token = ECSetExpressCheckout(email, returnURL, cancelURL, quantity);

                    if (!string.IsNullOrEmpty(token))
                    {
                        return Redirect(Constants.PayPalExpressCheckoutUrlSandbox + token);
                    }

                    return RedirectToAction("Failed");
                }
            }

            return RedirectToAction("Failed");
        }

        //
        // GET: /Subscription/Completed
        // called by PayPal after a successful subscription signup

        public ActionResult Completed(string subscribeAction, string email, int quantity, string token)
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
                        return RedirectToAction("Failed");
                    }

                    //save profile ID and profile status with user info
                    userInfo.Subscribed = true;
                    userInfo.PayPalProfileId = profile.ProfileID;
                    userInfo.PayPalProfileStatus = profile.ProfileStatus.ToString();

                    //save changes to DB
                    db.Entry(userInfo).State = EntityState.Modified;
                    db.SaveChanges();

                    //for view display
                    ViewBag.Email = email;
                    ViewBag.ProfileId = profile.ProfileID;
                    ViewBag.ProfileStatus = profile.ProfileStatus.ToString();

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

        private CustomSecurityHeaderType CreateCredentials()
        {
            CustomSecurityHeaderType credentials = new CustomSecurityHeaderType
            {
                Credentials = new UserIdPasswordType
                {
                    Username = Constants.PayPalApiUsername,
                    Password = Constants.PayPalApiPassword,
                    Signature = Constants.PayPalApiSignature,

                }
            };

            return credentials;
        }

        private string ECSetExpressCheckout(string email, string returnURL, string cancelURL, int quantity)
        {
            using (PayPalAPIAAInterfaceClient client = new PayPalAPIAAInterfaceClient())
            {
                CustomSecurityHeaderType credentials = CreateCredentials();

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
                CustomSecurityHeaderType credentials = CreateCredentials();

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

        private ManageRecurringPaymentsProfileStatusResponseDetailsType CancelRecurringPaymentsProfile(string profileID)
        {
            using (PayPalAPIAAInterfaceClient client = new PayPalAPIAAInterfaceClient())
            {
                CustomSecurityHeaderType credentials = CreateCredentials();

                ManageRecurringPaymentsProfileStatusRequestDetailsType details = new ManageRecurringPaymentsProfileStatusRequestDetailsType();
                details.Action = StatusChangeActionType.Cancel;
                details.ProfileID = profileID;
                details.Note = "MenuDart subscription cancellation via MenuDart.com";

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

        private UpdateRecurringPaymentsProfileResponseDetailsType ECUpdateRecurringPaymentsProfile(string profileID, int quantity)
        {
            using (PayPalAPIAAInterfaceClient client = new PayPalAPIAAInterfaceClient())
            {
                CustomSecurityHeaderType credentials = CreateCredentials();

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
