using System;
using System.Text.RegularExpressions;
using Game.Core.UI_Toolkit.Custom;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Logging;

namespace Game.Core.Scripts.OnlineService.Authentication
{
    /// <summary>
    /// Controls the UI elements and interactions for the authentication system
    /// Manages different authentication screens and their transitions
    /// </summary>
    public class AuthenticationUIController : MonoBehaviour
    {
        #region UI States
        
        private enum UIState
        {
            SignIn,
            SignUp,
            ForgotPassword,
            ResetPassword,
            ConfirmAccount
        }
        
        #endregion

        #region Inspector Fields
        
        [SerializeField] 
        private UIDocument uiDocument;
        
        [SerializeField]
        private float loadingSpeed = 1f;
        
        #endregion

        #region Private Fields
        
        private VisualElement _root;
        private VisualElement _loader;
        private CircularLoader _loading;
        private UIState _currentState;
        private const string Pattern = @"\[(.*?)\]";
        
        #endregion

        #region Event Handlers Storage
        
        // Store event handlers so we can unregister them
        // Common Handlers
        private EventCallback<ClickEvent> _exitHandler;
        
        // Sign In Handlers
        private EventCallback<ClickEvent> _signInHandler;
        private EventCallback<ClickEvent> _forgotPasswordHandler;
        private EventCallback<ClickEvent> _signInToSignUpHandler;
        private EventCallback<ChangeEvent<bool>> _signInShowPasswordHandler;
        private EventCallback<ChangeEvent<bool>> _rememberMeHandler;
        private EventCallback<InputEvent> _signInUsernameInputHandler;
        private EventCallback<InputEvent> _signInPasswordInputHandler;
        
        // Sign Up Handlers
        private EventCallback<ClickEvent> _signUpHandler;
        private EventCallback<ClickEvent> _signUpToSignInHandler;
        private EventCallback<ChangeEvent<bool>> _signUpShowPasswordHandler;
        private EventCallback<ChangeEvent<bool>> _signUpShowConfirmPasswordHandler;
        private EventCallback<InputEvent> _signUpEmailInputHandler;
        private EventCallback<InputEvent> _signUpUsernameInputHandler;
        private EventCallback<InputEvent> _signUpPasswordInputHandler;
        private EventCallback<InputEvent> _signUpConfirmPasswordInputHandler;
        
        // Forgot Password Handlers
        private EventCallback<ClickEvent> _resetPasswordUiHandler;
        private EventCallback<ClickEvent> _forgotPasswordBackHandler;
        private EventCallback<InputEvent> _forgotPasswordEmailInputHandler;
        
        // Reset Password Handlers
        private EventCallback<ClickEvent> _changePasswordHandler;
        private EventCallback<ClickEvent> _resetPasswordBackHandler;
        private EventCallback<ChangeEvent<bool>> _resetShowPasswordHandler;
        private EventCallback<ChangeEvent<bool>> _resetShowConfirmPasswordHandler;
        private EventCallback<InputEvent> _resetPasswordCodeInputHandler;
        private EventCallback<InputEvent> _resetPasswordPasswordInputHandler;
        private EventCallback<InputEvent> _resetPasswordConfirmPasswordInputHandler;

        // Confirm Account Handlers
        private EventCallback<ClickEvent> _confirmAccountHandler;
        private EventCallback<ClickEvent> _resendCodeHandler;
        private EventCallback<ClickEvent> _confirmAccountBackHandler;
        private EventCallback<InputEvent> _confirmAccountCodeInputHandler;
        
        #endregion

        #region Field Clearing Methods

        /// <summary>
        /// Clears the value of a specified TextField.
        /// </summary>
        /// <param name="field">The TextField to clear.</param>
        private void ClearTextField(TextField field)
        {
            if (field != null)
            {
                field.value = string.Empty;
            }
        }

        /// <summary>
        /// Clears the username and password fields in the sign-in form if 'Remember Me' is not checked.
        /// </summary>
        /// <param name="rememberMe">Indicates whether the 'Remember Me' toggle is checked.</param>
        private void ClearSignInFields(bool rememberMe)
        {
            if (rememberMe) return;
            
            var signIn = GetPanelForState(UIState.SignIn);
            ClearTextField(signIn.Q<TextField>("username"));
            ClearTextField(signIn.Q<TextField>("password"));
        }

        /// <summary>
        /// Clears all input fields in the sign-up form.
        /// </summary>
        private void ClearSignUpFields()
        {
            var signUp = GetPanelForState(UIState.SignUp);
            ClearTextField(signUp.Q<TextField>("email"));
            ClearTextField(signUp.Q<TextField>("username"));
            ClearTextField(signUp.Q<TextField>("password"));
            ClearTextField(signUp.Q<TextField>("confirm-password"));
        }

        /// <summary>
        /// Clears the email field in the forgot password form.
        /// </summary>
        private void ClearForgotPasswordFields()
        {
            var forgotPassword = GetPanelForState(UIState.ForgotPassword);
            ClearTextField(forgotPassword.Q<TextField>("email"));
        }

        /// <summary>
        /// Clears all input fields in the reset password form.
        /// </summary>
        private void ClearResetPasswordFields()
        {
            var resetPassword = GetPanelForState(UIState.ResetPassword);
            ClearTextField(resetPassword.Q<TextField>("code"));
            ClearTextField(resetPassword.Q<TextField>("password"));
            ClearTextField(resetPassword.Q<TextField>("confirm-password"));
        }

        /// <summary>
        /// Clears the code input field in the confirm account form.
        /// </summary>
        private void ClearConfirmAccountFields()
        {
            var confirmAccount = GetPanelForState(UIState.ConfirmAccount);
            ClearTextField(confirmAccount.Q<TextField>("code"));
        }

        #endregion

        #region Unity Lifecycle Methods

        /// <summary>
        /// Called when the script instance is being enabled. Initializes UI and subscribes to events.
        /// </summary>
        private void OnEnable()
        {
            InitializeUI();
        }

        /// <summary>
        /// Called when the script instance is being disabled. Unregisters UI callbacks and unsubscribes from events.
        /// </summary>
        private void OnDisable()
        {
            UnregisterUICallbacks();
        }

