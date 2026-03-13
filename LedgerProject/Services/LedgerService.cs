using LedgerProject.Data;
using LedgerProject.Models;
using LedgerProject.Models.Enums;
using LedgerProject.Models.Responses;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Net.NetworkInformation;

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

            if (entries == null || entries.Count == 0)
                throw new Exception("Transaction must contain at least one entry.");

            // Idempotency check
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);

            if (existingTransaction != null)
                return existingTransaction.TransactionId;

            // Validate zero-sum transaction
            var total = entries.Sum(e => e.amount);
            if (Math.Abs(total) > 0.0001m)
                throw new Exception("Transaction is not zero-sum.");

            var accountIds = entries.Select(e => e.accountId).Distinct().ToList();

            // Fetch accounts
            var accounts = await _context.Accounts
                .Where(a => accountIds.Contains(a.AccountId))
                .ToDictionaryAsync(a => a.AccountId);

            if (accounts.Count != accountIds.Count)
                throw new Exception("One or more accounts not found.");

            // Fetch balances
            var balances = await _context.AccountBalances
                .Where(b => accountIds.Contains(b.AccountId))
                .ToDictionaryAsync(b => b.AccountId);

            using var dbTransaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                // Validate accounts
                foreach (var entry in entries)
                {
                    var account = accounts[entry.accountId];

                    if (account.Status == AccountStatus.FullyFrozen)
                        throw new Exception($"Account fully frozen: {account.AccountName}");

                    if (account.Status == AccountStatus.DebitFrozen && entry.amount < 0)
                        throw new Exception($"Account debit frozen: {account.AccountName}");

                    if (entry.amount < 0 &&
                        (account.AccountType == "Person" || account.AccountType == "Cash"))
                    {
                        if (!balances.TryGetValue(entry.accountId, out var balance))
                            throw new Exception($"Balance record missing for account {account.AccountName}");

                        if (balance.Balance + entry.amount < 0)
                            throw new Exception($"Insufficient balance in account: {account.AccountName}");
                    }
                }

                // Create transaction
                var transaction = new Models.Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    Description = description,
                    Status = "Created",
                    IdempotencyKey = idempotencyKey,
                    TransactionType = TransactionType.Normal,
                    CreatedBy = currentUser,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(transaction);

                // Create ledger entries
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

                // Update balances
                foreach (var entry in entries)
                {
                    if (!balances.TryGetValue(entry.accountId, out var balance))
                    {
                        // create balance record if missing
                        balance = new AccountBalance
                        {
                            AccountId = entry.accountId,
                            Balance = entry.amount,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.AccountBalances.Add(balance);
                        balances[entry.accountId] = balance;
                    }
                    else
                    {
                        balance.Balance += entry.amount;
                        balance.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                transaction.Status = "Posted";
                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                await _auditService.LogEventAsync(
                    "TransactionPosted",
                    "Transaction",
                    transaction.TransactionId,
                    currentUser,
                    $"Transaction posted with {entries.Count} ledger entries.");

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
            //updating balance retrieval to use AccountBalances table for better performance
            var balance = await _context.AccountBalances.Where(b => b.AccountId == accountId).Select(b => b.Balance).FirstOrDefaultAsync();
            return new BalanceResponse
            {
                AccountId = account.AccountId,
                AccountName = account.AccountName,
                Balance = balance
            };
        }
        public async Task<List<StatementResponse>> GetMyStatementAsync(string username)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountName == username);

            if (account == null)
                throw new Exception("No account mapping found for the current user.");

            return await GetStatementAsync(account.AccountId);
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

            if (originalTransaction.Status == "Reversed")
                throw new Exception("Transaction already reversed.");

            if (originalTransaction.Status != "Posted")
                throw new Exception("Only posted transactions can be reversed.");

            var accountIds = originalTransaction.LedgerEntries
                .Select(e => e.AccountId)
                .Distinct()
                .ToList();

            // Fetch accounts once
            var accounts = await _context.Accounts
                .Where(a => accountIds.Contains(a.AccountId))
                .ToDictionaryAsync(a => a.AccountId);

            foreach (var account in accounts.Values)
            {
                if (account.Status == AccountStatus.FullyFrozen)
                    throw new Exception($"Cannot reverse transaction. Account frozen: {account.AccountName}");
            }

            using var dbTransaction = await _context.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            // Fetch balances
            var balances = await _context.AccountBalances
                .Where(b => accountIds.Contains(b.AccountId))
                .ToDictionaryAsync(b => b.AccountId);

            try
            {
                // Create reversal transaction
                var reversalTransaction = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    Description = $"Reversal of {originalTransaction.TransactionId}",
                    Status = "Created",
                    IdempotencyKey = $"REV-{Guid.NewGuid()}",
                    CreatedBy = reversedBy,
                    CreatedAt = DateTime.UtcNow,
                    TransactionType = TransactionType.Reversal,
                    ReversedTransactionId = originalTransaction.TransactionId
                };

                _context.Transactions.Add(reversalTransaction);

                // Create reversal ledger entries
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

                // Update balances (reverse effect)
                foreach (var entry in originalTransaction.LedgerEntries)
                {
                    if (!balances.TryGetValue(entry.AccountId, out var balance))
                    {
                        balance = new AccountBalance
                        {
                            AccountId = entry.AccountId,
                            Balance = -entry.Amount,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.AccountBalances.Add(balance);
                        balances[entry.AccountId] = balance;
                    }
                    else
                    {
                        balance.Balance -= entry.Amount;
                        balance.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Update transaction statuses
                originalTransaction.Status = "Reversed";
                reversalTransaction.Status = "Posted";

                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                await _auditService.LogEventAsync("TransactionReversed","Transaction",originalTransaction.TransactionId,reversedBy,$"Transaction reversed. Reversal ID: {reversalTransaction.TransactionId}");

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

            if (account.Status == AccountStatus.Active)
                throw new Exception("Account is already active.");

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

        public async Task<Guid> CreateAdjustmentAsync(string description,List<(Guid accountId, decimal amount)> entries,string idempotencyKey)
        {
            if (entries == null || entries.Count == 0)
                throw new Exception("Adjustment must contain at least one entry.");

            if (entries.Sum(e => e.amount) != 0)
                throw new Exception("Adjustment must be zero-sum.");

            var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

            if (string.IsNullOrEmpty(currentUser))
                throw new Exception("User identity not found.");

            // Idempotency check
            var existingTransaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);

            if (existingTransaction != null)
                return existingTransaction.TransactionId;

            var accountIds = entries.Select(e => e.accountId).Distinct().ToList();

            // Fetch balances
            var balances = await _context.AccountBalances
                .Where(b => accountIds.Contains(b.AccountId))
                .ToDictionaryAsync(b => b.AccountId);

            using var dbTransaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                var transaction = new Transaction
                {
                    TransactionId = Guid.NewGuid(),
                    Description = description,
                    Status = "Posted",
                    TransactionType = TransactionType.Adjustment,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUser,
                    IdempotencyKey = idempotencyKey
                };

                _context.Transactions.Add(transaction);

                // Create ledger entries
                foreach (var entry in entries)
                {
                    var ledgerEntry = new LedgerEntry
                    {
                        LedgerEntryId = Guid.NewGuid(),
                        TransactionId = transaction.TransactionId,
                        AccountId = entry.accountId,
                        Amount = entry.amount,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.LedgerEntries.Add(ledgerEntry);
                }

                await _context.SaveChangesAsync();

                // Update balances safely
                foreach (var entry in entries)
                {
                    if (!balances.TryGetValue(entry.accountId, out var balance))
                    {
                        balance = new AccountBalance
                        {
                            AccountId = entry.accountId,
                            Balance = entry.amount,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.AccountBalances.Add(balance);
                        balances[entry.accountId] = balance;
                    }
                    else
                    {
                        balance.Balance += entry.amount;
                        balance.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                await _auditService.LogEventAsync("AdjustmentPosted","Transaction",transaction.TransactionId,currentUser,$"Adjustment posted with {entries.Count} ledger entries.");

                return transaction.TransactionId;
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<PagedResult<Transaction>> GetTransactionsAsync(int page, int pageSize)
        {
            var query = _context.Transactions
                .OrderByDescending(t => t.CreatedAt);

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Transaction>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task<List<Account>> GetFrozenAccountsAsync()
        {
            return await _context.Accounts
                .Where(a => a.Status != AccountStatus.Active)
                .ToListAsync();
        }

        public async Task<List<Account>> GetAccountsAsync()
        {
            return await _context.Accounts.ToListAsync();
        }
    }
}