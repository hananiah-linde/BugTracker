using BugTracker.Data;
using BugTracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BugTracker.Controllers;

public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Notification, Ticket> applicationDbContext = _context.Notifications.Include(n => n.Recipient).Include(n => n.Sender).Include(n => n.Ticket);
        return View(await applicationDbContext.ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Notification notification = await _context.Notifications
            .Include(n => n.Recipient)
            .Include(n => n.Sender)
            .Include(n => n.Ticket)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (notification == null)
        {
            return NotFound();
        }

        return View(notification);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["RecipientId"] = new SelectList(_context.Users, "Id", "Id");
        ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Id");
        ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Description");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,TicketId,Title,Message,Created,RecipientId,SenderId,Viewed")] Notification notification)
    {
        if (ModelState.IsValid)
        {
            _context.Add(notification);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewData["RecipientId"] = new SelectList(_context.Users, "Id", "Id", notification.RecipientId);
        ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Id", notification.SenderId);
        ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Description", notification.TicketId);
        return View(notification);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Notification notification = await _context.Notifications.FindAsync(id);
        if (notification == null)
        {
            return NotFound();
        }
        ViewData["RecipientId"] = new SelectList(_context.Users, "Id", "Id", notification.RecipientId);
        ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Id", notification.SenderId);
        ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Description", notification.TicketId);
        return View(notification);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,TicketId,Title,Message,Created,RecipientId,SenderId,Viewed")] Notification notification)
    {
        if (id != notification.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(notification);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NotificationExists(notification.Id))
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
        ViewData["RecipientId"] = new SelectList(_context.Users, "Id", "Id", notification.RecipientId);
        ViewData["SenderId"] = new SelectList(_context.Users, "Id", "Id", notification.SenderId);
        ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Description", notification.TicketId);
        return View(notification);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Notification notification = await _context.Notifications
            .Include(n => n.Recipient)
            .Include(n => n.Sender)
            .Include(n => n.Ticket)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (notification == null)
        {
            return NotFound();
        }

        return View(notification);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        Notification notification = await _context.Notifications.FindAsync(id);
        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool NotificationExists(int id)
    {
        return _context.Notifications.Any(e => e.Id == id);
    }
}
