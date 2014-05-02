using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using System.Web.Hosting;
using System.Xml.Linq;
using CalendarSharing.Models;

namespace CalendarSharing.Utils
{
    public class DatabaseIssuerNameRegistry : ValidatingIssuerNameRegistry
    {
        public static bool ContainsTenant(string tenantId)
        {
            using (TenantDbContext context = new TenantDbContext())
            {
                return context.Tenants
                    .Where(tenant => tenant.Id == tenantId)
                    .Any();
            }
        }

        public static bool ContainsKey(string thumbprint)
        {
            using (TenantDbContext context = new TenantDbContext())
            {
                return context.IssuingAuthorityKeys
                    .Where(key => key.Id == thumbprint)
                    .Any();
            }
        }

        public static void RefreshKeys(string metadataLocation)
        {
            IssuingAuthority issuingAuthority = ValidatingIssuerNameRegistry.GetIssuingAuthority(metadataLocation);

            bool newKeys = false;
            foreach (string thumbprint in issuingAuthority.Thumbprints)
            {
                if (!ContainsKey(thumbprint))
                {
                    newKeys = true;
                    break;
                }
            }

            if (newKeys)
            {
                using (TenantDbContext context = new TenantDbContext())
                {
                    context.IssuingAuthorityKeys.RemoveRange(context.IssuingAuthorityKeys);
                    foreach (string thumbprint in issuingAuthority.Thumbprints)
                    {
                        context.IssuingAuthorityKeys.Add(new IssuingAuthorityKey { Id = thumbprint });
                    }
                    context.SaveChanges();
                }
            }
        }

        public static bool TryAddTenant(string tenantId, string signupToken)
        {
            if (!ContainsTenant(tenantId))
            {
                using (TenantDbContext context = new TenantDbContext())
                {
                    SignupToken existingToken = context.SignupTokens.Where(token => token.Id == signupToken).FirstOrDefault();
                    if (existingToken != null)
                    {
                        context.SignupTokens.Remove(existingToken);
                        context.Tenants.Add(new Tenant { Id = tenantId });
                        context.SaveChanges();
                        return true;
                    }
                }
            }

            return false;
        }

        public static void AddSignupToken(string signupToken, DateTimeOffset expirationTime)
        {
            using (TenantDbContext context = new TenantDbContext())
            {
                context.SignupTokens.Add(new SignupToken
                {
                    Id = signupToken,
                    ExpirationDate = expirationTime
                });
                context.SaveChanges();
            }
        }

        public static void CleanUpExpiredSignupTokens()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            using (TenantDbContext context = new TenantDbContext())
            {
                IQueryable<SignupToken> tokensToRemove = context.SignupTokens.Where(token => token.ExpirationDate <= now);
                if (tokensToRemove.Any())
                {
                    context.SignupTokens.RemoveRange(tokensToRemove);
                    context.SaveChanges();
                }
            }
        }

        protected override bool IsThumbprintValid(string thumbprint, string issuer)
        {
            string issuerID = issuer.TrimEnd('/').Split('/').Last();

            return ContainsTenant(issuerID) &&
                ContainsKey(thumbprint);
        }
    }
}