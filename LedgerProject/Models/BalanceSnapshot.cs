using System.ComponentModel.DataAnnotations;

namespace LedgerProject.Models
{
    public class BalanceSnapshot
    {
        [Key]
        public Guid AccountId { get; set; }
        public decimal SnapshotBalance { get; set; }
        public DateTime SnapshotTime { get; set; }

        public Account Account { get; set; }
    }
}