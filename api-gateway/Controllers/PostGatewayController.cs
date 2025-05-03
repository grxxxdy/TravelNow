using System.Security.Claims;
using System.Text.Json;
using api_gateway.Entities;
using api_gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> CreatePost([FromForm] Post request, IFormFile? image)
    {
        // Get user
        var userId = User.Identity?.Name ?? User.FindFirstValue("sub");

        if (userId == null) return Unauthorized("UserId not found in token");

        request.user_id = int.Parse(userId);
        
        // Base check
        if(request.text == String.Empty)
            return BadRequest(new { message = "Post text cannot be empty." });
        
        // Process image
        if (image != null)
        {
            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            var imageBytes = ms.ToArray();
            
            var imgResponse = await _gatewayService.SendMessageAsync("image.process", new
            {
                PostId = request.id,
                ImageData = Convert.ToBase64String(imageBytes)
            });

            var imgRespJson = JsonDocument.Parse(imgResponse);
            
            request.image_url = imgRespJson.RootElement.GetProperty("ImageUrl").GetString();
        }
        else
        {
            request.image_url = String.Empty;
        }
        
        // Process post
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
    
    [Authorize]
    [HttpPut("posts/update/{id}")]
    public async Task<IActionResult> UpdatePost([FromForm] Post request, IFormFile? image)
    {
        // Get user id and role
        var userId = User.Identity?.Name ?? User.FindFirstValue("sub");
        if (userId == null) return Unauthorized("UserId not found in token");
        
        var role = User.FindFirstValue("role");

        request.user_id = int.Parse(userId);
        
        // Base check
        if(request.text == String.Empty)
            return BadRequest(new { message = "Post text cannot be empty." });
        
        // Get post info
        var postInfo = await _gatewayService.SendMessageAsync("post.get_by_id", new { request.id });
        
        var postInfoJson = JsonDocument.Parse(postInfo);
            
        var userIdInPost = postInfoJson.RootElement.GetProperty("user_id").GetInt32();
        var userCreationDateInPost = postInfoJson.RootElement.GetProperty("created_at").GetDateTime();
        
        // Check user
        if (userIdInPost != int.Parse(userId) && role != "admin")
            return StatusCode(403, new { message = "You don't have permission to update this post." });
        
        // Assure the ids and dates match
        request.user_id = userIdInPost;
        //request.created_at = userCreationDateInPost;
        
        // Process image
        if (image != null)
        {
            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            var imageBytes = ms.ToArray();
            
            var imgResponse = await _gatewayService.SendMessageAsync("image.process", new
            {
                PostId = request.id,
                ImageData = Convert.ToBase64String(imageBytes)
            });

            var imgRespJson = JsonDocument.Parse(imgResponse);
            
            request.image_url = imgRespJson.RootElement.GetProperty("ImageUrl").GetString();
        }
        else
        {
            request.image_url = String.Empty;
        }
        
        // Update post
        var response = await _gatewayService.SendMessageAsync("post.update", request);
        return Ok(JsonSerializer.Deserialize<object>(response));
    }
    
    [Authorize]
    [HttpDelete("posts/delete/{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        // Get user id and role
        var userId = User.Identity?.Name ?? User.FindFirstValue("sub");
        if (userId == null) return Unauthorized("UserId not found in token");

        var role = User.FindFirstValue("role");
        
        // Get post info
        var postInfo = await _gatewayService.SendMessageAsync("post.get_by_id", new { id });
        
        var postInfoJson = JsonDocument.Parse(postInfo);
            
        var userIdInPost = postInfoJson.RootElement.GetProperty("user_id").GetInt32();
        
        // Check user
        if (userIdInPost != int.Parse(userId) && role != "admin")
            return StatusCode(403, new { message = "You don't have permission to delete this post." });
        
        // Delete post
        var response = await _gatewayService.SendMessageAsync("post.delete", new { id });
        return Ok(JsonSerializer.Deserialize<object>(response));
    }
    
    [Authorize]
    [HttpPost("posts/{id}/like")]
    public async Task<IActionResult> LikePost([FromBody] Like request)
    {
        // Get user
        var userId = User.Identity?.Name ?? User.FindFirstValue("sub");

        if (userId == null) return Unauthorized("UserId not found in token");

        request.user_id = int.Parse(userId);
        
        // Like post
        var response = await _gatewayService.SendMessageAsync("post.like", request);
        return Ok(JsonSerializer.Deserialize<object>(response));
    }
    
    [Authorize]
    [HttpPost("posts/{id}/comment")]
    public async Task<IActionResult> CommentPost([FromBody] Comment request)
    {
        // Get user
        var userId = User.Identity?.Name ?? User.FindFirstValue("sub");

        if (userId == null) return Unauthorized("UserId not found in token");

        request.user_id = int.Parse(userId);
        
        // Comment post
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