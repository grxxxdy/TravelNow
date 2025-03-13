using Microsoft.EntityFrameworkCore;
using post_service.Entities;

namespace post_service.Services;

public class PostsDbContext : DbContext
{
    public PostsDbContext(DbContextOptions<PostsDbContext> options) : base(options) { }

    public DbSet<Post> posts { get; set; }
    public DbSet<Like> likes { get; set; }
    public DbSet<Comment> comments { get; set; }
}