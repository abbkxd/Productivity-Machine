using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProductiveMachine.WebApp.Models;

namespace ProductiveMachine.WebApp.Data;

public static class SeedData
{
    public static async void Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        context.Database.EnsureCreated();

        try
        {
            await SeedRolesAsync(roleManager);
            await SeedAdminUserAsync(userManager);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        // Create roles if they don't exist
        string[] roleNames = { "Admin", "User" };
        
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
    {
        // Check if admin user exists
        var adminUser = await userManager.FindByNameAsync("admin");
        
        if (adminUser == null)
        {
            // Create admin user
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@example.com",
                EmailConfirmed = true,
                TimeZone = "UTC"
            };
            
            // Use a strong password in production or use a configuration value
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            
            if (result.Succeeded)
            {
                // Add admin to Admin role
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
} 