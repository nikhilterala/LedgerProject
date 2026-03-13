namespace LedgerProject.Models.Requests
{
    public class AdjustmentRequest
    {
        public string Description { get; set; }
        public string IdempotencyKey { get; set; }
        public List<AdjustmentEntry> Entries { get; set; }
    }

    public class AdjustmentEntry
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
