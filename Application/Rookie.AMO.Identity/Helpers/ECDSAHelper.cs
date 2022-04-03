using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Cryptography;

namespace Rookie.AMO.Identity.Helpers
{
    public static class ECDSAHelper
    {
        public static ECDsaSecurityKey GetSecurityKey()
        {
            var pemBytes = Convert.FromBase64String(
                 @"MHcCAQEEIB2EbKgBGbRxWTtWheDgaNw3P7TsSsMoWloU4NHO3MWYoAoGCCqGSM49
            AwEHoUQDQgAEVGVVEnzMZnTv/8Jk0/WlFs9poYA7XqI7ITHH78OPenhGS02GBjXM
            WV/akdaWBgIyUP8/86kJ2KRyuHR4c/jIuA==");

            var ecdsa = ECDsa.Create();
            ecdsa.ImportECPrivateKey(pemBytes, out _);
            var securityKey = new ECDsaSecurityKey(ecdsa) { KeyId = "ef208a01ef43406f833b267023766550" };
            return securityKey;
        }
    }
}
