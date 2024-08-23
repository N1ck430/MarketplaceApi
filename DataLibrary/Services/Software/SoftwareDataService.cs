using System.Collections.Immutable;
using DataLibrary.EntityFramework;
using DataLibrary.Models.Requests.Software;
using DataLibrary.Models.Software;
using DataLibrary.Services.DateTime;
using Microsoft.EntityFrameworkCore;

namespace DataLibrary.Services.Software;

public class SoftwareDataService : ISoftwareDataService
{
    private readonly MarketplaceDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SoftwareDataService(
        MarketplaceDbContext dbContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Models.Software.Software> AddSoftware(Models.Software.Software software)
    {
        var addResult = await _dbContext.Softwares.AddAsync(software);
        await _dbContext.SaveChangesAsync();
        return addResult.Entity;
    }

    public async Task<Models.Software.Software?> GetSoftware(int id)
    {
        return await _dbContext.Softwares
            .Include(s => s.SubscriptionTypes.Where(st => !st.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Models.Software.Software?> UpdateSoftware(UpdateSoftwareRequest updateSoftware)
    {
        var software = await GetSoftware(updateSoftware.Id);
        if (software is null)
        {
            return null;
        }

        software.Name = updateSoftware.Name;

        var updateResult = _dbContext.Softwares.Update(software);
        await _dbContext.SaveChangesAsync();

        return updateResult.Entity;
    }

    public async Task<SubscriptionType> AddSubscriptionType(SubscriptionType subscriptionType)
    {
        var addResult = await _dbContext.SubscriptionTypes.AddAsync(subscriptionType);
        await _dbContext.SaveChangesAsync();
        return addResult.Entity;
    }

    public async Task<SubscriptionType?> GetSubscriptionType(int subscriptionTypeId)
    {
        var subscriptionType =
            await _dbContext.SubscriptionTypes.FirstOrDefaultAsync(s => s.Id == subscriptionTypeId);

        return subscriptionType;
    }

    public async Task<SubscriptionType?> UpdateSubscriptionType(UpdateSubscriptionTypeRequest updateSubscriptionType)
    {
        var subscriptionType =
            await _dbContext.SubscriptionTypes.FirstOrDefaultAsync(s => s.Id == updateSubscriptionType.Id);
        if (subscriptionType is null)
        {
            return null;
        }

        subscriptionType.Name = updateSubscriptionType.Name;
        subscriptionType.LengthInDays = updateSubscriptionType.LengthInDays;

        var updateResult = _dbContext.SubscriptionTypes.Update(subscriptionType);
        await _dbContext.SaveChangesAsync();

        return updateResult.Entity;
    }

    public (IEnumerable<Models.Software.Software> softwareList, int totalCount) SearchSoftware(SoftwareSearchRequest searchRequest)
    {
        var softwares = _dbContext.Softwares
            .Include(s =>
                s.SubscriptionTypes.Where(st => !st.IsDeleted));
        IQueryable<Models.Software.Software> softwareSearch = searchRequest.SoftwareSearchOrder switch
        {
            SoftwareSearchOrder.Id => searchRequest.OrderDesc
                ? softwares.OrderByDescending(u => u.Id)
                : softwares.OrderBy(u => u.Id),
            SoftwareSearchOrder.Name => searchRequest.OrderDesc
                ? softwares.OrderByDescending(u => u.Name)
                : softwares.OrderBy(u => u.Name),
            _ => throw new ArgumentOutOfRangeException(nameof(searchRequest.SoftwareSearchOrder))
        };

        if (!string.IsNullOrEmpty(searchRequest.SearchText))
        {
            softwareSearch = softwareSearch.Where(u =>
                u.Name.Contains(searchRequest.SearchText, StringComparison.InvariantCultureIgnoreCase));
        }

        softwareSearch = softwareSearch.Where(x => !x.IsDeleted);

        var softwareList = softwareSearch.ToImmutableList();
        var totalCount = softwareList.Count;

        return (softwareSearch.Skip(searchRequest.PageSize * searchRequest.Page)
            .Take(searchRequest.PageSize + (searchRequest.PageSize * searchRequest.Page)), totalCount);
    }

    public async Task<bool> DeleteSoftware(int softwareId)
    {
        var software = await _dbContext.Softwares.FirstOrDefaultAsync(s => s.Id == softwareId);
        if (software is null || software.IsDeleted)
        {
            return false;
        }

        software.IsDeleted = true;
        software.DeletedAt = _dateTimeProvider.UtcNow;

        _dbContext.Softwares.Update(software);

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteSubscriptionType(int subscriptionTypeId)
    {
        var subscriptionType =
            await _dbContext.SubscriptionTypes.FirstOrDefaultAsync(s => s.Id == subscriptionTypeId);
        if (subscriptionType is null || subscriptionType.IsDeleted)
        {
            return false;
        }

        subscriptionType.IsDeleted = true;
        subscriptionType.DeletedAt = _dateTimeProvider.UtcNow;

        _dbContext.SubscriptionTypes.Update(subscriptionType);

        await _dbContext.SaveChangesAsync();

        return true;
    }
}