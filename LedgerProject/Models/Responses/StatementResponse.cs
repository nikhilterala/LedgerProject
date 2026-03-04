namespace LedgerProject.Models.Responses
{
    public class StatementResponse
    {
        public Guid TransactionId { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public decimal RunningBalance { get; set; }
    }
}