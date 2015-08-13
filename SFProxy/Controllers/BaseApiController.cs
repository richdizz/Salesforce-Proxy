using SFProxy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Net.Http;
using System.Threading;
using System.Web.Http.Controllers;

namespace SFProxy.Controllers
{
    public class BaseApiController : ApiController
    {
        public string AccessToken { get; set; }

        public BaseApiController()
        {
            
        }

        public override Task<HttpResponseMessage> ExecuteAsync(HttpControllerContext controllerContext, CancellationToken cancellationToken)
        {
            //get header values for access_token and instance_url off the request
            string access_token = controllerContext.Request.Headers.GetValues("Authorization").FirstOrDefault();
            if (String.IsNullOrEmpty(access_token) || access_token.Length < 8)
                throw new UnauthorizedAccessException();
            else
                AccessToken = access_token.Substring(7);

            return base.ExecuteAsync(controllerContext, cancellationToken);
        }
    }
}
