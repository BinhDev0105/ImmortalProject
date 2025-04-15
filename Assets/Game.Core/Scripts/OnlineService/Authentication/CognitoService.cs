using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.Runtime;
using Newtonsoft.Json;
using Unity.Logging;

namespace Game.Core.Scripts.OnlineService.Authentication
{
    /// <summary>
    /// Service class for handling AWS Cognito authentication operations.
    /// Provides methods for user management, authentication, and session handling.
    /// </summary>
    public class CognitoService
    {
        #region Constants

        /// <summary>
        /// AWS Cognito Client ID for authentication.
        /// </summary>
        private const string ClientId = "2fjasdef4r207vnmug7k962kgg";

        /// <summary>
        /// AWS Cognito Client Secret for authentication.
        /// </summary>
        private const string ClientSecret = "mb538lrqlklbqlsi0mjlc70l4o7cruqh51a0k77gm5ol0vscgt1";

        /// <summary>
        /// AWS Cognito Issuer URL for token validation.
        /// </summary>
        private const string Issuer = "https://cognito-idp.us-east-1.amazonaws.com";

        /// <summary>
        /// AWS Cognito User Pool ID for user management.
        /// </summary>
        private const string UserPoolId = "us-east-1_LagiyE5cU";

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calculates the secret hash required for AWS Cognito authentication.
        /// </summary>
        /// <param name="username">The username to generate hash for.</param>
        /// <returns>Base64 encoded secret hash.</returns>
        /// <exception cref="ArgumentException">Thrown when username is null or empty.</exception>
        /// <exception cref="Exception">Thrown when hash calculation fails.</exception>
        private static string CalculateSecretHash(string username)
        {
            try
            {
                if (string.IsNullOrEmpty(username))
                {
                    Log.Error("Username is null or empty for secret hash calculation");
                    throw new ArgumentException("Username cannot be null or empty for secret hash calculation");
                }
                
                username = username.Trim();
                Log.Debug($"Calculating secret hash for username: '{username}'");
                Log.Debug($"Using ClientId: '{ClientId}'");
                
                var message = username + ClientId;
                Log.Debug($"Message to hash: '{message}'");
                
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ClientSecret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                
                var result = Convert.ToBase64String(hash);
                Log.Debug($"Generated secret hash: '{result}'");
                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"Error calculating secret hash: {ex.Message}\nStack trace: {ex.StackTrace}");
                throw;
            }
        }

        #endregion
        
        /// <summary>
        /// Model for Cognito error responses.
        /// Used for deserializing error messages from AWS Cognito.
        /// </summary>
        [Serializable]
        public class CognitoErrorResponse
        {
            /// <summary>
            /// The type of error that occurred.
            /// </summary>
            [JsonProperty("__type")]
            public string Type { get; set; }

            /// <summary>
            /// The error message describing what went wrong.
            /// </summary>
            [JsonProperty("message")]
            public string Message { get; set; }
        }
        
        #region Fields and Properties

        private readonly AmazonCognitoIdentityProviderClient _provider;
        private readonly CognitoUserPool _userPool;
        private CognitoUser _user;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AwsCognitoService class.
        /// Sets up AWS Cognito client and user pool.
        /// </summary>
        public CognitoService()
        {
            var credentials = GetCredentials();
            
            _provider = new AmazonCognitoIdentityProviderClient(
                credentials, 
                Amazon.RegionEndpoint.USEast1
            );
            
            _userPool = new CognitoUserPool(
                UserPoolId,
                ClientId,
                _provider,
                ClientSecret
            );
        }

        #endregion

        #region Authentication Methods

