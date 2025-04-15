using System;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace Game.Core.Scripts.OnlineService.Authentication
{
    /// <summary>
    /// Provides validation services for authentication-related input fields
    /// Implements common validation patterns for usernames, passwords, emails, and confirmation codes
    /// </summary>
    public static class ValidationService
    {
        #region Password Validation Constants
        
        private const string MIN_PASSWORD_LENGTH = @"^.{8,}$";
        private const string PASSWORD_SPECIAL_CHARS = @"[@$!%*?&]";
        private const string PASSWORD_UPPERCASE = @"[A-Z]";
        private const string PASSWORD_LOWERCASE = @"[a-z]";
        private const string PASSWORD_NUMBER = @"\d";
        
        #endregion

        #region Username Validation

        /// <summary>
        /// Validates that a username is not empty
        /// </summary>
        public static bool ValidateUsername(TextField username)
        {
            return !string.IsNullOrEmpty(username.text);
        }

        #endregion

        #region Password Validation

        /// <summary>
        /// Validates that a password is not empty
        /// </summary>
        public static bool ValidateNullPassword(TextField password)
        {
            return !string.IsNullOrEmpty(password.text);
        }

        /// <summary>
        /// Validates that a password meets the minimum length requirement
        /// </summary>
        public static bool ValidateLengthPassword(TextField password)
        {
            return Regex.IsMatch(password.value, MIN_PASSWORD_LENGTH);
        }

        /// <summary>
        /// Validates that a password contains at least one number
        /// </summary>
        public static bool ValidateNumberPassword(TextField password)
        {
            return Regex.IsMatch(password.value, PASSWORD_NUMBER);
        }

        /// <summary>
        /// Validates that a password contains at least one special character
        /// </summary>
        public static bool ValidateSpecialPassword(TextField password)
        {
            return Regex.IsMatch(password.value, PASSWORD_SPECIAL_CHARS);
        }

        /// <summary>
        /// Validates that a password contains at least one uppercase letter
        /// </summary>
        public static bool ValidateUppercasePassword(TextField password)
        {
            return Regex.IsMatch(password.value, PASSWORD_UPPERCASE);
        }

        /// <summary>
        /// Validates that a password contains at least one lowercase letter
        /// </summary>
        public static bool ValidateLowercasePassword(TextField password)
        {
            return Regex.IsMatch(password.value, PASSWORD_LOWERCASE);
        }

        /// <summary>
        /// Validates that a password and its confirmation match
        /// </summary>
        public static bool ValidateConfirmPassword(TextField password, TextField confirmPassword)
        {
            return password.value == confirmPassword.value;
        }

        /// <summary>
        /// Validates all password requirements
        /// </summary>
        public static bool ValidatePassword(TextField password)
        {
            return ValidateNullPassword(password) &&
                   ValidateLengthPassword(password) &&
                   ValidateSpecialPassword(password) &&
                   ValidateUppercasePassword(password) &&
                   ValidateLowercasePassword(password) &&
                   ValidateNumberPassword(password);
        }

        #endregion

        #region Email Validation

        /// <summary>
        /// Validates that an email field contains a valid email address
        /// </summary>
        public static bool ValidateEmail(TextField email)
        {
            return IsValidEmail(email.value);
        }

        /// <summary>
        /// Validates that a string is a valid email address format
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Code Validation

        /// <summary>
        /// Validates that a confirmation code contains only digits
        /// </summary>
        public static bool ValidateCode(TextField code)
        {
            return Regex.IsMatch(code.value, @"^\d+$");
        }

        #endregion

        #region Validation Results

        /// <summary>
        /// Represents the result of a validation check
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; }
            public string Message { get; }

            public ValidationResult(bool isValid, string message = "")
            {
                IsValid = isValid;
                Message = message;
            }

            public static ValidationResult Success => new ValidationResult(true);
            public static ValidationResult Failure(string message) => new ValidationResult(false, message);
        }

        #endregion
    }
} 