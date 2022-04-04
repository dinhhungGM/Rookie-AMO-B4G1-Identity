using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Cryptography;

namespace Rookie.AMO.Identity.Helpers
{
    public static class ECDSAHelper
    {
        public static ECDsaSecurityKey GetSecurityKey()
        {
            string keyPem = ConfigurationHelper.config.GetSection("IdentityServerOptions:KeyPem").Value;
            string keyId = ConfigurationHelper.config.GetSection("IdentityServerOptions:KeyId").Value;

            /*var pemBytes = Convert.FromBase64String(
                 @"MHcCAQEEIB2EbKgBGbRxWTtWheDgaNw3P7TsSsMoWloU4NHO3MWYoAoGCCqGSM49
            AwEHoUQDQgAEVGVVEnzMZnTv/8Jk0/WlFs9poYA7XqI7ITHH78OPenhGS02GBjXM
            WV/akdaWBgIyUP8/86kJ2KRyuHR4c/jIuA==");*/

            var pemBytes = Convert.FromBase64String(keyPem);

            var ecdsa = ECDsa.Create();
            ecdsa.ImportECPrivateKey(pemBytes, out _);
            var securityKey = new ECDsaSecurityKey(ecdsa) { KeyId = keyId };
            return securityKey;
        }
    }
}
