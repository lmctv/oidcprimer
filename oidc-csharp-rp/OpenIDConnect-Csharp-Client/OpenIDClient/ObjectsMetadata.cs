﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace OpenIDClient
{
    public class OIDCProviderMetadata : Messages.OIDClientSerializableMessage
    {
        public string Issuer { get; set; }
        public string AuthorizationEndpoint { get; set; }
        public string JwksUri { get; set; }
        public string UserinfoEndpoint { get; set; }
        public string RegistrationEndpoint { get; set; }
        public string TokenEndpoint { get; set; }
        public List<string> ResponseTypesSupported { get; set; }
        public List<string> IdTokenEncryptionAlgValuesSupported { get; set; }
        public List<string> ClaimTypesSupported { get; set; }
        public List<string> AcrValuesSupported { get; set; }
        public bool RequireRequestUriRegistration { get; set; }
        public bool RequestUriParameterSupported { get; set; }
        public List<string> RequestObjectEncryptionAlgValuesSupported { get; set; }
        public List<string> IdTokenSigningAlgValuesSupported { get; set; }
        public List<string> ResponseModesSupported { get; set; }
        public List<string> RequestObjectSigningAlgValuesSupported { get; set; }
        public List<string> SubjectTypesSupported { get; set; }
        public List<string> IdTokenEncryptionEncValuesSupported { get; set; }
        public List<string> TokenEndpointAuthMethodsSupported { get; set; }
        public List<string> UserinfoEncryptionAlgValuesSupported { get; set; }
        public bool RequestParameterSupported { get; set; }
        public List<string> TokenEndpointAuthSigningAlgValuesSupported { get; set; }
        public List<string> UserinfoSigningAlgValuesSupported { get; set; }
        public List<string> ScopesSupported { get; set; }
        public string EndSessionEndpoint { get; set; }
        public string Version { get; set; }
        public bool ClaimsParameterSupported { get; set; }
        public List<string> RequestObjectEncryptionEncValuesSupported { get; set; }
        public List<string> UserinfoEncryptionEncValuesSupported { get; set; }
        public List<string> ClaimsSupported { get; set; }
        public List<string> GrantTypesSupported { get; set; }
        public List<OIDCKey> Keys { get; set; }

        public OIDCProviderMetadata()
        {
            // Empty constructor
        }

        public OIDCProviderMetadata(dynamic o)
        {
            deserializeFromDynamic(o);

            if (JwksUri != null)
            {
                Keys = new List<OIDCKey>();
                Dictionary<string, object> jwks = OpenIdRelyingParty.GetUrlContent(WebRequest.Create(JwksUri));
                ArrayList keys = (ArrayList)jwks["keys"];
                foreach (Dictionary<string, object> key in keys)
                {
                    OIDCKey newKey = new OIDCKey(key);
                    Keys.Add(newKey);
                }
            }
        }
    }

    public class OIDCClientInformation : Messages.OIDClientSerializableMessage
    {
        public List<string> RedirectUris { get; set; }
        public List<string> ResponseTypes { get; set; }
        public List<string> GrantTypes { get; set; }
        public string ApplicationType { get; set; }
        public List<string> Contacts { get; set; }
        public string ClientId { get; set; }
        public DateTime ClientIdIssuedAt { get; set; }
        public string ClientName { get; set; }
        public string ClientSecret { get; set; }
        public DateTime ClientSecretExpiresAt { get; set; }
        public string LogoUri { get; set; }
        public string ClientUri { get; set; }
        public string PolicyUri { get; set; }
        public string TosUri { get; set; }
        public string JwksUri { get; set; }
        public string SectorIdentifierUri { get; set; }
        public string SubjectType { get; set; }
        public string IdTokenSignedResponseAlg { get; set; }
        public string IdTokenEncryptedResponseAlg { get; set; }
        public string IdTokenEncryptedResponseEnc { get; set; }
        public string UserinfoAuthMethod { get; set; }
        public string UserinfoSignedResponseAlg { get; set; }
        public string UserinfoEncryptedResponseAlg { get; set; }
        public string UserinfoEncryptedResponseEnc { get; set; }
        public string RequestObjectSigningAlg { get; set; }
        public string RequestObjectEncryptionAlg { get; set; }
        public string RequestObjectEncryptionEnc { get; set; }
        public string TokenEndpointAuthMethod { get; set; }
        public string TokenEndpointAuthSigningAlg { get; set; }
        public string DefaultMaxAge { get; set; }
        public string RequireAuthTime { get; set; }
        public List<string> DefaultAcrValues { get; set; }
        public string InitiateLoginUri { get; set; }
        public List<string> RequestUris { get; set; }
        public string RegistrationAccessToken { get; set; }
        public string RegistrationClientUri { get; set; }

        public OIDCClientInformation()
        {
            // Empty constructor
        }

        public OIDCClientInformation(dynamic o)
        {
            deserializeFromDynamic(o);
        }

        public override void validate()
        {
            if (RedirectUris != null && ResponseTypes != null && RedirectUris.Count != ResponseTypes.Count)
            {
                throw new OIDCException("The redirect_uris do not match response_types.");
            }

            if (RedirectUris != null && SectorIdentifierUri != null)
            {
                List<string> siUris = new List<string>();
                dynamic uris = OpenIdRelyingParty.GetUrlContent(WebRequest.Create(SectorIdentifierUri));
                foreach (string uri in uris)
                {
                    siUris.Add(uri);
                }

                foreach (string uri in RedirectUris)
                {
                    if (!siUris.Contains(uri))
                    {
                        throw new OIDCException("The sector_identifier_uri json must include URIs from the redirect_uri array.");
                    }
                }
            }

            if (ResponseTypes != null && GrantTypes != null)
            {
                foreach (string responseType in ResponseTypes)
                {
                    if ((responseType == "code" && !GrantTypes.Contains("authorization_code")) ||
                        (responseType == "id_token" && !GrantTypes.Contains("implicit")) ||
                        (responseType == "token" && !GrantTypes.Contains("implicit")) ||
                        (responseType == "id_token" && !GrantTypes.Contains("implicit")))
                    {
                        throw new OIDCException("The response_types do not match grant_types.");
                    }
                }
            }
        }
    }

    public class OIDCKey : Messages.OIDClientSerializableMessage
    {
        public string Use { get; set; }
        public string Crv { get; set; }
        public string D { get; set; }
        public string Y { get; set; }
        public string X { get; set; }
        public string Kid { get; set; }
        public string Kty { get; set; }

        public OIDCKey(dynamic o)
        {
            deserializeFromDynamic(o);
        }

        public override void validate()
        {
            if (Use == null)
            {
                //Disabled control, since mitreid.org is not respecting this spec.
                //throw new OIDCException("The use parameter is missing in key.");
            }
        }
    }
}
