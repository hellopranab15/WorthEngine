using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorthEngine.Core.DTOs;
using WorthEngine.Services;

namespace WorthEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FireController : ControllerBase
{
    private readonly FireCalculatorService _fireCalculatorService;
    private readonly FireGoalService _fireGoalService;

    public FireController(
        FireCalculatorService fireCalculatorService,
        FireGoalService fireGoalService)
    {
        _fireCalculatorService = fireCalculatorService;
        _fireGoalService = fireGoalService;
    }

    [HttpPost("calculate")]
    public ActionResult<FireCalculationResponse> Calculate([FromBody] FireCalculationRequest request)
    {
        try
        {
            var result = _fireCalculatorService.CalculateFire(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("goal")]
    public async Task<ActionResult<FireGoalResponse>> SaveGoal([FromBody] FireGoalRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _fireGoalService.SaveGoalAsync(userId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("goal")]
    public async Task<ActionResult<FireGoalResponse>> GetGoal()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _fireGoalService.GetGoalAsync(userId);
            if (result == null)
                return NotFound(new { message = "No FIRE goal found" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("progress")]
    public async Task<ActionResult<FireProgressResponse>> GetProgress()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _fireGoalService.GetProgressAsync(userId);
            if (result == null)
                return NotFound(new { message = "No FIRE goal found" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("scenarios")]
    public async Task<ActionResult<List<FireScenarioResponse>>> GetScenarios()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _fireGoalService.GetScenariosAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
