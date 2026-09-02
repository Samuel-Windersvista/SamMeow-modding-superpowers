namespace _scorpion.Models;

public record StartEndDate
{
    public int StartMonth { get; set; }

    public int EndMonth { get; set; }

    public int StartDay { get; set; }

    public int EndDay { get; set; }
}
