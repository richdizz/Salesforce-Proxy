using Salesforce.Common;
using Salesforce.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;

namespace SFProxy.Salesforce
{
    /// <summary>
    /// An HttpTaskAsyncHandler that is used to handle the OAuth callback when making an authentication request to Salesforce.
    /// This handler is responsible for retrieving the access token from the authorization code Salesforce hands back
    /// on a successful authentication request.
    /// </summary>
    public class SalesforceOAuthRedirectHandler : HttpTaskAsyncHandler, IRequiresSessionState
    {
        /// <summary>
        /// Gets a value that indicates whether the task handler class instance can be reused for another
        /// asynchronous task.
        /// </summary>
        public override bool IsReusable
        {
            get { return true; }
        }

        /// <summary>
        /// Processes the authentication callback from Salesforce.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>The asynchronous task.</returns>
        public override async Task ProcessRequestAsync(HttpContext context)
        {
            await SalesforceService.AcquireTokenByAuthorizationCodeAsync(
                context.Request.QueryString["code"],
                SalesforceOAuthRedirectHandler.GetAbsoluteRedirectUri());

            string state = HttpUtility.UrlDecode(context.Request.QueryString["state"]);
            string redirectUrl = state == null ? "~/" : state;
            context.Response.Redirect(redirectUrl, false);
        }

        /// <summary>
        /// Gets the Salesforce authorization URL.  The optional target parameter allows the app to redirect to
        /// a specified page after authorization; if the parameter is not specified, the app redirects to the current
        /// request's URL.
        /// </summary>
        /// <returns>The Salesforce authorization URL.</returns>
        public static string GetAuthorizationUrl(string targetUri = null)
        {
            return Common.FormatAuthUrl(
                 SalesforceService.GetAppSetting("Salesforce:Domain") + "/services/oauth2/authorize",
                 ResponseTypes.Code,
                 SalesforceService.GetAppSetting("Salesforce:ConsumerKey"),
                 HttpUtility.UrlEncode(SalesforceOAuthRedirectHandler.GetAbsoluteRedirectUri()),
                 DisplayTypes.Page,
                 false,
                 HttpUtility.UrlEncode(string.IsNullOrEmpty(targetUri) ? HttpContext.Current.Request.Url.AbsoluteUri : targetUri));
        }

        /// <summary>
        /// Gets the absolute Uri to redirect to after Salesforce has completed authenticating a user.  This is the Uri
        /// to this handler.
        /// </summary>
        /// <returns>The absolute redirect Uri.</returns>
        private static string GetAbsoluteRedirectUri()
        {
            Uri redirectUri;
            Uri.TryCreate(SalesforceService.GetAppSetting("Salesforce:RedirectUri"), UriKind.RelativeOrAbsolute, out redirectUri);
            if (redirectUri.IsAbsoluteUri)
            {
                return redirectUri.ToString();
            }
            else
            {
                string uriAuthority = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
                return new Uri(new Uri(uriAuthority), redirectUri).ToString();
            }
        }
    }
}
