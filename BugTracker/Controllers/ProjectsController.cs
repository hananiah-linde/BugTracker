using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.Models;
using BugTracker.Extensions;
using BugTracker.Models.ViewModels;
using BugTracker.Services.Interfaces;
using BugTracker.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace BugTracker.Controllers;

public class ProjectsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IBTRolesService _rolesService;
    private readonly IBTLookupService _lookupService;
    private readonly IBTFileService _fileService;
    private readonly IBTProjectService _projectService;
    private readonly UserManager<BugTrackerUser> _userManager;
    private readonly IBTCompanyInfoService _companyInfoService;

    public ProjectsController(ApplicationDbContext context, IBTRolesService rolesService, IBTLookupService lookupsService, IBTFileService fileService, IBTProjectService projectService, UserManager<BugTrackerUser> userManager, IBTCompanyInfoService companyInfoService)
    {
        _context = context;
        _rolesService = rolesService;
        _lookupService = lookupsService;
        _fileService = fileService;
        _projectService = projectService;
        _userManager = userManager;
        _companyInfoService = companyInfoService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var applicationDbContext = _context.Projects.Include(p => p.Company).Include(p => p.ProjectPriority);
        return View(await applicationDbContext.ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> MyProjects()
    {
        string userId = _userManager.GetUserId(User);
        List<Project> projects = await _projectService.GetUserProjectsAsync(userId);
        return View(projects);
    }

    [HttpGet]
    public async Task<IActionResult> AllProjects()
    {

        List<Project> projects = new();

        int companyId = User.Identity.GetCompanyId().Value;

        if(User.IsInRole(nameof(Roles.Admin)) || User.IsInRole(nameof(Roles.ProjectManager)))
        {
            projects = await _companyInfoService.GetAllProjectsAsync(companyId);
        }
        else
        {
            projects = await _projectService.GetAllProjectsByCompanyAsync(companyId);
        }
        return View(projects);
    }

    [HttpGet]
    public async Task<IActionResult> ArchivedProjects()
    {
        int companyId = User.Identity.GetCompanyId().Value;

        List<Project> projects = await _projectService.GetArchivedProjectsByCompanyAsync(companyId);

        return View(projects);
    }

    [HttpGet]
    public async Task<IActionResult> UnassignedProjects()
    {
        int companyId = User.Identity.GetCompanyId().Value;

        List<Project> projects = new();

        projects = await _projectService.GetUnassignedProjectsAsync(companyId);

        return View(projects);
    }

    [HttpGet]
    public async Task<IActionResult> AssignPM(int projectId)
    {
        int companyId = User.Identity.GetCompanyId().Value;

        AssignPMViewModel model = new();

        model.Project = await _projectService.GetProjectByIdAsync(projectId, companyId);
        model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(Roles.ProjectManager), companyId), "Id", "FullName");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignPM(AssignPMViewModel model)
    {
        if (!string.IsNullOrEmpty(model.PMID))
        {
            await _projectService.AddProjectManagerAsync(model.PMID, model.Project.Id);

            return RedirectToAction(nameof(Details), new { id = model.Project.Id });
        }

        return RedirectToAction(nameof(AssignPM), new { projectId = model.Project.Id });
    }

    [HttpGet]
    public async Task<IActionResult> AssignMembers(int id)
    {
        int companyId = User.Identity.GetCompanyId().Value;

        ProjectMembersViewModel model = new();

        model.Project = await _projectService.GetProjectByIdAsync(id, companyId);

        List<BugTrackerUser> developers = await _rolesService.GetUsersInRoleAsync(nameof(Roles.Developer), companyId);

        List<BugTrackerUser> submitters = await _rolesService.GetUsersInRoleAsync(nameof(Roles.Submitter), companyId);

        List<BugTrackerUser> companyMembers = developers.Concat(submitters).ToList();

        List<string> projectMembers = model.Project.Members.Select(m => m.Id).ToList();

        model.Users = new MultiSelectList(companyMembers, "Id", "FullName", projectMembers);

        return View(model);

    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignMembers(ProjectMembersViewModel model)
    {
        if (model.SelectedUsers != null)
        {
            List<string> memberIds = (await _projectService.GetAllProjectMembersExceptPMAsync(model.Project.Id)).Select(m => m.Id).ToList();

            foreach (string member in memberIds)
            {
                await _projectService.RemoveUserFromProjectAsync(member, model.Project.Id);
            }

            foreach (string member in model.SelectedUsers)
            {
                await _projectService.AddUserToProjectAsync(member, model.Project.Id);
            }

            return RedirectToAction("Details", "Projects", new {id = model.Project.Id });
        }

        return RedirectToAction(nameof(AssignMembers), new { id = model.Project.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        int companyId = User.Identity.GetCompanyId().Value;

        Project project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

        if (project == null)
        {
            return NotFound();
        }

        return View(project);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        int companyId = User.Identity.GetCompanyId().Value;

        AddProjectWithPMViewModel model = new();

        //Load up selectlists with data
        model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(Roles.ProjectManager.ToString(), companyId), "Id", "FullName");
        model.PriorityList = new SelectList(await _lookupService.GetProjectPrioritiesAsync(), "Id", "Name");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AddProjectWithPMViewModel model)
    {
        if (model != null)
        {
            int companyId = User.Identity.GetCompanyId().Value;

            try
            {
                if (model.Project.ImageFormFile != null)
                {
                    model.Project.ImageFileData = await _fileService.ConvertFileToByteArrayAsync(model.Project.ImageFormFile);
                    model.Project.ImageFilename = model.Project.ImageFormFile.FileName;
                    model.Project.ImageContentType = model.Project.ImageFormFile.ContentType;
                }

                model.Project.CompanyId = companyId;

                await _projectService.AddNewProjectAsync(model.Project);

                if (!string.IsNullOrEmpty(model.PMId))
                {
                    await _projectService.AddProjectManagerAsync(model.PMId, model.Project.Id);
                }
            }
            catch (Exception)
            {

                throw;
            }

            //TODO: Redired to All Projects
            return RedirectToAction("Index");
        }

        return RedirectToAction("Create");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        int companyId = User.Identity.GetCompanyId().Value;

        AddProjectWithPMViewModel model = new();

        model.Project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

        model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(Roles.ProjectManager.ToString(), companyId), "Id", "FullName");
        model.PriorityList = new SelectList(await _lookupService.GetProjectPrioritiesAsync(), "Id", "Name");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AddProjectWithPMViewModel model)
    {
        if (model != null)
        {

            try
            {
                if (model.Project.ImageFormFile != null)
                {
                    model.Project.ImageFileData = await _fileService.ConvertFileToByteArrayAsync(model.Project.ImageFormFile);
                    model.Project.ImageFilename = model.Project.ImageFormFile.FileName;
                    model.Project.ImageContentType = model.Project.ImageFormFile.ContentType;
                }

                await _projectService.UpdateProjectAsync(model.Project);

                if (!string.IsNullOrEmpty(model.PMId))
                {
                    await _projectService.AddProjectManagerAsync(model.PMId, model.Project.Id);
                }
            }
            catch (Exception)
            {

                throw;
            }

            //TODO: Redired to All Projects
            return RedirectToAction("Index");
        }

        return RedirectToAction("Edit");
    }

    [HttpGet]
    public async Task<IActionResult> Archive(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        int companyId = User.Identity.GetCompanyId().Value;

        var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);


        if (project == null)
        {
            return NotFound();
        }

        return View(project);
    }

    [HttpPost, ActionName("Archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveConfirmed(int id)
    {
        int companyId = User.Identity.GetCompanyId().Value;
        var project = await _projectService.GetProjectByIdAsync(id, companyId);

        await _projectService.ArchiveProjectAsync(project);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Restore(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        int companyId = User.Identity.GetCompanyId().Value;

        var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);


        if (project == null)
        {
            return NotFound();
        }

        return View(project);
    }

    [HttpPost, ActionName("Restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreConfirmed(int id)
    {
        int companyId = User.Identity.GetCompanyId().Value;

        var project = await _projectService.GetProjectByIdAsync(id, companyId);
        await _projectService.RestoreProjectAsync(project);

        return RedirectToAction(nameof(Index));
    }

    private bool ProjectExists(int id)
    {
        return _context.Projects.Any(e => e.Id == id);
    }
}
