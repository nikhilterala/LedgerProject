namespace LedgerProject.Models
{
    public class Role
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; }

        public ICollection<UserRole> UserRoles { get; set; }
    }
}
