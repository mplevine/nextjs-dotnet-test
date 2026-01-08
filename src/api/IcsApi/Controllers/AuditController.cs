using IcsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IcsApi.Controllers;

[ApiController]
[Route("audit")]
public class AuditController : ControllerBase
{
    private readonly IAuditStore _auditStore;

    public AuditController(IAuditStore auditStore)
    {
        _auditStore = auditStore;
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult GetAll()
    {
        return Ok(_auditStore.GetAll());
    }
}
