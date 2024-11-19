using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using ASPCommerce.Models;
using ASPCommerce.Helpers; 
using System.Net.Mail;
using System.Net;


namespace ASPCommerce.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController>? _logger;
        private readonly string? _connectionString;

        public UserController(ILogger<UserController>? logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("ConnectionString");
        }

        [HttpGet("all-accounts")]
        public async Task<IActionResult> GetAllAccountsAsync()
        {
            try
            {
                _logger?.LogInformation("Starting to retrieve all user accounts");

                using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                _logger?.LogInformation("Database connection established");

                string query = @"SELECT u.UserId, ua.Username, ua.Email, ua.PasswordHash, 
                         u.FirstName, u.LastName, u.AddressLine1, 
                         u.AddressLine2, u.AddressDescription, 
                         u.PhoneNumber, u.Birthday, 
                         ua.VerificationCode, ua.IsEmailVerified 
                         FROM Users u
                         INNER JOIN UserAuthentication ua ON u.UserId = ua.UserId";

                _logger.LogInformation("Executing SQL query to fetch user accounts");

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                List<UserModel> users = new List<UserModel>();
                int rowCount = 0;
                while (await reader.ReadAsync())
                {
                    rowCount++;
                    UserModel user = new UserModel
                    {
                        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                        Username = reader.GetString(reader.GetOrdinal("Username")),
                        Email = reader.GetString(reader.GetOrdinal("Email")),
                        PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                        Name = reader.GetString(reader.GetOrdinal("FirstName")),
                        Surname = reader.GetString(reader.GetOrdinal("LastName")),
                        // Provide defaults for nullable fields
                        AddressLine1 = reader.IsDBNull(reader.GetOrdinal("AddressLine1")) ? string.Empty : reader.GetString(reader.GetOrdinal("AddressLine1")),
                        AddressLine2 = reader.IsDBNull(reader.GetOrdinal("AddressLine2")) ? string.Empty : reader.GetString(reader.GetOrdinal("AddressLine2")),
                        AddressDescription = reader.IsDBNull(reader.GetOrdinal("AddressDescription")) ? string.Empty : reader.GetString(reader.GetOrdinal("AddressDescription")),
                        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? string.Empty : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        // Default value for nullable DateTime
                        Birthday = reader.IsDBNull(reader.GetOrdinal("Birthday")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("Birthday")),
                        VerificationCode = reader.IsDBNull(reader.GetOrdinal("VerificationCode")) ? string.Empty : reader.GetString(reader.GetOrdinal("VerificationCode")),
                        IsEmailVerified = reader.GetBoolean(reader.GetOrdinal("IsEmailVerified"))
                    };

                    _logger.LogInformation($"Adding user: {user.UserId}, Username: {user.Username}");
                    users.Add(user);
                }

                if (rowCount == 0)
                {
                    _logger?.LogInformation("No user accounts found");
                }
                else
                {
                    _logger.LogInformation($"Successfully retrieved {rowCount} user accounts");
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                // Log detailed error information
                _logger.LogError($"An error occurred while retrieving user accounts: {ex.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");

                // Return internal server error
                return StatusCode(500, "An error occurred while processing the request");
            }
        }





        [HttpGet("get-user-by-id")]
        public async Task<IActionResult> GetUserById()
        {
            try
            {
                // Retrieve the user ID from the cookie named "UserId"
                if (!Request.Cookies.TryGetValue("UserId", out string userIdCookie))
                {
                    return BadRequest("User ID cookie not found");
                }

                // Convert the retrieved user ID from string to int
                if (!int.TryParse(userIdCookie, out int userId))
                {
                    return BadRequest("Invalid user ID format in cookie");
                }

                // Check if the user ID input is safe from SQL injection attacks
                if (!SqlAttackHelper.IsSafeSQL(userIdCookie))
                {
                    // SQL injection attempt detected
                    _logger?.LogError("SQL injection attempt detected: User ID cookie contains potentially dangerous SQL patterns.");
                    return BadRequest("Invalid user ID");
                }

                using var conn = new MySqlConnection(_connectionString);

                await conn.OpenAsync();
                string query = @"SELECT u.*, ua.Username, ua.Email, ua.PasswordHash, ua.VerificationCode, ua.IsEmailVerified
                         FROM Users u
                         INNER JOIN UserAuthentication ua ON u.UserId = ua.UserId
                         WHERE u.UserId = @UserId";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    UserModel user = new UserModel
                    {
                        UserId = reader.GetInt32("UserId"),
                        Name = reader.GetString("Name"),
                        Surname = reader.GetString("Surname"),
                        AddressLine1 = reader.IsDBNull(reader.GetOrdinal("AddressLine1")) ? null : reader.GetString(reader.GetOrdinal("AddressLine1")),
                        AddressLine2 = reader.IsDBNull(reader.GetOrdinal("AddressLine2")) ? null : reader.GetString(reader.GetOrdinal("AddressLine2")),
                        AddressDescription = reader.IsDBNull(reader.GetOrdinal("AddressDescription")) ? null : reader.GetString(reader.GetOrdinal("AddressDescription")),
                        PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                        Birthday = reader.IsDBNull(reader.GetOrdinal("Birthday")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("Birthday")),
                        Username = reader.GetString("Username"),
                        Email = reader.GetString("Email"),
                        PasswordHash = reader.GetString("PasswordHash"),
                        VerificationCode = reader.GetString(reader.GetOrdinal("VerificationCode")),
                        IsEmailVerified = reader.GetBoolean("IsEmailVerified")
                    };
                    return Ok(user);
                }
                else
                {
                    return NotFound("User not found");
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger?.LogError($"An error occurred while retrieving user by ID: {ex.Message}");

                // Return internal server error
                return StatusCode(500, "An error occurred while processing the request");
            }
        }



        [HttpPost("sign-up")]
        public IActionResult SignUp([FromBody] SignInUserModel signInUserModel)
        {
            try
            {

                // Check if the user input is safe from SQL injection attacks
                if (!SqlAttackHelper.IsSafeSQL(signInUserModel.Email) || !SqlAttackHelper.IsSafeSQL(signInUserModel.Password))
                {
                    // SQL injection attempt detected
                    _logger?.LogError("SQL injection attempt detected: User input contains potentially dangerous SQL patterns.");
                    return BadRequest("Invalid user data");
                }


                if (signInUserModel == null || !ModelState.IsValid)
                {
                    return BadRequest("Invalid user data");
                }

                if (UserExists(signInUserModel.Email))
                {
                    return BadRequest("Email is already in use");
                }

                string hashedPassword = PasswordHelper.GenerateSaltedHash(signInUserModel.Password);
                string verificationCode = VertificationHelper.GenerateVerificationCode();

                int userId = InsertUser(signInUserModel, hashedPassword, verificationCode);
                if (userId > 0)
                {
                    return Ok("User created successfully");
                }
                else
                {
                    return StatusCode(500, "User creation failed");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"An error occurred while processing the request: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        private bool UserExists(string email)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM UserAuthentication WHERE Email = @Email";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    long count = (long)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        private int InsertUser(SignInUserModel signInUserModel, string hashedPassword, string verificationCode)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string insertQuery = @"INSERT INTO UserAuthentication (Username, Email, PasswordHash, VerificationCode) 
                VALUES (@Username, @Email, @PasswordHash, @VerificationCode);
                SELECT LAST_INSERT_ID();";
                    using (MySqlCommand insertCommand = new(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@Username", signInUserModel.Username);
                        insertCommand.Parameters.AddWithValue("@Email", signInUserModel.Email);
                        insertCommand.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                        insertCommand.Parameters.AddWithValue("@VerificationCode", verificationCode);
                        object result = insertCommand.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"An error occurred while inserting user: {ex.Message}");
                throw;
            }
        }

        [HttpPost("log-in")]
        public IActionResult SignIn([FromBody] SignInUserModel signInModel)
        {
            try
            {

                // Check if the user input is safe from SQL injection attacks
                if (!SqlAttackHelper.IsSafeSQL(signInModel.Email) || !SqlAttackHelper.IsSafeSQL(signInModel.Password))
                {
                    // SQL injection attempt detected
                    _logger?.LogError("SQL injection attempt detected: User input contains potentially dangerous SQL patterns.");
                    return Unauthorized("Invalid email or password");
                }

                var user = GetUserByEmail(signInModel.Email);
                if (user == null)
                {
                    return Unauthorized("Invalid email or password");
                }

                if (!PasswordHelper.VerifyPassword(signInModel.Password, user.PasswordHash))
                {
                    return Unauthorized("Invalid email or password");
                }

                // Set all user properties as cookies in the response, replacing null values with undefined
                SetUserCookies(user);

                return Ok(user);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it accordingly
                _logger?.LogError($"An error occurred while processing the request: {ex.Message}");
                // Return internal server error
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        private UserModel? GetUserByEmail(string email)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM UserAuthentication WHERE Email = @Email";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapReaderToUserModel((MySqlDataReader)reader);
                        }
                    }
                }
            }
            return null;
        }

        private void SetUserCookies(UserModel user)
        {
            Response.Cookies.Append("UserId", user.UserId.ToString());
            Response.Cookies.Append("Username", user.Username ?? "undefined");
            Response.Cookies.Append("Email", user.Email ?? "undefined");
            Response.Cookies.Append("Password", user.PasswordHash ?? "undefined");
            Response.Cookies.Append("Name", user.Name ?? "undefined");
            Response.Cookies.Append("Surname", user.Surname ?? "undefined");
            Response.Cookies.Append("AddressLine1", user.AddressLine1 ?? "undefined");
            Response.Cookies.Append("AddressLine2", user.AddressLine2 ?? "undefined");
            Response.Cookies.Append("AddressDescription", user.AddressDescription ?? "undefined");
            Response.Cookies.Append("PhoneNumber", user.PhoneNumber ?? "undefined");
            Response.Cookies.Append("Birthday", user.Birthday?.ToString("yyyy-MM-dd") ?? "undefined");
            Response.Cookies.Append("VerificationCode", user.VerificationCode ?? "undefined");
            Response.Cookies.Append("IsEmailVerified", user.IsEmailVerified.ToString());
        }



        private UserModel MapReaderToUserModel(MySqlDataReader reader)
        {
            var userId = reader.GetInt32(reader.GetOrdinal("UserId"));
            var username = reader.GetString(reader.GetOrdinal("Username"));
            var email = reader.GetString(reader.GetOrdinal("Email"));
            var name = reader.GetString(reader.GetOrdinal("Name"));
            var surname = reader.GetString(reader.GetOrdinal("Surname"));
            var addressLine1 = reader.IsDBNull(reader.GetOrdinal("AddressLine1")) ? null : reader.GetString(reader.GetOrdinal("AddressLine1"));
            var addressLine2 = reader.IsDBNull(reader.GetOrdinal("AddressLine2")) ? null : reader.GetString(reader.GetOrdinal("AddressLine2"));
            var addressDescription = reader.IsDBNull(reader.GetOrdinal("AddressDescription")) ? null : reader.GetString(reader.GetOrdinal("AddressDescription"));
            var phoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber"));
            var birthday = reader.GetDateTime(reader.GetOrdinal("Birthday"));

            return new UserModel
            {
                UserId = userId,
                Username = username,
                Email = email,
                Name = name,
                Surname = surname,
                AddressLine1 = addressLine1,
                AddressLine2 = addressLine2,
                AddressDescription = addressDescription,
                PhoneNumber = phoneNumber,
                Birthday = birthday,
                PasswordHash = string.Empty,
                VerificationCode = Guid.NewGuid().ToString(),
                IsEmailVerified = false
            };
        }

        [HttpPatch("modify-user")]
        public IActionResult ModifyUser([FromBody] UserUpdateModel userModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid user data");
            }

            try
            {

                // Check if the user input is safe from SQL injection attacks
                if (!SqlAttackHelper.IsSafeSQL(userModel.Username) ||
                    !SqlAttackHelper.IsSafeSQL(userModel.Email) ||
                    !SqlAttackHelper.IsSafeSQL(userModel.PasswordHash) ||
                    !SqlAttackHelper.IsSafeSQL(userModel.Name) ||
                    !SqlAttackHelper.IsSafeSQL(userModel.Surname) ||
                    !SqlAttackHelper.IsSafeSQL(userModel.AddressLine1) ||
                    !SqlAttackHelper.IsSafeSQL(userModel.AddressLine2) ||
                    !SqlAttackHelper.IsSafeSQL(userModel.AddressDescription) ||
                    !SqlAttackHelper.IsSafeSQL(userModel.PhoneNumber) ||
                    !SqlAttackHelper.IsSafeSQL(userModel.VerificationCode))
                {
                    // SQL injection attempt detected
                    _logger?.LogError("SQL injection attempt detected: User input contains potentially dangerous SQL patterns.");
                    return BadRequest("SQL injection attempt detected.");
                }

                // Retrieve user credentials from cookies
                if (!Request.Cookies.TryGetValue("UserId", out string userIdCookie) ||
                    !Request.Cookies.TryGetValue("Email", out string emailCookie) ||
                    !Request.Cookies.TryGetValue("Password", out string passwordCookie))
                {
                    return BadRequest("User credentials not found in the cookie");
                }

                int userId;
                if (!int.TryParse(userIdCookie, out userId))
                {
                    return BadRequest("Invalid User Id");
                }

                // Authenticate user
                var authenticationResult = AuthenticateUser(userId, emailCookie, passwordCookie);
                if (!authenticationResult.Success)
                {
                    return BadRequest(authenticationResult.Message);
                }

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    var rowsAffected = UpdateUserInformation(connection, userId, userModel);
                    if (rowsAffected > 0)
                    {
                        return Ok("User information updated successfully");
                    }
                    else
                    {
                        return StatusCode(500, "User information update failed");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"An error occurred while processing the request: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        private AuthenticationResult AuthenticateUser(int userId, string email, string password)
        {
            if (!Request.Cookies.TryGetValue("UserId", out string cookieUserId) ||
                !Request.Cookies.TryGetValue("Email", out string cookieEmail) ||
                !Request.Cookies.TryGetValue("Password", out string cookiePassword) ||
                userId != int.Parse(cookieUserId) || email != cookieEmail || password != cookiePassword)
            {
                return new AuthenticationResult(false, "Unauthorized access");
            }

            using (var connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                var userCheckQuery = "SELECT COUNT(*) FROM Users WHERE UserId = @UserId AND Password = @Password";
                using (var userCheckCommand = new MySqlCommand(userCheckQuery, connection))
                {
                    userCheckCommand.Parameters.AddWithValue("@UserId", userId);
                    userCheckCommand.Parameters.AddWithValue("@Password", password);
                    long userCount = (long)userCheckCommand.ExecuteScalar();
                    if (userCount == 0)
                    {
                        return new AuthenticationResult(false, "Invalid user ID or password");
                    }
                }
            }

            return new AuthenticationResult(true, "User authenticated successfully");
        }

        private int UpdateUserInformation(MySqlConnection connection, int userId, UserUpdateModel userModel)
        {
            var updateQuery = @"UPDATE Users 
                        SET Username = @Username, Email = @Email, Password = @Password,
                            Name = @Name, Surname = @Surname, 
                            AddressLine1 = @AddressLine1, AddressLine2 = @AddressLine2, 
                            AddressDescription = @AddressDescription, PhoneNumber = @PhoneNumber, 
                            Birthday = @Birthday, VerificationCode = @VerificationCode, IsEmailVerified = @IsEmailVerified
                        WHERE UserId = @UserId";

            using (var updateCommand = new MySqlCommand(updateQuery, connection))
            {
                updateCommand.Parameters.AddWithValue("@UserId", userId);
                updateCommand.Parameters.AddWithValue("@Username", userModel.Username);
                updateCommand.Parameters.AddWithValue("@Email", userModel.Email);
                updateCommand.Parameters.AddWithValue("@Password", userModel.PasswordHash);
                updateCommand.Parameters.AddWithValue("@Name", userModel.Name);
                updateCommand.Parameters.AddWithValue("@Surname", userModel.Surname);
                updateCommand.Parameters.AddWithValue("@AddressLine1", userModel.AddressLine1);
                updateCommand.Parameters.AddWithValue("@AddressLine2", userModel.AddressLine2);
                updateCommand.Parameters.AddWithValue("@AddressDescription", userModel.AddressDescription);
                updateCommand.Parameters.AddWithValue("@PhoneNumber", userModel.PhoneNumber);
                updateCommand.Parameters.AddWithValue("@Birthday", userModel.Birthday);
                updateCommand.Parameters.AddWithValue("@VerificationCode", userModel.VerificationCode);
                updateCommand.Parameters.AddWithValue("@IsEmailVerified", userModel.IsEmailVerified);

                return updateCommand.ExecuteNonQuery();
            }
        }

        public class AuthenticationResult
        {
            public bool Success { get; }
            public string Message { get; }

            public AuthenticationResult(bool success, string message)
            {
                Success = success;
                Message = message;
            }
        }



        [HttpPatch("change-password")]
        public IActionResult ChangePassword([FromBody] ChangePasswordModel changePasswordModel)
        {
            // Check if the ChangePasswordModel is null or invalid
            if (changePasswordModel == null || !ModelState.IsValid)
            {
                // Return bad request with error message
                return BadRequest("Invalid change password data");
            }

            try
            {

                // Check if the email and passwords are safe from SQL injection attacks
                if (!SqlAttackHelper.IsSafeSQL(changePasswordModel.CurrentPassword) || !SqlAttackHelper.IsSafeSQL(changePasswordModel.NewPassword))
                {
                    // SQL injection attempt detected
                    _logger?.LogError("SQL injection attempt detected: Password contains potentially dangerous SQL patterns.");
                    return BadRequest("SQL injection attempt detected.");
                }

                // Retrieve user credentials from cookies
                if (!Request.Cookies.TryGetValue("Email", out string email) ||
                    !Request.Cookies.TryGetValue("UserId", out string userId))
                {
                    return BadRequest("User credentials not found in the cookie");
                }

                int parsedUserId;
                if (!int.TryParse(userId, out parsedUserId))
                {
                    return BadRequest("Invalid User Id");
                }

                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    // Check if email exists and matches the user ID
                    string emailCheckQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email AND UserId = @UserId";
                    using (MySqlCommand emailCheckCommand = new(emailCheckQuery, connection))
                    {
                        emailCheckCommand.Parameters.AddWithValue("@Email", email);
                        emailCheckCommand.Parameters.AddWithValue("@UserId", parsedUserId);
                        long emailCount = (long)emailCheckCommand.ExecuteScalar();
                        if (emailCount == 0)
                        {
                            // Email does not match the user ID, return BadRequest
                            return BadRequest("Invalid email or user ID");
                        }
                    }

                    // Email matches the user ID, proceed with changing the password
                    // Check if email exists and password matches
                    string passwordCheckQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email AND Password = @CurrentPassword";
                    using (MySqlCommand passwordCheckCommand = new(passwordCheckQuery, connection))
                    {
                        passwordCheckCommand.Parameters.AddWithValue("@Email", email);
                        passwordCheckCommand.Parameters.AddWithValue("@CurrentPassword", PasswordHelper.GenerateSaltedHash(changePasswordModel.CurrentPassword));
                        long passwordCount = (long)passwordCheckCommand.ExecuteScalar();
                        if (passwordCount == 0)
                        {
                            // Either email does not exist or password does not match, return BadRequest
                            return BadRequest("Invalid email or password");
                        }
                    }

                    // Email and password match, proceed with changing the password
                    // Generate a salted hash of the new password
                    string hashedNewPassword = PasswordHelper.GenerateSaltedHash(changePasswordModel.NewPassword);

                    // Update the user's password in the database
                    string updatePasswordQuery = "UPDATE Users SET Password = @NewPassword WHERE Email = @Email";
                    using (MySqlCommand updatePasswordCommand = new(updatePasswordQuery, connection))
                    {
                        updatePasswordCommand.Parameters.AddWithValue("@Email", email);
                        updatePasswordCommand.Parameters.AddWithValue("@NewPassword", hashedNewPassword);

                        int rowsAffected = updatePasswordCommand.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            // Password changed successfully
                            return Ok("Password changed successfully");
                        }
                        else
                        {
                            // Password change failed
                            return StatusCode(500, "Password change failed");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it accordingly
                _logger?.LogError($"An error occurred while processing the request: {ex.Message}");
                // Return internal server error
                return StatusCode(500, "An error occurred while processing the request");
            }
        }




        [HttpDelete("delete-account")]
        public IActionResult DeleteAccount([FromBody] DeleteAccountModel deleteAccountModel)
        {
            try
            {
                // Check if the DeleteAccountModel is null or invalid
                if (deleteAccountModel == null || !ModelState.IsValid)
                {
                    // Return bad request with error message
                    return BadRequest("Invalid delete account data");
                }

                // Check if the email and password are safe from SQL injection attacks
                if (!SqlAttackHelper.IsSafeSQL(deleteAccountModel.Email) || !SqlAttackHelper.IsSafeSQL(deleteAccountModel.Password))
                {
                    // SQL injection attempt detected
                    _logger?.LogError("SQL injection attempt detected: Email or password contains potentially dangerous SQL patterns.");
                    return BadRequest("SQL injection attempt detected.");
                }

                // Retrieve user email and user id from cookies
                if (!Request.Cookies.TryGetValue("Email", out string cookieEmail) || !Request.Cookies.TryGetValue("UserId", out string cookieUserId))
                {
                    return BadRequest("User email or user id not found in the cookie");
                }

                // Check if the email from cookie and the email from the model match
                if (deleteAccountModel.Email != cookieEmail)
                {
                    return BadRequest("Email does not match the cookie");
                }

                // Convert cookieUserId to integer
                if (!int.TryParse(cookieUserId, out int userId))
                {
                    return BadRequest("Invalid user id in the cookie");
                }

                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    // Check if the email and password match
                    string query = "SELECT COUNT(*) FROM Users WHERE UserId = @UserId AND Email = @Email AND Password = @Password";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        command.Parameters.AddWithValue("@Email", deleteAccountModel.Email);
                        command.Parameters.AddWithValue("@Password", deleteAccountModel.Password);
                        long count = (long)command.ExecuteScalar();
                        if (count == 0)
                        {
                            // Email, password, or user id do not match, return BadRequest
                            return BadRequest("Email, password, or user id is incorrect");
                        }
                    }

                    // Email, password, and user id match, proceed with deletion
                    string deleteQuery = "DELETE FROM Users WHERE UserId = @UserId";
                    using (MySqlCommand deleteCommand = new MySqlCommand(deleteQuery, connection))
                    {
                        deleteCommand.Parameters.AddWithValue("@UserId", userId);
                        int rowsAffected = deleteCommand.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            // User account deleted successfully
                            return Ok("User account deleted successfully");
                        }
                        else
                        {
                            // User account deletion failed
                            return StatusCode(500, "User account deletion failed");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger?.LogError($"An error occurred while deleting user account: {ex.Message}");

                // Return internal server error
                return StatusCode(500, "An error occurred while processing the request");
            }
        }





        [HttpPost("verify-email")]
        public IActionResult VerifyEmail(string email, string code)
        {
            try
            {
                // Check if the email and verification code are safe from SQL injection attacks
                if (!!SqlAttackHelper.IsSafeSQL(email) || !!SqlAttackHelper.IsSafeSQL(code))
                {
                    // SQL injection attempt detected
                    _logger?.LogError("SQL injection attempt detected: Email or verification code contains potentially dangerous SQL patterns.");
                    return BadRequest("SQL injection attempt detected.");
                }

                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    // Check if the verification code matches the one stored in the database
                    string verifyQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email AND VerificationCode = @VerificationCode";
                    using (MySqlCommand verifyCommand = new MySqlCommand(verifyQuery, connection))
                    {
                        verifyCommand.Parameters.AddWithValue("@Email", email);
                        verifyCommand.Parameters.AddWithValue("@VerificationCode", code);
                        long count = (long)verifyCommand.ExecuteScalar();
                        if (count == 0)
                        {
                            // Verification failed
                            return BadRequest("Invalid verification code");
                        }
                        else
                        {
                            // Update the user's email verification status
                            string updateQuery = "UPDATE Users SET IsEmailVerified = 1 WHERE Email = @Email";
                            using (MySqlCommand updateCommand = new MySqlCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.AddWithValue("@Email", email);
                                int rowsAffected = updateCommand.ExecuteNonQuery();
                                if (rowsAffected > 0)
                                {
                                    return Ok("Email verified successfully");
                                }
                                else
                                {
                                    return StatusCode(500, "Failed to verify email");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger?.LogError($"An error occurred while verifying email: {ex.Message}");
                // Return internal server error
                return StatusCode(500, "An error occurred while processing the request");
            }
        }



        private void SendPasswordResetEmail(string email, string resetToken)
        {
            // Configure the SMTP client
            SmtpClient client = new SmtpClient("smtp.example.com") // Replace "smtp.example.com" with your SMTP server address
            {
                Port = 587, // Replace 587 with your SMTP server port
                Credentials = new NetworkCredential("your-email@example.com", "your-password"), // Replace with your SMTP credentials
                EnableSsl = true // Set to true if your SMTP server requires SSL
            };

            // Compose the email message
            MailMessage message = new MailMessage
            {
                From = new MailAddress("your-email@example.com"), // Replace with your email address
                Subject = "Password Reset Request",
                Body = $"Hello,\n\nYou requested to reset your password. Please click the following link to reset your password:\n\nhttps://yourwebsite.com/reset-password?token={resetToken}\n\nIf you didn't request this, please ignore this email.\n\nRegards,\nYour Website Team"
            };
            message.To.Add(email);

            // Send the email
            try
            {
                client.Send(message);
                _logger?.LogInformation($"Password reset email sent to: {email}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error sending password reset email to {email}: {ex.Message}");
                // Handle the error, e.g., log it or return a failure response
            }
            finally
            {
                // Dispose of resources
                message.Dispose();
                client.Dispose();
            }
        }

        private void StoreResetTokenInDatabase(string email, string resetToken)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                string query = "UPDATE Users SET ResetToken = @ResetToken WHERE Email = @Email";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ResetToken", resetToken);
                    command.Parameters.AddWithValue("@Email", email);
                    command.ExecuteNonQuery();
                }
            }
        }

        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword(string email)
        {
            try
            {
                // Check if the provided email exists in the database
                if (!UserExists(email))
                {
                    return BadRequest("Email does not exist");
                }

                // Check if the email is safe from SQL injection attacks
                if (!SqlAttackHelper.IsSafeSQL(email))
                {
                    // SQL injection attempt detected
                    _logger?.LogError("SQL injection attempt detected: Email contains potentially dangerous SQL patterns.");
                    return BadRequest("SQL injection attempt detected.");
                }

                // Generate a password reset token
                string resetToken = VertificationHelper.GenerateRandomToken();

                // Store the reset token in the database
                StoreResetTokenInDatabase(email, resetToken);

                // Send the password reset email
                // SendPasswordResetEmail(email, resetToken);

                return Ok("Password reset email sent successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"An error occurred while processing the request: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }


        [HttpPost("resend-verification-email")]
        public IActionResult ResendVerificationEmail(string email)
        {
            try
            {
                // Check if the provided email exists in the database
                if (!UserExists(email))
                {
                    return BadRequest("Email does not exist");
                }

                if (!SqlAttackHelper.IsSafeSQL(email))
                {
                    // SQL injection attempt detected
                    _logger?.LogError("SQL injection attempt detected: Email contains potentially dangerous SQL patterns.");
                    return BadRequest("SQL injection attempt detected.");
                }

                // Retrieve the verification code from the database
                string? verificationCode = GetVerificationCodeFromDatabase(email);

                // Resend the verification email
                // SendVerificationEmail(email, verificationCode);

                return Ok("Verification email resent successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"An error occurred while processing the request: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        private void SendVerificationEmail(string email, string verificationCode)
        {
            string verificationLink = $"https://yourwebsite.com/verifyemail?code={verificationCode}";
            string body = $"Click the following link to verify your email address: {verificationLink}";

            // Implement email sending logic using your preferred email service
        }

        private string? GetVerificationCodeFromDatabase(string email)
        {
            using (MySqlConnection connection = new MySqlConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT VerificationCode FROM Users WHERE Email = @Email";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    object result = command.ExecuteScalar();
                    return result?.ToString();
                }
            }
        }





    }
}
