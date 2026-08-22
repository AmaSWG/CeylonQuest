using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IdentityService.Data;

namespace IdentityService.Services;

public class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string? message = null) : base(message) { }
}

public class RegistrationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly PasswordHasher<User> _passwordHasher;

    public RegistrationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<User> RegisterAsync(RegisterRequest request)
    {
        var emailLower = request.Email.Trim().ToLower();

        var exists = await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == emailLower);
        if (exists)
        {
            throw new DuplicateEmailException("Email already in use");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Nationality = request.Nationality.Trim(),
            Role = UserRole.Visitor
        };

        // Enforce Visitor-only registration regardless of requested type
        user.Role = UserRole.Visitor;

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _dbContext.Users.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException != null && ex.InnerException.Message?.Contains("Duplicate") == true)
            {
                throw new DuplicateEmailException("Email already in use");
            }

            throw;
        }

        return user;
    }
}