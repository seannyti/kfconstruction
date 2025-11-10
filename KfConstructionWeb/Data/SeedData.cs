using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using KfConstructionWeb.Models;

namespace KfConstructionWeb.Data;

public static class SeedData
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider, bool clearExistingUsers = false)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Clear all existing users if requested
        if (clearExistingUsers)
        {
            var allUsers = await userManager.Users.ToListAsync();
            foreach (var user in allUsers)
            {
                // Delete member from API if exists
                try
                {
                    var client = httpClientFactory.CreateClient("KfConstructionAPI");
                    var members = await client.GetFromJsonAsync<List<MemberDto>>("/api/v1/Members");
                    var member = members?.FirstOrDefault(m => m.UserId == user.Id);
                    if (member != null)
                    {
                        await client.DeleteAsync($"/api/v1/Members/{member.Id}");
                    }
                }
                catch
                {
                    // API might not be running, continue anyway
                }

                await userManager.DeleteAsync(user);
            }
        }

        string[] roles = new[] { "SuperAdmin", "Admin", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create default super admin user (env overridable)
        var superAdminEmail = Environment.GetEnvironmentVariable("INITIAL_SUPERADMIN_EMAIL") ?? "seannytheirish@gmail.com";
        var superAdminPassword = Environment.GetEnvironmentVariable("INITIAL_SUPERADMIN_PASSWORD") ?? "SuperAdmin@123";
        var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);
        if (superAdminUser == null)
        {
            superAdminUser = new IdentityUser 
            { 
                UserName = superAdminEmail, 
                Email = superAdminEmail, 
                EmailConfirmed = true 
            };
            var result = await userManager.CreateAsync(superAdminUser, superAdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");
                await userManager.AddToRoleAsync(superAdminUser, "Admin");
            }
        }

        // Create default admin user (env overridable)
        var adminEmail = Environment.GetEnvironmentVariable("INITIAL_ADMIN_EMAIL") ?? "knudsonfamilyconstruction@yahoo.com";
        var adminPassword = Environment.GetEnvironmentVariable("INITIAL_ADMIN_PASSWORD") ?? "Admin@123";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new IdentityUser 
            { 
                UserName = adminEmail, 
                Email = adminEmail, 
                EmailConfirmed = true 
            };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }

    // Helper class for API communication
    private class MemberDto
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
    }
}
