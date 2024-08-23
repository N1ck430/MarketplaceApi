using System.Security.Cryptography.X509Certificates;
using DataLibrary.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace DataLibrary.Services.User;

public class UserDataService : IUserDataService
{
    private readonly MarketplaceDbContext _dbContext;

    public UserDataService(MarketplaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> GetHighestUserSequence()
    {
        return (await _dbContext.Users.OrderByDescending(u => u.UserSequenceId).FirstOrDefaultAsync())
            ?.UserSequenceId ?? 1;
    }
}