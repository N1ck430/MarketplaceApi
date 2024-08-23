namespace DataLibrary.Services.DateTime;

public interface IDateTimeProvider
{
    public System.DateTime UtcNow { get; }
}