using Microsoft.AspNetCore.Mvc;
using VulnerabilityTracker.Models;
using VulnerabilityTracker.Services;

namespace VulnerabilityTracker.Controllers;

[ApiController]
[Route("api/vulnerabilities")]
public class VulnerabilitiesController : ControllerBase
{
    private readonly IVulnerabilityStore _store;

    public VulnerabilitiesController(IVulnerabilityStore store) => _store = store;

    /// <summary>Добавить новую уязвимость.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(VulnerabilityResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<VulnerabilityResponse> Create([FromBody] CreateVulnerabilityRequest request)
    {
        // [ApiController] сам вернёт 400, если данные не прошли валидацию
        var vulnerability = _store.Add(request.Name.Trim(), request.Severity!.Value);

        return CreatedAtAction(
            nameof(GetById),
            new { id = vulnerability.Id },
            ToResponse(vulnerability));
    }

    /// <summary>Получить информацию об уязвимости по ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VulnerabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<VulnerabilityResponse> GetById(Guid id)
    {
        var vulnerability = _store.Get(id);
        if (vulnerability is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Vulnerability not found",
                detail: $"Уязвимость с ID {id} не найдена.");
        }

        return ToResponse(vulnerability);
    }

    private static VulnerabilityResponse ToResponse(Vulnerability v) =>
        new(v.Id, v.Name, v.Severity, v.CreatedAt);
}
