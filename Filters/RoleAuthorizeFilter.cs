using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using N3DMMarket.Models.Db;
using System.Linq;
using System.Threading.Tasks;

namespace N3DMMarket.Filters;

public class RoleAuthorizeFilter : IAsyncActionFilter
{
    private readonly ThreedmContext _db;
    private readonly string[] _allowedRoles;

    public RoleAuthorizeFilter(ThreedmContext db, string roles)
    {
        _db = db;
        _allowedRoles = (roles ?? string.Empty).Split(',').Select(r => r.Trim()).Where(s => s.Length>0).ToArray();
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var email = http.Session.GetString("CurrentUserEmail");
        if (string.IsNullOrEmpty(email))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var normalized = email.Trim().ToUpper();
        var user = _db.Users.Include(u => u.Role).FirstOrDefault(u => u.Email.ToUpper() == normalized);
        var roleName = user?.Role?.RoleName ?? string.Empty;

        // allow if user role is in allowed list
        if (_allowedRoles.Length==0 || _allowedRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        // Forbidden
        context.Result = new ForbidResult();
    }
}
