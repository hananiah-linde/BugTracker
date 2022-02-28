using Microsoft.AspNetCore.Mvc.Rendering;

namespace BugTracker.Models.ViewModels;

public class ManageUserRolesViewModel
{
    public BugTrackerUser BugTrackerUser { get; set; }
    public MultiSelectList Roles { get; set; }
    public List<string> SelectedRoles { get; set; }
}
