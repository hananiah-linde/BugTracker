using BugTracker.Models;

namespace BugTracker.Services.Interfaces;

public interface IBTCompanyInfoService
{
    public Task<Company> GetCompanyInfoByIdAsync(int? companyId);
    public Task<List<BugTrackerUser>> GetAllMembersAsync(int companyId);
    public Task<List<Project>> GetAllProjectsAsync(int companyId);
    public Task<List<Ticket>> GetAllTicketsAsync(int companyId);

}