        /// <summary>
        /// Signs in a user with the specified credentials.
        /// </summary>
        /// <param name="username">The username to sign in with.</param>
        /// <param name="password">The password to sign in with.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown when authentication fails.</exception>
        public async Task<AuthFlowResponse> SignInAsync(string username, string password)
        {
            try
            {
                Log.Debug($"Starting sign in for user: {username}");
                
                var user = _userPool.GetUser(username);
                if (user == null)
                {
                    Log.Warning("Failed to get user from pool");
                    throw new Exception("Failed to get user from pool");
                }

                var authRequest = new InitiateSrpAuthRequest 
                { 
                    Password = password
                };

                Log.Debug("Starting SRP authentication...");
                var ret = await user.StartWithSrpAuthAsync(authRequest);
                
                if (ret.AuthenticationResult != null)
                {
                    _user = user;
                    Log.Debug($"Sign in successful - Access Token: {ret.AuthenticationResult.AccessToken}");
                    Log.Debug($"Sign in successful - ID Token: {ret.AuthenticationResult.IdToken}");
                    Log.Debug($"Sign in successful - Refresh Token: {ret.AuthenticationResult.RefreshToken}");
                    return ret;
                }
                else
                {
                    Log.Warning("Sign in failed - no authentication result");
                    throw new Exception("Sign in failed - no authentication result");
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error in SignInAsync: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Signs out the current user.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown when sign out fails.</exception>
        public async Task SignOutAsync()
        {
            if (_user != null)
            {
                try
                {
                    await _user.GlobalSignOutAsync();
                    Log.Debug("Successfully signed out");
                }
                catch (Exception e)
                {
                    Log.Error($"Error signing out: {e.Message}");
                    throw;
                }
            }
        }

        #endregion

        #region User Management Methods

        /// <summary>
        /// Registers a new user with AWS Cognito.
        /// </summary>
        /// <param name="username">The username for the new account.</param>
        /// <param name="password">The password for the new account.</param>
        /// <param name="email">The email address for the new account.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown when registration fails.</exception>
        public async Task<SignUpResponse> SignUpAsync(string username, string password, string email)
        {
            try
            {
                var signUpRequest = new SignUpRequest
                {
                    Username = username,
                    Password = password,
                    UserAttributes = new List<AttributeType>
                    {
                        new AttributeType
                        {
                            Name = "email",
                            Value = email
                        }
                    },
                    ClientId = _userPool.ClientID,
                    SecretHash = CalculateSecretHash(username)
                };

                var response = await _provider.SignUpAsync(signUpRequest);
                Log.Debug($"SignUp Response: {response.HttpStatusCode}");
                return response;
            }
            catch (Exception e)
            {
                Log.Error($"Error signing up: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Confirms a user's registration with the provided confirmation code.
        /// </summary>
        /// <param name="username">The username to confirm.</param>
        /// <param name="confirmationCode">The confirmation code sent to the user.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown when confirmation fails.</exception>
        public async Task<ConfirmSignUpResponse> ConfirmSignUpAsync(string username, string confirmationCode)
        {
            try
            {
                var request = new ConfirmSignUpRequest
                {
                    Username = username,
                    ConfirmationCode = confirmationCode,
                    ClientId = _userPool.ClientID,
                    SecretHash = CalculateSecretHash(username)
                };

                var response = await _provider.ConfirmSignUpAsync(request);
                Log.Debug($"ConfirmSignUp Response: {response.HttpStatusCode}");
                return response;
            }
            catch (Exception e)
            {
                Log.Error($"Error confirming sign up: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initiates the forgot password process for a user.
        /// </summary>
        /// <param name="username">The username of the account.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown when the process fails.</exception>
        public async Task<ForgotPasswordResponse> ForgotPasswordAsync(string username)
        {
            try
            {
                var request = new ForgotPasswordRequest
                {
                    Username = username,
                    ClientId = _userPool.ClientID,
                    SecretHash = CalculateSecretHash(username)
                };

                var response = await _provider.ForgotPasswordAsync(request);
                Log.Debug($"ForgotPassword Response: {response.HttpStatusCode}");
                return response;
            }
            catch (Exception e)
            {
                Log.Error($"Error initiating forgot password: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Confirms a password reset with the provided confirmation code and new password.
        /// </summary>
        /// <param name="username">The username of the account.</param>
        /// <param name="confirmationCode">The confirmation code sent to the user.</param>
        /// <param name="newPassword">The new password to set.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown when the reset fails.</exception>
        public async Task<ConfirmForgotPasswordResponse> ConfirmForgotPasswordAsync(string username, string confirmationCode, string newPassword)
        {
            try
            {
                var request = new ConfirmForgotPasswordRequest
                {
                    Username = username,
                    ConfirmationCode = confirmationCode,
                    Password = newPassword,
                    ClientId = _userPool.ClientID,
                    SecretHash = CalculateSecretHash(username)
                };

                var response = await _provider.ConfirmForgotPasswordAsync(request);
                Log.Debug($"ConfirmForgotPassword Response: {response.HttpStatusCode}");
                return response;
            }
            catch (Exception e)
            {
                Log.Error($"Error confirming forgot password: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Resends the confirmation code to a user.
        /// </summary>
        /// <param name="username">The username of the account.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown when resending fails.</exception>
        public async Task<ResendConfirmationCodeResponse> ResendConfirmationCodeAsync(string username)
        {
            try
            {
                var request = new ResendConfirmationCodeRequest
                {
                    Username = username,
                    ClientId = _userPool.ClientID,
                    SecretHash = CalculateSecretHash(username)
                };

                var response = await _provider.ResendConfirmationCodeAsync(request);
                Log.Debug($"ResendConfirmationCode Response: {response.HttpStatusCode}");
                return response;
            }
            catch (Exception e)
            {
                Log.Error($"Error resending confirmation code: {e.Message}");
                throw;
            }
        }

        #endregion

        #region User Information Methods

        /// <summary>
        /// Gets the user ID for a specified username.
        /// </summary>
        /// <param name="username">The username to get the ID for.</param>
        /// <returns>A task containing the user ID.</returns>
        /// <exception cref="Exception">Thrown when retrieval fails.</exception>
        public async Task<string> GetUserIdAsync(string username)
        {
            try
            {
                var request = new AdminGetUserRequest
                {
                    Username = username,
                    UserPoolId = UserPoolId
                };

                Log.Debug($"AdminGetUser Request:");
                Log.Debug($"- Username: {username}");
                Log.Debug($"- UserPoolId: {UserPoolId}");

                var response = await _provider.AdminGetUserAsync(request);
                Log.Debug($"GetUser Response: {response.HttpStatusCode}");
                
                var subAttribute = response.UserAttributes.Find(attr => attr.Name == "sub");
                if (subAttribute != null)
                {
                    Log.Debug($"Found User ID: {subAttribute.Value}");
                    return subAttribute.Value;
                }
                else
                {
                    Log.Warning("User ID (sub) not found in user attributes");
                    return null;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error getting user ID: {e.Message}");
                throw;
            }
        }

        #endregion

        #region Status Methods

        /// <summary>
        /// Checks if a user is currently signed in.
        /// </summary>
        /// <returns>True if the user is signed in, false otherwise.</returns>
        public bool IsSignedIn()
        {
            return _user != null && _user.SessionTokens != null && _user.SessionTokens.IsValid();
        }

        /// <summary>
        /// Checks the connection to AWS Cognito.
        /// </summary>
        /// <param name="username">The username to test with.</param>
        /// <param name="password">The password to test with.</param>
        /// <returns>A task containing the connection status.</returns>
        public async Task<bool> CheckCognitoConnectionAsync(string username, string password)
        {
            try
            {
                Log.Debug("Checking Cognito connection...");
                
                var user = _userPool.GetUser(username);
                if (user == null)
                {
                    Log.Warning("Failed to get user from pool");
                    return false;
                }

                var authRequest = new InitiateSrpAuthRequest 
                { 
                    Password = password
                };

                var authResponse = await user.StartWithSrpAuthAsync(authRequest);
                if (authResponse.AuthenticationResult == null)
                {
                    Log.Warning("Failed to authenticate user");
                    return false;
                }

                Log.Debug("Successfully connected to Cognito");
                Log.Debug($"User authenticated: {username}");
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"Error checking Cognito connection: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the appropriate AWS credentials based on the environment.
        /// </summary>
        /// <returns>The AWS credentials to use.</returns>
        private AWSCredentials GetCredentials()
        {
#if UNITY_EDITOR
            return new AnonymousAWSCredentials();
#else
            return FallbackCredentialsFactory.GetCredentials();
#endif
        }

        #endregion
    }
}
