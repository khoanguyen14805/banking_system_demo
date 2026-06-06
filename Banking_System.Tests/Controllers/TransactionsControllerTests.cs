using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Banking_System.Controllers;
using Banking_System.Models;
using Banking_System.Models.Context;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Banking_System.DTOs.Transaction;

namespace Banking_System.Tests.Controllers
{
    public class TransactionsControllerTests
    {
        // Hàm khởi tạo DB ảo trên RAM và cấu hình lờ đi Transaction cảnh báo
        private BankDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<BankDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new BankDbContext(options);
        }

        // Hàm giả lập Token danh tính User đăng nhập
        private void SetupUserClaims(ControllerBase controller, Guid userId, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }
        // TESTCASE 1: CHUYỂN TIỀN HỢP LỆ THÀNH CÔNG
        [Fact]
        public async Task TransferMoney_ValidRequest_ReturnsOk_UpdatesBalances()
        {
            // 1. Arrange
            var dbContext = GetInMemoryDbContext();
            var sourceUser = new User { Id = Guid.NewGuid(), Username = "source", IsActive = true };
            var destUser = new User { Id = Guid.NewGuid(), Username = "dest", IsActive = true };
            var sourceAccount = new BankAccount { Id = Guid.NewGuid(), UserId = sourceUser.Id, AccountNumber = "111111", Balance = 500000, IsActive = true };
            var destAccount = new BankAccount { Id = Guid.NewGuid(), UserId = destUser.Id, AccountNumber = "222222", Balance = 100000, IsActive = true };

            dbContext.Users.AddRange(sourceUser, destUser);
            dbContext.BankAccounts.AddRange(sourceAccount, destAccount);
            await dbContext.SaveChangesAsync();

            var controller = new TransactionsController(dbContext);
            SetupUserClaims(controller, sourceUser.Id, "Customer");

            var transferDto = new TransferDto
            {
                FromAccountId = sourceAccount.Id,
                ToAccountId = destAccount.Id,
                Amount = 200000,
                Description = "Chuyển tiền học phí EIU"
            };

            // 2. Act
            var result = await controller.Transfer(transferDto);

            // 3. Assert
            var okResult = result.As<OkObjectResult>();
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var updatedSource = await dbContext.BankAccounts.FindAsync(sourceAccount.Id);
            var updatedDest = await dbContext.BankAccounts.FindAsync(destAccount.Id);
            updatedSource!.Balance.Should().Be(300000); // 500k - 200k
            updatedDest!.Balance.Should().Be(300000);   // 100k + 200k
        }

        // TESTCASE 2: KHÔNG ĐỦ SỐ DƯ (INSUFFICIENT BALANCE)
        [Fact]
        public async Task TransferMoney_InsufficientBalance_ReturnsBadRequest()
        {
            // 1. Arrange
            var dbContext = GetInMemoryDbContext();
            var sourceUser = new User { Id = Guid.NewGuid(), Username = "source", IsActive = true };
            var destUser = new User { Id = Guid.NewGuid(), Username = "dest", IsActive = true };
            var sourceAccount = new BankAccount { Id = Guid.NewGuid(), UserId = sourceUser.Id, AccountNumber = "111111", Balance = 20000, IsActive = true }; // Chỉ có 20k
            var destAccount = new BankAccount { Id = Guid.NewGuid(), UserId = destUser.Id, AccountNumber = "222222", Balance = 100000, IsActive = true };

            dbContext.Users.AddRange(sourceUser, destUser);
            dbContext.BankAccounts.AddRange(sourceAccount, destAccount);
            await dbContext.SaveChangesAsync();

            var controller = new TransactionsController(dbContext);
            SetupUserClaims(controller, sourceUser.Id, "Customer");

            var transferDto = new TransferDto
            {
                FromAccountId = sourceAccount.Id,
                ToAccountId = destAccount.Id,
                Amount = 50000, // Đòi chuyển 50k
                Description = "Thử chuyển quá số dư"
            };

            // 2. Act
            var result = await controller.Transfer(transferDto);

            // 3. Assert
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be(400);

            var updatedSource = await dbContext.BankAccounts.FindAsync(sourceAccount.Id);
            updatedSource!.Balance.Should().Be(20000); // Tiền giữ nguyên
        }

        

        // TESTCASE 3: TÀI KHOẢN KHÔNG TỒN TẠI (NOT FOUND)
        [Fact]
        public async Task TransferMoney_AccountNotFound_ReturnsNotFound()
        {
            // 1. Arrange
            var dbContext = GetInMemoryDbContext();
            var sourceUser = new User { Id = Guid.NewGuid(), Username = "source", IsActive = true };
            var sourceAccount = new BankAccount { Id = Guid.NewGuid(), UserId = sourceUser.Id, AccountNumber = "111111", Balance = 500000, IsActive = true };
            
            dbContext.Users.Add(sourceUser);
            dbContext.BankAccounts.Add(sourceAccount);
            await dbContext.SaveChangesAsync();

            var controller = new TransactionsController(dbContext);
            SetupUserClaims(controller, sourceUser.Id, "Customer");

            var transferDto = new TransferDto
            {
                FromAccountId = sourceAccount.Id,
                ToAccountId = Guid.NewGuid(), // ID tài khoản đích ngẫu nhiên, không hề tồn tại trong DB
                Amount = 50000,
                Description = "Chuyển tiền tới tài khoản ma"
            };

            // 2. Act
            var result = await controller.Transfer(transferDto);

            // 3. Assert
            var notFoundResult = result.As<NotFoundObjectResult>();
            notFoundResult.Should().NotBeNull();
            notFoundResult.StatusCode.Should().Be(404);
        }

        // ==========================================
        // TESTCASE 4: TÀI KHOẢN ĐANG BỊ KHÓA (ACCOUNT INACTIVE)
        // ==========================================
        [Fact]
        public async Task TransferMoney_InactiveAccount_ReturnsBadRequest()
        {
            // 1. Arrange
            var dbContext = GetInMemoryDbContext();
            var sourceUser = new User { Id = Guid.NewGuid(), Username = "source", IsActive = true };
            var destUser = new User { Id = Guid.NewGuid(), Username = "dest", IsActive = true };
            
            // Tài khoản nguồn đang bị ĐÓNG BĂNG/KHÓA (IsActive = false)
            var sourceAccount = new BankAccount { Id = Guid.NewGuid(), UserId = sourceUser.Id, AccountNumber = "111111", Balance = 500000, IsActive = false }; 
            var destAccount = new BankAccount { Id = Guid.NewGuid(), UserId = destUser.Id, AccountNumber = "222222", Balance = 100000, IsActive = true };

            dbContext.Users.AddRange(sourceUser, destUser);
            dbContext.BankAccounts.AddRange(sourceAccount, destAccount);
            await dbContext.SaveChangesAsync();

            var controller = new TransactionsController(dbContext);
            SetupUserClaims(controller, sourceUser.Id, "Customer");

            var transferDto = new TransferDto
            {
                FromAccountId = sourceAccount.Id,
                ToAccountId = destAccount.Id,
                Amount = 50000,
                Description = "Tài khoản bị khóa cố chuyển tiền"
            };

            // 2. Act
            var result = await controller.Transfer(transferDto);

            // 3. Assert
            var badRequestResult = result.As<BadRequestObjectResult>();
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be(400);
        }
    }
}