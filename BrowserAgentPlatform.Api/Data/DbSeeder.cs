using BCrypt.Net;
using BrowserAgentPlatform.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BrowserAgentPlatform.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Users.AnyAsync())
        {
            db.Users.Add(new AppUser
            {
                Username = "admin",
                DisplayName = "Admin",
                Role = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456")
            });
        }

        if (!await db.FingerprintTemplates.AnyAsync())
        {
            db.FingerprintTemplates.Add(new FingerprintTemplate
            {
                Name = "Default Desktop Chrome",
                ConfigJson = """
                {
                  "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123 Safari/537.36",
                  "viewport": { "width": 1366, "height": 768 },
                  "locale": "zh-CN",
                  "timezoneId": "Asia/Singapore"
                }
                """
            });
        }

        await db.SaveChangesAsync();
    }
}
