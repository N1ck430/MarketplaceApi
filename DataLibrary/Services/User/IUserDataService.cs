namespace DataLibrary.Services.User;

public interface IUserDataService
{
    public Task<int> GetHighestUserSequence();
}