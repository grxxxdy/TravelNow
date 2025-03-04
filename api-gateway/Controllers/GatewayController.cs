using System.Text.Json;
using api_gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using user_service.Entities;

namespace api_gateway.Controllers;

[ApiController]
[Route("api/gateway")]
public class GatewayController : ControllerBase
{
    private readonly GatewayService _gatewayService;

    public GatewayController(GatewayService service)
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
    public async Task<IActionResult> LoginUser(string email, string password)
    {
        var response = await _gatewayService.SendMessageAsync("user.login", new { email, password });
        return Ok(JsonSerializer.Deserialize<object>(response));
    }
    
    [Authorize(Roles = "admin")]
    [HttpGet("user/list")]
    public async Task<IActionResult> GetUsers()
    {
        var response = await _gatewayService.SendMessageAsync("user.get_all", null);
        return Ok(JsonSerializer.Deserialize<object>(response));
    }   
    
    [Authorize(Roles = "admin")]
    [HttpGet("user/{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var response = await _gatewayService.SendMessageAsync("user.get_by_id", new { id });
        return Ok(JsonSerializer.Deserialize<object>(response));
    }
    
    [Authorize(Roles = "admin")]
    [HttpPut("user/update/{id}")]
    public async Task<IActionResult> UpdateUser([FromBody] User request)
    {
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