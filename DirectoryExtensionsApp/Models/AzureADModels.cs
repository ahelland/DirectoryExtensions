using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;

namespace DirectoryExtensionsApp.Models
{
    public class UserContext
    {
        [JsonProperty("odata.metadata")]
        public string userContext { get; set; }

        [JsonProperty("value")]
        public List<UserDetails> value { get; set; }
    }

    public class UserDetails
    {
        [JsonProperty("objectId")]
        public string objectId { get; set; }

        [DisplayName("Display Name")]
        public string displayName { get; set; }
        [DisplayName("Given Name")]
        public string givenName { get; set; }
        [DisplayName("Surname")]
        public string surname { get; set; }
        [DisplayName("Job Title")]
        public string jobTitle { get; set; }
        [DisplayName("Department")]
        public string department { get; set; }
        [DisplayName("Mobile")]
        public string mobile { get; set; }
        [DisplayName("City")]
        public string city { get; set; }
        [DisplayName("Street Address")]
        public string streetAddress { get; set; }
        [DisplayName("Country")]
        public string country { get; set; }
        [DisplayName("Postal Code")]
        public string postalCode { get; set; }
        [DisplayName("Phone Number")]
        public string telephoneNumber { get; set; }
        [DisplayName("Email Address")]
        public string mail { get; set; }
        [DisplayName("UPN")]
        public string userPrincipalName { get; set; }
        [DisplayName("Last DirSync")]
        public string lastDirSyncTime { get; set; }
        [DisplayName("YubiKey ID")]
        public string YubiKeyId { get; set; }
    }

    public class AppContext
    {
        [JsonProperty("odata.metadata")]
        public string metadata { get; set; }
        public List<AppDetails> value { get; set; }
    }

    public class AppDetails
    {
        public string objectId { get; set; }
        public string appId { get; set; }
    }

    public class ExtensionPropertiesContext
    {
        [JsonProperty("odata.metadata")]
        public string metadata { get; set; }
        public List<ExtensionProperty> value { get; set; }
    }

    public class ExtensionProperty
    {
        public string objectId { get; set; }
        public string objectType { get; set; }
        public string name { get; set; }
        public string dataType { get; set; }
        [JsonProperty("odata.metadata")]
        public string odataMetadata { get; set; }
        [JsonProperty("odata.type")]
        public string odataType { get; set; }
        public List<string> targetObjects { get; set; }
    }
}