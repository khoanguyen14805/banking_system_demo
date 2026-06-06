using Banking_System.DTOs.Transaction;
using Banking_System.Models.Context;
using Banking_System.Models.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Banking_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly BankDbContext _context;

        public TransactionsController(BankDbContext context)
        {
            _context = context;
        }

        // 1.==================== POST /api/transactions/deposit (Deposit into account)
        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositWithdrawDto request)
        {
            var account = await _context.BankAccounts.FirstOrDefaultAsync(a => a.AccountNumber == request.AccountNumber);
            if (account == null) return NotFound(new { message = "Account Not Found." });
            if (!account.IsActive) return BadRequest(new { message = "This account is currently locked/closed." });

            if (request.Amount <= 0)
            {
                return BadRequest("The amount must be larger than 0.");
            }

            // Use EF Core Transactions to securely wrap data
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                account.Balance += request.Amount;

                var log = new Models.Transaction //Distinguish between System.Transaction and Models.Transaction
                {
                    Id = Guid.NewGuid(),
                    SourceAccountId = account.Id,
                    DestinationAccountId = null, // Deposit transction does not have destination account
                    Amount = request.Amount,
                    Type = TransactionType.Deposit,
                    Description = $"Deposit into account: {account.AccountNumber}",
                    Status = BankTransactionStatus.Success,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BankAccounts.Update(account);
                _context.Transactions.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync(); // Confirm permanent saving to the database
                return Ok(new { message = "Successfull Deposit Transaction!", newBalance = account.Balance });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(); // Refund if system error occurs
                return StatusCode(500, new { message = "An error occurred during the deposit process." });
            }
        }

        // 2.==================== POST /api/transactions/withdraw (Withdraw from account)
        [HttpPost("withdraw")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Withdraw([FromBody] DepositWithdrawDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdClaim!);

            var account = await _context.BankAccounts.FirstOrDefaultAsync(a => a.AccountNumber == request.AccountNumber);
            if (account == null) return NotFound(new { message = "Account Not Found." });
            if (!account.IsActive) return BadRequest(new { message = "This account is currently locked/closed." });

            // Only account owner can withdraw money from their account, even Admin cannot withdraw from other account
            if (account.UserId != userId) return Forbid();
            if (account.Balance < request.Amount) return BadRequest(new { message = "Insufficient account balance to perform the withdrawal." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                account.Balance -= request.Amount;

                var log = new Models.Transaction
                {
                    Id = Guid.NewGuid(),
                    SourceAccountId = account.Id,
                    DestinationAccountId = null,
                    Amount = request.Amount,
                    Type = TransactionType.Withdraw,
                    Description = $"Withdraw from account: {account.AccountNumber}",
                    Status = BankTransactionStatus.Success,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BankAccounts.Update(account);
                _context.Transactions.Add(log);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { message = "Successful Withdraw Transaction!", newBalance = account.Balance });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred during the withdraw process." });
            }
        }

        // 3. ============== POST /api/transactions/transfer (Transfer funds)
        [HttpPost("transfer")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Transfer([FromBody] TransferDto request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new { message = "The transfer amount must be greater than 0." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdClaim!);

            //Find current user's account as source account for transfer
            var sourceAccount = await _context.BankAccounts.FirstOrDefaultAsync(a => a.UserId == userId);

            if (sourceAccount == null)
                return NotFound(new { message = "You have not activated a bank account in the system." });

            if (!sourceAccount.IsActive)
                return BadRequest(new { message = "Your account is currently locked or inactive." });

            //Find destination account based on the provided account number
            var destAcc = await _context.BankAccounts.FirstOrDefaultAsync(a => a.AccountNumber == request.ToAccountNumber);

            if (destAcc == null) return NotFound(new { message = "Destination Account Not Found." });

            if (!destAcc.IsActive) return BadRequest(new { message = "Destination account is locked." });

            if (sourceAccount.Balance < request.Amount) return BadRequest(new { message = "Insufficient account balance to perform the transfer." });


            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                sourceAccount.Balance -= request.Amount;
                destAcc.Balance += request.Amount;

                var log = new Models.Transaction
                {
                    Id = Guid.NewGuid(),
                    SourceAccountId = sourceAccount.Id,
                    DestinationAccountId = destAcc.Id,
                    Amount = request.Amount,
                    Type = TransactionType.Transfer,
                    Description = request.Description,
                    Status = BankTransactionStatus.Success,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BankAccounts.Update(sourceAccount);
                _context.BankAccounts.Update(destAcc);
                _context.Transactions.Add(log);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Transfer successfully!", currentBalance = sourceAccount.Balance });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred during the transfer process." });
            }
        }

        //================ 4. GET /api/transactions 
        [HttpGet]
        public async Task<IActionResult> GetTransactionHistory(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] TransactionType? type = null) // Filter based on transaction type 
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdClaim!);

            //Query includes source and destination account
            var query = _context.Transactions
                .Include(t => t.SourceBankAccount)
                .Include(t => t.DestinationBankAccount)
                .AsQueryable();

            // If not Admin, only show transactions related to the user's accounts 
            if (!User.IsInRole("Admin"))
            {
                var myAccountIds = await _context.BankAccounts
                    .Where(a => a.UserId == userId)
                    .Select(a => a.Id)
                    .ToListAsync();

                query = query.Where(t => myAccountIds.Contains(t.SourceAccountId) ||
                                         (t.DestinationAccountId != null && myAccountIds.Contains(t.DestinationAccountId.Value)));
            }

            // FILTERING: base on transaction type 
            if (type.HasValue)
            {
                query = query.Where(t => t.Type == type.Value);
            }

            // SORTING: base on created date, newest first
            query = query.OrderByDescending(t => t.CreatedAt);

            // PAGINATION
            int totalItems = await query.CountAsync();
            var transactions = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TransactionResponseDto
                {
                    Id = t.Id,
                    SourceAccountNumber = t.SourceBankAccount != null ? t.SourceBankAccount.AccountNumber : "N/A",
                    DestinationAccountNumber = t.DestinationBankAccount != null ? t.DestinationBankAccount.AccountNumber : "N/A",
                    Amount = t.Amount,
                    Type = t.Type.ToString(),
                    Description = t.Description,
                    Status = t.Status.ToString(),
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                totalItems,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                data = transactions
            });
        }

        //5. ========================= GET /api/transactions/{id} 
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransactionDetail(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdClaim!);

            var t = await _context.Transactions
                .Include(t => t.SourceBankAccount)
                .Include(t => t.DestinationBankAccount)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (t == null) return NotFound(new { message = "Transaction not found." });

            // SECURITY: Customers can usually only view transactions related to their accounts
            if (!User.IsInRole("Admin"))
            {
                bool isOwner = t.SourceBankAccount?.UserId == userId || t.DestinationBankAccount?.UserId == userId;
                if (!isOwner) return Forbid();
            }

            var response = new TransactionResponseDto
            {
                Id = t.Id,
                SourceAccountNumber = t.SourceBankAccount != null ? t.SourceBankAccount.AccountNumber : "N/A",
                DestinationAccountNumber = t.DestinationBankAccount != null ? t.DestinationBankAccount.AccountNumber : "N/A",
                Amount = t.Amount,
                Type = t.Type.ToString(),
                Description = t.Description,
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt
            };

            return Ok(response);
        }
    }
}
