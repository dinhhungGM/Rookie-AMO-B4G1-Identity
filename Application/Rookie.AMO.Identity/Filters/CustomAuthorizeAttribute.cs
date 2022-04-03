using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using Rookie.AMO.Identity.Helpers;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

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

                var handler = new JwtSecurityTokenHandler();
                try
                {
                    // validate token here

                    handler.ValidateToken(accessToken, new TokenValidationParameters()
                    {
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = ECDSAHelper.GetSecurityKey()
                    }, out SecurityToken validatedToken);

                    if (Role == null)
                        return;

                    var jwtSecurityToken = (JwtSecurityToken)validatedToken;

                    // Check Role
                    var hasClaim = jwtSecurityToken.Claims.Any(c => c.Type == "role" && c.Value == Role);
                    if (!hasClaim)
                    {
                        context.Result = new ForbidResult();
                    }

                    /*claimsIdentity.AddClaim(new Claim("sub", jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == "sub").Value));
                    claimsIdentity.AddClaim(new Claim("location", jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == "location").Value));*/

                    context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(jwtSecurityToken.Claims));

                }
                catch (Exception)
                {
                    context.Result = new UnauthorizedResult();

                }

            }


        }
    }
}
