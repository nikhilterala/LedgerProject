namespace LedgerProject.Models
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; }

        public ICollection<UserRole> UserRoles { get; set; }
    }
}
