using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace Rookie.AMO.Identity.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CustomAuthorizeAttribute : Attribute, IAuthorizationFilter
    {

        public string Role { get; set; }


        public void OnAuthorization(AuthorizationFilterContext context)
        {
            string authHeader = context.HttpContext.Request.Headers["Authorization"];
            if (authHeader == null)
            {
                context.Result = new UnauthorizedResult();
            }
            else
            {
                if (!authHeader.StartsWith("Bearer"))
                    context.Result = new UnauthorizedResult();
                var accessToken = authHeader.Split(" ").Last();

                if (context.HttpContext.User == null)
                {
                    context.Result = new UnauthorizedResult();
                }

                if (Role == null)
                    return;

                var handler = new JwtSecurityTokenHandler();
                try
                {
                    // validate token here

                    var fileName = Path.Combine(Directory.GetCurrentDirectory(), "myapp.pfx");
                    var rsaCertificate = new X509Certificate2(fileName, "123");
                    handler.ValidateToken(accessToken, new TokenValidationParameters()
                    {

                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ValidateLifetime = false,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new X509SecurityKey(rsaCertificate),

                    }, out SecurityToken validatedToken);


                    var jwtSecurityToken = handler.ReadJwtToken(accessToken);


                    var hasClaim = jwtSecurityToken.Claims.Any(c => c.Type == "role" && c.Value == Role);
                    if (!hasClaim)
                    {
                        context.Result = new ForbidResult();
                    }

                    var user = new ClaimsPrincipal();

                    ClaimsIdentity claimsIdentity = new ClaimsIdentity();
                    claimsIdentity.AddClaim(new Claim("sub", jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == "sub").Value));
                    /*claimsIdentity.AddClaim(new Claim("sub", jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == "location").Value));*/
                    user.AddIdentity(claimsIdentity);
                    context.HttpContext.User = user;

                }
                catch (Exception)
                {
                    context.Result = new UnauthorizedResult();

                }

            }


        }
    }
}
