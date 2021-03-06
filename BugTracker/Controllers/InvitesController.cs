using BugTracker.Data;
using BugTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace BugTracker.Controllers;

public class InvitesController : Controller
{
    private readonly ApplicationDbContext _context;

    public InvitesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        IIncludableQueryable<Invite, Project> applicationDbContext = _context.Invites.Include(i => i.Company).Include(i => i.Invitee).Include(i => i.Invitor).Include(i => i.Project);
        return View(await applicationDbContext.ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Invite invite = await _context.Invites
            .Include(i => i.Company)
            .Include(i => i.Invitee)
            .Include(i => i.Invitor)
            .Include(i => i.Project)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (invite == null)
        {
            return NotFound();
        }

        return View(invite);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Id");
        ViewData["InviteeId"] = new SelectList(_context.Users, "Id", "Id");
        ViewData["InvitorId"] = new SelectList(_context.Users, "Id", "Id");
        ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,InviteDate,JoinDate,CompanyToken,CompanyId,ProjectId,InvitorId,InviteeId,InviteeEmail,InviteeFirstName,InviteeLastName,IsValid")] Invite invite)
    {
        if (ModelState.IsValid)
        {
            _context.Add(invite);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Id", invite.CompanyId);
        ViewData["InviteeId"] = new SelectList(_context.Users, "Id", "Id", invite.InviteeId);
        ViewData["InvitorId"] = new SelectList(_context.Users, "Id", "Id", invite.InvitorId);
        ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", invite.ProjectId);
        return View(invite);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Invite invite = await _context.Invites.FindAsync(id);
        if (invite == null)
        {
            return NotFound();
        }
        ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Id", invite.CompanyId);
        ViewData["InviteeId"] = new SelectList(_context.Users, "Id", "Id", invite.InviteeId);
        ViewData["InvitorId"] = new SelectList(_context.Users, "Id", "Id", invite.InvitorId);
        ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", invite.ProjectId);
        return View(invite);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,InviteDate,JoinDate,CompanyToken,CompanyId,ProjectId,InvitorId,InviteeId,InviteeEmail,InviteeFirstName,InviteeLastName,IsValid")] Invite invite)
    {
        if (id != invite.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(invite);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InviteExists(invite.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        ViewData["CompanyId"] = new SelectList(_context.Companies, "Id", "Id", invite.CompanyId);
        ViewData["InviteeId"] = new SelectList(_context.Users, "Id", "Id", invite.InviteeId);
        ViewData["InvitorId"] = new SelectList(_context.Users, "Id", "Id", invite.InvitorId);
        ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", invite.ProjectId);
        return View(invite);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Invite invite = await _context.Invites
            .Include(i => i.Company)
            .Include(i => i.Invitee)
            .Include(i => i.Invitor)
            .Include(i => i.Project)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (invite == null)
        {
            return NotFound();
        }

        return View(invite);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        Invite invite = await _context.Invites.FindAsync(id);
        _context.Invites.Remove(invite);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool InviteExists(int id)
    {
        return _context.Invites.Any(e => e.Id == id);
    }
}
