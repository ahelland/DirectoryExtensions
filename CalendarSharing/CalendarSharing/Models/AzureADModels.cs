using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;


namespace CalendarSharing.Models
{
    public class UserContext
    {
        [JsonProperty("odata.metadata")]
        public string userContext;


        [JsonProperty("value")]
        public List<UserDetails> value;
    }


    public class UserDetails
    {
        [JsonProperty("objectId")]
        public string objectId { get; set; }

        public List<assignedPlan> assignedPlans { get; set; }

        [DisplayName("Display Name")]
        public string displayName { get; set; }
        [DisplayName("Given Name")]
        public string givenName { get; set; }
        [DisplayName("Surname")]
        public string surname { get; set; }
        [DisplayName("Alias")]
        public string mailNickname { get; set; }
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
        [DisplayName("Public Calendar")]        
        public bool PublishedCal { get; set; }
        [DisplayName("Published Calendar Url")]
        public string PublishedCalendarUrl { get; set; }
        [DisplayName("Published ICal Url")]
        public string PublishedICalUrl { get; set; }
    }

    public class assignedPlan
    {
        public string assignedTimestamp { get; set; }
        public string capabilityStatus { get; set; }
        public string service { get; set; }
        public string servicePlanId { get; set; }
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
