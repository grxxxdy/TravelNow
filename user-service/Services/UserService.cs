using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using user_service.Entities;
using user_service.Interfaces;

namespace user_service.Services;

public class UserService
{
    private readonly UserDbContext _dbContext;
    private readonly ITokenProvider _tokenProvider;

    public UserService(UserDbContext dbContext, ITokenProvider tokenProvider)
    {
        _dbContext = dbContext;
        _tokenProvider = tokenProvider;
    }
    
    public virtual async Task<string> CreateUserAsync(User user)
    {
        // CHECKS
        if (string.IsNullOrWhiteSpace(user.name) || 
            string.IsNullOrWhiteSpace(user.email) || 
            string.IsNullOrWhiteSpace(user.password))
        {
            return "Name, email and password fields are required.";
        }
        
        if (await _dbContext.users.AnyAsync(u => u.email == user.email))
        {
            return "User with this email already exists.";
        }
        
        if (!new EmailAddressAttribute().IsValid(user.email))
        {
            return "Invalid email format.";
        }
        
        if (user.role != "admin" && user.role != "user")
        {
            user.role = "user";
        }
        
        // End checks

        user.created_at = DateTime.UtcNow;
        
        _dbContext.users.Add(user);
        await _dbContext.SaveChangesAsync();
            
        return "User created successfully.";
    }

    public virtual async Task<string> LoginUser(string email, string password)
    {
        var user = await _dbContext.users.FirstOrDefaultAsync(u => u.email == email);

        if (user == null) return "User not found.";

        if (user.password != password) return "Incorrect password.";
        
        string token = _tokenProvider.Create(user);
        
        return token;
    }
    
    public virtual async Task<List<User>> GetAllUsersAsync()
    {
        return await _dbContext.users.ToListAsync();
    }
    
    public virtual async Task<User?> GetUserByIdAsync(int id)
    {
        return await _dbContext.users.FindAsync(id);
    }
    
    public virtual async Task<Tuple<string, string>> UpdateUserAsync(int id, User updatedUser)
    {
        // CHECKS
        if (string.IsNullOrWhiteSpace(updatedUser.name) || 
            string.IsNullOrWhiteSpace(updatedUser.email) || 
            string.IsNullOrWhiteSpace(updatedUser.password))
        {
            return new Tuple<string, string>("Name, email and password fields are required.", "");
        }
        
        if (!new EmailAddressAttribute().IsValid(updatedUser.email))
        {
            return new Tuple<string, string>("Invalid email format.", "");
        }
        
        var user = await _dbContext.users.FindAsync(id);
        
        if (user == null)
        {
            return new Tuple<string, string>("User with this id does not exist.", "");
        }

        if (updatedUser.role != "admin" && updatedUser.role != "user")
        {
            updatedUser.role = "user";
        }
        
        // ENd checks
        
        user.name = updatedUser.name;
        user.email = updatedUser.email;
        user.password = updatedUser.password;
        user.role = updatedUser.role;
        user.created_at = DateTime.SpecifyKind(user.created_at, DateTimeKind.Utc);

        _dbContext.users.Update(user);
        await _dbContext.SaveChangesAsync();
        
        // create new token
        updatedUser.created_at = DateTime.SpecifyKind(user.created_at, DateTimeKind.Utc);
        var newToken = _tokenProvider.Create(updatedUser);

        return new Tuple<string, string>("User updated successfully.", newToken);
    }
    public virtual async Task<bool> DeleteUserAsync(int id)
    {
        var user = await _dbContext.users.FindAsync(id);
        if (user == null)
        {
            return false;
        }

        _dbContext.users.Remove(user);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}