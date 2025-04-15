using System;
using System.Collections;
using System.Threading.Tasks;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Extensions.CognitoAuthentication;
using Game.Core.Runtime.Scripts.Utility;
using Unity.Logging;
using Unity.Logging.Sinks;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace Game.Core.Scripts.OnlineService.Authentication
{
    public class AuthenticationManager : PersistentSingleton<AuthenticationManager>
    {
        #region Services
        
        // Core authentication services
        private readonly CognitoService _cognitoService = new();
        private readonly UnityService _unityService = new();
        
        // UI Controller reference
        private AuthenticationUIController _uiController;
        
        #endregion

        #region Constants
        
        private const string OIDC_PROVIDER = "oidc-cognito"; // OpenID Connect provider name for Cognito
        
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Initialize services
            InitializeServices();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes all required authentication services and UI components
        /// </summary>
        private void InitializeServices()
        {
            // Initialize Unity authentication service
            _unityService.InitializeAsync();
            
            // Setup UI controller
            _uiController = FindAnyObjectByType<AuthenticationUIController>();
            if (_uiController == null)
            {
                Log.Error("AuthenticationUIController not found in scene");
            }
            
            // Configure logging
            ConfigureLogging();
        }

        /// <summary>
        /// Configures the logging system for authentication operations
        /// </summary>
        private void ConfigureLogging()
        {
            var logConfig = new LoggerConfig()
                .MinimumLevel.Debug()
                .OutputTemplate("{Level} - {Message}")
                .WriteTo.File("logs/auth-manager.txt")
                .WriteTo.UnityEditorConsole()
                .CreateLogger();
            Log.Logger = logConfig;
        }

        #endregion

        #region Public Authentication Methods

        /// <summary>
        /// Signs in a user with Cognito and links with Unity authentication via OpenID Connect
        /// </summary>
        public IEnumerator SignIn(string username, string password, bool rememberMe)
        {
            // Step 1: Cognito Authentication
            Task<AuthFlowResponse> cognitoTask;
            try
            {
                cognitoTask = _cognitoService.SignInAsync(username, password);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initiate Cognito sign-in: {ex.Message}");
                yield break;
            }

            yield return new WaitUntil(() => cognitoTask.IsCompleted);

            if (cognitoTask.IsFaulted)
            {
                Log.Error($"Cognito sign-in failed: {cognitoTask.Exception?.Message}");
                _uiController.HandleSignInSuccess();
                yield break;
            }

            var cognitoResult = cognitoTask.Result;
            if (cognitoResult == null)
            {
                Log.Error("Cognito result was null");
                yield break;
            }

            // Step 2: Save Credentials (if remember me is enabled)
            try
            {
                if (rememberMe)
                {
                    SaveUserCredentials(username, password);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to save credentials: {ex.Message}");
                // Continue execution as this is not critical
            }

            // Step 3: Unity Authentication
            Task<bool> unityTask;
            try
            {
                unityTask = _unityService.SignInWithOpenIdConnectAsync(
                    OIDC_PROVIDER, 
                    cognitoResult.AuthenticationResult.IdToken
                );
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initiate Unity authentication: {ex.Message}");
                yield break;
            }

            yield return new WaitUntil(() => unityTask.IsCompleted);

            if (unityTask.IsFaulted)
            {
                Log.Error($"Unity authentication failed: {unityTask.Exception?.Message}");
                yield break;
            }

            if (!unityTask.Result)
            {
                Log.Error("Unity authentication returned false");
                yield break;
            }
            _uiController.HandleSignInSuccess();
            Log.Info($"User {username} successfully authenticated");
        }

        /// <summary>
        /// Registers a new user account
        /// </summary>
        public IEnumerator SignUp(string username, string password, string email)
        {
            Task<SignUpResponse> task;
            try
            {
                task = _cognitoService.SignUpAsync(username, password, email);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initiate sign-up: {ex.Message}");
                yield break;
            }

            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Log.Error($"Sign-up failed: {task.Exception?.Message}");
                _uiController.HandleSignUpSuccess();
                yield break;
            }

            if (task.Result != null)
            {
                Log.Info($"User {username} successfully registered");
                _uiController.HandleSignUpSuccess();
                _uiController.ShowConfirmationUI(username);
            }
            else
            {
                Log.Error("Sign-up result was null");
            }
        }

        /// <summary>
        /// Initiates the password reset process
        /// </summary>
        public IEnumerator ForgotPassword(string username)// email
        {
            Task<ForgotPasswordResponse> task;
            try
            {
                task = _cognitoService.ForgotPasswordAsync(username);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initiate password reset: {ex.Message}");
                yield break;
            }

            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Log.Error($"Password reset failed: {task.Exception?.Message}");
                _uiController.HandleResetPasswordSuccess();
                yield break;
            }

            if (task.Result != null)
            {
                Log.Info($"Password reset initiated for user {username}");
                _uiController.HandleResetPasswordSuccess();
                _uiController.ShowResetPasswordUI(username);
            }
            else
            {
                Log.Error("Password reset result was null");
            }
        }

        /// <summary>
        /// Confirms a password reset using the verification code
        /// </summary>
        public IEnumerator ChangePassword(string username, string code, string newPassword)
        {
            Task<ConfirmForgotPasswordResponse> task;
            try
            {
                task = _cognitoService.ConfirmForgotPasswordAsync(username, code, newPassword);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initiate password change: {ex.Message}");
                yield break;
            }

            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Log.Error($"Change password failed: {task.Exception?.Message}");
                _uiController.HandlePasswordChangeSuccess();
                yield break;
            }

            if (task.Result != null)
            {
                Log.Info($"Password successfully changed for user {username}");
                _uiController.HandlePasswordChangeSuccess();
                _uiController.ShowSignInUI();
            }
            else
            {
                Log.Error("Change password result was null");
            }
        }

        /// <summary>
        /// Confirms a user account using the verification code
        /// </summary>
        public IEnumerator ConfirmAccount(string username, string code)
        {
            Task<ConfirmSignUpResponse> task;
            try
            {
                task = _cognitoService.ConfirmSignUpAsync(username, code);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initiate account confirmation: {ex.Message}");
                yield break;
            }

            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Log.Error($"Account confirmation failed: {task.Exception?.Message}");
                _uiController.HandleAccountConfirmationSuccess();
                yield break;
            }

            if (task.Result != null)
            {
                Log.Info($"Account successfully confirmed for user {username}");
                _uiController.HandleAccountConfirmationSuccess();
                _uiController.ShowSignInUI();
            }
            else
            {
                Log.Error("Account confirmation result was null");
            }
        }

        /// <summary>
        /// Resends the account confirmation code
        /// </summary>
        public IEnumerator ResendConfirmationCode(string username)
        {
            Task<ResendConfirmationCodeResponse> task;
            try
            {
                task = _cognitoService.ResendConfirmationCodeAsync(username);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initiate code resend: {ex.Message}");
                yield break;
            }

            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Log.Error($"Resend code failed: {task.Exception?.Message}");
                yield break;
            }

            if (task.Result != null)
            {
                Log.Info($"Confirmation code resent to user {username}");
            }
            else
            {
                Log.Error("Resend code result was null");
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Saves user credentials for remember me functionality
        /// </summary>
        private void SaveUserCredentials(string username, string password)
        {
            RememberAccount.SaveAccountData(new RememberAccount.RememberData
            {
                active = true,
                username = username,
                password = password
            });
        }

        #endregion
    }
}