using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SpotIt.Application.Authorization;
using SpotIt.Application.DTOs;
using SpotIt.Domain.Entities;
using System.Security.Claims;

namespace SpotIt.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = Permissions.Roles.Manage)]
public class AdminController(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager) : ControllerBase
{
    private static readonly string[] BuiltInRoles = ["Admin", "CityHallEmployee", "Citizen"];

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var result = new List<RoleDto>();
        foreach (var role in roleManager.Roles.ToList())
        {
            var claims = await roleManager.GetClaimsAsync(role);
            result.Add(new RoleDto(role.Name!, claims.Select(c => c.Value).ToList()));
        }
        return Ok(result);
    }

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var result = await roleManager.CreateAsync(new IdentityRole(request.Name));
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok();
    }

    [HttpDelete("roles/{roleName}")]
    public async Task<IActionResult> DeleteRole([FromRoute] string roleName)
    {
        if (BuiltInRoles.Contains(roleName))
            return BadRequest($"Cannot delete built-in role '{roleName}'.");

        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null) return NotFound();

        await roleManager.DeleteAsync(role);
        return NoContent();
    }

    [HttpGet("roles/{roleName}/claims")]
    public async Task<IActionResult> GetRoleClaims([FromRoute] string roleName)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null) return NotFound();

        var claims = await roleManager.GetClaimsAsync(role);
        return Ok(claims.Select(c => c.Value));
    }

    [HttpPost("roles/{roleName}/claims")]
    public async Task<IActionResult> AddClaimToRole(
        [FromRoute] string roleName,
        [FromBody]  AddClaimRequest request)
    {
        if (!Permissions.GetAll().Contains(request.Permission)) return BadRequest("The permision does not exist");

        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null) return NotFound("The role is null");

        var claims = await roleManager.GetClaimsAsync(role);
        if (claims.Any(c=>c.Value==request.Permission)) return Ok();

        var newClaim=new Claim("permission", request.Permission);
        var result = await roleManager.AddClaimAsync(role, newClaim);
        if (!result.Succeeded) return BadRequest("Adding the claim");
        return Ok();
    }

    [HttpDelete("roles/{roleName}/claims/{permission}")]
    public async Task<IActionResult> RemoveClaimFromRole(
        [FromRoute] string roleName,
        [FromRoute] string permission)
    {
        if (roleName == "Admin" && permission == Permissions.Roles.Manage)
            return BadRequest("Cannot remove 'roles:manage' from the Admin role.");

        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null) return NotFound();

        var existing = await roleManager.GetClaimsAsync(role);
        var claim    = existing.FirstOrDefault(c => c.Value == permission);
        if (claim == null) return NotFound();

        await roleManager.RemoveClaimAsync(role, claim);
        return NoContent();
    }

    [HttpGet("permissions")]
    public IActionResult GetPermissions() => Ok(Permissions.GetAll());

    [HttpGet("users")]
    [Authorize(Policy = Permissions.Users.Manage)]
    public async Task<IActionResult> GetUsers()
    {
        var users = userManager.Users.ToList();
        var result = new List<UserSummaryDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? string.Empty;
            result.Add(new UserSummaryDto(user.Id, user.Email!, user.FullName, user.City, role));
        }
        return Ok(result);
    }

    [HttpPost("users/{userId}/role")]
    [Authorize(Policy = Permissions.Users.Manage)]
    public async Task<IActionResult> AssignRole([FromRoute] string userId, [FromBody] AssignRoleRequest request)
    {
        if (!BuiltInRoles.Contains(request.RoleName))
            return BadRequest($"Invalid role '{request.RoleName}'. Must be one of: {string.Join(", ", BuiltInRoles)}.");

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound($"User '{userId}' not found.");

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, request.RoleName);

        return Ok(new { message = $"User '{user.Email}' has been assigned the role '{request.RoleName}'." });
    }
}

public record RoleDto(string Name, List<string> Claims);
public record CreateRoleRequest(string Name);
public record AddClaimRequest(string Permission);
public record AssignRoleRequest(string RoleName);
