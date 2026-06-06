using Banking_System.DTOs.ATMCard;
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
    public class CardsController : ControllerBase
    {
        private readonly BankDbContext _context;

        public CardsController(BankDbContext context)
        {
            _context = context;
        }

        // 1.======================== POST: /api/cards (Issue new ATM card)
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateCard([FromBody] CreateCardDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdClaim!);

            var account = await _context.BankAccounts.FirstOrDefaultAsync(a => a.AccountNumber == request.AccountNumber);
            if (account == null) return NotFound(new { message = "Linked Account Not Found." });

            // The linked account must be owned by the person currently logged in and must be active
            if (account.UserId != userId) return Forbid();
            if (!account.IsActive) return BadRequest(new { message = "Your bank account is locked; you cannot be issued an ATM card." });

            // Generate ATM number
            string cardNumber;
            var random = new Random();
            do
            {
                cardNumber = "9704"; // Default PIN number for Napas Vietnam domestic cards
                for (int i = 0; i < 12; i++)
                {
                    cardNumber += random.Next(0, 10).ToString();
                }
            } while (await _context.ATMCards.AnyAsync(c => c.CardNumber == cardNumber));

            // Generate a random 6-digit PIN (this code must be hashed, similar to a password)
            string pinCode = random.Next(100000, 999999).ToString();

            var newCard = new ATMCard
            {
                Id = Guid.NewGuid(),
                BankAccountId = account.Id,
                CardNumber = cardNumber,
                PinHash = BCrypt.Net.BCrypt.HashPassword(pinCode),
                ExpiryDate = DateTime.UtcNow.AddYears(5),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.ATMCards.Add(newCard);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Issue ATM Card successfully!",
                cardNumber = newCard.CardNumber,
                initialPin = pinCode, // This PIN code will only be displayed once, immediately after creation, for the customer to record
                expiryDate = newCard.ExpiryDate
            });
        }

        // 2.==================== GET: /api/cards
        [HttpGet]
        public async Task<IActionResult> GetMyCards(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? cardNumberSearch = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdClaim!);

            // include account and user info
            var query = _context.ATMCards
                .Include(c => c.BankAccount)
                .ThenInclude(a => a!.User)
                .AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                //Cusotmer can only view their own card
                query = query.Where(c => c.BankAccount!.UserId == userId);
            }
            else
            {
                // Admin can search by cardnumber
                if (!string.IsNullOrWhiteSpace(cardNumberSearch))
                {
                    query = query.Where(c => c.CardNumber.Contains(cardNumberSearch));
                }
            }

            // Sort by newest created day (by default)
            query = query.OrderByDescending(c => c.CreatedAt);

            // Pagination
            int totalItems = await query.CountAsync();
            var cards = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CardResponseDto
                {
                    Id = c.Id,
                    CardNumber = c.CardNumber,
                    AssociatedAccountNumber = c.BankAccount != null ? c.BankAccount.AccountNumber : "N/A",
                    OwnerName = c.BankAccount != null && c.BankAccount.User != null ? c.BankAccount.User.FullName : "Unknown",
                    ExpiryDate = c.ExpiryDate,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                totalItems,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                data = cards
            });
        }

        // 3.=============== PUT: /api/cards/{id}/block (Emergency card lock)
        [HttpPut("{id}/block")]
        public async Task<IActionResult> BlockCard(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdClaim!);

            var card = await _context.ATMCards
                .Include(c => c.BankAccount)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (card == null) return NotFound(new { message = "ATM card not found." });

            // SECURITY: Only Admin or the owner of the linked account can block the card
            if (!User.IsInRole("Admin") && card.BankAccount?.UserId != userId)
            {
                return Forbid();
            }

            if (!card.IsActive)
            {
                return BadRequest(new { message = "This ATM card is already blocked." });
            }

            // Proceed to block the card
            card.IsActive = false;
            _context.ATMCards.Update(card);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"ATM card number {card.CardNumber} has been successfully blocked!" });
        }
    }
}
