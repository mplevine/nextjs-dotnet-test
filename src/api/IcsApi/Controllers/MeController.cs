using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IcsApi.Controllers;

[ApiController]
[Route("[controller]")]
public class MeController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "AdminOrAttorney")]
    public IActionResult Get()
    {
        var user = User;
        var roles = user.FindAll("roles")
            .Select(r => r.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        
        if (roles.Count == 0)
        {
            roles = user.FindAll(ClaimTypes.Role)
                .Select(r => r.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return Ok(new
        {
            oid = user.FindFirstValue("oid"),
            username = user.FindFirstValue("preferred_username") ?? user.Identity?.Name,
            roles
        });
    }
}
