using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using user_service.Entities;

namespace user_service.Services;

public class UserService
{
    private readonly UserDbContext _dbContext;
    private readonly TokenProvider _tokenProvider;

    public UserService(UserDbContext dbContext, TokenProvider tokenProvider)
    {
        _dbContext = dbContext;
        _tokenProvider = tokenProvider;
    }
    
    public async Task<string> CreateUserAsync(User user)
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
        
        // End checks
        
        _dbContext.users.Add(user);
        await _dbContext.SaveChangesAsync();
            
        return "User created successfully.";
    }

    public async Task<string> LoginUser(string email, string password)
    {
        var user = await _dbContext.users.FirstOrDefaultAsync(u => u.email == email);

        if (user == null) return "User not found.";

        if (user.password != password) return "Incorrect password.";
        
        string token = _tokenProvider.Create(user);
        
        return token;
    }
    
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _dbContext.users.ToListAsync();
    }
    
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _dbContext.users.FindAsync(id);
    }
    
    public async Task<string> UpdateUserAsync(int id, User updatedUser)
    {
        // CHECKS
        if (string.IsNullOrWhiteSpace(updatedUser.name) || 
            string.IsNullOrWhiteSpace(updatedUser.email) || 
            string.IsNullOrWhiteSpace(updatedUser.password))
        {
            return "Name, email and password fields are required.";
        }
        
        if (await _dbContext.users.AnyAsync(u => u.email == updatedUser.email))
        {
            return "User with this email already exists.";
        }
        
        if (!new EmailAddressAttribute().IsValid(updatedUser.email))
        {
            return "Invalid email format.";
        }
        
        var user = await _dbContext.users.FindAsync(id);
        
        if (user == null)
        {
            return "User with this id does not exist.";
        }
        
        // ENd checks
        
        user.name = updatedUser.name;
        user.email = updatedUser.email;
        user.password = updatedUser.password;
        user.role = updatedUser.role;
        user.created_at = updatedUser.created_at;

        _dbContext.users.Update(user);
        await _dbContext.SaveChangesAsync();

        return "User updated successfully.";
    }
    public async Task<bool> DeleteUserAsync(int id)
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