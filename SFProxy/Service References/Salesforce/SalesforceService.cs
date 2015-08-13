using Salesforce.Common;
using Salesforce.Common.Models;
using Salesforce.Force;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SFProxy.Salesforce
{
    /// <summary>
    /// Provides methods to interact with a Salesforce service.
    /// </summary>
    public static class SalesforceService
    {
        private static string TokenCacheKey = "SalesforceToken";

        /// <summary>
        /// Makes a request to the Salesforce client, wrapping the specified logic with the code necessary to handle authentication.
        /// </summary>
        /// <typeparam name="T">The type of value returned from the request.</typeparam>
        /// <param name="request">The request to make on the Salesforce client.</param>
        /// <exception cref="InvalidOperationException">The current user is not authenticated.</exception>
        /// <returns>The value returned by the request.</returns>
        public static async Task<T> MakeAuthenticatedClientRequestAsync<T>(Func<ForceClient, Task<T>> request)
        {
            bool done = false;
            bool refreshedToken = false;
            T result = default(T);

            do
            {
                // 1. Get the token from the cache
                SalesforceToken salesforceToken = HttpContext.Current.Session[SalesforceService.TokenCacheKey] as SalesforceToken;

                // 2. If no token is available, redirect to authorize
                if (salesforceToken == null)
                {
                    // This exception message should be used to trigger the sign-in UI
                    throw new InvalidOperationException("AuthorizationRequired");
                }

                // Initialize the ForceClient
                ForceClient forceClient = salesforceToken.GetForceClient();

                try
                {
                    // 3. Invoke the request with the acquired token
                    result = await request(forceClient);
                    done = true;
                }
                catch (ForceException e)
                {
                    // If message is "Session expired or invalid", the access token is invalid, so eat the
                    // exception and fall through to try to update it using the refresh token.
                    if (e.Message != "Session expired or invalid")
                    {
                        throw;
                    }

                    if (refreshedToken)
                    {
                        // The access token is invalid and was already refreshed once, give up and
                        // re-throw the exception.
                        throw;
                    }
                }

                // 4. If the token is invalid, attempt to acquire a new access token from the refresh token
                if (!done)
                {
                    await SalesforceService.AcquireAccessTokenFromRefreshTokenAsync(salesforceToken);
                    refreshedToken = true;
                }

            } while (!done);

            return result;
        }

        /// <summary>
        /// Gets a Salesforce token from an authorization code and adds it to the cache.
        /// </summary>
        /// <param name="authorizationCode">The code that was returned to the OAuth callback.</param>
        /// <param name="redirectUri">The redirect URI that was used to acquire the code.</param>
        /// <returns>The asynchronous task.</returns>
        public static async Task AcquireTokenByAuthorizationCodeAsync(string authorizationCode, string redirectUri)
        {
            using (AuthenticationClient authenticationClient = new AuthenticationClient())
            {
                await authenticationClient.WebServerAsync(
                    SalesforceService.GetAppSetting("Salesforce:ConsumerKey"),
                    SalesforceService.GetAppSetting("Salesforce:ConsumerSecret"),
                    redirectUri,
                    authorizationCode,
                    SalesforceService.GetAppSetting("Salesforce:Domain") + "/services/oauth2/token");

                SalesforceToken salesforceToken = new SalesforceToken(authenticationClient);

                // Add the new token to the cache
                HttpContext.Current.Session[SalesforceService.TokenCacheKey] = salesforceToken;
            }
        }

        /// <summary>
        /// Gets a Salesforce token based on the refresh token and adds it to the cache.
        /// </summary>
        /// <param name="salesforceToken">The Salesforce token that contains the refresh token to use.</param>
        /// <returns>The asynchronous task.</returns>
        private static async Task AcquireAccessTokenFromRefreshTokenAsync(SalesforceToken salesforceToken)
        {
            // Remove the old token from the cache
            HttpContext.Current.Session[SalesforceService.TokenCacheKey] = null;

            try
            {
                using (AuthenticationClient authenticationClient = salesforceToken.GetAuthenticationClient())
                {
                    // Attempt to refresh the token
                    await authenticationClient.TokenRefreshAsync(
                        SalesforceService.GetAppSetting("Salesforce:ConsumerKey"),
                        salesforceToken.RefreshToken,
                        string.Empty,
                        SalesforceService.GetAppSetting("Salesforce:Domain") + "/services/oauth2/token");

                    salesforceToken = new SalesforceToken(authenticationClient);

                    // Add the new token to the cache
                    HttpContext.Current.Session[SalesforceService.TokenCacheKey] = salesforceToken;
                }
            }
            catch (ForceException e)
            {
                // InvalidGrant means that the refresh token is invalid.
                // Just return in that case, so re-authorization can occur.
                if (e.Error != Error.InvalidGrant)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the value of the AppSetting with the specified name from the config file.
        /// </summary>
        /// <param name="name">The name of the AppSetting to retrieve.</param>
        /// <param name="isOptional">A Boolean value indicating whether or not the AppSetting is considered optional.</param>
        /// <exception cref="InvalidOperationException">If isOptional is set to false and the AppSetting is not found.</exception>
        /// <returns>
        /// The value of the AppSetting if found.  If isOptional is set to true and the AppSetting is not found, null is returned.
        /// </returns>
        internal static string GetAppSetting(string name, bool isOptional = false)
        {
            string setting = ConfigurationManager.AppSettings[name];
            if (!isOptional && (String.IsNullOrWhiteSpace(setting) || string.Equals(setting, "RequiredValue", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.InvariantCulture, "The value for the '{0}' key is missing from the appSettings section of the config file.", name));
            }
            else if (isOptional && (String.IsNullOrWhiteSpace(setting) || string.Equals(setting, "OptionalValue", StringComparison.OrdinalIgnoreCase)))
            {
                setting = null;
            }

            return setting;
        }

        /// <summary>
        /// Represents a Salesforce authentication token.
        /// </summary>
        private class SalesforceToken
        {
            /// <summary>
            /// Gets or sets the access token that can be used to make authenticated Salesforce service requests.
            /// </summary>
            public string AccessToken { get; set; }

            /// <summary>
            /// Gets or sets the refresh token that can be used to obtain a new access token.
            /// </summary>
            public string RefreshToken { get; set; }

            /// <summary>
            /// Gets or sets the Salesforce Service Url that was used to obtain this token.
            /// </summary>
            public string InstanceUrl { get; set; }

            /// <summary>
            /// Gets or sets the Salesforce API version that this token is valid for.
            /// </summary>
            public string ApiVersion { get; set; }

            /// <summary>
            /// Initializes a Salesforce token using an existing AuthenticationClient.
            /// </summary>
            /// <param name="authenticationClient">The AuthenticationClient from which to initialize the token.</param>
            /// <exception cref="ArgumentNullException">authenticationClient is null.</exception>
            public SalesforceToken(AuthenticationClient authenticationClient)
            {
                if (authenticationClient == null)
                {
                    throw new ArgumentNullException("authenticationClient");
                }

                this.AccessToken = authenticationClient.AccessToken;
                this.RefreshToken = authenticationClient.RefreshToken;
                this.InstanceUrl = authenticationClient.InstanceUrl;
                this.ApiVersion = authenticationClient.ApiVersion;
            }

            /// <summary>
            /// Get the AuthenticationClient from the Salesforce token.
            /// </summary>
            /// <returns>The new AuthenticationClient.</returns>
            public AuthenticationClient GetAuthenticationClient()
            {
                AuthenticationClient authenticationClient = new AuthenticationClient();
                authenticationClient.AccessToken = this.AccessToken;
                authenticationClient.RefreshToken = this.RefreshToken;
                authenticationClient.InstanceUrl = this.InstanceUrl;
                authenticationClient.ApiVersion = this.ApiVersion;

                return authenticationClient;
            }

            /// <summary>
            /// Get a ForceClient based on the Salesforce token.
            /// </summary>
            /// <returns>The new ForceClient.</returns>
            public ForceClient GetForceClient()
            {
                return new ForceClient(this.InstanceUrl, this.AccessToken, this.ApiVersion);
            }
        }
    }
}