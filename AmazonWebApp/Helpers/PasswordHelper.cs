using System;
using System.Security.Cryptography;
using System.Text;

namespace AmazonWebApp.Helpers
{
    public static class PasswordHelper
    {
        // Verify if the provided password matches the stored hash
        public static bool VerifyPassword(string password, string storedHash)
        {
            // Extract the salt from the stored hash
            byte[] salt = Convert.FromBase64String(storedHash.Split('.')[0]);

            // Compute the hash of the provided password using the stored salt
            byte[] inputHash;
            using (var sha256 = SHA256.Create())
            {
                inputHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password), 0, Encoding.UTF8.GetByteCount(password));
            }

            // Combine the salt and hash into a single byte array
            byte[] combinedBytes = new byte[salt.Length + inputHash.Length];
            Array.Copy(salt, 0, combinedBytes, 0, salt.Length);
            Array.Copy(inputHash, 0, combinedBytes, salt.Length, inputHash.Length);

            // Compute the hash of the combined bytes
            byte[] computedHash;
            using (var sha256 = SHA256.Create())
            {
                computedHash = sha256.ComputeHash(combinedBytes);
            }

            // Compare the computed hash with the stored hash
            return storedHash.Equals(Convert.ToBase64String(salt) + "." + Convert.ToBase64String(computedHash));
        }

        // Generate a salted hash of the provided password
        public static string GenerateSaltedHash(string password)
        {
            // Generate a random salt using RandomNumberGenerator.Create() for secure randomness
            byte[] salt = CreateSalt();

            // Compute the hash of the provided password using the generated salt
            byte[] inputHash;
            using (var sha256 = SHA256.Create())
            {
                inputHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password), 0, Encoding.UTF8.GetByteCount(password));
            }

            // Combine the salt and hash into a single byte array
            byte[] combinedBytes = new byte[salt.Length + inputHash.Length];
            Array.Copy(salt, 0, combinedBytes, 0, salt.Length);
            Array.Copy(inputHash, 0, combinedBytes, salt.Length, inputHash.Length);

            // Compute the hash of the combined bytes
            byte[] computedHash;
            using (var sha256 = SHA256.Create())
            {
                computedHash = sha256.ComputeHash(combinedBytes);
            }

            // Return the salt and hash as a base64-encoded string
            return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(computedHash);
        }

        // Method to create a secure random salt using RandomNumberGenerator
        private static byte[] CreateSalt()
        {
            byte[] salt = new byte[128 / 8]; // 16 bytes salt (128 bits)
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            return salt;
        }
    }
}
