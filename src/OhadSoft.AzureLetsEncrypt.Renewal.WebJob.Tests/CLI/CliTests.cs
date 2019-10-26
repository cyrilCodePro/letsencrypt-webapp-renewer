﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OhadSoft.AzureLetsEncrypt.Renewal.WebJob.Cli;
using OhadSoft.AzureLetsEncrypt.Renewal.WebJob.Tests.Util;

namespace OhadSoft.AzureLetsEncrypt.Renewal.WebJob.Tests.Cli
{
    [TestClass]
    public class CliTests : RenewalTestBase
    {
        private IReadOnlyDictionary<(string shortName, string longName), string> FullValidArgs { get; } = new Dictionary<(string, string), string>
        {
#pragma warning disable SA1008
            // ---- Mandatory ----
            { ("-s", "--subscriptionId"), Subscription1.ToString() },
            { ("-t", "--tenantId"), Tenant1 },
            { ("-r", "--resourceGroup"), ResourceGroup1 },
            { ("-w", "--webApp"), WebApp1Name },
            { ("-o", "--hosts"), String.Join(";", Hosts1) },
            { ("-e", "--email"), Email1 },
            { ("-c", "--clientId"), ClientId1.ToString() },
            { ("-l", "--clientSecret"), ClientSecret1 },

            // ---- Optional ----
            { ("-p", "--servicePlanResourceGroup"), ServicePlanResourceGroup1 },
            { ("-d", "--siteSlotName"), SiteSlotName1 },
            { ("-i", "--useIpBasedSsl"), UseIpBasedSsl1.ToString() },
            { ("-k", "--rsaKeyLength"), RsaKeyLength1.ToString(CultureInfo.InvariantCulture) },
            { ("-a", "--acmeBaseUri"), AcmeBaseUri1.ToString() },
            { ("-x", "--webRootPath"), WebRootPath1 },
            { ("-n", "--renewXNumberOfDaysBeforeExpiration"), RenewXNumberOfDaysBeforeExpiration1.ToString(CultureInfo.InvariantCulture) },
            { ("-h", "--azureAuthenticationEndpoint"), AzureAuthenticationEndpoint1.ToString() },
            { ("-u", "--azureTokenAudience"), AzureTokenAudience1.ToString() },
            { ("-m", "--azureManagementEndpoint"), AzureManagementEndpoint1.ToString() },
            { ("-b", "--azureDefaultWebSiteDomainName"), AzureDefaultWebsiteDomainName1 },

            // -- Azure DNS ---
            { ("-f", "--azureDnsTenantId"), TenantAzureDns },
            { ("-g", "--azureDnsSubscriptionId"), SubscriptionAzureDns.ToString() },
            { ("-j", "--azureDnsResourceGroup"), ResourceGroupAzureDns },
            { ("-q", "--azureDnsClientId"), ClientIdAzureDns.ToString() },
            { ("-v", "--azureDnsClientSecret"), ClientSecretAzureDns },
            { ("-z", "--azureDnsZoneName"), AzureDnsZoneName },
            { ("-y", "--azureDnsRelativeRecordSetName"), AzureDnsRelativeRecordSetName },
#pragma warning restore SA1008
        };

        private IEnumerable<KeyValuePair<(string shortName, string longName), string>> GetMinimalValidArgs() => FullValidArgs.Take(8);

        private readonly CliRenewer m_renewer;

        public CliTests()
        {
            m_renewer = new CliRenewer(RenewalManager, new CommandlineRenewalParamsReader());
        }

        // ------------------ INVALID PARAM TESTS ------------------
        // Note - some params don't require testing (absolute Uri, Bool, Int) as they are already enforced by the Commandline parser
        [TestMethod]
        public void InvalidSubscriptionId()
        {
            AssertInvalidParameter("-s", Guid.Empty.ToString(), "subscriptionId");
            AssertInvalidParameter("-g", Guid.Empty.ToString(), "subscriptionId", "Azure DNS");
        }

