namespace BugTracker.Models.ChartModels;

public class ChartJsData
{
    public ChartItem[] Data { get; set; }
}

public class ChartItem
{
    public string Project { get; set; }
    public int Tickets { get; set; }
    public int Developers { get; set; }
}
