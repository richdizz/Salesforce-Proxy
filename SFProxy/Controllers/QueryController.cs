using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace SFProxy.Controllers
{
    public class QueryController : BaseApiController
    {
        public async Task<JObject> Get(string q)
        {
            JObject oResponse = JObject.Parse("{}");
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + base.AccessToken);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            using (HttpResponseMessage response = await client.GetAsync(new Uri(q)))
            {
                string json = await response.Content.ReadAsStringAsync();
                oResponse = JObject.Parse(json);
            }

            return oResponse;
        }
    }
}
