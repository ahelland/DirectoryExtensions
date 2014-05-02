using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using CalendarSharing.Models;
using System.Management.Automation.Runspaces;
using CalendarSharing.Utils;

namespace CalendarSharing.Controllers
{
    public class HomeController : Controller
    {
        private const string TenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
        private const string LoginUrl = "https://login.windows.net/{0}";

        private const string ApiVersion = "1.21-preview";
        private const string GraphUrl = "https://graph.windows.net";
        private const string GraphUserUrl = "https://graph.windows.net/{0}/users/{1}?api-version=2013-04-05";
        private const string GraphUsersByTenantUrl = "https://graph.windows.net/{0}/users?api-version=" + ApiVersion;
        private const string GraphApps = "https://graph.windows.net/{0}/applications?api-version=" + ApiVersion;
        private const string GraphAppUrl = "https://graph.windows.net/{0}/applications/{1}/extensionProperties?api-version=" + ApiVersion;
        
        private static readonly string AppPrincipalId = ConfigurationManager.AppSettings["ida:ClientID"];
        private static readonly string AppKey = ConfigurationManager.AppSettings["ida:Password"];

        [Authorize]
        public async Task<ActionResult> Index()
        {
            string tenantId = ClaimsPrincipal.Current.FindFirst(TenantIdClaimType).Value;

            // Get a token for calling the Windows Azure Active Directory Graph
            AuthenticationContext authContext = new AuthenticationContext(String.Format(CultureInfo.InvariantCulture, LoginUrl, tenantId));
            ClientCredential credential = new ClientCredential(AppPrincipalId, AppKey);
            AuthenticationResult assertionCredential = authContext.AcquireToken(GraphUrl, credential);
            string authHeader = assertionCredential.CreateAuthorizationHeader();

            string appObjectId = await DirectoryExtensions.getAppObjectId(tenantId, authHeader);

            string extensionName = string.Empty;

            //Check if PublishedCalendar extension is registered by trying to get the id
            extensionName = await DirectoryExtensions.checkExtensionRegistered(tenantId, authHeader, appObjectId, "PublishedCalendarUrl");

            if (extensionName == "false")
            {
                ViewBag.UserCount = 0;
                return View();
            }

            string requestUrl = String.Format(
                CultureInfo.InvariantCulture,
                GraphUsersByTenantUrl,
                HttpUtility.UrlEncode(tenantId));

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
            HttpResponseMessage response = await client.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();
            UserContext userCtx = JsonConvert.DeserializeObject<UserContext>(responseString);

            List<UserDetails> users = new List<UserDetails>(userCtx.value);
            List<UserDetails> usersWithPublishedCalendar = new List<UserDetails>();
            foreach (var user in users)
            {
                //If a user doesn't have an Office 365 plan we can't use their calendar so we exclude them from the list
                if (user.assignedPlans.Count > 0)
                {
                    //If a user isn't assigned to the "Exchange" plan they don't have a calendar either
                    var exchangePlan = user.assignedPlans.Exists(e => e.service.Contains("exchange"));
                    if (exchangePlan)
                    {
                        user.PublishedCalendarUrl = await DirectoryExtensions.getExtensionValue(tenantId, authHeader, user.userPrincipalName, extensionName);
                        //If a user hasn't shared his calendar, don't add to list
                        if (user.PublishedCalendarUrl != null)
                            usersWithPublishedCalendar.Add(user);
                    }                    
                }                                
            }            

            ViewBag.UserCount = usersWithPublishedCalendar.Count;

            return View(usersWithPublishedCalendar);
        }        

