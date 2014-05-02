using CalendarSharing.Models;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace CalendarSharing.Utils
{
    public static class DirectoryExtensions
    {
        private const string TenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
        private const string LoginUrl = "https://login.windows.net/{0}";

        private const string ApiVersion = "1.21-preview";
        private const string GraphUrl = "https://graph.windows.net";
        private const string GraphUserUrl = "https://graph.windows.net/{0}/users/{1}?api-version=" + ApiVersion;
        private const string GraphUsersUrl = "https://graph.windows.net/{0}/users?api-version=" + ApiVersion;
        private const string GraphUsersByTenantUrl = "https://graph.windows.net/{0}/users?api-version=" + ApiVersion;
        private const string GraphApps = "https://graph.windows.net/{0}/applications?api-version=" + ApiVersion;
        private const string GraphAppUrl = "https://graph.windows.net/{0}/applications/{1}/extensionProperties?api-version=" + ApiVersion;
        private const string GraphExtensionValueUrl = "https://graph.windows.net/{0}/users/{1}?api-version=" + ApiVersion;        
        private const string GraphExtensionUrl = "https://graph.windows.net/{0}/applications/{1}/extensionProperties?api-version=" + ApiVersion;        

        private static readonly string AppPrincipalId = ConfigurationManager.AppSettings["ida:ClientID"];
        private static readonly string AppKey = ConfigurationManager.AppSettings["ida:Password"];

        public static async Task<string> getAppObjectId(string tenantId, string authHeader)
        {
            string appObjectId = string.Empty;

            //Get the objectId for this particular app
            string requestUrl = String.Format(
                CultureInfo.InvariantCulture,
                GraphApps,
                HttpUtility.UrlEncode(tenantId));

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
            HttpResponseMessage response = await client.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();

            var apps = JsonConvert.DeserializeObject<AppContext>(responseString);

            //Iterate through the list to find the correct application,
            //and retrieve it's object id
            for (int i = 0; i < apps.value.Count; i++)
            {
                if (apps.value[i].appId == AppPrincipalId)
                {
                    appObjectId = apps.value[i].objectId;
                }
            }

            return appObjectId;
        }

        public static async Task<string> checkExtensionRegistered(string tenantId, string authHeader, string appObjectId, string extensionName)
        {
            //Get extensions for this app
            string requestUrl = String.Format(
                CultureInfo.InvariantCulture,
                GraphAppUrl,
                HttpUtility.UrlEncode(tenantId),
                HttpUtility.UrlEncode(appObjectId));

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
            HttpResponseMessage response = await client.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();

            var extensionproperties = JsonConvert.DeserializeObject<ExtensionPropertiesContext>(responseString);

            if (extensionproperties.value.Count == 0)
                return "false";
            else
            {                
                var extensions = extensionproperties.value;
                for (int i = 0; i < extensionproperties.value.Count; i++)
                {
                    if (extensionproperties.value[i].name.Contains(extensionName))
                        return extensionproperties.value[i].name;
                }
            }

            return "false";
        }

        public static async Task<string> registerExtension(string tenantId, string authHeader, string appObjectId, string extensionName)
        {
            //Get the objectId for this particular app
            string requestUrl = String.Format(
                CultureInfo.InvariantCulture,
                GraphExtensionUrl,
                HttpUtility.UrlEncode(tenantId),
                HttpUtility.UrlEncode(appObjectId));

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.ExpectContinue = false;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);

            request.Content = new StringContent("{\"name\": \"" + extensionName + "\",\"dataType\": \"String\",\"targetObjects\": [\"User\"]}", System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.Created)
            {
                var extension = JsonConvert.DeserializeObject<ExtensionProperty>(responseString);
                return extension.name;
            }
            else
                return string.Empty;
        }

        public static async Task<bool> setExtensionValue(string tenantId, string authHeader, string upn, string extensionName, string extensionValue)
        {
            //Get the objectId for this particular app
            string requestUrl = String.Format(
                CultureInfo.InvariantCulture,
                GraphExtensionValueUrl,
                HttpUtility.UrlEncode(tenantId),
                HttpUtility.UrlEncode(upn));

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.ExpectContinue = false;
            //PATCH isn't a default method
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUrl);
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);

            string extensionProperty = "{\"" + extensionName + "\":\"" + extensionValue + "\"}";

            request.Content = new StringContent(extensionProperty, System.Text.Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.SendAsync(request);
            string responseString = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return true;
            }
            else
                return false;
        }

        public static async Task<string> getExtensionValue(string tenantId, string authHeader, string upn, string extensionName)
        {            
            string requestUrl = String.Format(
                CultureInfo.InvariantCulture,
                GraphUserUrl,
                HttpUtility.UrlEncode(tenantId),
                HttpUtility.UrlEncode(upn));
            
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);
            HttpResponseMessage response = client.SendAsync(request).Result;

            string responseString = await response.Content.ReadAsStringAsync();
            
            Newtonsoft.Json.Linq.JObject jUser = Newtonsoft.Json.Linq.JObject.Parse(responseString);
            string calendarUrl = (string)jUser[extensionName];
            return calendarUrl;
        }

    }
}