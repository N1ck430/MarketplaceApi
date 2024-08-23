namespace DataLibrary.Models.Responses.General;

public class TimeSpanResponse
{
    public int Days { get; set; }
    public int Hours { get; set; }
    public int Minutes { get; set; }

    public TimeSpanResponse()
    {
    }

    private TimeSpanResponse(TimeSpan timeSpan)
    {
        Days = timeSpan.Days;
        Hours = timeSpan.Hours;
        Minutes = timeSpan.Minutes;
    }

    public static implicit operator TimeSpanResponse(TimeSpan t) => new(t);
}