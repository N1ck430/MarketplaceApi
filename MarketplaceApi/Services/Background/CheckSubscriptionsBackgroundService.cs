using DataLibrary.EntityFramework;
using DataLibrary.Models.User;
using DataLibrary.Services.DateTime;
using MarketplaceApi.Services.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceApi.Services.Background;

public class CheckSubscriptionsBackgroundService : BackgroundService
{
    private readonly ILogger<CheckSubscriptionsBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public CheckSubscriptionsBackgroundService(
        ILogger<CheckSubscriptionsBackgroundService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Started {nameof(CheckSubscriptionsBackgroundService)}");

        await DoWork();

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await DoWork();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation($"{nameof(CheckSubscriptionsBackgroundService)} is stopping");
        }

    }

    private async Task DoWork()
    {
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<DataLibrary.Models.User.User>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarketplaceDbContext>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var subscribers = await userManager.GetUsersInRoleAsync(nameof(Role.Subscriber));
        await Task.WhenAll(subscribers.Select(s => CheckAndUpdateSubscriptionStatus(userManager, dbContext, s, userService, dateTimeProvider)));
    }

    private async Task CheckAndUpdateSubscriptionStatus(UserManager<DataLibrary.Models.User.User> userManager,
        MarketplaceDbContext dbContext,
        DataLibrary.Models.User.User user,
        IUserService userService,
        IDateTimeProvider dateTimeProvider)
    {
        if (await dbContext.Subscriptions.AnyAsync(s =>
                s.UserId == user.Id && s.StartDate <= dateTimeProvider.UtcNow &&
                s.EndDate >= dateTimeProvider.UtcNow))
        {
            return;
        }

        var result = await userManager.RemoveFromRoleAsync(user, nameof(Role.Subscriber));

        if (result.Succeeded)
        {
            userService.RemoveUserFromCache(user);
            return;
        }

        _logger.LogError("Could not update UserRole for user {userId} | Errors:\n {errors}", user.Id,
            string.Join("\n", result.Errors.Select(e => $"{e.Code} | {e.Description}")));
    }
}