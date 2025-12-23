namespace RAMA_TMS.Models.Auth
{
    public class AppUserListItemDto
    {
        public long Id { get; set; }
        public string Email { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class UpdateAppUserDto
    {
        public string Role { get; set; } = null!;
        public bool IsActive { get; set; }
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

}
