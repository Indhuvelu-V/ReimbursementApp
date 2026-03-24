using ReimbursementTrackerApp.Interfaces;
using System.Security.Cryptography;

namespace ReimbursementTrackerApp.Services
{
        public class PasswordService : IPasswordService
        {
        public byte[] HashPassword(string password, byte[]? dbHashKey, out byte[]? hashkey)
        {

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));
            }
            HMACSHA256 hmac;
            if (dbHashKey == null)
            {
                hmac = new HMACSHA256();// Generate a random key for registering user
                hashkey = hmac.Key;
            }
            else
            {
                hmac = new HMACSHA256(dbHashKey);// Use the existing key for login
                hashkey = null;
            }
            var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hashedPassword = hmac.ComputeHash(passwordBytes);
            return hashedPassword;

        }
       
    }
    

}

