using System;
using System.Collections.Generic;
using System.Linq;

namespace DirectoryExtensionsApp.Models
{
    public class Tenant
    {
        public string Id { get; set; }
    }

    public class SignupToken
    {
        public string Id { get; set; }

        public DateTimeOffset ExpirationDate { get; set; }
    }

    public class IssuingAuthorityKey
    {
        public string Id { get; set; }
    }
}