using Banking_System.DTOs.Admin;
using Banking_System.Models.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Banking_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly BankDbContext _context;

        public AdminController(BankDbContext context)
        {
            _context = context;
        }

        // 1.===============  GET /api/admin/users (Get list of all users)
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchKeyword = null)
        {
            var query = _context.Users.AsQueryable();

            // FILTERING: Search by name, username, email
            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                query = query.Where(u => u.Username.Contains(searchKeyword) ||
                                         u.FullName.Contains(searchKeyword) ||
                                         u.Email.Contains(searchKeyword));
            }

            // SORTING: Sort by name from A-Z
            query = query.OrderBy(u => u.FullName);

            // PAGINATION
            int totalItems = await query.CountAsync();
            var users = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    FullName = u.FullName,
                    PhoneNumber = u.PhoneNumber,
                    IsActive = u.IsActive
                })
                .ToListAsync();

            return Ok(new
            {
                totalItems,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                data = users
            });
        }

        // 2.================ PUT /api/admin/users/{id}/lock (Lock user account)
        [HttpPut("users/{id}/lock")]
        public async Task<IActionResult> LockUser(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound(new { message = "User Not Found." });

            // Admin cannot lock their own account
            var currentAdminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentAdminId != null && user.Id == Guid.Parse(currentAdminId))
            {
                return BadRequest(new { message = "You cannot lock your own Admin account." });
            }

            if (!user.IsActive) return BadRequest(new { message = "This user account is already locked." });

            user.IsActive = false;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Successfully locked the user account {user.FullName}." });
        }

        // 3. PUT /api/admin/users/{id}/unlock (Unlock user account)
        [HttpPut("users/{id}/unlock")]
        public async Task<IActionResult> UnlockUser(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound(new { message = "User Not Found." });

            if (user.IsActive) return BadRequest(new { message = "This user account is already active." });

            user.IsActive = true; // Reactivate the account
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Successfully unlocked the user account {user.FullName}." });
        }
    }
}
