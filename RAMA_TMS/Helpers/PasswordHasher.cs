using System.Security.Cryptography;
using System.Text;

namespace RAMA_TMS.Helpers
{
    public static class PasswordHasher 
    {
        public static (string hash, string salt) HashPassword(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            var salt = Convert.ToBase64String(saltBytes);
            var hash = ComputeHash(password, salt);
            return (hash, salt);
        }

        public static bool Verify(string password, string hash, string salt)
        {
            var computed = ComputeHash(password, salt);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed),
                Encoding.UTF8.GetBytes(hash));
        }

        private static string ComputeHash(string password, string salt)
        {
            var key = Encoding.UTF8.GetBytes(salt);
            using var hmac = new HMACSHA256(key);
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
