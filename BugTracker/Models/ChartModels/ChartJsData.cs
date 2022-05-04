namespace BugTracker.Models.ChartModels;

public class ChartJsData
{
    public ChartJsItem[] Data { get; set; }
}

public class ChartJsItem
{
    public string Project { get; set; }
    public int Tickets { get; set; }
    public int Developers { get; set; }
}