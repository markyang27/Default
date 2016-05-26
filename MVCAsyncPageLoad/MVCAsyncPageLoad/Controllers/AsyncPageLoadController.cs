using MVCAsyncPageLoad.Common;
using MVCAsyncPageLoad.Models;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Script.Serialization;

namespace MVCAsyncPageLoad.Controllers
{
    public class AsyncPageLoadController : Controller
    {
        const string Comma = ",";
        const string DefaultHttpMethod = "GET";
        const string Prefix = "HTTP";
        const string Sufix = "ATTRIBUTE";

        static readonly int _prefixLenth;
        static readonly int _sufixLenth;

        // Mapping from controller and action name to http method.
        static ConcurrentDictionary<string, string> _controller2HttpMethod;

        JavaScriptSerializer jsSerializer = new JavaScriptSerializer();

        static AsyncPageLoadController()
        {
            _prefixLenth = Prefix.Length;
            _sufixLenth = Sufix.Length;

            _controller2HttpMethod = new ConcurrentDictionary<string, string>();
        }
        
        /// <summary>
        /// Show loading pages to client, and invoke ~/returnController/returnAction/routeValues from client side via ajax.
        /// This action does not need authentication, because returnAction will perform auth check.
        /// </summary>
        /// <param name="returnAction">Action to be invoked by loading page.</param>
        /// <param name="returnController">Controller to be invoked by loading page.</param>
        /// <param name="routeValues">Parameters for loading page's ajax call.</param>
        /// <returns>result</returns>
        [AllowAnonymous]
        public ActionResult _Loading(string returnAction, string returnController, RouteValueDictionary routeValues)
        {
            AsyncPageLoadModel model = new AsyncPageLoadModel();

            // Generat unique id, as current page may have several children loading pages.
            model.DivContentID = Guid.NewGuid().ToString();

            model.HttpMethod = FindHttpMethod(returnAction, returnController, routeValues);

            // Pass parameter of 'GET' request via query string in URL.
            // So parameter is under restriction of URL max length.
            if (string.Equals(model.HttpMethod, "GET", StringComparison.InvariantCultureIgnoreCase))
            {
                model.TargetURL = Url.Encode(Url.Action(returnAction, returnController, routeValues));
            }
            // For other request types, send parameters in request body in Json format.
            else
            {
                model.TargetURL = Url.Encode(Url.Action(returnAction, returnController));
                model.ParametersInJSON = Url.Encode(DicToJson(routeValues));
            }

            return PartialView("~/Views/Shared/_Loading.cshtml", model);
        }


