using System.ComponentModel;

namespace BugTracker.Models;

public class TicketPriority
{
    public int Id { get; set; }

    [DisplayName("Ticket Priority")]
    public string Name { get; set; }
}
