using System.Security.Claims;
using System.Text.Json;
using api_gateway.Entities;
using api_gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api_gateway.Controllers;

[ApiController]
[Route("api/gateway")]
public class UserGatewayController : ControllerBase
{
    private readonly GatewayService _gatewayService;

    public UserGatewayController(GatewayService service)
    {
        _gatewayService = service;
    }
    
    [HttpPost("user/register")]
    public async Task<IActionResult> RegisterUser([FromBody] User request)
    {
        var response = await _gatewayService.SendMessageAsync("user.register", request);
        return Ok(JsonSerializer.Deserialize<object>(response));
    }    
    
    [HttpPost("user/login")]
    public async Task<IActionResult> LoginUser([FromBody] LoginRequest request)
    {
        var response = await _gatewayService.SendMessageAsync("user.login", new { request.email, request.password });
        return Ok(JsonSerializer.Deserialize<object>(response));
    }
    
    [Authorize(Roles = "admin")]
    [HttpGet("user/list")]
    public async Task<IActionResult> GetUsers()
    {
        var response = await _gatewayService.SendMessageAsync("user.get_all", null);
        return Ok(JsonSerializer.Deserialize<object>(response));
    }   
    
    [Authorize]
    [HttpGet("user/{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var response = await _gatewayService.SendMessageAsync("user.get_by_id", new { id });
        return Ok(JsonSerializer.Deserialize<object>(response));
    }
    
    [Authorize]
    [HttpPut("user/update/{id}")]
    public async Task<IActionResult> UpdateUser([FromBody] User request)
    {
        // Get user id and role
        var userId = User.Identity?.Name ?? User.FindFirstValue("sub");
        if (userId == null) return Unauthorized("UserId not found in token");
        
        var role = User.FindFirstValue("role");
        
        // Check for role
        if (role != "admin" && int.Parse(userId) != request.id)
            return StatusCode(403, new { message = "You don't have permission to update this user." });
        
        // Update user
        var response = await _gatewayService.SendMessageAsync("user.update", request);
        return Ok(JsonSerializer.Deserialize<object>(response));
    }
    
    [Authorize(Roles = "admin")]
    [HttpDelete("user/delete/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var response = await _gatewayService.SendMessageAsync("user.delete", new { id });
        return Ok(JsonSerializer.Deserialize<object>(response));
    }
}