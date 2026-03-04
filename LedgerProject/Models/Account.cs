using LedgerProject.Models.Enums;
using System;
using System.Collections.Generic;

namespace LedgerProject.Models
{
    public class Account
    {
        public Guid AccountId { get; set; }
        public string AccountName { get; set; }
        public string AccountType { get; set; }
        public AccountStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<LedgerEntry> LedgerEntries { get; set; }
    }
}