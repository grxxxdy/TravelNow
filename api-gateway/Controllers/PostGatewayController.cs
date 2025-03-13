using System.Text.Json;
using api_gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using post_service.Entities;

namespace api_gateway.Controllers;

[ApiController]
[Route("api/gateway")]
public class PostGatewayController : ControllerBase
{
    private readonly GatewayService _gatewayService;
    
    public PostGatewayController(GatewayService service)
    {
        _gatewayService = service;
    }
    
    [Authorize]
    [HttpPost("posts/create")]
    public async Task<IActionResult> CreatePost([FromBody] Post request)
    {
        var response = await _gatewayService.SendMessageAsync("post.create", request);
        return Ok(JsonSerializer.Deserialize<object>(response));
    }
    
    [HttpGet("posts/list")]
    public async Task<IActionResult> GetPosts()
    {
        var response = await _gatewayService.SendMessageAsync("post.get_all", null);
        return Ok(JsonSerializer.Deserialize<object>(response));
    } 
    
    [Authorize(Roles = "admin")]
    [HttpGet("posts/{id}")]
    public async Task<IActionResult> GetPostById(int id)
    {
        var response = await _gatewayService.SendMessageAsync("post.get_by_id", new { id });
        return Ok(JsonSerializer.Deserialize<object>(response));
    } 
    
    [Authorize(Roles = "admin")]
    [HttpPut("posts/update/{id}")]
    public async Task<IActionResult> UpdatePost([FromBody] Post request)
    {
        var response = await _gatewayService.SendMessageAsync("post.update", request);
        return Ok(JsonSerializer.Deserialize<object>(response));
    }
    
    [Authorize(Roles = "admin")]
    [HttpDelete("posts/delete/{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var response = await _gatewayService.SendMessageAsync("post.delete", new { id });
        return Ok(JsonSerializer.Deserialize<object>(response));
    }
    
    [Authorize]
    [HttpPost("posts/{id}/like")]
    public async Task<IActionResult> LikePost([FromBody] Like request)
    {
        var response = await _gatewayService.SendMessageAsync("post.like", request);
        return Ok(JsonSerializer.Deserialize<object>(response));
    }
    
    [Authorize]
    [HttpPost("posts/{id}/comment")]
    public async Task<IActionResult> CommentPost([FromBody] Comment request)
    {
        var response = await _gatewayService.SendMessageAsync("post.comment", request);
        return Ok(JsonSerializer.Deserialize<object>(response));
    }
    
    [HttpGet("posts/{id}/likes")]
    public async Task<IActionResult> GetPostLikes(int id)
    {
        var response = await _gatewayService.SendMessageAsync("post.likes_get", new { id });
        return Ok(JsonSerializer.Deserialize<object>(response));
    }

    [HttpGet("posts/{id}/comments")]
    public async Task<IActionResult> GetPostComments(int id)
    {
        var response = await _gatewayService.SendMessageAsync("post.comments_get", new { id });
        return Ok(JsonSerializer.Deserialize<object>(response));
    }
}