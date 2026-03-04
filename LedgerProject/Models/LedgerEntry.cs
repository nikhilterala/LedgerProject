namespace LedgerProject.Models
{
    public class LedgerEntry
    {
        public Guid LedgerEntryId { get; set; }

        public Guid TransactionId { get; set; }
        public Transaction Transaction { get; set; }

        public Guid AccountId { get; set; }
        public Account Account { get; set; }

        public decimal Amount { get; set; }
        public string EntryType { get; set; }
        public string Narration { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