        /// <summary>
        /// Called when the script instance is being destroyed. Ensures cleanup of UI callbacks and event subscriptions.
        /// </summary>
        private void OnDestroy()
        {
            UnregisterUICallbacks();
        }

        /// <summary>
        /// Called every frame. Updates the loading indicator animation if visible.
        /// </summary>
        private void Update()
        {
            UpdateLoader();
        }

        #endregion

        #region Authentication Event Handling

        /// <summary>
        /// Handles the successful signin event from AuthenticationManager. Clears relevant fields.
        /// </summary>
        public void HandleSignInSuccess()
        {
            HideLoader();
        }

        /// <summary>
        /// Handles the successful signup event from AuthenticationManager. Clears relevant fields.
        /// </summary>
        public void HandleSignUpSuccess()
        {
            HideLoader();
        }

        /// <summary>
        /// Handles the successful reset password event from AuthenticationManager. Clears relevant fields.
        /// </summary>
        public void HandleResetPasswordSuccess()
        {
            HideLoader();
        }
        
        /// <summary>
        /// Handles the successful password change event from AuthenticationManager. Clears relevant fields.
        /// </summary>
        public void HandlePasswordChangeSuccess()
        {
            ClearForgotPasswordFields();
            ClearResetPasswordFields();
            HideForgotPasswordValidationError();
            HideResetPasswordValidationError();
            HideLoader();
        }

        /// <summary>
        /// Handles the successful account confirmation event from AuthenticationManager. Clears relevant fields.
        /// </summary>
        public void HandleAccountConfirmationSuccess()
        {
            HideSignUpValidationError();
            ClearSignUpFields();
            HideConfirmAccountValidationError();
            ClearConfirmAccountFields();
            HideLoader();
        }

        #endregion

        #region UI Initialization

        /// <summary>
        /// Initializes all UI components, sets up the initial state, registers callbacks, and loads remembered account data.
        /// </summary>
        private void InitializeUI()
        {
            // Initialize UI document
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
            
            // Get root element and loader
            _root = uiDocument.rootVisualElement;
            _loader = _root.Q<VisualElement>("loader");
            _loading = _loader.Q<CircularLoader>("loading");
            
            // Hide loader initially
            HideLoader();
            
            // Register UI event handlers
            RegisterUICallbacks();
            
            // Set initial state
            SwitchUIState(UIState.SignIn);
            
            // Load remembered account if available
            LoadRememberedAccount();
        }

        /// <summary>
        /// Registers callbacks for all UI panels (Sign In, Sign Up, Forgot Password, etc.).
        /// </summary>
        private void RegisterUICallbacks()
        {
            RegisterSignInCallbacks();
            RegisterSignUpCallbacks();
            RegisterForgotPasswordCallbacks();
            RegisterResetPasswordCallbacks();
            RegisterConfirmAccountCallbacks();
        }

        #endregion

        #region UI State Management

        /// <summary>
        /// Switches the visible UI panel based on the target authentication state. Hides all other panels. Clears Sign In fields if navigating away and 'Remember Me' is off.
        /// </summary>
        /// <param name="state">The UIState to transition to.</param>
        private void SwitchUIState(UIState state)
        {
            // If switching from SignIn to another state, check RememberMe
            if (_currentState == UIState.SignIn && state != UIState.SignIn)
            {
                var signIn = GetPanelForState(UIState.SignIn);
                var rememberToggle = signIn.Q<Toggle>("remember-toggle");
                if (!rememberToggle.value)
                {
                    ClearSignInFields(false);
                }
            }

            // Hide all panels first
            foreach (UIState uiState in Enum.GetValues(typeof(UIState)))
            {
                var panel = GetPanelForState(uiState);
                if (panel != null)
                {
                    panel.style.display = DisplayStyle.None;
                }
            }

            // Show the requested panel
            var activePanel = GetPanelForState(state);
            if (activePanel != null)
            {
                activePanel.style.display = DisplayStyle.Flex;
                _currentState = state;
            }
            else
            {
                Log.Error($"Panel not found for state: {state}");
            }
        }

        /// <summary>
        /// Retrieves the VisualElement panel corresponding to the specified UIState.
        /// </summary>
        /// <param name="state">The UIState whose panel is needed.</param>
        /// <returns>The VisualElement for the state, or null if not found.</returns>
        private VisualElement GetPanelForState(UIState state)
        {
            return state switch
            {
                UIState.SignIn => _root.Q<VisualElement>("sign-in"),
                UIState.SignUp => _root.Q<VisualElement>("sign-up"),
                UIState.ForgotPassword => _root.Q<VisualElement>("forgot-password"),
                UIState.ResetPassword => _root.Q<VisualElement>("reset-password"),
                UIState.ConfirmAccount => _root.Q<VisualElement>("confirm-account"),
                _ => null
            };
        }

        #endregion

        #region UI Callback Registration

        /// <summary>
        /// Registers all callbacks for the Sign In panel UI elements.
        /// </summary>
        private void RegisterSignInCallbacks()
        {
            var signIn = GetPanelForState(UIState.SignIn);
            var exitButton = signIn.Q<Button>("exit-button");
            var signInButton = signIn.Q<Button>("sign-in-button");
            var forgotPasswordButton = signIn.Q<Button>("forgot-password-ui-button");
            var signUpButton = signIn.Q<Button>("sign-up-ui-button");
            var rememberToggle = signIn.Q<Toggle>("remember-toggle");
            var showPasswordToggle = signIn.Q<Toggle>("show-password");
            var passwordField = signIn.Q<TextField>("password");
            var usernameField = signIn.Q<TextField>("username");

            // Store handlers
            _exitHandler = _ => HandleExit();
            _signInHandler = _ => HandleSignIn();
            _forgotPasswordHandler = _ => SwitchUIState(UIState.ForgotPassword);
            _signInToSignUpHandler = _ => HandleSignInToSignUp();
            _signInShowPasswordHandler = evt => passwordField.isPasswordField = !evt.newValue;
            _rememberMeHandler = evt => HandleRememberMe(evt.newValue);
            _signInUsernameInputHandler = evt => HideValidationError(evt.target as VisualElement, "username-validate");
            _signInPasswordInputHandler = evt =>
            {
                HideValidationError(evt.target as VisualElement, "password-validate");
                HideValidationError(evt.target as VisualElement, "password-length-validate");
                HideValidationError(evt.target as VisualElement, "password-special-validate");
                HideValidationError(evt.target as VisualElement, "password-upper-case-validate");
                HideValidationError(evt.target as VisualElement, "password-lower-case-validate");
                HideValidationError(evt.target as VisualElement, "password-number-validate");
            };
            // Register handlers
            exitButton.RegisterCallback(_exitHandler);
            signInButton.RegisterCallback(_signInHandler);
            forgotPasswordButton.RegisterCallback(_forgotPasswordHandler);
            signUpButton.RegisterCallback(_signInToSignUpHandler);
            showPasswordToggle.RegisterCallback(_signInShowPasswordHandler);
            rememberToggle.RegisterCallback(_rememberMeHandler);

            // Register input handlers to hide validation errors
            usernameField.RegisterCallback(_signInUsernameInputHandler);
            passwordField.RegisterCallback(_signInPasswordInputHandler);
        }

