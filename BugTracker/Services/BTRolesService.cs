using BugTracker.Data;
using BugTracker.Models;
using BugTracker.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BugTracker.Services;

public class BTRolesService : IBTRolesService
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<BugTrackerUser> _userManager;

    public BTRolesService(ApplicationDbContext context, RoleManager<IdentityRole> roleManager, UserManager<BugTrackerUser> userManager)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<bool> AddUserToRoleAsync(BugTrackerUser user, string roleName)
    {
        bool result = (await _userManager.AddToRoleAsync(user, roleName)).Succeeded;
        return result;
    }

    #region Get Roles
    public async Task<List<IdentityRole>> GetRolesAsync()
    {
        try
        {
            List<IdentityRole> result = new();

            result = await _context.Roles.ToListAsync();

            return result;
        }
        catch (Exception)
        {

            throw;
        }
    }
    #endregion

    public async Task<string> GetRoleNameByIdAsync(string roleId)
    {
        IdentityRole role = _context.Roles.Find(roleId);
        string result = await _roleManager.GetRoleNameAsync(role);
        return result;
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(BugTrackerUser user)
    {
        IEnumerable<string> result = await _userManager.GetRolesAsync(user);
        return result;
    }

    public async Task<List<BugTrackerUser>> GetUsersInRoleAsync(string roleName, int companyId)
    {
        List<BugTrackerUser> users = (await _userManager.GetUsersInRoleAsync(roleName)).ToList();
        List<BugTrackerUser> result = users.Where(u => u.CompanyId == companyId).ToList();
        return result;
    }

    public async Task<List<BugTrackerUser>> GetUsersNotInRoleAsync(string roleName, int companyId)
    {
        List<string> userIds = (await _userManager.GetUsersInRoleAsync(roleName)).Select(u => u.Id).ToList();
        List<BugTrackerUser> roleUsers = _context.Users.Where(u => !userIds.Contains(u.Id)).ToList();

        List<BugTrackerUser> result = roleUsers.Where(u => u.CompanyId == companyId).ToList();
        return result;

    }

    public async Task<bool> IsUserInRoleAsync(BugTrackerUser user, string roleName)
    {
        bool result = await _userManager.IsInRoleAsync(user, roleName);
        return result;
    }

    public async Task<bool> RemoveUserFromRoleAsync(BugTrackerUser user, string roleName)
    {
        bool result = (await _userManager.RemoveFromRoleAsync(user, roleName)).Succeeded;
        return result;
    }

    public async Task<bool> RemoveUserFromRolesAsync(BugTrackerUser user, IEnumerable<string> roles)
    {
        bool result = (await _userManager.RemoveFromRolesAsync(user, roles)).Succeeded;
        return result;
    }
}
