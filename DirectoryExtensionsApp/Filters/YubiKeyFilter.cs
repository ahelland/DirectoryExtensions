using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Globalization;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using DirectoryExtensionsApp.Models;
using System.Net;
using System.Security.Claims;

namespace DirectoryExtensionsApp.Filters
{
    public class YubiKeyFilterAttribute : AuthorizeAttribute
    {        
        private const string LoginUrl = "https://login.windows.net/{0}";
        
        private const string GraphUrl = "https://graph.windows.net";
        private const string GraphApiVersion = "1.21-preview";
        private const string GraphUsersUrl = "https://graph.windows.net/{0}/users?api-version=" + GraphApiVersion;        

        private static readonly string AppPrincipalId = ConfigurationManager.AppSettings["ida:ClientID"];
        private static readonly string AppKey = ConfigurationManager.AppSettings["ida:Password"];
        private static readonly string ExtensionName = ConfigurationManager.AppSettings["ida:ExtensionName"];

        public string AuthorizedYubiKey { get; set; }
        public string tenantId { get; set; }

        public YubiKeyFilterAttribute(string tenantId)
        {
            this.tenantId = tenantId;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var clientYubiKeyId = httpContext.Request.QueryString["keyId"];
            var isAuthorized = IsAuthorizedYubiKeyId(clientYubiKeyId, tenantId);
            
            return isAuthorized;
        }

        //The default behavior is a 401 Unauthorized when failing the auth process.
        //This will trigger a new auth attempt, which is not what we want.
        //So we override and return 403 instead.
        protected override void HandleUnauthorizedRequest(System.Web.Mvc.AuthorizationContext filterContext)
        {
            filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden);
        }

        private static bool IsAuthorizedYubiKeyId(string YubiKeyId, string tenantId)
        {
            //If user has already been authenticated we will OK that without further processing.
            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(YubiKeyId))
            {                
                // Get a token for calling the Windows Azure Active Directory Graph
                AuthenticationContext authContext = new AuthenticationContext(String.Format(CultureInfo.InvariantCulture, LoginUrl, tenantId));
                ClientCredential credential = new ClientCredential(AppPrincipalId, AppKey);
                AuthenticationResult assertionCredential = authContext.AcquireToken(GraphUrl, credential);
                string authHeader = assertionCredential.CreateAuthorizationHeader();

                string requestUrl = String.Format(
                    CultureInfo.InvariantCulture,
                    GraphUsersUrl,
                    HttpUtility.UrlEncode(tenantId));
                
                //Only interested in the users with a matching YubiKeyID
                requestUrl += "&$filter=" + ExtensionName + " eq " + "'" + YubiKeyId + "'";

                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);
                HttpResponseMessage response = client.SendAsync(request).Result;
                
                string responseString = response.Content.ReadAsStringAsync().Result;
                                
                UserContext ctx = JsonConvert.DeserializeObject<UserContext>(responseString);
                //If we're not getting any results your id was not valid.
                if (ctx.value.Count == 0)
                {
                    return false;
                }

                UserDetails user = ctx.value[0];                                
                user.YubiKeyId = YubiKeyId;

                //Now that we know you're OK we'll create a new identity for you,
                //and attach it to the current context.
                ClaimsIdentity ci = new ClaimsIdentity(
                    "Federated", 
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", 
                    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
                Claim name = new Claim(
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", 
                    user.userPrincipalName);
                ci.AddClaim(name);                

                ClaimsPrincipal cp = new ClaimsPrincipal(ci);
                HttpContext.Current.User = cp;
                
                return true;
            }
            return false;
        }        
    }
}