using LedgerProject.Data;
using LedgerProject.Models;
using LedgerProject.Models.Enums;
using LedgerProject.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace LedgerProject.Services
{
    public class LedgerService
    {
        private readonly AppDbContext _context;
        private readonly AuditService _auditService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LedgerService(AppDbContext context, AuditService auditService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _auditService = auditService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Guid> CreateTransactionAsync(string description,string idempotencyKey,List<(Guid accountId, decimal amount, string entryType, string narration)> entries)
        {
            var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

            if (string.IsNullOrEmpty(currentUser))
                throw new Exception("User identity not found.");
            // idempotency
            var existingTransaction = await _context.Transactions.FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);

            if (existingTransaction != null)
            {
                return existingTransaction.TransactionId;
            }
            // status validation
            foreach (var entry in entries)
            {
                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == entry.accountId);

                if (account == null)
                    throw new Exception("Account not found.");

                if (account.Status == AccountStatus.FullyFrozen)
                    throw new Exception($"Account fully frozen: {account.AccountName}");

                if (account.Status == AccountStatus.DebitFrozen && entry.amount < 0)
                    throw new Exception($"Account debit frozen: {account.AccountName}");
            }
            // validate sufficient balance for debit entries
            foreach (var entry in entries.Where(e => e.amount < 0))
            {
                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == entry.accountId);

                if (account == null)
                    throw new Exception("Account not found.");

                // only enforce for Person and Cash accounts
                if (account.AccountType == "Person" || account.AccountType == "Cash")
                {
                    var currentBalance = await _context.LedgerEntries.Where(l => l.AccountId == entry.accountId).SumAsync(l => (decimal?)l.Amount) ?? 0;

                    if (currentBalance + entry.amount < 0)
                        throw new Exception($"Insufficient balance in account: {account.AccountName}");
                }
            }

            if (entries.Sum(e => e.amount) != 0)
                throw new Exception("Transaction is not zero-sum.");

            // concurrency control between transactions
            using var dbTransaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                var transaction = new Models.Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    Description = description,
                    Status = "Created",
                    IdempotencyKey = idempotencyKey,
                    CreatedBy = currentUser,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                foreach (var entry in entries)
                {
                    var ledgerEntry = new LedgerEntry
                    {
                        LedgerEntryId = Guid.NewGuid(),
                        TransactionId = transaction.TransactionId,
                        AccountId = entry.accountId,
                        Amount = entry.amount,
                        EntryType = entry.entryType,
                        Narration = entry.narration,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.LedgerEntries.Add(ledgerEntry);
                }

                await _context.SaveChangesAsync();
                transaction.Status = "Posted";
                _context.Transactions.Update(transaction);
                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                await _auditService.LogEventAsync("TransactionPosted", "Transaction", transaction.TransactionId, currentUser, $"Transaction posted with {entries.Count} ledger entries.");
                return transaction.TransactionId;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<BalanceResponse> GetBalanceAsync(Guid accountId)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountId == accountId);

            if (account == null) throw new Exception("Account not found.");

            var balance = await _context.LedgerEntries.Where(l => l.AccountId == accountId).SumAsync(l => (decimal?)l.Amount) ?? 0;

            return new BalanceResponse
            {
                AccountId = account.AccountId,
                AccountName = account.AccountName,
                Balance = balance
            };
        }

        public async Task<List<StatementResponse>> GetStatementAsync(Guid accountId)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountId == accountId);

            if (account == null)
                throw new Exception("Account not found.");

            var entries = await _context.LedgerEntries.Where(l => l.AccountId == accountId).Include(l => l.Transaction).OrderBy(l => l.CreatedAt).ToListAsync();

            decimal runningBalance = 0;

            var statement = new List<StatementResponse>();

            foreach (var entry in entries)
            {
                runningBalance += entry.Amount;

                statement.Add(new StatementResponse
                {
                    TransactionId = entry.TransactionId,
                    Date = entry.CreatedAt,
                    Description = entry.Transaction.Description,
                    Amount = entry.Amount,
                    RunningBalance = runningBalance
                });
            }

            return statement;
        }

        public async Task<Guid> ReverseTransactionAsync(Guid transactionId, string reversedBy)
        {
            var originalTransaction = await _context.Transactions
                .Include(t => t.LedgerEntries)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (originalTransaction == null)
                throw new Exception("Transaction not found.");

            if (originalTransaction.Status != "Posted")
                throw new Exception("Only posted transactions can be reversed.");

            if (originalTransaction.Status == "Reversed")
                throw new Exception("Transaction already reversed.");

            foreach (var entry in originalTransaction.LedgerEntries)
            {
                var account = await _context.Accounts
                    .FirstOrDefaultAsync(a => a.AccountId == entry.AccountId);

                if (account.Status == AccountStatus.FullyFrozen)
                    throw new Exception("Cannot reverse transaction while account is fully frozen.");
            }
            using var dbTransaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                var reversalTransaction = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    Description = $"Reversal of {originalTransaction.TransactionId}",
                    Status = "Created",
                    IdempotencyKey = $"REV-{Guid.NewGuid()}",
                    CreatedBy = reversedBy,
                    CreatedAt = DateTime.UtcNow,
                    ReversedTransactionId = originalTransaction.TransactionId
                };

                _context.Transactions.Add(reversalTransaction);
                await _context.SaveChangesAsync();

                foreach (var entry in originalTransaction.LedgerEntries)
                {
                    var reversalEntry = new LedgerEntry
                    {
                        LedgerEntryId = Guid.NewGuid(),
                        TransactionId = reversalTransaction.TransactionId,
                        AccountId = entry.AccountId,
                        Amount = -entry.Amount,
                        EntryType = entry.Amount > 0 ? "Debit" : "Credit",
                        Narration = $"Reversal entry for {originalTransaction.TransactionId}",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.LedgerEntries.Add(reversalEntry);
                }

                await _context.SaveChangesAsync();

                // Update statuses
                originalTransaction.Status = "Reversed";
                reversalTransaction.Status = "Posted";

                _context.Transactions.Update(originalTransaction);
                _context.Transactions.Update(reversalTransaction);

                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();
                await _auditService.LogEventAsync("TransactionReversed", "Transaction", originalTransaction.TransactionId, reversedBy, $"Transaction reversed. Reversal ID: {reversalTransaction.TransactionId}");

                return reversalTransaction.TransactionId;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task UnfreezeAccountAsync(Guid accountId, string reason)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountId == accountId);

            if (account == null)
                throw new Exception("Account not found.");

            if (account.Status != AccountStatus.FullyFrozen)
                throw new Exception("Account is not frozen.");

            account.Status = AccountStatus.Active;

            await _context.SaveChangesAsync();

            var currentUser = _httpContextAccessor.HttpContext.User.Identity.Name;

            try
            {
                await _auditService.LogEventAsync("AccountUnfrozen","Account",account.AccountId,currentUser,$"Account unfrozen. Reason: {reason}");
            }
            catch
            {
                Console.WriteLine($"Failed to log audit event for account unfreeze. AccountId: {account.AccountId}, Reason: {reason}");
            }
        }
    }
}