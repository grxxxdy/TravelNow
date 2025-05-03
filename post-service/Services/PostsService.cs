using Microsoft.EntityFrameworkCore;
using Npgsql;
using post_service.Entities;

namespace post_service.Services;

public class PostsService
{
    private readonly PostsDbContext _dbContext;
    
    public PostsService(PostsDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<string> CreatePostAsync(Post post)
    {
        // CHECKS
        
        if (string.IsNullOrWhiteSpace(post.text))
        {
            return "Post text cannot be empty.";
        }
        
        // End checks
        
        post.created_at = DateTime.UtcNow;

        try
        {
            _dbContext.posts.Add(post);
            await _dbContext.SaveChangesAsync();

            return "Post created successfully.";
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException { SqlState: "23503" }) // foreign key violation
            {
                return "User with this ID does not exist.";
            }

            return "An error occurred while creating the post.";
        }
    }

    public async Task<List<Post>> GetAllPostsAsync()
    {
        return await _dbContext.posts.ToListAsync();
    }

    public async Task<Post?> GetPostByIdAsync(int id)
    {
        return await _dbContext.posts.FindAsync(id);
    }

    public async Task<string> UpdatePostAsync(int id, Post updatedPost)
    {
        // CHECKS
        
        if (string.IsNullOrWhiteSpace(updatedPost.text))
        {
            return "Post text cannot be empty.";
        }
        
        var post = await _dbContext.posts.FindAsync(id);
        
        if (post == null)
        {
            return "Post with this id does not exist.";
        }
        
        // End checks
        
        post.text = updatedPost.text;
        post.image_url = updatedPost.image_url;
        post.created_at = DateTime.SpecifyKind(post.created_at, DateTimeKind.Utc);
        
        _dbContext.posts.Update(post);
        await _dbContext.SaveChangesAsync();

        return "Post updated successfully.";
    }

    public async Task<bool> DeletePostAsync(int id)
    {
        var post = await _dbContext.posts.FindAsync(id);
        if (post == null)
        {
            return false;
        }

        _dbContext.posts.Remove(post);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<object> LikePostAsync(Like like)
    {
        var existingLike = await _dbContext.likes
            .FirstOrDefaultAsync(l => l.user_id == like.user_id && l.post_id == like.post_id);

        if (existingLike != null)
        {
            _dbContext.likes.Remove(existingLike);
            await _dbContext.SaveChangesAsync();
            return new { success = true, message = "Post unliked successfully." };
        }

        like.created_at = DateTime.UtcNow;
        
        try
        {
            _dbContext.likes.Add(like);
            await _dbContext.SaveChangesAsync();

            return new { success = true, message = "Post liked successfully." };
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException { SqlState: "23503" })   // foreign key violation
            {
                return new { success = false, message = "User or Post with this ID does not exist." };
            }
            
            return new { success = false, message = "An error occurred while liking the post." };
        }
    }
    
    public async Task<object> CommentPostAsync(Comment comment)
    {
        comment.created_at = DateTime.UtcNow;
        
        try
        {
            _dbContext.comments.Add(comment);
            await _dbContext.SaveChangesAsync();

            return new { success = true, message = "Post commented successfully." };
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException { SqlState: "23503" })   // foreign key violation
            {
                return new { success = false, message = "User or Post with this ID does not exist." };
            }
            
            return new { success = false, message = "An error occurred while commenting on the post." };
        }
    }

    public async Task<List<Like>> GetAllLikesAsync(int postId)
    {
        return await _dbContext.likes.Where(l => l.post_id == postId).ToListAsync();
    }
    
    public async Task<List<Comment>> GetAllCommentsAsync(int postId)
    {
        return await _dbContext.comments.Where(c => c.post_id == postId).ToListAsync();
    }
}