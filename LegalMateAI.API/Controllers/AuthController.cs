using Microsoft.AspNetCore.Mvc;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.Domain.Entities;
using LegalMateAI.DAL.DBContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LegalMateDbContext _context;
    private readonly PasswordHasher<object> _passwordHasher;

    public AuthController(LegalMateDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<object>();
    }


    private async Task<bool> EmailExists(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email)
            || await _context.Admins.AnyAsync(a => a.Email == email)
            || await _context.Lawyers.AnyAsync(l => l.Email == email);
    }

    // ============================
    // Register User
    // ============================
    [HttpPost("register-user")]
    public async Task<IActionResult> RegisterUser(UserCreateDTO dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email?.Trim().ToLower();
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is required");

            if (await EmailExists(email))
                return BadRequest("Email is already taken");

            var user = new User
            {
                Name = dto.Name,
                Email = email
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    // ============================
    // Register Lawyer
    // ============================
    [HttpPost("register-lawyer")]
    public async Task<IActionResult> RegisterLawyer(LawyerCreateDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email?.Trim().ToLower();
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is required");

            if (await EmailExists(email))
                return BadRequest("Email is already taken");

            var lawyer = new Lawyer
            {
                FullName = dto.FullName,
                Email = email,
                Phone = dto.Phone,
                Address = dto.Address,
                Description = dto.Description,
                ExperienceYears = dto.ExperienceYears
            };

            lawyer.PasswordHash = _passwordHasher.HashPassword(lawyer, dto.Password);

            await _context.Lawyers.AddAsync(lawyer);
            await _context.SaveChangesAsync();

            return Ok("Lawyer registered successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    // ============================
    // Register Admin
    // ============================
    [HttpPost("register-admin")]
    public async Task<IActionResult> RegisterAdmin(AdminCreateDTO dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email?.Trim().ToLower();
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is required");

            if (await EmailExists(email))
                return BadRequest("Email is already taken");

            var admin = new Admin
            {
                Name = dto.Name,
                Email = email
            };

            admin.PasswordHash = _passwordHasher.HashPassword(admin, dto.Password);

            await _context.Admins.AddAsync(admin);
            await _context.SaveChangesAsync();

            return Ok("Admin registered successfully");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    // ============================
    // Login
    // ============================
    [HttpPost("login")]
    public async Task<IActionResult> Login(UserReadDTO dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var email = dto.Email?.Trim().ToLower();
            if (string.IsNullOrEmpty(email))
                return BadRequest("Email is required");

            // Users
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
                if (result != PasswordVerificationResult.Failed)
                    return Ok(new { Type = "User", user.Id, user.Email });
            }

            // Lawyers
            var lawyer = await _context.Lawyers.FirstOrDefaultAsync(l => l.Email == email);
            if (lawyer != null)
            {
                var result = _passwordHasher.VerifyHashedPassword(lawyer, lawyer.PasswordHash, dto.Password);
                if (result != PasswordVerificationResult.Failed)
                    return Ok(new { Type = "Lawyer", lawyer.Id, lawyer.Email });
            }

            // Admins
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Email == email);
            if (admin != null)
            {
                var result = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, dto.Password);
                if (result != PasswordVerificationResult.Failed)
                    return Ok(new { Type = "Admin", admin.Id, admin.Email });
            }


            return Unauthorized("Invalid credentials");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }
}