        /// <summary>
        /// Unregisters all callbacks for the Sign In panel UI elements.
        /// </summary>
        private void UnregisterSignInCallbacks()
        {
            if (_root == null) return;

            var signIn = GetPanelForState(UIState.SignIn);
            if (signIn == null) return;

            var exitButton = signIn.Q<Button>("exit-button");
            var signInButton = signIn.Q<Button>("sign-in-button");
            var forgotPasswordButton = signIn.Q<Button>("forgot-password-ui-button");
            var signUpButton = signIn.Q<Button>("sign-up-ui-button");
            var showPasswordToggle = signIn.Q<Toggle>("show-password");
            var rememberToggle = signIn.Q<Toggle>("remember-toggle");

            if (_exitHandler != null && exitButton != null)
            {
                exitButton.UnregisterCallback(_exitHandler);
            }

            if (_signInHandler != null && signInButton != null)
                signInButton.UnregisterCallback(_signInHandler);

            if (_forgotPasswordHandler != null && forgotPasswordButton != null)
                forgotPasswordButton.UnregisterCallback(_forgotPasswordHandler);
                
            if (_signInToSignUpHandler != null && signUpButton != null)
                signUpButton.UnregisterCallback(_signInToSignUpHandler);

            if (_signInShowPasswordHandler != null && showPasswordToggle != null)
                showPasswordToggle.UnregisterCallback(_signInShowPasswordHandler);

            if (_rememberMeHandler != null && rememberToggle != null)
                rememberToggle.UnregisterCallback(_rememberMeHandler);
        }

        /// <summary>
        /// Registers all callbacks for the Sign Up panel UI elements.
        /// </summary>
        private void RegisterSignUpCallbacks()
        {
            var signUp = GetPanelForState(UIState.SignUp);
            var exitButton = signUp.Q<Button>("exit-button");
            var signUpButton = signUp.Q<Button>("sign-up-button");
            var signInButton = signUp.Q<Button>("sign-in-ui-button");
            var showPasswordToggle = signUp.Q<Toggle>("show-password");
            var showConfirmPasswordToggle = signUp.Q<Toggle>("show-confirm-password");
            var passwordField = signUp.Q<TextField>("password");
            var confirmPasswordField = signUp.Q<TextField>("confirm-password");
            var emailField = signUp.Q<TextField>("email");
            var usernameField = signUp.Q<TextField>("username");

            // Store handlers
            _exitHandler = _ => HandleExit();
            _signUpHandler = _ => HandleSignUp();
            _signUpToSignInHandler = _ => HandleSignUpToSignIn();
            _signUpShowPasswordHandler = evt => passwordField.isPasswordField = !evt.newValue;
            _signUpShowConfirmPasswordHandler = evt => confirmPasswordField.isPasswordField = !evt.newValue;
            _signUpEmailInputHandler = evt => HideValidationError(evt.target as VisualElement, "email-validate");
            _signUpUsernameInputHandler = evt => HideValidationError(evt.target as VisualElement, "username-validate");
            _signUpPasswordInputHandler = evt =>
            {
                HideValidationError(evt.target as VisualElement, "password-validate");
                HideValidationError(evt.target as VisualElement, "password-length-validate");
                HideValidationError(evt.target as VisualElement, "password-special-validate");
                HideValidationError(evt.target as VisualElement, "password-upper-case-validate");
                HideValidationError(evt.target as VisualElement, "password-lower-case-validate");
                HideValidationError(evt.target as VisualElement, "password-number-validate");
            };
            _signUpConfirmPasswordInputHandler = evt => HideValidationError(evt.target as VisualElement, "confirm-password-validate");

            // Register handlers
            exitButton.RegisterCallback(_exitHandler);
            signUpButton.RegisterCallback(_signUpHandler);
            signInButton.RegisterCallback(_signUpToSignInHandler);
            showPasswordToggle.RegisterCallback(_signUpShowPasswordHandler);
            showConfirmPasswordToggle.RegisterCallback(_signUpShowConfirmPasswordHandler);

            // Register input handlers to hide validation errors
            emailField.RegisterCallback(_signUpEmailInputHandler);
            usernameField.RegisterCallback(_signUpUsernameInputHandler);
            passwordField.RegisterCallback(_signUpPasswordInputHandler);
            confirmPasswordField.RegisterCallback(_signUpConfirmPasswordInputHandler);
        }

