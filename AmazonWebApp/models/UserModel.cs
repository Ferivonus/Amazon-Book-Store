using System;
using System.ComponentModel.DataAnnotations;

namespace ASPCommerce.Models
{
    public class CustomEmailValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return false;
            }

            string email = value.ToString().ToLower();

            // Check if email is in the correct format
            if (!new EmailAddressAttribute().IsValid(email))
            {
                return false;
            }

            // Check if email is from Gmail or Hotmail
            if (!email.EndsWith("@gmail.com") && !email.EndsWith("@hotmail.com"))
            {
                return false;
            }

            return true;
        }
    }

    public class UserModel
    {
        public required int UserId { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(100, ErrorMessage = "Username length must be between 1 and 100 characters", MinimumLength = 1)]
        public required string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [CustomEmailValidation(ErrorMessage = "Email must be from Gmail or Hotmail")]
        public required string Email { get; set; }

        public required string PasswordHash { get; set; } = string.Empty; // Boş bir string varsayılan değer

        [Required(ErrorMessage = "Name is required")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Surname is required")]
        public required string Surname { get; set; }

        public required string? AddressLine1 { get; set; }

        public required string? AddressLine2 { get; set; }

        public required string? AddressDescription { get; set; }

        public required string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Birthday is required")]
        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }


        public required string VerificationCode { get; set; } = Guid.NewGuid().ToString(); // Varsayılan olarak rastgele bir GUID
        public required bool IsEmailVerified { get; set; } = false;      // Varsayılan olarak doğrulanmamış
    }

    public class UserUpdateModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(100, ErrorMessage = "Username length must be between 1 and 100 characters", MinimumLength = 1)]
        public required string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [CustomEmailValidation(ErrorMessage = "Email must be from Gmail or Hotmail")]
        public required string Email { get; set; }

        public required string PasswordHash { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Surname is required")]
        public required string Surname { get; set; }

        public required string AddressLine1 { get; set; }

        public required string AddressLine2 { get; set; }

        public required string AddressDescription { get; set; }

        public required string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Birthday is required")]
        [DataType(DataType.Date)]
        public DateTime Birthday { get; set; }

        public required string VerificationCode { get; set; }
        public required bool IsEmailVerified { get; set; }
    }


    public class SignInUserModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Username is required")]
        public required string Username { get; set; }
    }

    public class ChangePasswordModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Current password is required")]
        public required string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters long and contain at least one lowercase letter, one uppercase letter, one digit, and one special character.")]
        public required string NewPassword { get; set; }
    }

    public class DeleteAccountModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public required string Password { get; set; }
    }

    public class CommentModel
    {
        public required int CommentId { get; set; }
        public required int ProductId { get; set; }
        public required int UserId { get; set; }
        public required string Comment { get; set; }
        public required DateTime CommentDate { get; set; }
    }

}
