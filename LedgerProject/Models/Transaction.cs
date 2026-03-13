using LedgerProject.Models.Enums;

namespace LedgerProject.Models
{
    public class Transaction
    {
        public Guid TransactionId { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string IdempotencyKey { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<LedgerEntry> LedgerEntries { get; set; }
        public TransactionType TransactionType { get; set; }
        public Guid? ReversedTransactionId { get; set; }
    }
}
