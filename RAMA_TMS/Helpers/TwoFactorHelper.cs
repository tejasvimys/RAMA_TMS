using System.Security.Cryptography;
using System.Text;

namespace RAMA_TMS.Helpers
{
    public static class TwoFactorHelper
    {
        private const int SecretKeyLength = 20; // 160 bits
        private const int CodeLength = 6;
        private const int TimeStep = 30; // seconds

        /// <summary>
        /// Generates a random base32-encoded secret key for TOTP
        /// </summary>
        public static string GenerateSecret()
        {
            var bytes = new byte[SecretKeyLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Base32Encode(bytes);
        }

        /// <summary>
        /// Generates backup recovery codes
        /// </summary>
        public static List<string> GenerateBackupCodes(int count = 10)
        {
            var codes = new List<string>();
            using (var rng = RandomNumberGenerator.Create())
            {
                for (int i = 0; i < count; i++)
                {
                    var bytes = new byte[4];
                    rng.GetBytes(bytes);
                    var code = BitConverter.ToUInt32(bytes, 0) % 100000000;
                    codes.Add(code.ToString("D8"));
                }
            }
            return codes;
        }

        /// <summary>
        /// Validates a TOTP code
        /// </summary>
        public static bool ValidateCode(string secret, string code, int windowSize = 1)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(code))
                return false;

            // Remove any spaces from code
            code = code.Trim().Replace(" ", "");

            if (code.Length != 6 && code.Length != 8)
                return false;

            var currentTimestamp = GetCurrentTimestamp();

            // Check current time window and adjacent windows (±30 seconds)
            for (int i = -windowSize; i <= windowSize; i++)
            {
                var timestamp = currentTimestamp + i;
                var expectedCode = GenerateCode(secret, timestamp);

                if (expectedCode == code)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Generates a provisioning URI for QR code
        /// </summary>
        public static string GetProvisioningUri(string email, string secret, string issuer = "RAMA TMS")
        {
            return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
        }

        private static string GenerateCode(string secret, long timestamp)
        {
            var key = Base32Decode(secret);
            var counter = BitConverter.GetBytes(timestamp);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(counter);

            using var hmac = new HMACSHA1(key);
            var hash = hmac.ComputeHash(counter);

            var offset = hash[hash.Length - 1] & 0x0F;
            var binary =
                ((hash[offset] & 0x7F) << 24) |
                ((hash[offset + 1] & 0xFF) << 16) |
                ((hash[offset + 2] & 0xFF) << 8) |
                (hash[offset + 3] & 0xFF);

            var otp = binary % 1000000;
            return otp.ToString("D6");
        }

        private static long GetCurrentTimestamp()
        {
            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return unixTimestamp / TimeStep;
        }

        private static string Base32Encode(byte[] data)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new StringBuilder();
            int buffer = data[0];
            int bitsLeft = 8;
            int arrayIndex = 1;

            while (bitsLeft > 0 || arrayIndex < data.Length)
            {
                if (bitsLeft < 5)
                {
                    if (arrayIndex < data.Length)
                    {
                        buffer <<= 8;
                        buffer |= data[arrayIndex++];
                        bitsLeft += 8;
                    }
                    else
                    {
                        int pad = 5 - bitsLeft;
                        buffer <<= pad;
                        bitsLeft += pad;
                    }
                }

                int index = (buffer >> (bitsLeft - 5)) & 0x1F;
                bitsLeft -= 5;
                result.Append(alphabet[index]);
            }

            return result.ToString();
        }

        private static byte[] Base32Decode(string encoded)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            encoded = encoded.ToUpperInvariant().Replace(" ", "").Replace("-", "");

            var result = new List<byte>();
            int buffer = 0;
            int bitsLeft = 0;

            foreach (char c in encoded)
            {
                int value = alphabet.IndexOf(c);
                if (value < 0) continue;

                buffer <<= 5;
                buffer |= value;
                bitsLeft += 5;

                if (bitsLeft >= 8)
                {
                    result.Add((byte)(buffer >> (bitsLeft - 8)));
                    bitsLeft -= 8;
                }
            }

            return result.ToArray();
        }
    }
}