        [Authorize]
        public async Task<ActionResult> PublishCalendars()
        {
            string tenantId = ClaimsPrincipal.Current.FindFirst(TenantIdClaimType).Value;

            // Get a token for calling the Windows Azure Active Directory Graph
            AuthenticationContext authContext = new AuthenticationContext(String.Format(CultureInfo.InvariantCulture, LoginUrl, tenantId));
            ClientCredential credential = new ClientCredential(AppPrincipalId, AppKey);
            AuthenticationResult assertionCredential = authContext.AcquireToken(GraphUrl, credential);
            string authHeader = assertionCredential.CreateAuthorizationHeader();
            string requestUrl = String.Format(
                CultureInfo.InvariantCulture,
                GraphUsersByTenantUrl,
                HttpUtility.UrlEncode(tenantId));                

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
            HttpResponseMessage response = await client.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();
            UserContext userCtx = JsonConvert.DeserializeObject<UserContext>(responseString);            

            List<UserDetails> usersInTenant = new List<UserDetails>(userCtx.value);
            List<UserDetails> usersWithCalendar = new List<UserDetails>();

            //Since the extension name isn't known in advance it's not included in the default serialization,
            //so we extract it manually after looking up the name.
            string appObjectId = await DirectoryExtensions.getAppObjectId(tenantId, authHeader);
            string extensionName = string.Empty;
            extensionName = await DirectoryExtensions.checkExtensionRegistered(tenantId, authHeader, appObjectId, "PublishedCalendarUrl");

            foreach (var user in usersInTenant)
            {
                //If a user doesn't have an Office 365 plan we can't use their calendar so we exclude them from the list
                if (user.assignedPlans.Count > 0)
                {
                    //If a user isn't assigned to the "Exchange" plan they don't have a calendar either
                    var exchangePlan = user.assignedPlans.Exists(e => e.service.Contains("exchange"));
                    if (exchangePlan)
                    {
                        user.PublishedCalendarUrl = await DirectoryExtensions.getExtensionValue(tenantId, authHeader, user.userPrincipalName, extensionName);                        
                        usersWithCalendar.Add(user);
                    }
                }
            }
            return View(usersWithCalendar);            
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> PublishCalendars(List<UserDetails> users)
        {
            //We create separate lists for users who will have a published calendar,
            //and those who will not have one (terming it unpublished)
            List<UserDetails> usersPublished = new List<UserDetails>();
            List<UserDetails> usersUnpublished = new List<UserDetails>();
            foreach(var item in users)
            {
                if (item.PublishedCal == true)
                {                    
                    usersPublished.Add(item);
                }
                else
                {                    
                    usersUnpublished.Add(item);
                }
            }

            //Powershell it
            //Connect to Exchange Online Using Remote PowerShell
            //http://technet.microsoft.com/en-US/library/jj984289.aspx
            
            WSManConnectionInfo o365Connection = Office365Remoting.getOffice365Connection();

            //Enable calendar for the "checked" users
            Office365Remoting.enablePublishedCalendars(usersPublished,o365Connection);
            //Disable for the "unchecked" users
            Office365Remoting.disablePublishedCalendars(usersUnpublished, o365Connection);

            //Retrieve the urls for the users who have a public calendar
            users = Office365Remoting.getCalendarUrls(usersPublished,o365Connection);

            //When presenting the view we want to display all users
            //Users without a public calendar on the lower part of the list
            users.AddRange(usersUnpublished);

            //If we've come this far we would like to add the values to our directory
            string tenantId = ClaimsPrincipal.Current.FindFirst(TenantIdClaimType).Value;

			// Get a token for calling the Windows Azure Active Directory Graph
			AuthenticationContext authContext = new AuthenticationContext(String.Format(CultureInfo.InvariantCulture, LoginUrl, tenantId));
			ClientCredential credential = new ClientCredential(AppPrincipalId, AppKey);
			AuthenticationResult assertionCredential = authContext.AcquireToken(GraphUrl, credential);
			string authHeader = assertionCredential.CreateAuthorizationHeader();

			string appObjectId = await DirectoryExtensions.getAppObjectId(tenantId, authHeader);

            string extensionName = string.Empty;

            //Check if PublishedCalendar extension is registered by trying to get the id
            extensionName = await DirectoryExtensions.checkExtensionRegistered(tenantId, authHeader, appObjectId, "PublishedCalendarUrl");

            if (extensionName == "false")
            {
                extensionName = await DirectoryExtensions.registerExtension(tenantId, authHeader, appObjectId, "PublishedCalendarUrl");
            }

            foreach (var user in users)
            {
                await DirectoryExtensions.setExtensionValue(tenantId, authHeader, user.userPrincipalName, extensionName, user.PublishedCalendarUrl);
            }

            return View(users);
        }
        
        

    }
}