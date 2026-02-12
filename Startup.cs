using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using ShareKaoMao.Data;
using ShareKaoMao.Services;

namespace ShareKaoMao
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // สร้าง connection string จาก env vars แยก (รองรับ Render/Neon)
            var connectionString = Environment.GetEnvironmentVariable("SHAREKAOMAO_CONNECTION_STRING");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
                var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
                var database = Environment.GetEnvironmentVariable("POSTGRES_DATABASE");
                var user = Environment.GetEnvironmentVariable("POSTGRES_USER");
                var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

                if (!string.IsNullOrWhiteSpace(host))
                {
                    connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require";
                }
                else
                {
                    throw new InvalidOperationException(
                        "============================================================\n" +
                        "ERROR: No database configuration found.\n" +
                        "Set SHAREKAOMAO_CONNECTION_STRING or individual POSTGRES_* variables.\n" +
                        "============================================================");
                }
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<BillCalculationService>();
            services.AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "ShareKaoMao API",
                    Version = "v1",
                    Description = "API สำหรับจัดการบิลหารค่าใช้จ่าย"
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ShareKaoMao API v1");
            });

            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
