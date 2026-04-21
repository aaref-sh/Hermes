using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using HStore.Application.DTOs;
using HStore.Application.Interfaces;
using HStore.Domain.Entities;
using HStore.Domain.Interfaces;
using HStore.Domain.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HStore.Application.Services;

public class AuthService(IUnitOfWork unitOfWork,
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    RoleManager<Role> roleManager,
    IEmailService emailService, IMapper mapper, IOptions<JwtSettings> options)
    : IAuthService
{
    /// <summary>
    /// Authenticates a user and generates a JWT token if successful.
    /// </summary>
    /// <param name="login">The login credentials.</param>
    /// <returns>The JWT token DTO containing the access token, and expiration time, or null if authentication failed.</returns>
    public async Task<JwtTokenDto?> LoginUserAsync(LoginDto login)
    {
        var signInResult = await signInManager.PasswordSignInAsync(login.Username, login.Password, false, false);
        var user = await unitOfWork.Users.GetByUsernameAsync(login.Username);
        if (!signInResult.Succeeded)
        {
            return null;
        }

        var token = await GenerateJwtToken(user.Id, user.UserName, user.Role);

        return token;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="newUser">The new user registration details.</param>
    /// <returns>True if registration was successful, false otherwise.</returns>
    public async Task<bool> RegisterAsync(RegisterDto newUser)
    {
        if (await unitOfWork.Users.UserExistsAsync(newUser.Username))
        {
            return false;
        }

        if (await unitOfWork.Users.GetByEmailAsync(newUser.Email) != null)
        {
            return false;
        }
        var identityUser = new User
        {
            UserName = newUser.Username,
            Email = newUser.Email,
            FirstName = newUser.FirstName,
            LastName = newUser.LastName,
            PhoneNumber = newUser.PhoneNumber,
            Role = newUser.Role,
            Address = mapper.Map<Address>(newUser.Address),
        };

        var result = await userManager.CreateAsync(identityUser, newUser.Password);
        // Add role if provided
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(newUser.Role))
            {
                if (!await roleManager.RoleExistsAsync(newUser.Role))
                {
                    await roleManager.CreateAsync(new() { Name = newUser.Role });
                }
                await userManager.AddToRoleAsync(identityUser, newUser.Role);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Resets the user's password using a password reset token.
    /// </summary>
    /// <param name="dto">The reset password details.</param>
    /// <returns>True if the password was reset successfully, false otherwise.</returns>
    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return false;
        }

        var result = await userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        return result.Succeeded;
    }

    /// <summary>
    /// Sends a password reset email to the specified email address.
    /// </summary>
    /// <param name="email">The email address to send the password reset email to.</param>
    /// <returns>True if the email was sent successfully, false otherwise.</returns>
    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return false;
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        await emailService.SendPasswordResetEmailAsync(user.Email, resetToken);
        return true;
    }

    /// <summary>
    /// Generates a JWT and refresh token for a given user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="userName">The username of the user.</param>
    /// <param name="role">The role of the user.</param>
    /// <returns>The JWT token DTO containing the access token, refresh token, and expiration times.</returns>
    public async Task<JwtTokenDto> GenerateJwtToken(int userId, string userName, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(options.Value.Secret);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, userName),
                new(ClaimTypes.Role, role)
            ]),
            Expires = DateTime.UtcNow.AddDays(options.Value.AccessTokenExpirationInDays),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var accessToken = tokenHandler.CreateToken(tokenDescriptor);
        var refreshToken = GenerateRefreshToken();
        var refreshExpirationDate = DateTime.UtcNow.AddDays(options.Value.RefreshTokenExpirationInDays);

        await unitOfWork.RefreshTokens.AddAsync(new RefreshToken
            { UserId = userId, Token = refreshToken, Expires = refreshExpirationDate, Role = role });
        
        return new JwtTokenDto
        {
            AccessToken = tokenHandler.WriteToken(accessToken),
            AccessTokenExpiration = tokenDescriptor.Expires.Value,
            RefreshToken = refreshToken,
            RefreshTokenExpiration = refreshExpirationDate
        };
    }

    /// <summary>
    /// Generates a random refresh token.
    /// </summary>
    /// <returns>The generated refresh token.</returns>
    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Validates a JWT token and returns the associated ClaimsPrincipal if valid.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>The ClaimsPrincipal associated with the token, or null if the token is invalid.</returns>
    public Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(options.Value.Secret);
            var claimsPrincipal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero,
                ValidateLifetime = true
            }, out _);

            return Task.FromResult(claimsPrincipal)!;
        }
        catch
        {
            return Task.FromResult<ClaimsPrincipal>(null!)!;
        }
    }

}