        [TestMethod]
        public void InvalidTenant()
        {
            AssertInvalidParameter("-t", String.Empty, "tenantId");
            AssertInvalidParameter("-f", " ", "tenantId", "Azure DNS");
        }

        [TestMethod]
        public void InvalidResourceGroup()
        {
            AssertInvalidParameter("-r", " ", "resourceGroup");
            AssertInvalidParameter("-j", String.Empty, "resourceGroup", "Azure DNS");
            AssertInvalidParameter("--servicePlanResourceGroup", "    ", "servicePlanResourceGroup");
        }

        [TestMethod]
        public void InvalidSiteSlotName()
        {
            AssertInvalidParameter("-d", String.Empty, "siteSlotName");
        }

        [TestMethod]
        public void InvalidWebApp()
        {
            AssertInvalidParameter("-w", "     ", "webApp");
        }

        [TestMethod]
        public void InvalidHosts()
        {
            AssertInvalidParameter("-o", "/", "hosts");
        }

        [TestMethod]
        public void InvalidEmail()
        {
            AssertInvalidParameter("-e", "@notAnEmail", "email");
        }

        [TestMethod]
        public void InvalidClientId()
        {
            AssertInvalidParameter("-c", Guid.Empty.ToString(), "clientId");
            AssertInvalidParameter("-q", Guid.Empty.ToString(), "clientId", "Azure DNS");
        }

        [TestMethod]
        public void InvalidClientSecret()
        {
            AssertInvalidParameter("-l", String.Empty, "clientSecret");
            AssertInvalidParameter("-v", " ", "clientSecret", "Azure DNS");
        }

        [TestMethod]
        public void InvalidRsaKeyLength()
        {
            AssertInvalidParameter("-k", "-1", "rsaKeyLength");
        }

        [TestMethod]
        public void InvalidWebRootPath()
        {
            AssertInvalidParameter("--webRootPath", " ", "webRootPath");
        }

        [TestMethod]
        public void InvalidDefaultWebsiteDomainName()
        {
            AssertInvalidParameter("--azureDefaultWebSiteDomainName", "@", "azureDefaultWebsiteDomainName");
        }

        [TestMethod]
        public void InvalidAzureDnsZoneName()
        {
            AssertInvalidParameter("--azureDnsZoneName", String.Empty, "azureDnsZoneName");
        }

        [TestMethod]
        public void InvalidAzureDnsRelativeRecordSetName()
        {
            AssertInvalidParameter("--azureDnsRelativeRecordSetName", "   ", "azureDnsRelativeRecordSetName");
        }

        private void AssertInvalidParameter(string name, string value, params string[] expectedTexts)
        {
            AssertExtensions.Throws<ArgumentException>(
                () => m_renewer.Renew(CompleteArgs(new[] { name, value })),
                e => expectedTexts.All(t => e.Message.Contains(t)));
        }

        [TestMethod]
        public void MinimalProperParametersShouldSucceed()
        {
            m_renewer.Renew(GetMinimalValidArgs().SelectMany(kvp => new[] { kvp.Key.shortName, kvp.Value }).ToArray());
            VerifySuccessfulRenewal(ExpectedPartialRenewalParameters1);
        }

        [TestMethod]
        public void MaximalProperParametersShouldSucceed()
        {
            m_renewer.Renew(FullValidArgs.SelectMany(kvp => new[] { kvp.Key.longName, kvp.Value }).ToArray());
            VerifySuccessfulRenewal(ExpectedFullRenewalParameters1);
        }

        private string[] CompleteArgs(string[] args)
        {
            var list = new List<string>(args);

            var argNames = new HashSet<string>(args.Where((value, index) => index % 2 == 0)); // the even indices are the parameter names
            foreach (var kvp in GetMinimalValidArgs().Where(kvp => !argNames.Contains(kvp.Key.shortName) && !argNames.Contains(kvp.Key.longName)))
            {
                list.Add(kvp.Key.longName);
                list.Add(kvp.Value);
            }

            return list.ToArray();
        }
    }
}