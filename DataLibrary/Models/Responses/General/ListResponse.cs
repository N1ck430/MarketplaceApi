namespace DataLibrary.Models.Responses.General;

public class ListResponse<T>
{
    public IEnumerable<T> ListEntries { get; set; } = null!;
    public int Count { get; set; }
}