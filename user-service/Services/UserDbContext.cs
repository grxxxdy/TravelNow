using Microsoft.EntityFrameworkCore;
using user_service.Entities;

namespace user_service.Services;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<User> users { get; set; }
}