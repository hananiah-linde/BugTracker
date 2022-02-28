using System.Security.Claims;
using System.Security.Principal;

namespace BugTracker.Extensions;

public static class IdentityExtensions
{
    public static int? GetCompanyId(this IIdentity identity)
    {
        Claim claim = ((ClaimsIdentity)identity).FindFirst("CompanyId");
        return (claim != null) ? int.Parse(claim.Value) : null;
    }
}
