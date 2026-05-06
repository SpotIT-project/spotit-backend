using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SpotIt.Application.DTOs;
using SpotIt.Application.Interfaces;
using SpotIt.Domain.Entities;
using SpotIt.Domain.Interfaces;

namespace SpotIt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController:ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtService jwtService,
        IUnitOfWork unitOfWork,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _unitOfWork = unitOfWork;
        _roleManager = roleManager;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            City = request.City,
            CreatedAt = DateTime.UtcNow
        };

        var result= await _userManager.CreateAsync(user,request.Password);
        if(!result.Succeeded)
            return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, "Citizen");
        return Ok(new { message = "Registration successful" });

    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var user= await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized("Invalid credentials");

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
        if (!result.Succeeded)
            return Unauthorized("Invalid credentials");

        var roles= await _userManager.GetRolesAsync(user);
        var permissionClaims = await LoadPermissionClaimsAsync(roles);
        var accessToken= _jwtService.GenerateAccessToken(user, roles, permissionClaims);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        SetTokenCookies(accessToken, refreshToken);

        var userRole = roles.FirstOrDefault() ?? "Citizen";
        return Ok(new AuthResponseDto(user.Id, user.Email!, user.FullName, userRole));
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh()
    {
        var accessToken = Request.Cookies["accessToken"];
        var refreshToken = Request.Cookies["refreshToken"];

        if (accessToken == null || refreshToken == null)
            return Unauthorized("Missing tokens");

        var principal=_jwtService.GetPrincipalFromExpiredToken(accessToken);
        if (principal == null) 
            return Unauthorized("Invalid acces token");

        var userId=principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? principal.FindFirst("sub")?.Value;
        if (userId == null)
            return Unauthorized("Invalid token claims");

        var storedToken=(await _unitOfWork.RefreshTokens
            .FindAsync(r=>r.Token==refreshToken && r.UserId==userId))
            .FirstOrDefault();

        if (storedToken == null || storedToken.IsUsed || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            return Unauthorized("Invalid or expired refresh token");

        storedToken.IsUsed=true;
        _unitOfWork.RefreshTokens.Update(storedToken);

        var user= await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var permissionClaims = await LoadPermissionClaimsAsync(roles);
        var newAccessToken = _jwtService.GenerateAccessToken(user, roles, permissionClaims);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.RefreshTokens.AddAsync(newRefreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        SetTokenCookies(newAccessToken, newRefreshToken);

        return Ok(new { message = "Token refreshed" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Citizen";
        return Ok(new UserProfileDto(user.Id, user.Email!, user.FullName, user.City, role));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> LogOut()
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (refreshToken != null)
        {
            var token= (await _unitOfWork.RefreshTokens
                .FindAsync(t => t.Token == refreshToken))
                .FirstOrDefault();
            if (token != null)
            {
                token.IsRevoked = true;
                _unitOfWork.RefreshTokens.Update(token);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        Response.Cookies.Delete("accessToken");
        Response.Cookies.Delete("refreshToken");
        return Ok(new { message = "Logged out" });
    }

    private async Task<IList<Claim>> LoadPermissionClaimsAsync(IList<string> roles)
    {
        var allClaims = new List<Claim>();

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) continue;
            var claims = await _roleManager.GetClaimsAsync(role);
            allClaims.AddRange(claims);
        }

        return allClaims.DistinctBy(c=>c.Value).ToList();
        
    }

    private void SetTokenCookies(string accessToken, string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("accessToken", accessToken, cookieOptions);
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

}
