# DirectoryExtensions
## A sample solution for how to use Azure Active Directory Extension Properties

This is a Proof-of-Concept type of solution for testing out/playing with schema extensions in Azure Active Directory. It consists of two web apps where one is responsible for inputing data into Azure AD, and the other uses this data as part of it's own flow.

For some background info on this check out the following blog posts:  
http://mobilitydojo.net/2014/04/08/extending-your-azure-active-directory-part-1/
http://mobilitydojo.net/2014/04/09/extending-your-azure-active-directory-part-2/

### Getting Started
To test the code you will need to have an Azure Active Directory tenant, which currently requires an Azure subscription. (The apps themselves do not have to run on Azure, but there needs to be entries for them in Azure AD.) 
The Azure AD features you will use does not require any paid services, so there should be no direct costs attached to setting it up from the developer side.

For a detailed step-by-step guide on how to setup a new Azure subscription for Azure AD:
http://blogs.msdn.com/b/exchangedev/archive/2014/04/07/enabling-microsoft-azure-portal-access-to-manage-applications-using-the-oauth2-protocol.aspx

After setting up Azure AD you should add two apps to your tenant. Both of the type "apps that my organization is developing".

The scenario as described references the use of YubiKeys as an authentication mechanism. Currently this code is written to work entirely without actual physical YubiKeys, but code for handling real keys can easily be added.

There are two files you need to change to get the code to work (assuming you have the requirements above in place):  
_DirectoryExtensions/Web.config_
_DirectoryExtensionsApp/Web.config_

The following keys/attributes must be changed (in both files):
```
<add key="ida:FederationMetadataLocation" value="https://login.windows.net/contoso.onmicrosoft.com/FederationMetadata/2007-06/FederationMetadata.xml" />
<add key="ida:Realm" value="https://contoso.onmicrosoft.com/DirectoryExtensions" />
<add key="ida:AudienceUri" value="https://contoso.onmicrosoft.com/DirectoryExtensions" />
<add key="ida:ClientID" value="Retrieve this value from the Azure Management Portal." />
<add key="ida:Password" value="Retrieve this value from the Azure Management Portal." />

<audienceUris>
        <add value="https://contoso.onmicrosoft.com/DirectoryExtensions" />
</audienceUris>

<wsFederation passiveRedirectEnabled="false" issuer="https://login.windows.net/common/wsfed" 
    realm="https://contoso.onmicrosoft.com/DirectoryExtensions" requireHttps="true" />
```

These settings can be found on the "Configure" tab of your application in the Azure Management Portal.

_ClientID_ is the "Client ID" guid on the page.  
_Password_ is one of the "keys" specified. (You can create multiple keys.)

_Contoso_ should be replaced with the name of your AD tenant.

In addition _Web.config_ for _DirectoryExtensionsApp_ needs the following key changed:
```
<add key="ida:ExtensionName" value="extension_xyz_YubiKeyId" />
```
This value needs to be retrieved (possibly in debug mode) after it has been registered as Azure AD is responsible for generating the xyz-part of the key.