        /// <summary>
        /// Unregisters all callbacks for the Sign Up panel UI elements.
        /// </summary>
        private void UnregisterSignUpCallbacks()
        {
            if (_root == null) return;

            var signUp = GetPanelForState(UIState.SignUp);
            if (signUp == null) return;

            var exitButton = signUp.Q<Button>("exit-button");
            var signUpButton = signUp.Q<Button>("sign-up-button");
            var signInButton = signUp.Q<Button>("sign-in-ui-button");
            var showPasswordToggle = signUp.Q<Toggle>("show-password");
            var showConfirmPasswordToggle = signUp.Q<Toggle>("show-confirm-password");

            if (_exitHandler != null && exitButton != null)
            {
                exitButton.UnregisterCallback(_exitHandler);
            }
            
            if (_signUpHandler != null && signUpButton != null)
                signUpButton.UnregisterCallback(_signUpHandler);
                
            if (_signUpToSignInHandler != null && signInButton != null)
                signInButton.UnregisterCallback(_signUpToSignInHandler);

            if (_signUpShowPasswordHandler != null && showPasswordToggle != null)
                showPasswordToggle.UnregisterCallback(_signUpShowPasswordHandler);

            if (_signUpShowConfirmPasswordHandler != null && showConfirmPasswordToggle != null)
                showConfirmPasswordToggle.UnregisterCallback(_signUpShowConfirmPasswordHandler);
        }

        /// <summary>
        /// Registers callbacks for the Forgot Password panel UI elements.
        /// </summary>
        private void RegisterForgotPasswordCallbacks()
        {
            var forgotPassword = GetPanelForState(UIState.ForgotPassword);
            var resetButton = forgotPassword.Q<Button>("reset-password-ui-button");
            var backButton = forgotPassword.Q<Button>("back-button");
            var emailField = forgotPassword.Q<TextField>("email");

            // Store handler
            _resetPasswordUiHandler = _ => HandleResetPassword();
            _forgotPasswordBackHandler = _ => HandleForgotPasswordBack();
            _forgotPasswordEmailInputHandler = evt => HideValidationError(evt.target as VisualElement, "email-validate");

            resetButton.RegisterCallback(_resetPasswordUiHandler);
            backButton.RegisterCallback(_forgotPasswordBackHandler);

            // Register input handler to hide validation error
            emailField.RegisterCallback(_forgotPasswordEmailInputHandler);
        }
        
        /// <summary>
        /// Unregisters callbacks for the Forgot Password panel UI elements.
        /// </summary>
        private void UnregisterForgotPasswordCallbacks()
        {
            if (_root == null) return;

            var forgotPassword = GetPanelForState(UIState.ForgotPassword);
            if (forgotPassword == null) return;

            var resetButton = forgotPassword.Q<Button>("reset-password-ui-button");
            var backButton = forgotPassword.Q<Button>("back-button");
            var emailField = forgotPassword.Q<TextField>("email");

            if (_resetPasswordUiHandler != null && resetButton != null)
                resetButton.UnregisterCallback(_resetPasswordUiHandler);

            if (_forgotPasswordBackHandler != null && backButton != null)
                backButton.UnregisterCallback(_forgotPasswordBackHandler);

            if (_forgotPasswordEmailInputHandler != null && emailField != null)
                emailField.UnregisterCallback(_forgotPasswordEmailInputHandler);
        }

        /// <summary>
        /// Registers callbacks for the Reset Password panel UI elements.
        /// </summary>
        private void RegisterResetPasswordCallbacks()
        {
            var resetPassword = GetPanelForState(UIState.ResetPassword);
            var changePasswordButton = resetPassword.Q<Button>("change-password-button");
            var backButton = resetPassword.Q<Button>("back-button");
            var showPasswordToggle = resetPassword.Q<Toggle>("show-password");
            var showConfirmPasswordToggle = resetPassword.Q<Toggle>("show-confirm-password");
            var passwordField = resetPassword.Q<TextField>("password");
            var confirmPasswordField = resetPassword.Q<TextField>("confirm-password");
            var codeField = resetPassword.Q<TextField>("code");

            // Store handlers
            _changePasswordHandler = _ => HandleChangePassword();
            _resetPasswordBackHandler = _ => HandleResetPasswordBack();
            _resetShowPasswordHandler = evt => passwordField.isPasswordField = !evt.newValue;
            _resetShowConfirmPasswordHandler = evt => confirmPasswordField.isPasswordField = !evt.newValue;
            _resetPasswordCodeInputHandler = evt => HideValidationError(evt.target as VisualElement, "code-validate");
            _resetPasswordPasswordInputHandler = evt =>
            {
                HideValidationError(evt.target as VisualElement, "password-validate");
                HideValidationError(evt.target as VisualElement, "password-length-validate");
                HideValidationError(evt.target as VisualElement, "password-special-validate");
                HideValidationError(evt.target as VisualElement, "password-upper-case-validate");
                HideValidationError(evt.target as VisualElement, "password-lower-case-validate");
                HideValidationError(evt.target as VisualElement, "password-number-validate");
            };
            _resetPasswordConfirmPasswordInputHandler = evt => HideValidationError(evt.target as VisualElement, "confirm-password-validate");
            
            changePasswordButton.RegisterCallback(_changePasswordHandler);
            backButton.RegisterCallback(_resetPasswordBackHandler);
            
            showPasswordToggle.RegisterValueChangedCallback(_resetShowPasswordHandler);
            showConfirmPasswordToggle.RegisterValueChangedCallback(_resetShowConfirmPasswordHandler);

            // Register input handlers to hide validation errors
            codeField.RegisterCallback(_resetPasswordCodeInputHandler);
            passwordField.RegisterCallback(_resetPasswordPasswordInputHandler);
            confirmPasswordField.RegisterCallback(_resetPasswordConfirmPasswordInputHandler);
        }

