using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShareKaoMao.Data;

namespace ShareKaoMao
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // โหลด .env เฉพาะตอน dev (production ใช้ env var จาก Render)
            if (File.Exists(".env"))
            {
                DotNetEnv.Env.Load();
            }

            var host = CreateHostBuilder(args).Build();

            // Auto-migrate ใน production
            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // Render กำหนด PORT env var ให้ — ถ้าไม่มีใช้ 8080
                    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls($"http://0.0.0.0:{port}");
                });
    }
}
