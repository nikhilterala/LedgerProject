namespace LedgerProject.Models.Requests
{
    public class CreateUserRequest
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public List<string> Roles { get; set; }
    }
}
