using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BugTracker.Models;

public class Ticket
{
    //Primary key for Ticket table
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [DisplayName("Title")]
    public string Title { get; set; }

    [Required]
    [DisplayName("Description")]
    public string Description { get; set; }

    [DataType(DataType.Date)]
    [DisplayName("Created")]
    public DateTimeOffset Created { get; set; }

    [DataType(DataType.Date)]
    [DisplayName("Updated")]
    public DateTimeOffset? Updated { get; set;}

    [DisplayName("Archived")]
    public bool Archived { get; set; }

    [DisplayName("Archived By Project")]
    public bool ArchivedByProject { get; set; }

    [DisplayName("Project")]
    public int ProjectId { get; set; }

    [DisplayName("Ticket Type")]
    public int TicketTypeId { get; set; } //Foreign Key of TicketType

    [DisplayName("Ticket Priority")]
    public int TicketPriorityId { get; set; } //Foreign Key of TicketPriority

    [DisplayName("Ticket Status")]
    public int TicketStatusId { get; set; } //Foreign Key of TicketStatus

    [DisplayName("Ticket Owner")]
    public string OwnerUserId { get; set; } //Foreign Key of BugTrackerUser

    [DisplayName("Ticket Developer")]
    public string DeveloperUserId { get; set; } //Foreign Key of BugTrackerUser

    //Navigation Properties (not stored in database)
    public virtual Project Project { get; set; }
    public virtual TicketType TicketType { get; set; }
    public virtual TicketPriority TicketPriority { get; set; }
    public virtual TicketStatus TicketStatus { get; set; }
    public virtual BugTrackerUser OwnerUser { get; set; }
    public virtual BugTrackerUser DeveloperUser { get; set; }

    public virtual ICollection<TicketComment> Comments { get; set; } = new HashSet<TicketComment>();
    public virtual ICollection<TicketAttachment> Attachments { get; set; } = new HashSet<TicketAttachment>();
    public virtual ICollection<Notification> Notifications { get; set; } = new HashSet<Notification>();
    public virtual ICollection<TicketHistory> History { get; set; } = new HashSet<TicketHistory>();
}
