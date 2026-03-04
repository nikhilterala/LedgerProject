namespace LedgerProject.Models.Responses
{
    public class BalanceResponse
    {
        public Guid AccountId { get; set; }
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
    }
}