        /// <summary>
        /// Unregisters callbacks for the Reset Password panel UI elements.
        /// </summary>
        private void UnregisterResetPasswordCallbacks()
        {
            if (_root == null) return;

            var resetPassword = GetPanelForState(UIState.ResetPassword);
            if (resetPassword == null) return;

            var changePasswordButton = resetPassword.Q<Button>("change-password-button");
            var backButton = resetPassword.Q<Button>("back-button");
            var showPasswordToggle = resetPassword.Q<Toggle>("show-password");
            var showConfirmPasswordToggle = resetPassword.Q<Toggle>("show-confirm-password");
            var passwordField = resetPassword.Q<TextField>("password");
            var confirmPasswordField = resetPassword.Q<TextField>("confirm-password");
            var codeField = resetPassword.Q<TextField>("code");

            if (_changePasswordHandler != null && changePasswordButton != null)
                changePasswordButton.UnregisterCallback(_changePasswordHandler);

            if (_resetPasswordBackHandler != null && backButton != null)
                backButton.UnregisterCallback(_resetPasswordBackHandler);

            if (_resetShowPasswordHandler != null && showPasswordToggle != null)
                showPasswordToggle.UnregisterCallback(_resetShowPasswordHandler);

            if (_resetShowConfirmPasswordHandler != null && showConfirmPasswordToggle != null)
                showConfirmPasswordToggle.UnregisterCallback(_resetShowConfirmPasswordHandler);

            if (_resetPasswordPasswordInputHandler != null && passwordField != null)
                passwordField.UnregisterCallback(_resetPasswordPasswordInputHandler);

            if (_resetPasswordConfirmPasswordInputHandler != null && confirmPasswordField != null)
                confirmPasswordField.UnregisterCallback(_resetPasswordConfirmPasswordInputHandler);

            if (_resetPasswordCodeInputHandler != null && codeField != null)
                codeField.UnregisterCallback(_resetPasswordCodeInputHandler);
        }

        /// <summary>
        /// Registers callbacks for the Confirm Account panel UI elements.
        /// </summary>
        private void RegisterConfirmAccountCallbacks()
        {
            var confirmAccount = GetPanelForState(UIState.ConfirmAccount);
            var confirmButton = confirmAccount.Q<Button>("confirm-account-button");
            var resendButton = confirmAccount.Q<Button>("resend-code-button");
            var backButton = confirmAccount.Q<Button>("back-button");
            var codeField = confirmAccount.Q<TextField>("code");

            // Store handler
            _confirmAccountHandler = _ => HandleConfirmAccount();
            _resendCodeHandler = _ => HandleResendCode();
            _confirmAccountBackHandler = _ => HandleConfirmAccountBack();
            _confirmAccountCodeInputHandler = evt => HideValidationError(evt.target as VisualElement, "code-validate");
            
            confirmButton.RegisterCallback(_confirmAccountHandler);
            resendButton.RegisterCallback(_resendCodeHandler);
            backButton.RegisterCallback(_confirmAccountBackHandler);

            // Register input handler to hide validation error
            codeField.RegisterCallback(_confirmAccountCodeInputHandler);
        }

        /// <summary>
        /// Unregisters callbacks for the Confirm Account panel UI elements.
        /// </summary>
        private void UnregisterConfirmAccountCallbacks()
        {
            if (_root == null) return;

            var confirmAccount = GetPanelForState(UIState.ConfirmAccount);
            if (confirmAccount == null) return;

            var confirmButton = confirmAccount.Q<Button>("confirm-account-button");
            var resendButton = confirmAccount.Q<Button>("resend-code-button");
            var backButton = confirmAccount.Q<Button>("back-button");
            var codeField = confirmAccount.Q<TextField>("code");

            if (_confirmAccountHandler != null && confirmButton != null)
                confirmButton.UnregisterCallback(_confirmAccountHandler);

            if (_resendCodeHandler != null && resendButton != null)
                resendButton.UnregisterCallback(_resendCodeHandler);

            if (_confirmAccountBackHandler != null && backButton != null)
                backButton.UnregisterCallback(_confirmAccountBackHandler);

            if (_confirmAccountCodeInputHandler != null && codeField != null)
                codeField.UnregisterCallback(_confirmAccountCodeInputHandler);
        }

        /// <summary>
        /// Unregisters all UI callbacks across all panels and clears handler references.
        /// </summary>
        private void UnregisterUICallbacks()
        {
            UnregisterSignInCallbacks();
            UnregisterSignUpCallbacks();
            UnregisterForgotPasswordCallbacks();
            UnregisterResetPasswordCallbacks();
            UnregisterConfirmAccountCallbacks();

            // Clear handler references
            _exitHandler = null;
            
            // Sign In
            _signInHandler = null;
            _forgotPasswordHandler = null;
            _signInToSignUpHandler = null;
            _signInShowPasswordHandler = null;
            _rememberMeHandler = null;
            _signInUsernameInputHandler = null;
            _signInPasswordInputHandler = null;
            
            // Sign Up
            _signUpHandler = null;
            _signUpToSignInHandler = null;
            _signUpShowPasswordHandler = null;
            _signUpShowConfirmPasswordHandler = null;
            _signUpEmailInputHandler = null;
            _signUpUsernameInputHandler = null;
            _signUpPasswordInputHandler = null;
            _signUpConfirmPasswordInputHandler = null;
            
            // Forgot Password
            _resetPasswordUiHandler = null;
            _forgotPasswordBackHandler = null;
            _forgotPasswordEmailInputHandler = null;
            
            // Reset Password
            _changePasswordHandler = null;
            _resetPasswordBackHandler = null;
            _resetShowPasswordHandler = null;
            _resetShowConfirmPasswordHandler = null;
            _resetPasswordCodeInputHandler = null;
            _resetPasswordPasswordInputHandler = null;
            _resetPasswordConfirmPasswordInputHandler = null;
            
            // Confirm Account
            _confirmAccountHandler = null;
            _resendCodeHandler = null;
            _confirmAccountBackHandler = null;
            _confirmAccountCodeInputHandler = null;
        }

        #endregion

        #region UI Action Handlers

        /// <summary>
        /// Handles the exit button click event, quitting the application.
        /// </summary>
        private void HandleExit()
        {
            Application.Quit();
        }
        
        /// <summary>
        /// Handles the sign-in button click. Validates inputs and initiates the sign-in process via AuthenticationManager. Shows loader.
        /// </summary>
        private void HandleSignIn()
        {
            var signIn = GetPanelForState(UIState.SignIn);
            var usernameField = signIn.Q<TextField>("username");
            var passwordField = signIn.Q<TextField>("password");
            var rememberToggle = signIn.Q<Toggle>("remember-toggle");

            if (ValidateSignInInputs(usernameField, passwordField))
            {
                ShowLoader();
                StartCoroutine(AuthenticationManager.Instance.SignIn(
                    usernameField.value,
                    passwordField.value,
                    rememberToggle.value
                ));
            }
        }

