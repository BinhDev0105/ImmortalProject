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
        private EventCallback<ClickEvent> _signInHandler;
        private EventCallback<ClickEvent> _signUpHandler;
        private EventCallback<ClickEvent> _forgotPasswordHandler;
        private EventCallback<ChangeEvent<bool>> _showPasswordHandler;
        private EventCallback<ChangeEvent<bool>> _showConfirmPasswordHandler;
        private EventCallback<ChangeEvent<bool>> _rememberMeHandler;
        private EventCallback<InputEvent> _usernameInputHandler;
        private EventCallback<InputEvent> _passwordInputHandler;
        private EventCallback<InputEvent> _emailInputHandler;
        private EventCallback<InputEvent> _confirmPasswordInputHandler;
        private EventCallback<InputEvent> _codeInputHandler;
        
        #endregion

        #region Clear Methods

        /// <summary>
        /// Clears a text field's value
        /// </summary>
        private void ClearTextField(TextField field)
        {
            if (field != null)
            {
                field.value = string.Empty;
            }
        }

        /// <summary>
        /// Clears all fields in the signin form based on remember me setting
        /// </summary>
        private void ClearSignInFields(bool rememberMe)
        {
            if (rememberMe) return;
            
            var signIn = GetPanelForState(UIState.SignIn);
            ClearTextField(signIn.Q<TextField>("username"));
            ClearTextField(signIn.Q<TextField>("password"));
        }

        /// <summary>
        /// Clears all fields in the signup form
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
        /// Clears all fields in the forgot password form
        /// </summary>
        private void ClearForgotPasswordFields()
        {
            var forgotPassword = GetPanelForState(UIState.ForgotPassword);
            ClearTextField(forgotPassword.Q<TextField>("email"));
        }

        /// <summary>
        /// Clears all fields in the reset password form
        /// </summary>
        private void ClearResetPasswordFields()
        {
            var resetPassword = GetPanelForState(UIState.ResetPassword);
            ClearTextField(resetPassword.Q<TextField>("code"));
            ClearTextField(resetPassword.Q<TextField>("password"));
            ClearTextField(resetPassword.Q<TextField>("confirm-password"));
        }

        /// <summary>
        /// Clears all fields in the confirm account form
        /// </summary>
        private void ClearConfirmAccountFields()
        {
            var confirmAccount = GetPanelForState(UIState.ConfirmAccount);
            ClearTextField(confirmAccount.Q<TextField>("code"));
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            InitializeUI();
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnregisterUICallbacks();
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            UnregisterUICallbacks();
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            UpdateLoader();
        }

        #endregion

        #region Event Subscription

        private void SubscribeToEvents()
        {
            var authManager = AuthenticationManager.Instance;
            if (authManager != null)
            {
                authManager.OnPasswordChangeSuccess += HandlePasswordChangeSuccess;
                authManager.OnAccountConfirmationSuccess += HandleAccountConfirmationSuccess;
            }
        }

        private void UnsubscribeFromEvents()
        {
            var authManager = AuthenticationManager.Instance;
            if (authManager != null)
            {
                authManager.OnPasswordChangeSuccess -= HandlePasswordChangeSuccess;
                authManager.OnAccountConfirmationSuccess -= HandleAccountConfirmationSuccess;
            }
        }

        private void HandlePasswordChangeSuccess()
        {
            ClearForgotPasswordFields();
            ClearResetPasswordFields();
            SwitchUIState(UIState.SignIn);
        }

        private void HandleAccountConfirmationSuccess()
        {
            ClearSignUpFields();
            ClearConfirmAccountFields();
            SwitchUIState(UIState.SignIn);
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes all UI components and sets up the initial state
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
        /// Registers all UI element callbacks
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
        /// Switches the UI to display the specified authentication state
        /// </summary>
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
        /// Gets the VisualElement panel corresponding to the given UI state
        /// </summary>
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

        #region UI Event Registration

        private void RegisterSignInCallbacks()
        {
            var signIn = GetPanelForState(UIState.SignIn);
            var signInButton = signIn.Q<Button>("sign-in-button");
            var forgotPasswordButton = signIn.Q<Button>("forgot-password-ui-button");
            var signUpButton = signIn.Q<Button>("sign-up-ui-button");
            var rememberToggle = signIn.Q<Toggle>("remember-toggle");
            var showPasswordToggle = signIn.Q<Toggle>("show-password");
            var passwordField = signIn.Q<TextField>("password");
            var usernameField = signIn.Q<TextField>("username");

            // Store handlers
            _signInHandler = _ => HandleSignIn();
            _forgotPasswordHandler = _ => SwitchUIState(UIState.ForgotPassword);
            _showPasswordHandler = evt => passwordField.isPasswordField = !evt.newValue;
            _rememberMeHandler = evt => HandleRememberMe(evt.newValue);
            _usernameInputHandler = evt => HideValidationError(evt.target as VisualElement, "username-validate");
            _passwordInputHandler = evt =>
            {
                HideValidationError(evt.target as VisualElement, "password-validate");
                HideValidationError(evt.target as VisualElement, "password-length-validate");
                HideValidationError(evt.target as VisualElement, "password-special-validate");
                HideValidationError(evt.target as VisualElement, "password-upper-case-validate");
                HideValidationError(evt.target as VisualElement, "password-lower-case-validate");
                HideValidationError(evt.target as VisualElement, "password-number-validate");
            };
            // Register handlers
            signInButton.RegisterCallback(_signInHandler);
            forgotPasswordButton.RegisterCallback(_forgotPasswordHandler);
            signUpButton.RegisterCallback(new EventCallback<ClickEvent>(_ =>
            {
                HideValidationError(usernameField, "username-validate");
                HideValidationError(passwordField, "password-validate");
                HideValidationError(passwordField, "password-length-validate");
                HideValidationError(passwordField, "password-special-validate");
                HideValidationError(passwordField, "password-upper-case-validate");
                HideValidationError(passwordField, "password-lower-case-validate");
                HideValidationError(passwordField, "password-number-validate");
                SwitchUIState(UIState.SignUp);
            }));
            showPasswordToggle.RegisterCallback(_showPasswordHandler);
            rememberToggle.RegisterCallback(_rememberMeHandler);

            // Register input handlers to hide validation errors
            usernameField.RegisterCallback(_usernameInputHandler);
            passwordField.RegisterCallback(_passwordInputHandler);
        }

        private void UnregisterSignInCallbacks()
        {
            if (_root == null) return;

            var signIn = GetPanelForState(UIState.SignIn);
            if (signIn == null) return;

            var signInButton = signIn.Q<Button>("sign-in-button");
            var forgotPasswordButton = signIn.Q<Button>("forgot-password-ui-button");
            var showPasswordToggle = signIn.Q<Toggle>("show-password");
            var rememberToggle = signIn.Q<Toggle>("remember-toggle");

            if (_signInHandler != null && signInButton != null)
                signInButton.UnregisterCallback(_signInHandler);

            if (_forgotPasswordHandler != null && forgotPasswordButton != null)
                forgotPasswordButton.UnregisterCallback(_forgotPasswordHandler);

            if (_showPasswordHandler != null && showPasswordToggle != null)
                showPasswordToggle.UnregisterCallback(_showPasswordHandler);

            if (_rememberMeHandler != null && rememberToggle != null)
                rememberToggle.UnregisterCallback(_rememberMeHandler);
        }

        private void RegisterSignUpCallbacks()
        {
            var signUp = GetPanelForState(UIState.SignUp);
            var signUpButton = signUp.Q<Button>("sign-up-button");
            var signInButton = signUp.Q<Button>("sign-in-ui-button");
            var showPasswordToggle = signUp.Q<Toggle>("show-password");
            var showConfirmPasswordToggle = signUp.Q<Toggle>("show-confirm-password");
            var passwordField = signUp.Q<TextField>("password");
            var confirmPasswordField = signUp.Q<TextField>("confirm-password");
            var emailField = signUp.Q<TextField>("email");
            var usernameField = signUp.Q<TextField>("username");

            // Store handlers
            _signUpHandler = _ => HandleSignUp();
            _showPasswordHandler = evt => passwordField.isPasswordField = !evt.newValue;
            _showConfirmPasswordHandler = evt => confirmPasswordField.isPasswordField = !evt.newValue;
            _emailInputHandler = evt => HideValidationError(evt.target as VisualElement, "email-validate");
            _usernameInputHandler = evt => HideValidationError(evt.target as VisualElement, "username-validate");
            _passwordInputHandler = evt =>
            {
                HideValidationError(evt.target as VisualElement, "password-validate");
                HideValidationError(evt.target as VisualElement, "password-length-validate");
                HideValidationError(evt.target as VisualElement, "password-special-validate");
                HideValidationError(evt.target as VisualElement, "password-upper-case-validate");
                HideValidationError(evt.target as VisualElement, "password-lower-case-validate");
                HideValidationError(evt.target as VisualElement, "password-number-validate");
            };
            _confirmPasswordInputHandler = evt => HideValidationError(evt.target as VisualElement, "confirm-password-validate");

            // Register handlers
            signUpButton.RegisterCallback(_signUpHandler);
            signInButton.RegisterCallback(new EventCallback<ClickEvent>(_ =>
            {
                HideValidationError(emailField, "email-validate");
                HideValidationError(usernameField, "username-validate");
                HideValidationError(passwordField, "password-validate");
                HideValidationError(passwordField, "password-length-validate");
                HideValidationError(passwordField, "password-special-validate");
                HideValidationError(passwordField, "password-upper-case-validate");
                HideValidationError(passwordField, "password-lower-case-validate");
                HideValidationError(passwordField, "password-number-validate");
                HideValidationError(confirmPasswordField, "confirm-password-validate");
                SwitchUIState(UIState.SignIn);
            }));
            showPasswordToggle.RegisterCallback(_showPasswordHandler);
            showConfirmPasswordToggle.RegisterCallback(_showConfirmPasswordHandler);

            // Register input handlers to hide validation errors
            emailField.RegisterCallback(_emailInputHandler);
            usernameField.RegisterCallback(_usernameInputHandler);
            passwordField.RegisterCallback(_passwordInputHandler);
            confirmPasswordField.RegisterCallback(_confirmPasswordInputHandler);
        }

        private void UnregisterSignUpCallbacks()
        {
            if (_root == null) return;

            var signUp = GetPanelForState(UIState.SignUp);
            if (signUp == null) return;

            var signUpButton = signUp.Q<Button>("sign-up-button");
            var showPasswordToggle = signUp.Q<Toggle>("show-password");
            var showConfirmPasswordToggle = signUp.Q<Toggle>("show-confirm-password");

            if (_signUpHandler != null && signUpButton != null)
                signUpButton.UnregisterCallback(_signUpHandler);

            if (_showPasswordHandler != null && showPasswordToggle != null)
                showPasswordToggle.UnregisterCallback(_showPasswordHandler);

            if (_showConfirmPasswordHandler != null && showConfirmPasswordToggle != null)
                showConfirmPasswordToggle.UnregisterCallback(_showConfirmPasswordHandler);
        }

        private void RegisterForgotPasswordCallbacks()
        {
            var forgotPassword = GetPanelForState(UIState.ForgotPassword);
            var resetButton = forgotPassword.Q<Button>("reset-password-ui-button");
            var backButton = forgotPassword.Q<Button>("back-button");
            var emailField = forgotPassword.Q<TextField>("email");

            // Store handler
            _emailInputHandler = evt => HideValidationError(evt.target as VisualElement, "email-validate");

            resetButton.clicked += HandleResetPassword;
            backButton.clicked += () =>
            {
                ClearForgotPasswordFields();
                HideValidationError(emailField, "email-validate");
                SwitchUIState(UIState.SignIn);
            };

            // Register input handler to hide validation error
            emailField.RegisterCallback(_emailInputHandler);
        }

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
            _codeInputHandler = evt => HideValidationError(evt.target as VisualElement, "code-validate");
            _passwordInputHandler = evt =>
            {
                HideValidationError(evt.target as VisualElement, "password-validate");
                HideValidationError(evt.target as VisualElement, "password-length-validate");
                HideValidationError(evt.target as VisualElement, "password-special-validate");
                HideValidationError(evt.target as VisualElement, "password-upper-case-validate");
                HideValidationError(evt.target as VisualElement, "password-lower-case-validate");
                HideValidationError(evt.target as VisualElement, "password-number-validate");
            };
            _confirmPasswordInputHandler = evt => HideValidationError(evt.target as VisualElement, "confirm-password-validate");

            
            changePasswordButton.clicked += HandleChangePassword;
            backButton.clicked += () =>
            {
                ClearResetPasswordFields();
                HideValidationError(passwordField, "password-validate");
                HideValidationError(passwordField, "password-length-validate");
                HideValidationError(passwordField, "password-special-validate");
                HideValidationError(passwordField, "password-upper-case-validate");
                HideValidationError(passwordField, "password-lower-case-validate");
                HideValidationError(passwordField, "password-number-validate");
                HideValidationError(confirmPasswordField, "confirm-password-validate");
                HideValidationError(codeField, "code-validate");
                SwitchUIState(UIState.SignIn);
            };
            showPasswordToggle.RegisterValueChangedCallback(evt => 
                passwordField.isPasswordField = !evt.newValue);
            showConfirmPasswordToggle.RegisterValueChangedCallback(evt => 
                confirmPasswordField.isPasswordField = !evt.newValue);

            // Register input handlers to hide validation errors
            codeField.RegisterCallback(_codeInputHandler);
            passwordField.RegisterCallback(_passwordInputHandler);
            confirmPasswordField.RegisterCallback(_confirmPasswordInputHandler);
        }

        private void RegisterConfirmAccountCallbacks()
        {
            var confirmAccount = GetPanelForState(UIState.ConfirmAccount);
            var confirmButton = confirmAccount.Q<Button>("confirm-account-button");
            var resendButton = confirmAccount.Q<Button>("resend-code-button");
            var backButton = confirmAccount.Q<Button>("back-button");
            var codeField = confirmAccount.Q<TextField>("code");

            // Store handler
            _codeInputHandler = evt => HideValidationError(evt.target as VisualElement, "code-validate");

            confirmButton.clicked += HandleConfirmAccount;
            resendButton.clicked += HandleResendCode;
            backButton.clicked += () =>
            {
                ClearConfirmAccountFields();
                HideValidationError(codeField, "code-validate");
                SwitchUIState(UIState.SignIn);
            };

            // Register input handler to hide validation error
            codeField.RegisterCallback(_codeInputHandler);
        }

        private void UnregisterUICallbacks()
        {
            UnregisterSignInCallbacks();
            UnregisterSignUpCallbacks();
            // Unregister other callbacks...

            // Clear handler references
            _signInHandler = null;
            _signUpHandler = null;
            _forgotPasswordHandler = null;
            _showPasswordHandler = null;
            _showConfirmPasswordHandler = null;
            _rememberMeHandler = null;
        }

        #endregion

        #region UI Event Handlers

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

        private void HandleResetPassword()
        {
            var forgotPassword = GetPanelForState(UIState.ForgotPassword);
            var emailField = forgotPassword.Q<TextField>("email");

            if (ValidationService.ValidateEmail(emailField))
            {
                ShowLoader();
                StartCoroutine(AuthenticationManager.Instance.ResetPassword(emailField.value));
            }
            else
            {
                ShowValidationError(emailField, "email-validate");
            }
        }

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

        #endregion

        #region Validation Methods

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
        /// Hides the validation error message for a specific field
        /// </summary>
        private void HideValidationError(VisualElement field, string validationLabelName)
        {
            var parent = field.parent;
            var validationLabel = parent.Q<Label>(validationLabelName);
            if (validationLabel != null)
            {
                validationLabel.style.display = DisplayStyle.None;
            }
        }

        #endregion

        #region UI Utility Methods

        /// <summary>
        /// Shows the confirmation UI with the specified username
        /// </summary>
        public void ShowConfirmationUI(string username)
        {
            SwitchUIState(UIState.ConfirmAccount);
            var confirmAccount = GetPanelForState(UIState.ConfirmAccount);
            var infoLabel = confirmAccount.Q<Label>("information-label");
            infoLabel.text = Regex.Replace(infoLabel.text, Pattern, $"[{username}]");
        }

        /// <summary>
        /// Shows the reset password UI with the specified username
        /// </summary>
        public void ShowResetPasswordUI(string username)
        {
            SwitchUIState(UIState.ResetPassword);
            var resetPassword = GetPanelForState(UIState.ResetPassword);
            var infoLabel = resetPassword.Q<Label>("information-label");
            infoLabel.text = Regex.Replace(infoLabel.text, Pattern, $"[{username}]");
        }

        /// <summary>
        /// Shows the loading indicator
        /// </summary>
        private void ShowLoader()
        {
            _loader.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Hides the loading indicator
        /// </summary>
        private void HideLoader()
        {
            _loader.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Updates the loading animation
        /// </summary>
        private void UpdateLoader()
        {
            if (_loader.style.display == DisplayStyle.Flex)
            {
                _loading.progress += Time.deltaTime * loadingSpeed;
            }
        }

        /// <summary>
        /// Loads remembered account information if available
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