        /// <summary>
        /// Determine HTTP Method from Action method's attribute.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="controller"></param>
        /// <returns></returns>
        private string FindHttpMethod(string action, string controller, RouteValueDictionary routeValues)
        {
            // Defaultly set HTTP Method to "GET".
            string httpMethodStr = DefaultHttpMethod;

            IController ic = null;
            IControllerFactory icf = null;

            try
            {
                icf = ControllerBuilder.Current.GetControllerFactory();

                // If the requested page is under an area, 
                // meanwhile, user specified 'area' in routeValues parameter (as below),
                //      @{Html.RenderActionAsync("Header", "Home", new { p1 = "a", area = ""});}
                // since we always trigger _Loading action at root website (not inside any area), 
                // we cannot use routeData of current request context,
                // instead, here we need to create another routeData object for specified area.
                RouteData rt = GetRouteDataByArea(routeValues[RouteDataTokenKeys.Area] as string);
                RequestContext rc = new RequestContext(this.HttpContext, rt);

                ic = icf.CreateController(rc, controller);

                Type controllerType = ic.GetType();
                if (controllerType != null)
                {
                    string keyControllerAction = string.Format("{0}-{1}", controllerType.FullName, action);

                    string cachedHttpMethodStr;
                    if (!_controller2HttpMethod.TryGetValue(keyControllerAction, out cachedHttpMethodStr))
                    {
                        MethodInfo actionMI = controllerType.GetMethod(action, BindingFlags.Public | BindingFlags.Instance);
                        if (actionMI != null)
                        {
                            foreach (var attribute in actionMI.GetCustomAttributes(typeof(ActionMethodSelectorAttribute), false))
                            {
                                string fullAttributeTypeName = attribute.GetType().Name.ToUpperInvariant();

                                // Extract HTTP Method string from attribute name like "HttpPostAttribute".
                                // Ignore customized attribute that does not match "HttpxxxAttribute" format.
                                if (fullAttributeTypeName.StartsWith(Prefix) &&
                                    fullAttributeTypeName.EndsWith(Sufix))
                                {
                                    httpMethodStr = fullAttributeTypeName.Substring(
                                        _prefixLenth,
                                        fullAttributeTypeName.Length - (_prefixLenth + _sufixLenth));

                                    break;
                                }
                            }
                        }

                        _controller2HttpMethod.TryAdd(keyControllerAction, httpMethodStr);
                    }
                    else
                    {
                        httpMethodStr = cachedHttpMethodStr;
                    }
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (icf != null)
                {
                    if (ic != null)
                    {
                        // ReleaseController() internally call ic's Dispose method.
                        icf.ReleaseController(ic);
                        ic = null;
                    }

                    if (icf is IDisposable)
                    {
                        (icf as IDisposable).Dispose();
                    }
                    
                    icf = null;
                }
            }

            return httpMethodStr;
        }

        private RouteData GetRouteDataByArea(string area)
        {
            RouteData rt = null;

            foreach (Route route in RouteTable.Routes)
            {
                string realTypeName = route.GetType().FullName;

                // Filter out types HttpWebRoute and IgnoreRouteInternal.
                if (realTypeName == typeof(Route).FullName)
                {
                    if (route.DataTokens != null)
                    {
                        string dtArea = route.DataTokens[RouteDataTokenKeys.Area] as string;

                        if (dtArea == area || 
                            (string.IsNullOrEmpty(dtArea) && string.IsNullOrEmpty(area)))
                        {
                            rt = new RouteData(route, route.RouteHandler);

                            rt.DataTokens[RouteDataTokenKeys.Area] = dtArea;
                            rt.DataTokens[RouteDataTokenKeys.Namespaces] = route.DataTokens[RouteDataTokenKeys.Namespaces];
                            rt.DataTokens[RouteDataTokenKeys.Controller] = route.DataTokens[RouteDataTokenKeys.Controller];
                            rt.DataTokens[RouteDataTokenKeys.UseNamespaceFallback] = route.DataTokens[RouteDataTokenKeys.UseNamespaceFallback];

                            rt.DataTokens[RouteDataTokenKeys.Actions] = route.DataTokens[RouteDataTokenKeys.Actions];
                            rt.DataTokens[RouteDataTokenKeys.Order] = route.DataTokens[RouteDataTokenKeys.Order];
                            rt.DataTokens[RouteDataTokenKeys.TargetIsAction] = route.DataTokens[RouteDataTokenKeys.TargetIsAction];
                            rt.DataTokens[RouteDataTokenKeys.Precedence] = route.DataTokens[RouteDataTokenKeys.Precedence];
                            rt.DataTokens[RouteDataTokenKeys.DirectRouteMatches] = route.DataTokens[RouteDataTokenKeys.DirectRouteMatches];

                            break;
                        }
                    }
                }
            }

            return rt;
        }

        private string DicToJson(RouteValueDictionary dic)
        {
            StringBuilder json = new StringBuilder();

            if (dic != null && dic.Count > 0)
            {
                json.Append("{");

                foreach (var key in dic.Keys)
                {
                    json.AppendFormat("\"{0}\":\"{1}\"{2}", key, dic[key], Comma);
                }

                json.Remove(json.Length - Comma.Length, Comma.Length);

                json.Append("}");
            }

            return json.ToString();
        }

    }
}
