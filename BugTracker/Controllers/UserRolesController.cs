using BugTracker.Extensions;
using BugTracker.Models;
using BugTracker.Models.ViewModels;
using BugTracker.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BugTracker.Controllers;

[Authorize]
public class UserRolesController : Controller
{

    private readonly IBTRolesService _rolesService;
    private readonly IBTCompanyInfoService _companyInfoService;

    public UserRolesController(IBTRolesService rolesService, IBTCompanyInfoService companyInfoService)
    {
        _rolesService = rolesService;
        _companyInfoService = companyInfoService;
    }

    [HttpGet]
    public async Task<IActionResult> ManageUserRoles()
    {
        //Add an instance of the ViewModel as a list
        List<ManageUserRolesViewModel> model = new();

        //Get CompanyId
        int companyId = User.Identity.GetCompanyId().Value;

        //Get all company users
        List<BugTrackerUser> users = await _companyInfoService.GetAllMembersAsync(companyId);

        //Loop over the users to populate the ViewModel
        foreach (BugTrackerUser user in users)
        {
            ManageUserRolesViewModel viewModel = new();
            viewModel.BugTrackerUser = user;
            IEnumerable<string> selected = await _rolesService.GetUserRolesAsync(user);
            viewModel.Roles = new MultiSelectList(await _rolesService.GetRolesAsync(), "Name", "Name", selected);

            model.Add(viewModel);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageUserRoles(ManageUserRolesViewModel member)
    {
        //Get the company Id
        int companyId = User.Identity.GetCompanyId().Value;

        //Instantiate user
        BugTrackerUser bugTrackerUser = (await _companyInfoService.GetAllMembersAsync(companyId)).FirstOrDefault(u => u.Id == member.BugTrackerUser.Id);

        //Get roles for the user
        IEnumerable<string> roles = await _rolesService.GetUserRolesAsync(bugTrackerUser);

        //Grab the selected roles
        string userRole = member.SelectedRoles.FirstOrDefault();

        if (!string.IsNullOrEmpty(userRole))
        {
            //Remove from their roles
            if (await _rolesService.RemoveUserFromRolesAsync(bugTrackerUser, roles))
            {
                //Add user to new role
                await _rolesService.AddUserToRoleAsync(bugTrackerUser, userRole);
            }

        }

        //Navigate back to the view
        return RedirectToAction(nameof(ManageUserRoles));

    }

}
