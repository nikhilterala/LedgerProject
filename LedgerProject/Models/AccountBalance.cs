using System.ComponentModel.DataAnnotations;

namespace LedgerProject.Models
{
    public class AccountBalance
    {
        [Key]
        public Guid AccountId { get; set; }
        public decimal Balance { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Account Account { get; set; }
    }
}