        /// <summary>
        /// Handles the sign-up button click. Validates inputs and initiates the sign-up process via AuthenticationManager. Shows loader.
        /// </summary>
        private void HandleSignUp()
        {
            var signUp = GetPanelForState(UIState.SignUp);
            var emailField = signUp.Q<TextField>("email");
            var usernameField = signUp.Q<TextField>("username");
            var passwordField = signUp.Q<TextField>("password");
            var confirmPasswordField = signUp.Q<TextField>("confirm-password");

            if (ValidateSignUpInputs(emailField, usernameField, passwordField, confirmPasswordField))
            {
                ShowLoader();
                StartCoroutine(AuthenticationManager.Instance.SignUp(
                    usernameField.value,
                    passwordField.value,
                    emailField.value
                ));
            }
        }

        /// <summary>
        /// Handles the request to reset the password from the Forgot Password screen. Validates email and initiates the reset password process via AuthenticationManager. Shows loader.
        /// </summary>
        private void HandleResetPassword()
        {
            var forgotPassword = GetPanelForState(UIState.ForgotPassword);
            var emailField = forgotPassword.Q<TextField>("email");

            if (ValidationService.ValidateEmail(emailField))
            {
                ShowLoader();
                StartCoroutine(AuthenticationManager.Instance.ForgotPassword(emailField.value));
            }
            else
            {
                ShowValidationError(emailField, "email-validate");
            }
        }

        /// <summary>
        /// Handles the change password button click on the Reset Password screen. Validates inputs and initiates the change password process via AuthenticationManager. Shows loader.
        /// </summary>
        private void HandleChangePassword()
        {
            var resetPassword = GetPanelForState(UIState.ResetPassword);
            var codeField = resetPassword.Q<TextField>("code");
            var passwordField = resetPassword.Q<TextField>("password");
            var confirmPasswordField = resetPassword.Q<TextField>("confirm-password");

            if (ValidateChangePasswordInputs(codeField, passwordField, confirmPasswordField))
            {
                var forgotPassword = GetPanelForState(UIState.ForgotPassword);
                var emailField = forgotPassword.Q<TextField>("email");
                
                ShowLoader();
                StartCoroutine(AuthenticationManager.Instance.ChangePassword(
                    emailField.value,
                    codeField.value,
                    passwordField.value
                ));
            }
        }

        /// <summary>
        /// Handles the confirm account button click. Validates inputs and initiates the account confirmation process via AuthenticationManager. Shows loader.
        /// </summary>
        private void HandleConfirmAccount()
        {
            var confirmAccount = GetPanelForState(UIState.ConfirmAccount);
            var codeField = confirmAccount.Q<TextField>("code");
            var signUp = GetPanelForState(UIState.SignUp);
            var usernameField = signUp.Q<TextField>("username");

            if (ValidationService.ValidateUsername(usernameField) && ValidationService.ValidateCode(codeField))
            {
                ShowLoader();
                StartCoroutine(AuthenticationManager.Instance.ConfirmAccount(
                    usernameField.value,
                    codeField.value
                ));
            }
            else
            {
                if (!ValidationService.ValidateUsername(usernameField))
                {
                    ShowValidationError(usernameField, "username-validate");
                }
                if (!ValidationService.ValidateCode(codeField))
                {
                    ShowValidationError(codeField, "code-validate");
                }
            }
        }

        /// <summary>
        /// Handles the resend code button click. Validates username and initiates the resend code process via AuthenticationManager.
        /// </summary>
        private void HandleResendCode()
        {
            var signUp = GetPanelForState(UIState.SignUp);
            var usernameField = signUp.Q<TextField>("username");

            if (ValidationService.ValidateUsername(usernameField))
            {
                StartCoroutine(AuthenticationManager.Instance.ResendConfirmationCode(usernameField.value));
            }
            else
            {
                ShowValidationError(usernameField, "username-validate");
            }
        }

        /// <summary>
        /// Handles the state change of the 'Remember Me' toggle. Saves or deletes account data accordingly.
        /// </summary>
        /// <param name="isChecked">The new state of the toggle.</param>
        private void HandleRememberMe(bool isChecked)
        {
            var signIn = GetPanelForState(UIState.SignIn);
            var usernameField = signIn.Q<TextField>("username");
            var passwordField = signIn.Q<TextField>("password");

            if (isChecked)
            {
                RememberAccount.SaveAccountData(new RememberAccount.RememberData
                {
                    active = true,
                    username = usernameField.value,
                    password = passwordField.value
                });
            }
            else
            {
                RememberAccount.DeleteAccountData();
            }
        }

        /// <summary>
        /// Handles the navigation from the Sign In screen to the SignUp screen. Hides any validation errors on the Sign In screen before switching.
        /// </summary>
        private void HandleSignInToSignUp()
        {
            HideSignInValidationError();
            SwitchUIState(UIState.SignUp);
        }

        /// <summary>
        /// Handles the navigation from the SignUp screen to the Sign In screen. Hides any validation errors on the Sign Up screen before switching.
        /// </summary>
        private void HandleSignUpToSignIn()
        {
            HideSignUpValidationError();
            ClearSignUpFields();
            SwitchUIState(UIState.SignIn);
        }

        /// <summary>
        /// Handles the back button click on the Forgot Password screen. Clears fields, hides validation errors, and switches to the Sign In screen.
        /// </summary>
        private void HandleForgotPasswordBack()
        {
            ClearForgotPasswordFields();
            HideForgotPasswordValidationError();
            SwitchUIState(UIState.SignIn);
        }

        /// <summary>
        /// Handles the back button click on the Reset Password screen. Clears fields, hides validation errors, and switches to the Sign In screen.
        /// </summary>
        private void HandleResetPasswordBack()
        {
            ClearResetPasswordFields();
            HideResetPasswordValidationError();
            SwitchUIState(UIState.SignIn);
        }

        /// <summary>
        /// Handles the back button click on the Confirm Account screen. Clears fields, hides validation errors, and switches to the Sign In screen.
        /// </summary>
        private void HandleConfirmAccountBack()
        {
            ClearSignUpFields();
            HideSignUpValidationError();
            ClearConfirmAccountFields();
            HideConfirmAccountValidationError();
            SwitchUIState(UIState.SignIn);
        }

        #endregion

