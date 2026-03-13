using LedgerProject.Data;
using LedgerProject.Models;
using LedgerProject.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LedgerProject.Services
{
    public class ReconciliationService
    {
        private readonly AppDbContext _context;
        private readonly AuditService _auditService;
        public ReconciliationService(AppDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task ReconcileAsync()
        {
            var accounts = await _context.Accounts.ToListAsync();

            var accountIds = accounts.Select(a => a.AccountId).ToList();
            var cachedBalances = await _context.AccountBalances
                .Where(b => accountIds.Contains(b.AccountId))
                .ToDictionaryAsync(b => b.AccountId);

            foreach (var account in accounts)
            {
                var ledgerBalance = await _context.LedgerEntries
                    .Where(l => l.AccountId == account.AccountId)
                    .SumAsync(l => (decimal?)l.Amount) ?? 0;

                var snapshot = await _context.BalanceSnapshots
                    .FirstOrDefaultAsync(s => s.AccountId == account.AccountId);

                if (snapshot == null)
                {
                    if (cachedBalances.TryGetValue(account.AccountId, out var cachedBalance))
                    {
                        if (cachedBalance.Balance != ledgerBalance)
                        {
                            account.Status = AccountStatus.FullyFrozen;
                            await _auditService.LogEventAsync("AccountFrozen","Account",account.AccountId,"System","Account frozen: cached balance does not match ledger on initial reconciliation.");
                            continue;
                        }
                    }

                    _context.BalanceSnapshots.Add(new BalanceSnapshot
                    {
                        AccountId = account.AccountId,
                        SnapshotBalance = ledgerBalance,
                        SnapshotTime = DateTime.UtcNow
                    });

                    continue;
                }

                if (snapshot.SnapshotBalance != ledgerBalance)
                {
                    account.Status = AccountStatus.FullyFrozen;
                    await _auditService.LogEventAsync(
                        "AccountFrozen",
                        "Account",
                        account.AccountId,
                        "System",
                        "Account frozen due to reconciliation mismatch.");
                    continue;
                }

                if (cachedBalances.TryGetValue(account.AccountId, out var cached))
                {
                    if (cached.Balance != snapshot.SnapshotBalance)
                    {
                        account.Status = AccountStatus.FullyFrozen;
                        await _auditService.LogEventAsync(
                            "AccountFrozen",
                            "Account",
                            account.AccountId,
                            "System",
                            "Account frozen: cached balance diverged from ledger snapshot.");
                        continue;
                    }
                }

                snapshot.SnapshotBalance = ledgerBalance;
                snapshot.SnapshotTime = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}