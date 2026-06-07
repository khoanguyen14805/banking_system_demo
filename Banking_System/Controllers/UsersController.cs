using Banking_System.DTOs.User;
using Banking_System.Models;
using Banking_System.Models.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Banking_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly BankDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public UsersController(BankDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        //====================  1. GET /api/users/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // Get user id from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var userId = Guid.Parse(userIdClaim);

            // Find user profile
            var user = await _context.Users
                .Include(u => u.CustomerProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound(new { message = "User Not Found." });

            return Ok(new
            {
                user.Username,
                user.Email,
                user.FullName,
                user.PhoneNumber,
                Profile = user.CustomerProfile != null ? new
                {
                    user.CustomerProfile.Address,
                    user.CustomerProfile.DateOfBirth,
                    user.CustomerProfile.CitizenId,
                    // Return full image URL for client display
                    AvatarUrl = string.IsNullOrEmpty(user.CustomerProfile.AvatarUrl) ? null : $"{Request.Scheme}://{Request.Host}/{user.CustomerProfile.AvatarUrl}".TrimStart('/')
                } : null
            });
        }

        //=======================  2. PUT /api/users/profile 
        [HttpPut("profile")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            var userId = Guid.Parse(userIdClaim);


            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            bool isNewProfile = false;

            if (profile == null)
            {
                profile = new CustomerProfile { UserId = userId };
                isNewProfile = true;
            }

            if (!string.IsNullOrWhiteSpace(request.Address))
            {
                profile.Address = request.Address;
            }

            if (!string.IsNullOrWhiteSpace(request.CitizenId))
            {
                profile.CitizenId = request.CitizenId;
            }
            else if (isNewProfile)
            {
                // Nếu là profile mới tinh và Frontend không gửi CCCD, ép nó thành null thay vì chuỗi rỗng
                profile.CitizenId = null;
                profile.Address = null;
            }

            // Check ngày sinh hợp lệ
            if (request.DateOfBirth.HasValue && request.DateOfBirth.Value != default(DateTime) && request.DateOfBirth.Value.Year > 1753)
            {
                profile.DateOfBirth = request.DateOfBirth.Value;
            }
            else if (isNewProfile)
            {
                profile.DateOfBirth = null;
            }

            // Handle UPLOAD FILE IMG
            if (request.AvatarFile != null && request.AvatarFile.Length > 0)
            {
                // Check file extension
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(request.AvatarFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { message = "Invalid file extension. Only .jpg, .jpeg, .png are allowed." });
                }

                // Create directory wwwroot/uploads/avatars if it doesn't exist
                string webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
                string uploadsFolder = Path.Combine(webRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate a unique file name to avoid overwriting existing files
                string uniqueFileName = Guid.NewGuid().ToString() + extension;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file to the server's physical directory
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await request.AvatarFile.CopyToAsync(fileStream);
                }

                // Save relative path to database
                profile.AvatarUrl = $"/uploads/avatars/{uniqueFileName}";
            }

            if (isNewProfile) _context.CustomerProfiles.Add(profile);
            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Update profile successfully!", avatarUrl = profile.AvatarUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lỗi SaveChanges Profile]: {ex.InnerException?.Message ?? ex.Message}");
                return StatusCode(500, new { message = "Lỗi hệ thống khi lưu thông tin vào cơ sở dữ liệu." });
            }
        }

        //============= 3. PUT /api/users/change-password
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            var userId = Guid.Parse(userIdClaim);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(new { message = "User Not Found." });

            // Check if the old password matches
            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Old password is incorrect." });
            }

            // Hash the new password and update it in the DB
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Change Password Successfully!" });
        }
    }
}
