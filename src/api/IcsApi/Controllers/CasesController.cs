using IcsApi.Models;
using IcsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IcsApi.Controllers;

[ApiController]
[Route("cases")]
public class CasesController : ControllerBase
{
    private readonly ICaseStore _store;

    public CasesController(ICaseStore store)
    {
        _store = store;
    }

    [HttpGet]
    [Authorize(Policy = "AdminOrAttorney")]
    public IActionResult GetAll()
    {
        return Ok(_store.GetAll());
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOrAttorney")]
    public IActionResult Get(string id)
    {
        var item = _store.Get(id);
        if (item is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Case not found",
                detail: $"Case '{id}' was not found."
            );
        }
        return Ok(item);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult Create([FromBody] CaseItem request)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation error",
                detail: "Id is required."
            );
        }

        var now = DateTime.UtcNow;
        var created = _store.Upsert(request with 
        { 
            CreatedUtc = request.CreatedUtc == default ? now : request.CreatedUtc 
        });
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult Delete(string id)
    {
        var deleted = _store.Delete(id);
        return deleted
            ? NoContent()
            : Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Case not found",
                detail: $"Case '{id}' was not found."
            );
    }
}
