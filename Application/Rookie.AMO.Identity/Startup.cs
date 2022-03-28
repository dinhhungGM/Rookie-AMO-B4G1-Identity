using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rookie.AMO.Identity.DataAccessor;
using Rookie.AMO.Identity.DataAccessor.Data;
using Rookie.AMO.Identity.DataAccessor.Entities;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

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

            services.AddDbContext<AppIdentityDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly(migrationsAssembly));
            });

            services.AddIdentity<User, IdentityRole>()
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

            services.AddMvc();

            if (CurrentEnvironment.IsDevelopment())
            {

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
                   b.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly(migrationsAssembly));
               })
               .AddOperationalStore(options =>
               {
                   options.ConfigureDbContext = b =>
                   b.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly(migrationsAssembly));
               })
               .AddDeveloperSigningCredential();
            }
            else
            {
                var rsaCertificate = new X509Certificate2(
                Path.Combine(CurrentEnvironment.ContentRootPath, "idsrv3test.pfx"), "idsrv3test");

                services.AddIdentityServer()
                .AddSigningCredential(rsaCertificate)
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = b =>
                    b.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly(migrationsAssembly));
                })
               .AddOperationalStore(options =>
               {
                   options.ConfigureDbContext = b =>
                   b.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly(migrationsAssembly));
               })
                .AddAspNetIdentity<User>();
            }

            //seed data
            SeedIdentityData.EnsureSeedData(Configuration.GetConnectionString("DefaultConnection"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApplicationBuilder applicationBuilder)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCors("AllowOrigins");
            app.UseStaticFiles();
            app.UseRouting();

            app.UseIdentityServer();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });

            if (env.IsDevelopment())
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
            }

        }
    }
}
