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

            foreach (var account in accounts)
            {
                var actualBalance = await _context.LedgerEntries
                    .Where(l => l.AccountId == account.AccountId)
                    .SumAsync(l => (decimal?)l.Amount) ?? 0;

                var snapshot = await _context.BalanceSnapshots
                    .FirstOrDefaultAsync(s => s.AccountId == account.AccountId);

                if (snapshot == null)
                {
                    _context.BalanceSnapshots.Add(new BalanceSnapshot
                    {
                        AccountId = account.AccountId,
                        SnapshotBalance = actualBalance,
                        SnapshotTime = DateTime.UtcNow
                    });

                    continue;
                }

                if (snapshot.SnapshotBalance != actualBalance)
                {
                    account.Status = AccountStatus.FullyFrozen;
                    await _auditService.LogEventAsync("AccountFrozen","Account",account.AccountId,"System","Account frozen due to reconciliation mismatch.");
                    continue;
                }

                snapshot.SnapshotBalance = actualBalance;
                snapshot.SnapshotTime = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}