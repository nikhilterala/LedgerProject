namespace LedgerProject.Models.Requests
{
    public class CreateTransactionRequest
    {
        public string Description { get; set; }
        public string IdempotencyKey { get; set; }

        public List<LedgerEntryRequest> Entries { get; set; }
    }

    public class LedgerEntryRequest
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public string EntryType { get; set; }
        public string Narration { get; set; }
    }
}