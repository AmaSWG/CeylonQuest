using IdentityService.DTOs;
using IdentityService.Models;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Services;

public class RegistrationService
{
    private readonly PasswordHasher<User> _passwordHasher;

    public RegistrationService()
    {
        _passwordHasher = new PasswordHasher<User>();
    }

    public User Register(RegisterRequest request)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Nationality = request.Nationality,
            Role = UserRole.Visitor
        };

        if (request.RegistrationType == RegistrationType.ServiceProvider)
        {
            user.Role = UserRole.Visitor;
        }

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password
        );

        return user;
    }
}