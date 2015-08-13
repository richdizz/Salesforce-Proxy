using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace SFProxy.Controllers
{
    public class OAuthHub : Hub
    {
        public void Initialize()
        {

        }
        public void OAuthComplete(string clientID, string token)
        {
            Clients.Client(clientID).oAuthComplete(token);
        }
    }
}