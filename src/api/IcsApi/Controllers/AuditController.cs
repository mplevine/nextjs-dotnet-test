using IcsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IcsApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AuditController : ControllerBase
{
    private readonly InMemoryAuditStore _auditStore;

    public AuditController(InMemoryAuditStore auditStore)
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
