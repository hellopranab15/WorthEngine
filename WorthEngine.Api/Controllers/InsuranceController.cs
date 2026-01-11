using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorthEngine.Core.DTOs;
using WorthEngine.Core.Interfaces;

namespace WorthEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InsuranceController : ControllerBase
{
    private readonly IInsuranceService _insuranceService;

    public InsuranceController(IInsuranceService insuranceService)
    {
        _insuranceService = insuranceService;
    }

    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InsuranceResponse>>> GetAll()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var insurances = await _insuranceService.GetUserInsurancesAsync(userId);
        return Ok(insurances);
    }

    [HttpPost]
    public async Task<ActionResult<InsuranceResponse>> Create([FromBody] InsuranceRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var insurance = await _insuranceService.CreateInsuranceAsync(userId, request);
        return Created("", insurance);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _insuranceService.DeleteInsuranceAsync(id, userId);
        return NoContent();
    }
}