        #region Input Validation Methods

        /// <summary>
        /// Validates the username and password fields on the Sign In screen using ValidationService. Shows validation errors if inputs are invalid.
        /// </summary>
        /// <param name="usernameField">The username TextField.</param>
        /// <param name="passwordField">The password TextField.</param>
        /// <returns>True if inputs are valid, false otherwise.</returns>
        private bool ValidateSignInInputs(TextField usernameField, TextField passwordField)
        {
            bool isValid = true;

            if (!ValidationService.ValidateUsername(usernameField))
            {
                ShowValidationError(usernameField, "username-validate");
                isValid = false;
            }

            if (!ValidationService.ValidatePassword(passwordField))
            {
                ShowValidationError(passwordField, "password-validate");
                ShowValidationError(passwordField, "password-length-validate");
                ShowValidationError(passwordField, "password-special-validate");
                ShowValidationError(passwordField, "password-upper-case-validate");
                ShowValidationError(passwordField, "password-lower-case-validate");
                ShowValidationError(passwordField, "password-number-validate");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Validates the email, username, password, and confirm password fields on the Sign Up screen using ValidationService. Shows validation errors if inputs are invalid.
        /// </summary>
        /// <param name="emailField">The email TextField.</param>
        /// <param name="usernameField">The username TextField.</param>
        /// <param name="passwordField">The password TextField.</param>
        /// <param name="confirmPasswordField">The confirmation password TextField.</param>
        /// <returns>True if inputs are valid, false otherwise.</returns>
        private bool ValidateSignUpInputs(TextField emailField, TextField usernameField, 
            TextField passwordField, TextField confirmPasswordField)
        {
            bool isValid = true;

            if (!ValidationService.ValidateEmail(emailField))
            {
                ShowValidationError(emailField, "email-validate");
                isValid = false;
            }

            if (!ValidationService.ValidateUsername(usernameField))
            {
                ShowValidationError(usernameField, "username-validate");
                isValid = false;
            }

            if (!ValidationService.ValidatePassword(passwordField))
            {
                ShowValidationError(passwordField, "password-validate");
                ShowValidationError(passwordField, "password-length-validate");
                ShowValidationError(passwordField, "password-special-validate");
                ShowValidationError(passwordField, "password-upper-case-validate");
                ShowValidationError(passwordField, "password-lower-case-validate");
                ShowValidationError(passwordField, "password-number-validate");
                isValid = false;
            }

            if (!ValidationService.ValidateConfirmPassword(passwordField, confirmPasswordField))
            {
                ShowValidationError(confirmPasswordField, "confirm-password-validate");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Validates the code, password, and confirm password fields on the Reset Password screen using ValidationService. Shows validation errors if inputs are invalid.
        /// </summary>
        /// <param name="codeField">The code TextField.</param>
        /// <param name="passwordField">The password TextField.</param>
        /// <param name="confirmPasswordField">The confirmation password TextField.</param>
        /// <returns>True if inputs are valid, false otherwise.</returns>
        private bool ValidateChangePasswordInputs(TextField codeField, TextField passwordField, 
            TextField confirmPasswordField)
        {
            bool isValid = true;

            if (!ValidationService.ValidateCode(codeField))
            {
                ShowValidationError(codeField, "code-validate");
                isValid = false;
            }

            if (!ValidationService.ValidatePassword(passwordField))
            {
                ShowValidationError(passwordField, "password-validate");
                ShowValidationError(passwordField, "password-length-validate");
                ShowValidationError(passwordField, "password-special-validate");
                ShowValidationError(passwordField, "password-upper-case-validate");
                ShowValidationError(passwordField, "password-lower-case-validate");
                ShowValidationError(passwordField, "password-number-validate");
                isValid = false;
            }

            if (!ValidationService.ValidateConfirmPassword(passwordField, confirmPasswordField))
            {
                ShowValidationError(confirmPasswordField, "confirm-password-validate");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Shows the validation error Label associated with a specific input field.
        /// </summary>
        /// <param name="field">The input field (or its parent) that failed validation.</param>
        /// <param name="validationLabelName">The name of the validation Label element.</param>
        private void ShowValidationError(VisualElement field, string validationLabelName)
        {
            var parent = field.parent;
            var validationLabel = parent.Q<Label>(validationLabelName);
            if (validationLabel != null)
            {
                validationLabel.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// Hides the validation error Label associated with a specific input field. Typically called when the user starts typing in the field.
        /// </summary>
        /// <param name="field">The input field (or its parent) whose error message should be hidden.</param>
        /// <param name="validationLabelName">The name of the validation Label element.</param>
        private void HideValidationError(VisualElement field, string validationLabelName)
        {
            var parent = field.parent;
            var validationLabel = parent.Q<Label>(validationLabelName);
            if (validationLabel != null)
            {
                validationLabel.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Hides the signin validation error Label associated with a forgot password button. Typically called when the user clicked.
        /// </summary>
        private void HideSignInValidationError()
        {
            var signIn = GetPanelForState(UIState.SignIn);
            
            var usernameValidationLabel = signIn.Q<Label>("username-validate");
            var passwordValidationLabel = signIn.Q<Label>("password-validate");
            var passwordLengthValidationLabel = signIn.Q<Label>("password-length-validate");
            var passwordSpecialValidationLabel = signIn.Q<Label>("password-special-validate");
            var passwordUpperValidationLabel = signIn.Q<Label>("password-upper-case-validate");
            var passwordLowerValidationLabel = signIn.Q<Label>("password-lower-case-validate");
            var passwordNumberValidationLabel = signIn.Q<Label>("password-number-validate");
            
            usernameValidationLabel.style.display = DisplayStyle.None;
            passwordValidationLabel.style.display = DisplayStyle.None;
            passwordLengthValidationLabel.style.display = DisplayStyle.None;
            passwordSpecialValidationLabel.style.display = DisplayStyle.None;
            passwordUpperValidationLabel.style.display = DisplayStyle.None;
            passwordLowerValidationLabel.style.display = DisplayStyle.None;
            passwordNumberValidationLabel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Hides the forgot password validation error Label associated with a forgot password button. Typically called when the user clicked.
        /// </summary>
        private void HideForgotPasswordValidationError()
        {
            var forgotPassword = GetPanelForState(UIState.ForgotPassword);
            
            var emailValidationLabel = forgotPassword.Q<Label>("email-validate");
            
            emailValidationLabel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Hides the reset password validation error Label associated with a forgot password button. Typically called when the user clicked.
        /// </summary>
        private void HideResetPasswordValidationError()
        {
            var resetPassword = GetPanelForState(UIState.ResetPassword);
            
            var codeValidationLabel = resetPassword.Q<Label>("code-validate");
            var passwordValidationLabel = resetPassword.Q<Label>("password-validate");
            var passwordLengthValidationLabel = resetPassword.Q<Label>("password-length-validate");
            var passwordSpecialValidationLabel = resetPassword.Q<Label>("password-special-validate");
            var passwordUpperValidationLabel = resetPassword.Q<Label>("password-upper-case-validate");
            var passwordLowerValidationLabel = resetPassword.Q<Label>("password-lower-case-validate");
            var passwordNumberValidationLabel = resetPassword.Q<Label>("password-number-validate");
            var confirmPasswordValidationLabel = resetPassword.Q<Label>("confirm-password-validate");
            
            codeValidationLabel.style.display = DisplayStyle.None;
            passwordValidationLabel.style.display = DisplayStyle.None;
            passwordLengthValidationLabel.style.display = DisplayStyle.None;
            passwordSpecialValidationLabel.style.display = DisplayStyle.None;
            passwordUpperValidationLabel.style.display = DisplayStyle.None;
            passwordLowerValidationLabel.style.display = DisplayStyle.None;
            passwordNumberValidationLabel.style.display = DisplayStyle.None;
            confirmPasswordValidationLabel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Hides the signup validation error Label associated with a forgot password button. Typically called when the user clicked.
        /// </summary>
        private void HideSignUpValidationError()
        {
            var signUp = GetPanelForState(UIState.SignUp);
            
            var emailValidationLabel = signUp.Q<Label>("email-validate");
            var usernameValidationLabel = signUp.Q<Label>("username-validate");
            var passwordValidationLabel = signUp.Q<Label>("password-validate");
            var passwordLengthValidationLabel = signUp.Q<Label>("password-length-validate");
            var passwordSpecialValidationLabel = signUp.Q<Label>("password-special-validate");
            var passwordUpperValidationLabel = signUp.Q<Label>("password-upper-case-validate");
            var passwordLowerValidationLabel = signUp.Q<Label>("password-lower-case-validate");
            var passwordNumberValidationLabel = signUp.Q<Label>("password-number-validate");
            var confirmPasswordValidationLabel = signUp.Q<Label>("confirm-password-validate");
            
            emailValidationLabel.style.display = DisplayStyle.None;
            usernameValidationLabel.style.display = DisplayStyle.None;
            passwordValidationLabel.style.display = DisplayStyle.None;
            passwordLengthValidationLabel.style.display = DisplayStyle.None;
            passwordSpecialValidationLabel.style.display = DisplayStyle.None;
            passwordUpperValidationLabel.style.display = DisplayStyle.None;
            passwordLowerValidationLabel.style.display = DisplayStyle.None;
            passwordNumberValidationLabel.style.display = DisplayStyle.None;
            confirmPasswordValidationLabel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Hides the confirmation password validation error Label associated with a forgot password button. Typically called when the user clicked.
        /// </summary>
        private void HideConfirmAccountValidationError()
        {
            var confirmAccount = GetPanelForState(UIState.ConfirmAccount);
            
            var codeValidationLabel = confirmAccount.Q<Label>("code-validate");
            
            codeValidationLabel.style.display = DisplayStyle.None;
        }

        #endregion

        #region UI Utility Methods

        /// <summary>
        /// Shows the SignIn UI panel
        /// </summary>
        public void ShowSignInUI()
        {
            SwitchUIState(UIState.SignIn);
        }
        
        /// <summary>
        /// Shows the Confirmation Account UI panel and updates its information label with the provided username.
        /// </summary>
        /// <param name="username">The username to display.</param>
        public void ShowConfirmationUI(string username)
        {
            SwitchUIState(UIState.ConfirmAccount);
            var confirmAccount = GetPanelForState(UIState.ConfirmAccount);
            var infoLabel = confirmAccount.Q<Label>("information-label");
            infoLabel.text = Regex.Replace(infoLabel.text, Pattern, $"[{username}]");
        }

        /// <summary>
        /// Shows the Reset Password UI panel and updates its information label with the provided username (or email used for reset).
        /// </summary>
        /// <param name="username">The identifier (username/email) to display.</param>
        public void ShowResetPasswordUI(string username)
        {
            SwitchUIState(UIState.ResetPassword);
            var resetPassword = GetPanelForState(UIState.ResetPassword);
            var infoLabel = resetPassword.Q<Label>("information-label");
            infoLabel.text = Regex.Replace(infoLabel.text, Pattern, $"[{username}]");
        }

        /// <summary>
        /// Shows the loading indicator visual element.
        /// </summary>
        private void ShowLoader()
        {
            _loader.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Hides the loading indicator visual element.
        /// </summary>
        private void HideLoader()
        {
            _loader.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Updates the progress of the circular loading animation if the loader is visible.
        /// </summary>
        private void UpdateLoader()
        {
            if (_loader.style.display == DisplayStyle.Flex)
            {
                _loading.progress += Time.deltaTime * loadingSpeed;
            }
        }

        /// <summary>
        /// Loads saved account data (if any) using RememberAccount and populates the Sign In fields. Sets the 'Remember Me' toggle state.
        /// </summary>
        private void LoadRememberedAccount()
        {
            if (RememberAccount.LoadAccountData(out var data))
            {
                var signIn = GetPanelForState(UIState.SignIn);
                var rememberToggle = signIn.Q<Toggle>("remember-toggle");
                var usernameField = signIn.Q<TextField>("username");
                var passwordField = signIn.Q<TextField>("password");

                rememberToggle.value = data.active;
                usernameField.value = data.username;
                passwordField.value = data.password;
            }
        }

        #endregion
    }
} 