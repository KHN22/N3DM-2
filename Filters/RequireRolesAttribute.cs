using Microsoft.AspNetCore.Mvc;

namespace N3DMMarket.Filters;

public class RequireRolesAttribute : TypeFilterAttribute
{
    public RequireRolesAttribute(string roles) : base(typeof(RoleAuthorizeFilter))
    {
        Arguments = new object[] { roles };
    }
}
