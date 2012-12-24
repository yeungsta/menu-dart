using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using ActionMailer.Net.Mvc;
using MenuDart.Models;

namespace MenuDart.Controllers
{
    [Authorize]
    public class MailController : MailerBase
    {
        public EmailResult SendPasswordResetEmail(string email, string resetLink)
        {
            To.Add(email);
            From = "Menu Dart <" + Constants.ReplyEmail + ">";
            Subject = "Your Menu Dart Password Has Been Reset";
            //Attachments.Inline["corplogo.jpg"] = DocMgr.GetContentFileBytes("/images/corp-logo.jpg");

            SendPasswordResetEmailViewModel viewModel = new SendPasswordResetEmailViewModel();
            viewModel.Email = email;
            viewModel.ResetLink = resetLink;

            return Email("SendPasswordResetEmail", viewModel);
        }

        public EmailResult SendSignUpEmail(string email)
        {
            To.Add(email);
            From = "Menu Dart Welcome Team <" + Constants.SupportEmail + ">";
            Subject = "Welcome to Menu Dart!";

            return Email("SendSignUpEmail", email);
        }

        public EmailResult SendActivateEmail(string email, int amount, IList<MenuAndLink> menusJustActivated, IList<MenuAndLink> allActivatedMenus)
        {
            To.Add(email);
            From = "Menu Dart <" + Constants.SupportEmail + ">";
            Subject = "Your Menu Is Activated!";

            SendActivateEmailViewModel viewModel = new SendActivateEmailViewModel();
            viewModel.Email = email;
            viewModel.MonthlyBill = amount;
            viewModel.MenusJustActivated = menusJustActivated;
            viewModel.AllActivatedMenus = allActivatedMenus;

            return Email("SendActivateEmail", viewModel);
        }

        public EmailResult SendActivateEmailTrial(string email, IList<MenuAndLink> menusJustActivated)
        {
            To.Add(email);
            From = "Menu Dart <" + Constants.SupportEmail + ">";
            Subject = "Your Menu Is Activated!";

            SendActivateEmailViewModel viewModel = new SendActivateEmailViewModel();
            viewModel.Email = email;
            viewModel.MenusJustActivated = menusJustActivated;

            return Email("SendActivateEmailTrial", viewModel);
        }

        public EmailResult SendDeactivateEmail(string email, int amount, IList<MenuAndLink> remainingActiveMenus, IList<MenuAndLink> deactivatedMenus)
        {
            To.Add(email);
            From = "Menu Dart <" + Constants.SupportEmail + ">";
            Subject = "Your Menu Is Deactivated";

            SendDeactivateEmailViewModel viewModel = new SendDeactivateEmailViewModel();
            viewModel.Email = email;
            viewModel.MonthlyBill = amount;
            viewModel.RemainingActiveMenus = remainingActiveMenus;
            viewModel.DeactivatedMenus = deactivatedMenus;

            return Email("SendDeactivateEmail", viewModel);
        }

        public EmailResult SendFeedbackEmail(string email, string htmlfeedback)
        {
            //send to MenuDart support team
            To.Add(Constants.SupportEmail);
            From = email;
            Subject = "Customer Feedback from " + email;

            return Email("SendFeedbackEmail", htmlfeedback);
        }

        public EmailResult SendTrialExpiredEmail(string email, IList<MenuAndLink> deactivatedMenus)
        {
            To.Add(email);
            From = "Menu Dart <" + Constants.SupportEmail + ">";
            Subject = "Your 30-day free trial has expired. Activate your mobile menu!";

            SendTrialExpiredEmailViewModel viewModel = new SendTrialExpiredEmailViewModel();
            viewModel.Email = email;
            viewModel.DeactivatedMenus = deactivatedMenus;

            return Email("SendTrialExpiredEmail", viewModel);
        }

        //Email for when trial is about to expire
        public EmailResult SendTrialWarningEmail(string email, IList<MenuAndLink> deactivatedMenus)
        {
            To.Add(email);
            From = "Menu Dart <" + Constants.SupportEmail + ">";
            Subject = "Your 30-day free trial is expiring soon. Activate your mobile menu!";

            SendTrialExpiredEmailViewModel viewModel = new SendTrialExpiredEmailViewModel();
            viewModel.Email = email;
            viewModel.DeactivatedMenus = deactivatedMenus;

            return Email("SendTrialWarningEmail", viewModel);
        }
    }
}
