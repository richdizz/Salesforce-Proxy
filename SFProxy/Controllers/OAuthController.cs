using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFProxy.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SFProxy.Controllers
{
    public class OAuthController : Controller
    {
        // GET: OAuth
        [Route("OAuth/AuthCode")]
        public async Task<ActionResult> AuthCode(string state)
        {
            string host = "https://o365workshop.azurewebsites.net";
#if DEBUG
            host = "https://localhost:44300";
#endif

            //Request should have a code from AAD and an id that represents the user in the data store
            if (String.IsNullOrEmpty(state))
                return RedirectToAction("Error", "Home", new { error = "State not passed back from authentication flow" });
            else if (Request["code"] == null)
                return RedirectToAction("Error", "Home", new { error = "Authorization code not passed from the authentication flow" });

            //Retrieve access token using authorization code
            TokenModel token = null;
            HttpClient client = new HttpClient();
            HttpContent content = new StringContent(String.Format(@"grant_type=authorization_code&redirect_uri={0}/OAuth/AuthCode&client_id={1}&client_secret={2}&code={3}", ConfigurationManager.AppSettings["Salesforce:ConsumerKey"], host, ConfigurationManager.AppSettings["Salesforce:ConsumerSecret"], Request["code"]));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
            using (HttpResponseMessage response = await client.PostAsync("https://login.salesforce.com/services/oauth2/token", content))
            {
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    JObject oResponse = JObject.Parse(json);
                    token = JsonConvert.DeserializeObject<TokenModel>(json);
                }
            }

            //notify the client through the hub
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<OAuthHub>();
            hubContext.Clients.Client(state).oAuthComplete(token);

            return View();
        }
    }
}