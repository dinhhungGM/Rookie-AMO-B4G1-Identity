﻿using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.EntityFramework.Storage;
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

            services.AddIdentity<User, IdentityRole>(options =>
            {
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

            services.Configure<IdentityOptions>(options =>
            {
                // Default Password settings.
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;
            });
            services.AddOperationalDbContext(
                options =>
                {
                    options.ConfigureDbContext = db =>
                        db.UseSqlServer(
                            connectionString,
                            sql => sql.MigrationsAssembly(typeof(SeedIdentityData).Assembly.FullName)
                        );
                }
            );
            services.AddConfigurationDbContext(
                options =>
                {
                    options.ConfigureDbContext = db =>
                        db.UseSqlServer(
                            connectionString,
                            sql => sql.MigrationsAssembly(typeof(SeedIdentityData).Assembly.FullName)
                        );
                }
            );

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            scope.ServiceProvider.GetService<PersistedGrantDbContext>().Database.Migrate();


            var context = scope.ServiceProvider.GetService<ConfigurationDbContext>();
            context.Database.Migrate();

            EnsureSeedData(context);

            var ctx = scope.ServiceProvider.GetService<AppIdentityDbContext>();
            ctx.Database.Migrate();

            EnsureRoles(scope);
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

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
        }
        private static void EnsureSeedData(ConfigurationDbContext context)
        {
            if (!context.Clients.Any())
            {
                foreach (var client in InitData.Clients.ToList())
                {
                    context.Clients.Add(client.ToEntity());
                }

                context.SaveChanges();
            }

            if (!context.IdentityResources.Any())
            {
                foreach (var resource in InitData.IdentityResources.ToList())
                {
                    context.IdentityResources.Add(resource.ToEntity());
                }

                context.SaveChanges();
            }

            if (!context.ApiScopes.Any())
            {
                foreach (var resource in InitData.ApiScopes.ToList())
                {
                    context.ApiScopes.Add(resource.ToEntity());
                }

                context.SaveChanges();
            }

            if (!context.ApiResources.Any())
            {
                foreach (var resource in InitData.ApiResources.ToList())
                {
                    context.ApiResources.Add(resource.ToEntity());
                }

                context.SaveChanges();
            }
        }

        private static void EnsureRoles(IServiceScope scope)
        {
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
        }
    }
}
