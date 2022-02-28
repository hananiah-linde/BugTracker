using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BugTracker.Data;
using BugTracker.Models;
using Microsoft.AspNetCore.Identity;
using BugTracker.Extensions;
using BugTracker.Models.Enums;
using BugTracker.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using BugTracker.Models.ViewModels;

namespace BugTracker.Controllers;

public class TicketsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<BugTrackerUser> _userManager;
    private readonly IBTProjectService _projectService;
    private readonly IBTLookupService _lookupService;
    private readonly IBTTicketService _ticketService;
    private readonly IBTFileService _fileService;
    private readonly IBTTicketHistoryService _historyService;

    public TicketsController(ApplicationDbContext context, UserManager<BugTrackerUser> userManager, IBTProjectService projectService, IBTLookupService lookupService, IBTTicketService ticketService, IBTFileService fileService, IBTTicketHistoryService historyService)
    {
        _context = context;
        _userManager = userManager;
        _projectService = projectService;
        _lookupService = lookupService;
        _ticketService = ticketService;
        _fileService = fileService;
        _historyService = historyService;
    }

    #region GET Index
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var applicationDbContext = _context.Tickets.Include(t => t.DeveloperUser).Include(t => t.OwnerUser).Include(t => t.Project).Include(t => t.TicketPriority).Include(t => t.TicketStatus).Include(t => t.TicketType);
        return View(await applicationDbContext.ToListAsync());
    }
    #endregion

    #region GET My Tickets
    [HttpGet]
    public async Task<IActionResult> MyTickets()
    {
        BugTrackerUser bugTrackerUser = await _userManager.GetUserAsync(User);

        List<Ticket> tickets = await _ticketService.GetTicketsByUserIdAsync(bugTrackerUser.Id, bugTrackerUser.CompanyId);

        return View(tickets);
    }
    #endregion

    #region GET All Tickets
    [HttpGet]
    public async Task<IActionResult> AllTickets()
    {
        int companyId = User.Identity.GetCompanyId().Value;

        List<Ticket> tickets = await _ticketService.GetAllTicketsByCompanyAsync(companyId);

        if (User.IsInRole(nameof(Roles.Developer)) || User.IsInRole(nameof(Roles.Submitter)))
        {
            return View(tickets.Where(t => t.Archived == false));
        }
        else
        {
            return View(tickets);
        }
    }
    #endregion

    #region GET Archived Tickets
    [HttpGet]
    public async Task<IActionResult> ArchivedTickets()
    {
        int companyId = User.Identity.GetCompanyId().Value;

        List<Ticket> tickets = await _ticketService.GetArchivedTicketsAsync(companyId);

        return View(tickets);
    }
    #endregion

    #region GET Unassigned Tickets
    [HttpGet]
    [Authorize(Roles = "Admin,ProjectManager")]
    public async Task<IActionResult> UnassignedTickets()
    {
        int companyId = User.Identity.GetCompanyId().Value;

        List<Ticket> tickets = await _ticketService.GetUnassignedTicketsAsync(companyId);

        string bugTrackerUserId = _userManager.GetUserId(User);

        if(User.IsInRole(nameof(Roles.Admin)))
        {
            return View(tickets);
        }
        else
        {
            List<Ticket> pmTickets = new();

            foreach (Ticket ticket in tickets)
            {
                if(await _projectService.IsAssignedProjectManagerAsync(bugTrackerUserId, ticket.ProjectId))
                {
                    pmTickets.Add(ticket);
                }
            }
            return View(pmTickets);
        }
    }
    #endregion

    #region GET Assign Developer
    [HttpGet]
    public async Task<IActionResult> AssignDeveloper(int id)
    {
        AssignDeveloperViewModel model = new();
        model.Ticket = await _ticketService.GetTicketByIdAsync(id);
        model.Developers = new SelectList(await _projectService.GetProjectMembersByRoleAsync(model.Ticket.ProjectId, nameof(Roles.Developer)), "Id", "FullName");
        return View(model);
    }
    #endregion

    #region POST Assign Developer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignDeveloper(AssignDeveloperViewModel model)
    {
        if (model.DeveloperId != null)
        {
            await _ticketService.AssignTicketAsync(model.Ticket.Id, model.DeveloperId);
            return RedirectToAction(nameof(Details), new { id = model.Ticket.Id });
        }

        return RedirectToAction(nameof(AssignDeveloper), new { id = model.Ticket.Id });
    }
    #endregion

    #region GET Details
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

        if (ticket == null)
        {
            return NotFound();
        }

        return View(ticket);
    }
    #endregion

    #region GET Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        BugTrackerUser bugTrackerUser = await _userManager.GetUserAsync(User);

        int companyId = User.Identity.GetCompanyId().Value;

        if (User.IsInRole(nameof(Roles.Admin)))
        {
            ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjectsByCompanyAsync(companyId), "Id", "Name");
        }
        else
        {
            ViewData["ProjectId"] = new SelectList(await _projectService.GetUserProjectsAsync(bugTrackerUser.Id), "Id", "Name");
        }


        ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name");
        ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name");
        return View();
    }
    #endregion

    #region POST Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Title,Description,ProjectId,TicketTypeId,TicketPriorityId")] Ticket ticket)
    {

        BugTrackerUser bugTrackerUser = await _userManager.GetUserAsync(User);

        if (ModelState.IsValid)
        {


            ticket.Created = DateTimeOffset.Now;
            ticket.OwnerUserId = bugTrackerUser.Id;

            ticket.TicketStatusId = (await _ticketService.LookupTicketStatusIdAsync(nameof(BTTicketStatus.New))).Value;

            await _ticketService.AddNewTicketAsync(ticket);
            //TODO: Ticket History
            //TODO: Ticket Notification
            return RedirectToAction(nameof(Index));
        }


        if (User.IsInRole(nameof(Roles.Admin)))
        {
            ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjectsByCompanyAsync(bugTrackerUser.CompanyId), "Id", "Name");
        }
        else
        {
            ViewData["ProjectId"] = new SelectList(await _projectService.GetUserProjectsAsync(bugTrackerUser.Id), "Id", "Name");
        }


        ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name");
        ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name");
        return View(ticket);
    }
    #endregion

    #region GET Edit
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

        if (ticket == null)
        {
            return NotFound();
        }

        ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
        ViewData["TicketStatusId"] = new SelectList(await _lookupService.GetTicketStatusesAsync(), "Id", "Name", ticket.TicketStatusId);
        ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name", ticket.TicketTypeId);

        return View(ticket);

    }
    #endregion

    #region POST Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Created,Updated,Archived,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,OwnerUserId,DeveloperUserId")] Ticket ticket)
    {
        if (id != ticket.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            BugTrackerUser bugTrackerUser = await _userManager.GetUserAsync(User);

            Ticket oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);

            try
            {
                ticket.Updated = DateTimeOffset.Now;
                await _ticketService.UpdateTicketAsync(ticket);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TicketExists(ticket.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);
            await _historyService.AddHistoryAsync(oldTicket, newTicket, bugTrackerUser.Id);

            return RedirectToAction(nameof(Index));
        }

        ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
        ViewData["TicketStatusId"] = new SelectList(await _lookupService.GetTicketStatusesAsync(), "Id", "Name", ticket.TicketStatusId);
        ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name", ticket.TicketTypeId);

        return View(ticket);

    }
    #endregion

    #region POST Add Ticket Comment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTicketComment([Bind("Id,TicketId,Comment")] TicketComment ticketComment)
    {
        if (ModelState.IsValid)
        {
            try
            {
                ticketComment.UserId = _userManager.GetUserId(User);
                ticketComment.Created = DateTimeOffset.Now;

                await _ticketService.AddTicketCommentAsync(ticketComment);
            }
            catch (Exception)
            {

                throw;
            }
        }

        return RedirectToAction("Details", new { id = ticketComment.Id });
    }
    #endregion

    #region POST Add Ticket Attachment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTicketAttachment([Bind("Id,FormFile,Description,TicketId")] TicketAttachment ticketAttachment)
    {
        string statusMessage;

        if (ModelState.IsValid && ticketAttachment.FormFile != null)
        {
            ticketAttachment.FileData = await _fileService.ConvertFileToByteArrayAsync(ticketAttachment.FormFile);
            ticketAttachment.Filename = ticketAttachment.FormFile.FileName;
            ticketAttachment.FileContentType = ticketAttachment.FormFile.ContentType;

            ticketAttachment.Created = DateTimeOffset.Now;
            ticketAttachment.UserId = _userManager.GetUserId(User);

            await _ticketService.AddTicketAttachmentAsync(ticketAttachment);
            statusMessage = "Success: New attachment added to Ticket.";
        }
        else
        {
            statusMessage = "Error: Invalid data.";

        }

        return RedirectToAction("Details", new { id = ticketAttachment.TicketId, message = statusMessage });
    }
    #endregion

    #region GET Show File
    [HttpGet]
    public async Task<IActionResult> ShowFile(int id)
    {
        TicketAttachment ticketAttachment = await _ticketService.GetTicketAttachmentByIdAsync(id);
        string fileName = ticketAttachment.Filename;
        byte[] fileData = ticketAttachment.FileData;
        string ext = Path.GetExtension(fileName).Replace(".", "");

        Response.Headers.Add("Content-Disposition", $"inline; filename={fileName}");
        return File(fileData, $"application/{ext}");
    }
    #endregion

    #region GET Archive
    [HttpGet]
    public async Task<IActionResult> Archive(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

        if (ticket == null)
        {
            return NotFound();
        }

        return View(ticket);
    }
    #endregion

    #region POST Archive
    [HttpPost, ActionName("Archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveConfirmed(int id)
    {
        Ticket ticket = await _ticketService.GetTicketByIdAsync(id);
        ticket.Archived = true;

        await _ticketService.UpdateTicketAsync(ticket);

        return RedirectToAction(nameof(Index));
    }
    #endregion

    #region GET Restore
    [HttpGet]
    public async Task<IActionResult> Restore(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

        if (ticket == null)
        {
            return NotFound();
        }

        return View(ticket);
    }
    #endregion

    #region POST Restore
    [HttpPost, ActionName("Restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreConfirmed(int id)
    {
        Ticket ticket = await _ticketService.GetTicketByIdAsync(id);
        ticket.Archived = false;

        await _ticketService.UpdateTicketAsync(ticket);

        return RedirectToAction(nameof(Index));
    }
    #endregion

    #region Ticket Exists
    private async Task<bool> TicketExists(int id)
    {
        int companyId = User.Identity.GetCompanyId().Value;

        return (await _ticketService.GetAllTicketsByCompanyAsync(companyId)).Any(t => t.Id == id);
    } 
    #endregion

}
