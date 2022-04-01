using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

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
                var jwtSecurityToken = handler.ReadJwtToken(accessToken);
                var hasClaim = context.HttpContext.User.Claims.Any(c => c.Type == "role" && c.Value == Role);
                if (!hasClaim)
                {
                    context.Result = new ForbidResult();
                }

            }


        }
    }
}
