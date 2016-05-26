using System.Linq;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;

namespace MVCAsyncPageLoad.Common
{
    /// Invoke the specified child action method asynchrously by ajax call.
    /// detailed code logic as below:
    /// 1. call ~/AsyncPageLoad/_Loading action which return a loading page to client.
    /// 2. after loading page rendered at client side, an ajax call will be triggered.
    /// 3. this ajax call make an invoke to actual action. 
    public static class ChildActionAsyncExtensions
    {
        const string DefaultAction = "_Loading";
        const string DefaultController = "AsyncPageLoad";
        const string TopArea = "";

        /// <summary>
        /// Invoke the specified child action method asynchrously by ajax call.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper instance that this method extends.</param>
        /// <param name="actionName">The name of the action method to invoke.</param>
        /// <param name="controllerName">The name of the controller that contains the action method.</param>
        /// <returns>The loading page as an HTML string.</returns>
        public static MvcHtmlString ActionAsync(this HtmlHelper htmlHelper, string actionName, string controllerName)
        {
            return ActionAsync(htmlHelper, actionName, controllerName, null);
        }

        /// <summary>
        /// Invoke the specified child action method asynchrously by ajax call.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper instance that this method extends.</param>
        /// <param name="actionName">The name of the action method to invoke.</param>
        /// <param name="controllerName">The name of the controller that contains the action method.</param>
        /// <param name="routeValues">An object that contains the parameters for a route.</param>
        /// <returns>The loading page as an HTML string.</returns>
        public static MvcHtmlString ActionAsync(this HtmlHelper htmlHelper, string actionName, string controllerName, object routeValues)
        {
            RouteData routeData = htmlHelper.ViewContext.Controller.ControllerContext.RouteData;

            RouteValueDictionary routesDic = GetAreaAwaredRouteValues(routeData, routeValues);

            MvcHtmlString htmlResult = htmlHelper.Action(
                DefaultAction,
                DefaultController,
                new
                {
                    returnAction = actionName,
                    returnController = controllerName,
                    routeValues = routesDic,
                    area = TopArea // Force _Loading() action execut at top area.
                });

            return htmlResult;
        }

        /// <summary>
        /// Invoke the specified child action method asynchrously by ajax call.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper instance that this method extends.</param>
        /// <param name="actionName">The name of the action method to invoke.</param>
        /// <param name="controllerName">The name of the controller that contains the action method.</param>
        /// <param name="routeValues">An object that contains the parameters for a route.</param>
        public static void RenderActionAsync(this HtmlHelper htmlHelper, string actionName, string controllerName)
        {
            RenderActionAsync(htmlHelper, actionName, controllerName, null);
        }

        /// <summary>
        /// Invoke the specified child action method asynchrously by ajax call.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper instance that this method extends.</param>
        /// <param name="actionName">The name of the action method to invoke.</param>
        /// <param name="controllerName">The name of the controller that contains the action method.</param>
        /// <param name="routeValues">An object that contains the parameters for a route.</param>
        public static void RenderActionAsync(this HtmlHelper htmlHelper, string actionName, string controllerName, object routeValues)
        {
            RouteData routeData = htmlHelper.ViewContext.Controller.ControllerContext.RouteData;

            RouteValueDictionary routesDic = GetAreaAwaredRouteValues(routeData, routeValues);

            htmlHelper.RenderAction(
                DefaultAction,
                DefaultController,
                new
                {
                    returnAction = actionName,
                    returnController = controllerName,
                    routeValues = routesDic,
                    area = TopArea
                });
        }

        /// <summary>
        /// Add support for area.
        /// </summary>
        /// <param name="routeData"></param>
        /// <param name="oriRouteValues"></param>
        /// <returns></returns>
        private static RouteValueDictionary GetAreaAwaredRouteValues(RouteData routeData, object oriRouteValues)
        {
            RouteValueDictionary routesDic = TypeHelper.ObjectToDictionary(oriRouteValues);

            return GetAreaAwaredRouteValues(routeData, routesDic);
        }

        private static RouteValueDictionary GetAreaAwaredRouteValues(RouteData routeData, RouteValueDictionary oriRouteValues)
        {
            if (!oriRouteValues.Keys.Contains<string>(RouteDataTokenKeys.Area))
            {
                string callingArea = routeData.DataTokens[RouteDataTokenKeys.Area] as string;

                oriRouteValues.Add(RouteDataTokenKeys.Area, callingArea);
            }

            return oriRouteValues;
        }


    }
}
