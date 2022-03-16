using BugTracker.Data;
using BugTracker.Models;
using BugTracker.Models.Enums;
using BugTracker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BugTracker.Services;

public class BTProjectService : IBTProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly IBTRolesService _rolesService;

    public BTProjectService(ApplicationDbContext context, IBTRolesService roleService)
    {
        _context = context;
        _rolesService = roleService;
    }

    #region Add New Project
    public async Task AddNewProjectAsync(Project project)
    {
        _context.Add(project);
        await _context.SaveChangesAsync();
    }
    #endregion

    #region Add Project Manager
    public async Task<bool> AddProjectManagerAsync(string userId, int projectId)
    {
        BugTrackerUser currentPM = await GetProjectManagerAsync(projectId);

        if (currentPM != null)
        {
            try
            {
                await RemoveProjectManagerAsync(projectId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"**** ERROR **** Error Removing current PM. ---> {ex.Message}");
                return false;
            }
        }

        try
        {
            await AddUserToProjectAsync(userId, projectId);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"**** ERROR **** Error adding new PM. ---> {ex.Message}");
            return false;
        }

    }
    #endregion

    #region Add User To Project
    public async Task<bool> AddUserToProjectAsync(string userId, int projectId)
    {
        BugTrackerUser user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user != null)
        {
            Project project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

            if (!await IsUserOnProjectAsync(userId, projectId))
            {
                try
                {
                    project.Members.Add(user);
                    await _context.SaveChangesAsync();
                    return true;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }
    #endregion

    #region Archive Project
    public async Task ArchiveProjectAsync(Project project)
    {
        try
        {
            project.Archived = true;
            await UpdateProjectAsync(project);

            foreach (Ticket ticket in project.Tickets)
            {
                ticket.ArchivedByProject = true;
                _context.Update(ticket);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception)
        {

            throw;
        }
    }
    #endregion

    #region Get All Project Members Except PM
    public async Task<List<BugTrackerUser>> GetAllProjectMembersExceptPMAsync(int projectId)
    {
        List<BugTrackerUser> developers = await GetProjectMembersByRoleAsync(projectId, Roles.Developer.ToString());
        List<BugTrackerUser> submitters = await GetProjectMembersByRoleAsync(projectId, Roles.Submitter.ToString());
        List<BugTrackerUser> admins = await GetProjectMembersByRoleAsync(projectId, Roles.Admin.ToString());

        List<BugTrackerUser> teamMembers = developers.Concat(submitters).Concat(admins).ToList();

        return teamMembers;
    }
    #endregion

    #region Get All Projects By Company
    public async Task<List<Project>> GetAllProjectsByCompanyAsync(int companyId)
    {
        List<Project> projects = new();
        projects = await _context.Projects.Where(p => p.CompanyId == companyId && p.Archived == false)
                                          .Include(p => p.Members)
                                          .Include(p => p.Tickets)
                                              .ThenInclude(t => t.Comments)
                                          .Include(p => p.Tickets)
                                              .ThenInclude(t => t.Attachments)
                                          .Include(p => p.Tickets)
                                              .ThenInclude(t => t.History)
                                          .Include(p => p.Tickets)
                                              .ThenInclude(t => t.Notifications)
                                          .Include(p => p.Tickets)
                                              .ThenInclude(t => t.DeveloperUser)
                                          .Include(p => p.Tickets)
                                              .ThenInclude(t => t.OwnerUser)
                                          .Include(p => p.Tickets)
                                              .ThenInclude(t => t.TicketStatus)
                                          .Include(p => p.Tickets)
                                              .ThenInclude(t => t.TicketPriority)
                                          .Include(p => p.Tickets)
                                              .ThenInclude(t => t.TicketType)
                                          .Include(p => p.ProjectPriority)
                                          .ToListAsync();
        return projects;
    }
    #endregion

    #region Get All Projects By Priority
    public async Task<List<Project>> GetAllProjectsByPriorityAsync(int companyId, string priorityName)
    {
        List<Project> projects = await GetAllProjectsByCompanyAsync(companyId);
        int priorityId = await LookupProjectPriorityId(priorityName);

        return projects.Where(p => p.ProjectPriorityId == priorityId).ToList();

    }
    #endregion

    #region Get Archived Project By Company
    public async Task<List<Project>> GetArchivedProjectsByCompanyAsync(int companyId)
    {
        try
        {
            List<Project> projects = await _context.Projects.Where(p => p.CompanyId == companyId && p.Archived == true)
                                                              .Include(p => p.Members)
                                                              .Include(p => p.Tickets)
                                                                  .ThenInclude(t => t.Comments)
                                                              .Include(p => p.Tickets)
                                                                  .ThenInclude(t => t.Attachments)
                                                              .Include(p => p.Tickets)
                                                                  .ThenInclude(t => t.History)
                                                              .Include(p => p.Tickets)
                                                                  .ThenInclude(t => t.Notifications)
                                                              .Include(p => p.Tickets)
                                                                  .ThenInclude(t => t.DeveloperUser)
                                                              .Include(p => p.Tickets)
                                                                  .ThenInclude(t => t.OwnerUser)
                                                              .Include(p => p.Tickets)
                                                                  .ThenInclude(t => t.TicketStatus)
                                                              .Include(p => p.Tickets)
                                                                  .ThenInclude(t => t.TicketPriority)
                                                              .Include(p => p.Tickets)
                                                                  .ThenInclude(t => t.TicketType)
                                                              .Include(p => p.ProjectPriority)
                                                              .ToListAsync();
            return projects;
        }
        catch (Exception)
        {

            throw;
        }

    }
    #endregion

    #region Get Developers On Project
    public Task<List<BugTrackerUser>> GetDevelopersOnProjectAsync(int projectId)
    {
        throw new NotImplementedException();
    }
    #endregion

    #region Get Project By Id
    public async Task<Project> GetProjectByIdAsync(int projectId, int companyId)
    {
        Project project = await _context.Projects
                                        .Include(p => p.Tickets)
                                            .ThenInclude(t => t.TicketPriority)
                                        .Include(p => p.Tickets)
                                            .ThenInclude(t => t.TicketStatus)
                                        .Include(p => p.Tickets)
                                            .ThenInclude(t => t.TicketType)
                                        .Include(p => p.Tickets)
                                            .ThenInclude(t => t.DeveloperUser)
                                        .Include(p => p.Tickets)
                                            .ThenInclude(t => t.OwnerUser)
                                        .Include(p => p.Members)
                                        .Include(p => p.ProjectPriority)
                                        .FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId);
        return project;
    }
    #endregion

    #region Get Project Manager
    public async Task<BugTrackerUser> GetProjectManagerAsync(int projectId)
    {
        Project project = await _context.Projects
                                        .Include(p => p.Members)
                                        .FirstOrDefaultAsync(p => p.Id == projectId);

        foreach (BugTrackerUser member in project?.Members)
        {
            if (await _rolesService.IsUserInRoleAsync(member, Roles.ProjectManager.ToString()))
            {
                return member;
            }
        }

        return null;
    }
    #endregion

    #region Get Project Members By Role
    public async Task<List<BugTrackerUser>> GetProjectMembersByRoleAsync(int projectId, string role)
    {
        Project project = await _context.Projects
                                        .Include(p => p.Members)
                                        .FirstOrDefaultAsync(p => p.Id == projectId);

        List<BugTrackerUser> members = new();

        foreach (BugTrackerUser user in project.Members)
        {
            if (await _rolesService.IsUserInRoleAsync(user, role))
            {
                members.Add(user);
            }
        }

        return members;
    }
    #endregion

    #region Get Submitters On Project
    public Task<List<BugTrackerUser>> GetSubmittersOnProjectAsync(int projectId)
    {
        throw new NotImplementedException();
    }
    #endregion

    #region Get Unassigned Projects
    public async Task<List<Project>> GetUnassignedProjectsAsync(int companyId)
    {
        List<Project> result = new();
        List<Project> projects = new();

        try
        {
            projects = await _context.Projects.Include(p => p.ProjectPriority)
                                              .Where(p => p.CompanyId == companyId)
                                              .ToListAsync();

            foreach (Project project in projects)
            {
                if ((await GetProjectMembersByRoleAsync(project.Id, nameof(Roles.ProjectManager))).Count == 0)
                {
                    result.Add(project);
                }
            }
        }
        catch (Exception)
        {

            throw;
        }

        return result;
    }
    #endregion

    #region Get User Projects
    public async Task<List<Project>> GetUserProjectsAsync(string userId)
    {
        try
        {
            List<Project> userProjects = (await _context.Users
                                                        .Include(u => u.Projects)
                                                            .ThenInclude(p => p.Company)
                                                        .Include(u => u.Projects)
                                                            .ThenInclude(p => p.Members)
                                                        .Include(u => u.Projects)
                                                           .ThenInclude(p => p.Tickets)
                                                        .Include(u => u.Projects)
                                                            .ThenInclude(t => t.Tickets)
                                                                .ThenInclude(t => t.DeveloperUser)
                                                        .Include(u => u.Projects)
                                                            .ThenInclude(t => t.Tickets)
                                                                .ThenInclude(t => t.OwnerUser)
                                                        .Include(u => u.Projects)
                                                            .ThenInclude(t => t.Tickets)
                                                                .ThenInclude(t => t.TicketPriority)
                                                        .Include(u => u.Projects)
                                                            .ThenInclude(t => t.Tickets)
                                                                .ThenInclude(t => t.TicketStatus)
                                                        .Include(u => u.Projects)
                                                            .ThenInclude(t => t.Tickets)
                                                                .ThenInclude(t => t.TicketType)
                                                        .FirstOrDefaultAsync(u => u.Id == userId)).Projects.ToList();

            return userProjects;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"**** ERROR **** Error Getting usser projects list. ---> {ex.Message}");
            throw;
        }
    }
    #endregion

    #region Get Users Not On Project
    public async Task<List<BugTrackerUser>> GetUsersNotOnProjectAsync(int projectId, int companyId)
    {
        List<BugTrackerUser> users = await _context.Users.Where(u => u.Projects.All(p => p.Id != projectId)).ToListAsync();

        return users.Where(u => u.CompanyId == companyId).ToList();
    }
    #endregion

    #region Is Assigned Project Manager
    public async Task<bool> IsAssignedProjectManagerAsync(string userId, int projectId)
    {
        try
        {
            string projectManagerId = (await GetProjectManagerAsync(projectId))?.Id;

            if (projectManagerId == userId)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception)
        {

            throw;
        }
    }
    #endregion

    #region Is User On Project
    public async Task<bool> IsUserOnProjectAsync(string userId, int projectId)
    {
        Project project = await _context.Projects
                                        .Include(p => p.Members)
                                        .FirstOrDefaultAsync(p => p.Id == projectId);

        bool result = false;

        if (project != null)
        {
            result = project.Members.Any(m => m.Id == userId);
        }
        return result;
    }
    #endregion

    #region Lookup Project Priority Id
    public async Task<int> LookupProjectPriorityId(string priorityName)
    {
        int priorityId = (await _context.ProjectPriorities.FirstOrDefaultAsync(p => p.Name == priorityName)).Id;
        return priorityId;
    }
    #endregion

    #region Remove Project Manager
    public async Task RemoveProjectManagerAsync(int projectId)
    {
        Project project = await _context.Projects
                                        .Include(p => p.Members)
                                        .FirstOrDefaultAsync(p => p.Id == projectId);

        try
        {
            foreach (BugTrackerUser member in project?.Members)
            {
                if (await _rolesService.IsUserInRoleAsync(member, Roles.ProjectManager.ToString()))
                {
                    await RemoveUserFromProjectAsync(member.Id, projectId);
                }
            }
        }
        catch
        {
            throw;
        }
    }
    #endregion

    #region Remove User From Project
    public async Task RemoveUserFromProjectAsync(string userId, int projectId)
    {
        try
        {
            BugTrackerUser user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            Project project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

            try
            {
                if (await IsUserOnProjectAsync(userId, projectId))
                {
                    project.Members.Remove(user);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"**** ERROR **** Error Removing User from project. ---> {ex.Message}");
        }
    }
    #endregion

    #region Remove Users From Project By Role
    public async Task RemoveUsersFromProjectByRoleAsync(string role, int projectId)
    {
        try
        {
            List<BugTrackerUser> members = await GetProjectMembersByRoleAsync(projectId, role);
            Project project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

            foreach (BugTrackerUser user in members)
            {
                try
                {
                    project.Members.Remove(user);
                    await _context.SaveChangesAsync();
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"**** ERROR **** Error Removing Users from project. ---> {ex.Message}");
            throw;
        }
    }
    #endregion

    #region Restore Project
    public async Task RestoreProjectAsync(Project project)
    {
        try
        {
            project.Archived = false;
            await UpdateProjectAsync(project);

            foreach (Ticket ticket in project.Tickets)
            {
                ticket.ArchivedByProject = false;
                _context.Update(ticket);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception)
        {

            throw;
        }
    }
    #endregion

    #region Update Project
    public async Task UpdateProjectAsync(Project project)
    {
        _context.Update(project);
        await _context.SaveChangesAsync();
    }
    #endregion

}
