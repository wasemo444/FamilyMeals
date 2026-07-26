using LinkNest.Api.Data;
using LinkNest.Api.Data.Entities;
using LinkNest.Api.Identity;
using LinkNest.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinkNest.Tests.Api;

public static class AuthTestHelpers
{
    public static async Task ConfirmEmailAsync(IServiceProvider services, string email)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            throw new InvalidOperationException($"User '{email}' was not found.");
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Failed to confirm email in test helper.");
        }
    }

    public static async Task AddGroupMemberAsync(
        IServiceProvider services,
        Guid groupId,
        string memberEmail,
        GroupRole role = GroupRole.Member)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await userManager.FindByEmailAsync(memberEmail)
            ?? throw new InvalidOperationException($"User '{memberEmail}' was not found.");

        dbContext.GroupMemberships.Add(new GroupMembershipEntity
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = user.Id,
            Role = role,
            JoinedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    public static async Task RegisterUserAsync(
        IServiceProvider services,
        string email,
        bool confirmEmail = true,
        string password = ApiWebApplicationFactory.DefaultTestPassword)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = email,
            EmailConfirmed = confirmEmail,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Failed to register test user.");
        }
    }

    public static async Task RegisterAndConfirmUserAsync(
        IServiceProvider services,
        string email,
        string password = ApiWebApplicationFactory.DefaultTestPassword)
    {
        await RegisterUserAsync(services, email, confirmEmail: false, password);
        await ConfirmEmailAsync(services, email);
    }
}
