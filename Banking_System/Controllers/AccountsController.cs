using Banking_System.DTOs.BankAccount;
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
    public class AccountsController : ControllerBase
    {
        private readonly BankDbContext _context;

        public AccountsController(BankDbContext context)
        {
            _context = context;
        }

        // ================== 1. POST: /api/accounts (Create bank account)
        [HttpPost]
        [Authorize(Roles = "Customer")] // Only customers can open a new account themeselves
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            var userId = Guid.Parse(userIdClaim);

            // Genertae random account number
            string accountNumber;
            do
            {
                accountNumber = new Random().Next(100000000, 999999999).ToString() + new Random().Next(0, 9).ToString();
            } while (await _context.BankAccounts.AnyAsync(a => a.AccountNumber == accountNumber));

            var newAccount = new BankAccount
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountNumber = accountNumber,
                AccountType = request.AccountType,
                Balance = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.BankAccounts.Add(newAccount);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Open bank account successfully!", accountNumber = newAccount.AccountNumber });
        }

        //================ 2. GET: /api/accounts (Get account list)
        [HttpGet]
        public async Task<IActionResult> GetAllAccounts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool isDescending = false)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            var userId = Guid.Parse(userIdClaim);

            // Check user role to determine access level
            bool isAdmin = User.IsInRole("Admin");


            var query = _context.BankAccounts.Include(a => a.User).AsQueryable();

            if (!isAdmin)
            {
                // CUSTOMER: only see their own accounts, no advanced features
                query = query.Where(a => a.UserId == userId);
            }
            else
            {
                // ADMIN: apply advanced features (Filtering, Pagination, Sorting)

                // FILTERING: Filter by account number or account owner's name
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(a => a.AccountNumber.Contains(searchTerm) || a.User!.FullName.Contains(searchTerm));
                }

                // SORTING: Dynamic sorting
                if (!string.IsNullOrWhiteSpace(sortBy))
                {
                    if (sortBy.Equals("Balance", StringComparison.OrdinalIgnoreCase))
                    {
                        query = isDescending ? query.OrderByDescending(a => a.Balance) : query.OrderBy(a => a.Balance);
                    }
                    else if (sortBy.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase))
                    {
                        query = isDescending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt);
                    }
                }
                else
                {
                    // Sorting by newest created accounts by default for Admin
                    query = query.OrderByDescending(a => a.CreatedAt);
                }
            }

            // PAGINATION
            int totalItems = await query.CountAsync();
            var accounts = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AccountResponseDto
                {
                    Id = a.Id,
                    AccountNumber = a.AccountNumber,
                    AccountType = a.AccountType.ToString(),
                    Balance = a.Balance,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt,
                    OwnerName = a.User != null ? a.User.FullName : "Unknown"
                })
                .ToListAsync();

            // Return paginated structure 
            return Ok(new
            {
                totalItems,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                data = accounts
            });
        }

        // 3.================== GET: /api/accounts/{number} (Lấy chi tiết 1 tài khoản)
        [HttpGet("{number}")]
        public async Task<IActionResult> GetAccountByNumber(string number)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            var userId = Guid.Parse(userIdClaim);

            var account = await _context.BankAccounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.AccountNumber == number);

            if (account == null) return NotFound(new { message = "Account Not Found." });

            //  Customers are generally not allowed to view other users' account IDs
            if (!User.IsInRole("Admin") && account.UserId != userId)
            {
                return Forbid(); // Deny access (Return HTTP 403)
            }

            var response = new AccountResponseDto
            {
                Id = account.Id,
                AccountNumber = account.AccountNumber,
                AccountType = account.AccountType.ToString(),
                Balance = account.Balance,
                IsActive = account.IsActive,
                CreatedAt = account.CreatedAt,
                OwnerName = account.User != null ? account.User.FullName : "Unknown"
            };

            return Ok(response);
        }

        // 4.================== PUT: /api/accounts/{number}/close (Close or Lock Bank Account - SOFT DELETE)
        [HttpPut("{number}/close")]
        public async Task<IActionResult> CloseAccount(string number)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();
            var userId = Guid.Parse(userIdClaim);

            var account = await _context.BankAccounts.FirstOrDefaultAsync(a => a.AccountNumber == number);
            if (account == null) return NotFound(new { message = "Account Not Found." });

            // Only Admin or the account owner can close the account
            if (!User.IsInRole("Admin") && account.UserId != userId)
            {
                return Forbid();
            }

            if (!account.IsActive)
            {
                return BadRequest(new { message = "This account is already closed/locked." });
            }

            // Proceed to update the account status to closed
            account.IsActive = false;
            _context.BankAccounts.Update(account);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Account number {account.AccountNumber} has been successfully closed/locked!" });
        }


        // 5. DELETE: /api/accounts/{id} (Hard Delete - ONLY ADMIN)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Ép phân quyền: Chỉ Token có chứa Role "Admin" mới gọi được API này
        public async Task<IActionResult> HardDeleteAccount(Guid id)
        {
            var account = await _context.BankAccounts.FirstOrDefaultAsync(a => a.Id == id);
            if (account == null)
            {
                return NotFound(new { message = "Không tìm thấy tài khoản ngân hàng cần xóa." });
            }

            // Cannot delete if balance is greater than 0
            if (account.Balance > 0)
            {
                return BadRequest(new { message = "Cannot delete this account because the balance is greater than 0. Please withdraw all funds before deleting." });
            }

            // Avoid deleting accounts that have transaction history to maintain integrity of financial records
            bool hasTransactions = await _context.Transactions.AnyAsync(t => t.SourceAccountId == id || t.DestinationAccountId == id);
            if (hasTransactions)
            {
                return BadRequest(new
                {
                    message = "This account has transaction history. " +
                              "To maintain the integrity of financial records, Admin cannot perform a hard delete. Please use the Close/Lock account feature instead."
                });
            }

            // If all checks passed, proceed to hard delete the account
            _context.BankAccounts.Remove(account);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Admin has successfully hard deleted the account number {account.AccountNumber} from the system." });
        }


    }
}
