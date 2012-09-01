using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace MenuDart.Controllers
{
    //Helper for menu items that will add an "active" class if the calling controller
    //matches the controller of this menu item. Used for the top menu bar of the website.
    public static class MenuExtensions
    {
        public static MvcHtmlString MenuItem(
            this HtmlHelper htmlHelper,
            string text,
            string action,
            string controller
        )
        {
            var li = new TagBuilder("li");
            var routeData = htmlHelper.ViewContext.RouteData;
            var currentAction = routeData.GetRequiredString("action");
            var currentController = routeData.GetRequiredString("controller");
            if (string.Equals(currentAction, action, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(currentController, controller, StringComparison.OrdinalIgnoreCase))
            {
                li.AddCssClass("active");
            }
            li.InnerHtml = htmlHelper.ActionLink(text, action, controller).ToHtmlString();
            return MvcHtmlString.Create(li.ToString());
        }

        public static MvcHtmlString EditMenuItem(
            this HtmlHelper htmlHelper,
            string text,
            string action,
            string controller,
            object routeValues
        )
        {
            var routeData = htmlHelper.ViewContext.RouteData;
            var currentAction = routeData.GetRequiredString("action");
            var currentController = routeData.GetRequiredString("controller");
            if (string.Equals(currentAction, action, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(currentController, controller, StringComparison.OrdinalIgnoreCase))
            {
                return MvcHtmlString.Create(htmlHelper.ActionLink(text, action, controller, routeValues, new { @class = "activeEditorMenuItem" }).ToHtmlString());
            }

            return MvcHtmlString.Create(htmlHelper.ActionLink(text, action, controller, routeValues, null).ToHtmlString());
        }
    }
}