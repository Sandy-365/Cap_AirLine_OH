using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StaffService.Data;
using StaffService.Models;
using StaffService.DTOs;
using Shared.Security;
using StaffService.Services;

namespace StaffService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StaffController : ControllerBase
{
    private readonly StaffDbContext _context;
    private readonly IStaffAuthService _authService;

    public StaffController(StaffDbContext context, IStaffAuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    /// <summary>
    /// Retrieves all staff profiles from the system.
    /// [Allowed Roles: Admin, SuperAdmin, HR]
    /// </summary>
    [HttpGet("users")]
    [Authorize(Roles = "Admin,SuperAdmin,HR")]
    public async Task<IActionResult> GetUsers()
    {
        return Ok(await _authService.GetAllStaffAsync());
    }

    /// <summary>
    /// Retrieves a staff profile by ID. Returns 404 if not found.
    /// [Allowed Roles: Staff (own profile), Admin, SuperAdmin, HR]
    /// </summary>
    [HttpGet("users/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetUser(int userId)
    {
        var user = await _authService.GetUserAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });
        return Ok(user);
    }

    /// <summary>
    /// Updates staff profile name and email. Returns 404 if not found.
    /// [Allowed Roles: Staff (own profile), Admin, SuperAdmin, HR]
    /// </summary>
    [HttpPut("users/{userId}/profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(int userId, [FromBody] StaffUpdateProfileDto dto)
    {
        try { return Ok(await _authService.UpdateProfileAsync(userId, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    /// <summary>
    /// Activates or deactivates a staff account. Returns 404 if not found.
    /// [Allowed Roles: Admin, SuperAdmin, HR]
    /// </summary>
    [HttpPut("users/{userId}/status")]
    [Authorize(Roles = "Admin,SuperAdmin,HR")]
    public async Task<IActionResult> UpdateUserStatus(int userId, [FromBody] StaffUpdateStatusDto dto)
    {
        try { await _authService.UpdateUserStatusAsync(userId, dto.IsActive); return Ok(new { message = "Status updated" }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    /// <summary>
    /// Permanently deletes a staff profile. Returns 404 if not found.
    /// [Allowed Roles: Admin, SuperAdmin, HR]
    /// </summary>
    [HttpDelete("users/{userId}")]
    [Authorize(Roles = "Admin,SuperAdmin,HR")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        try { await _authService.DeleteUserAsync(userId); return Ok(new { message = "User deleted" }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // ─── Legacy Staff Endpoints ───

    /// <summary>
    /// Retrieves all staff profiles from the database directly.
    /// [Allowed Roles: Admin, SuperAdmin, HR]
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin,HR")]
    public async Task<IActionResult> GetAllStaff()
    {
        var staffList = await _context.StaffProfiles.ToListAsync();
        return Ok(staffList);
    }

    /// <summary>
    /// Fetches a single staff profile by ID directly from DB. Returns 404 if not found.
    /// [Allowed Roles: Staff (own profile), Admin, SuperAdmin, HR]
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetStaffById(int id)
    {
        var staff = await _context.StaffProfiles.FindAsync(id);
        if (staff == null)
        {
            return NotFound();
        }
        return Ok(staff);
    }

    /// <summary>
    /// Creates a new staff profile directly with hashed password. Validates email uniqueness and enforces "Staff" role.
    /// [Allowed Roles: Admin, SuperAdmin, HR]
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,HR")]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDto dto)
    {
        if (await _context.StaffProfiles.AnyAsync(s => s.Email == dto.Email))
        {
            return BadRequest(new { message = "Email already exists" });
        }

        var profile = new StaffProfile
        {
            Email = dto.Email,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            Name = dto.Name,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Department = dto.Department,
            RoleTitle = dto.RoleTitle,
            AssignedAirportCode = dto.AssignedAirportCode,
            Role = "Staff",
            IsActive = true
        };

        _context.StaffProfiles.Add(profile);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetStaffById), new { id = profile.Id }, profile);
    }

    /// <summary>
    /// Updates staff profile fields (name, department, role title, airport code, active status).
    /// [Allowed Roles: Admin, SuperAdmin, HR]
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,HR")]
    public async Task<IActionResult> UpdateStaff(int id, [FromBody] UpdateStaffDto dto)
    {
        var profile = await _context.StaffProfiles.FindAsync(id);
        if (profile == null)
        {
            return NotFound();
        }

        profile.Name = dto.Name;
        profile.FirstName = dto.FirstName;
        profile.LastName = dto.LastName;
        profile.Department = dto.Department;
        profile.RoleTitle = dto.RoleTitle;
        profile.AssignedAirportCode = dto.AssignedAirportCode;
        profile.IsActive = dto.IsActive;
        profile.UpdatedAt = DateTime.UtcNow;

        _context.Entry(profile).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound(new { message = "Staff record was not found or has been modified by another user" });
        }

        return NoContent();
    }

    /// <summary>
    /// Permanently removes a staff profile from the database directly.
    /// [Allowed Roles: Admin, SuperAdmin, HR]
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin,HR")]
    public async Task<IActionResult> DeleteStaff(int id)
    {
        var staff = await _context.StaffProfiles.FindAsync(id);
        if (staff == null)
        {
            return NotFound();
        }

        _context.StaffProfiles.Remove(staff);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
