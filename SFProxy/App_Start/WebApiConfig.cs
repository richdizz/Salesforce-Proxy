using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json.Serialization;
using System.Web.Http.Cors;

namespace SFProxy
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            // Configure Web API to use only bearer token authentication.
            config.SuppressDefaultHostAuthentication();
            config.Filters.Add(new HostAuthenticationFilter(OAuthDefaults.AuthenticationType));

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            //enable CORs
            EnableCorsAttribute cors;
            cors = new EnableCorsAttribute("https://dreamforce.azurewebsites.net", "*", "*");
            cors.Origins.Add("https://o365workshop.azurewebsites.net");
            cors.Origins.Add("https://www.napacloudapp.com");
            cors.Origins.Add("http://localhost:4400");
            config.EnableCors(cors);
        }
    }
}
