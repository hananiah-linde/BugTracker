using BugTracker.Models.ChartModels;

namespace BugTracker.Models.ViewModels;

public class DashboardViewModel
{
    public Company Company { get; set; }
    public List<Project> Projects { get; set; }
    public List<Ticket> Tickets { get; set; }
    public List<BugTrackerUser> Members { get; set; }
}
