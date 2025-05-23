using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SistemErasmusRazmjena.Models;

namespace SistemErasmusRazmjena.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "Student", "ECTSKoordinator" };

            // Kreiraj uloge ako ne postoje
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Ensure ECTSKoordinator role exists
            if (!await roleManager.RoleExistsAsync("ECTSKoordinator"))
            {
                await roleManager.CreateAsync(new IdentityRole("ECTSKoordinator"));
            }

            // ==================== ADMIN ====================
            var adminEmail = "admin@erasmus.ba";
            var adminPassword = "Admin123!";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Uloga = "Admin"
                };

                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // ==================== STUDENT ====================
            var studentEmail = "student@erasmus.ba";
            var studentPassword = "Student123!";

            if (await userManager.FindByEmailAsync(studentEmail) == null)
            {
                var student = new ApplicationUser
                {
                    UserName = studentEmail,
                    Email = studentEmail,
                    EmailConfirmed = true,
                    Uloga = "Student"
                };

                var result = await userManager.CreateAsync(student, studentPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(student, "Student");
                }
            }

            // ==================== ECTS KOORDINATOR ====================
            var koordinatorEmail = "koordinator@erasmus.ba";
            var koordinatorPassword = "Koordinator123!";

            if (await userManager.FindByEmailAsync(koordinatorEmail) == null)
            {
                var koordinator = new ApplicationUser
                {
                    UserName = koordinatorEmail,
                    Email = koordinatorEmail,
                    EmailConfirmed = true,
                    Uloga = "ECTSKoordinator"
                };

                var result = await userManager.CreateAsync(koordinator, koordinatorPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(koordinator, "ECTSKoordinator");
                }
            }
        }
    }
}
