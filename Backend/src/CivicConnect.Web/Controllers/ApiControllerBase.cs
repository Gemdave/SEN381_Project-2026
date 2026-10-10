using CivicConnect.Core.Models;
using CivicConnect.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace CivicConnect.Web.Controllers;

[ApiController]
public abstract class ApiControllerBase(CurrentUserAccessor accessor) : ControllerBase
{
    protected Task<CurrentUser> UserAsync(CancellationToken ct) => accessor.GetAsync(HttpContext, ct);
}
