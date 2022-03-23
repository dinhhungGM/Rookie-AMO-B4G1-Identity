using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rookie.AMO.Identity.DataAccessor.Data;
using Rookie.AMO.Identity.DataAccessor.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Rookie.AMO.Identity.DataAccessor
{
    public class SeedIdentityData
    {
        public static void EnsureSeedData(string connectionString)
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddDbContext<AppIdentityDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            services.AddIdentity<User, IdentityRole>(options => {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 0;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequiredUniqueChars = 0;
            })
            .AddEntityFrameworkStores<AppIdentityDbContext>()
            .AddDefaultTokenProviders();

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var context = scope.ServiceProvider.GetService<AppIdentityDbContext>();
            context.Database.Migrate();

            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            
            var admin = roleMgr.FindByNameAsync("Admin").Result;
            if (admin == null)
            {
                admin = new IdentityRole
                {
                    Name = "Admin",
                };

                var result = roleMgr.CreateAsync(admin).Result;
                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }
            }

            var customer = roleMgr.FindByNameAsync("Staff").Result;
            if (customer == null)
            {
                customer = new IdentityRole()
                {
                    Name = "Staff"
                };

                var result = roleMgr.CreateAsync(customer).Result;

                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }

            }

            var user1 = userMgr.FindByNameAsync("Admin1").Result;
            if (user1 == null)
            {
                user1 = new User
                {
                    FirstName = "John",
                    LastName = "Doe",
                    FullName = "John Doe",
                    UserName = "Admin1",
                    CodeStaff = "SD0001",
                    Type = "Admin",
                    Gender = "Male",
                    Location = "HN",
                    JoinedDate = DateTime.Now,
                    DateOfBirth = DateTime.ParseExact("2001-02-02", "yyyy-MM-dd", null)
                };
                var result = userMgr.CreateAsync(user1, "P@33word1").Result;
                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }

                result = userMgr.AddClaimsAsync(user1, new List<Claim>
                    {
                        new Claim(IdentityModel.JwtClaimTypes.GivenName, "John"),
                        new Claim(IdentityModel.JwtClaimTypes.FamilyName, "Doe"),
                        new Claim(IdentityModel.JwtClaimTypes.Role, "Admin"),
                        new Claim("location", "HN")
                    }).Result;

                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }

                result = userMgr.AddToRoleAsync(user1, "Admin").Result;
                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }
            }

            var user2 = userMgr.FindByNameAsync("Staff1").Result;
            if (user2 == null)
            {
                user2 = new User
                {
                    FirstName = "John",
                    LastName = "Doe",
                    FullName = "John Doe",
                    UserName = "Staff1",
                    CodeStaff = "SD0002",
                    Type = "Staff",
                    Gender = "Male",
                    JoinedDate = DateTime.Now,
                    DateOfBirth = DateTime.ParseExact("2001-01-01", "yyyy-MM-dd", null)
                };
                var result = userMgr.CreateAsync(user2, "password@123").Result;
                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }

                result = userMgr.AddClaimsAsync(user2, new List<Claim>
                    {
                        new Claim(IdentityModel.JwtClaimTypes.GivenName, "John"),
                        new Claim(IdentityModel.JwtClaimTypes.FamilyName, "Doe"),
                        new Claim(IdentityModel.JwtClaimTypes.Role, "Staff"),
                        new Claim("location", "HN")
                    }).Result;

                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }
                result = userMgr.AddToRoleAsync(user2, "Staff").Result;
                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }

            }

            //var user3 = userMgr.FindByNameAsync("Staff2").Result;
            //if (user3 == null)
            //{
            //    user3 = new User
            //    {
            //        FirstName = "Khanh Son",
            //        LastName = "Hoang",
            //        FullName = "Hoang KHanh Son",
            //        UserName = "HKSon1",
            //        CodeStaff = "SD0003",
            //        Type = "Staff",
            //        Gender = "Male",
            //        Location = "Ho Chi Minh",
            //        JoinedDate = DateTime.Now,
            //        DateOfBirth = DateTime.ParseExact("2000-08-02", "yyyy-MM-dd", null)
            //    };
            //    var result = userMgr.CreateAsync(user3, "123456@a").Result;
            //    if (!result.Succeeded)
            //    {
            //        throw new Exception(result.Errors.First().Description);
            //    }

  

            //    result = userMgr.AddClaimsAsync(user3, new List<Claim>
            //        {
            //            new Claim(IdentityModel.JwtClaimTypes.GivenName, "Khanh Son"),
            //            new Claim(IdentityModel.JwtClaimTypes.FamilyName, "Hoang"),
            //            new Claim(IdentityModel.JwtClaimTypes.Role, "Staff"),
            //            new Claim("location", "Ho Chi Minh")
            //        }).Result;

            //    if (!result.Succeeded)
            //    {
            //        throw new Exception(result.Errors.First().Description);
            //    }
            //    result = userMgr.AddToRoleAsync(user3, "Staff").Result;
            //    if (!result.Succeeded)
            //    {
            //        throw new Exception(result.Errors.First().Description);
            //    }
            //}
        }
    }
}
