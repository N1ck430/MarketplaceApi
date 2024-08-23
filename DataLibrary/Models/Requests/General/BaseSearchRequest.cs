namespace DataLibrary.Models.Requests.General;

public class BaseSearchRequest
{
    public string? SearchText { get; set; }
    public int Page { get; set; } = 0;
    public int PageSize { get; set; } = 20;
}