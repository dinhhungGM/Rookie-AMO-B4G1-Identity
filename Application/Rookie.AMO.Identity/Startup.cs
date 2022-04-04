using FluentValidation.AspNetCore;
using IdentityServer4;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Rookie.AMO.Identity.Bussiness.Interfaces;
using Rookie.AMO.Identity.Bussiness.Services;
using Rookie.AMO.Identity.DataAccessor;
using Rookie.AMO.Identity.DataAccessor.Data;
using Rookie.AMO.Identity.DataAccessor.Entities;
using Rookie.AMO.Identity.Helpers;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;

namespace Rookie.AMO.Identity
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment CurrentEnvironment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            CurrentEnvironment = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            string defaultDbConnection = Configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AppIdentityDbContext>(options =>
            {
                options.UseSqlServer(defaultDbConnection, b => b.MigrationsAssembly(migrationsAssembly));
            });
            ConfigurationHelper.Initialize(Configuration);
            services.AddIdentity<User, IdentityRole>(options =>
            {
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 1;
            })
             .AddEntityFrameworkStores<AppIdentityDbContext>()
            .AddDefaultTokenProviders();
            services.AddCors(options =>
            {
                options.AddPolicy("AllowOrigins",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                    });
            });

            services.AddMvc()
                .AddFluentValidation(fv =>
                {
                    fv.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly());
                })
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
                options.SerializerSettings.DateFormatString = "dd'/'MM'/'yyyy";
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = c =>
                {
                    var errors = string.Join('\n', c.ModelState.Values.Where(v => v.Errors.Count > 0)
                        .SelectMany(v => v.Errors)
                        .Select(v => v.ErrorMessage));

                    return new BadRequestObjectResult(new
                    {
                        ErrorCode = StatusCodes.Status400BadRequest,
                        Message = errors
                    });
                };
            });
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IEmailSender, EmailSenderService>();
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
            /*if (CurrentEnvironment.IsDevelopment())
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "Bearer";
                    options.DefaultChallengeScheme = "Bearer";
                    options.DefaultForbidScheme = "Bearer";
                })
                .AddIdentityServerAuthentication("Bearer", options =>
                {
                    options.ApiName = "api1";
                    options.Authority = "https://localhost:5001";
                    options.SupportedTokens = SupportedTokens.Jwt;
                });
            }
            else
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = "Bearer";
                    options.DefaultChallengeScheme = "Bearer";
                    options.DefaultForbidScheme = "Bearer";
                })
                .AddIdentityServerAuthentication("Bearer", options =>
                {
                    options.ApiName = "api1";
                    options.Authority = "https://b4g1-amo-id4.azurewebsites.net";
                    options.SupportedTokens = SupportedTokens.Jwt;

                });
            }*/

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Bearer";
                options.DefaultChallengeScheme = "Bearer";
                options.DefaultForbidScheme = "Bearer";
            })
                .AddIdentityServerAuthentication("Bearer", options =>
                {
                    options.ApiName = Configuration.GetSection("IdentityServerOptions:Audience").Value;
                    options.Authority = Configuration.GetSection("IdentityServerOptions:Authority").Value;
                    options.SupportedTokens = SupportedTokens.Jwt;

                });


            services.AddAuthorization(options =>
            {
                options.AddPolicy("ADMIN_ROLE_POLICY", policy =>
                {
                    policy.AddAuthenticationSchemes("Bearer");
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("Admin");
                });
                options.AddPolicy("STAFF_ROLE_POLICY", policy =>
                {
                    policy.AddAuthenticationSchemes("Bearer");
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("Staff");
                });
                options.AddPolicy("DEFAULT_AUTHENTICATE_POLICY", policy =>
                {
                    policy.AddAuthenticationSchemes("Bearer");
                    policy.RequireAuthenticatedUser();
                });
                options.DefaultPolicy = options.GetPolicy("DEFAULT_AUTHENTICATE_POLICY");
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Rookie id4 Api",
                    Version = "v1"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                  {
                    {
                      new OpenApiSecurityScheme
                      {
                        Reference = new OpenApiReference
                          {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                          },
                          Scheme = "oauth2",
                          Name = "Bearer",
                          In = ParameterLocation.Header,

                        },
                        new List<string>()
                      }
                    });
            });

            services.AddIdentityServer(options =>
               {
                   options.Events.RaiseErrorEvents = true;
                   options.Events.RaiseInformationEvents = true;
                   options.Events.RaiseFailureEvents = true;
                   options.Events.RaiseSuccessEvents = true;
               })
           .AddAspNetIdentity<User>()
           .AddConfigurationStore(options =>
           {
               options.ConfigureDbContext = b =>
               b.UseSqlServer(defaultDbConnection, b => b.MigrationsAssembly(migrationsAssembly));
           })
           .AddOperationalStore(options =>
           {
               options.ConfigureDbContext = b =>
               b.UseSqlServer(defaultDbConnection, b => b.MigrationsAssembly(migrationsAssembly));
           }).AddSigningCredential(ECDSAHelper.GetSecurityKey(), IdentityServerConstants.ECDsaSigningAlgorithm.ES256)
           .AddValidationKey(ECDSAHelper.GetSecurityKey())
           .AddProfileService<ProfileService>();

            //seed data
            SeedIdentityData.EnsureSeedData(defaultDbConnection);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApplicationBuilder applicationBuilder)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseStaticFiles();
            app.UseRouting();

            app.UseCors("AllowOrigins");

            app.UseIdentityServer();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
            app.UseSwagger();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "PlaceInfo Services"));
            /*if (env.IsDevelopment())
            {
                //seed database
                var serviceScopeFactory = applicationBuilder.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
                using (var serviceScope = serviceScopeFactory.CreateScope())
                {
                    var dbContext = serviceScope.ServiceProvider.GetService<AppIdentityDbContext>();
                    if (!dbContext.Database.CanConnect())
                    {
                        dbContext.Database.Migrate();
                    }
                    var configDbContext = serviceScope.ServiceProvider.GetService<ConfigurationDbContext>();
                    if (!configDbContext.Database.CanConnect())
                    {
                        configDbContext.Database.Migrate();
                    }

                    var persistedGrantDbContext = serviceScope.ServiceProvider.GetService<PersistedGrantDbContext>();
                    if (!persistedGrantDbContext.Database.CanConnect())
                    {
                        persistedGrantDbContext.Database.Migrate();
                    }

                }
            }*/

        }
    }
}
