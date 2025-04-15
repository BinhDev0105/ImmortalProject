using System;
using System.Collections;
using System.Text.RegularExpressions;
using Game.Core.UI_Toolkit.Custom;
using Unity.Logging;
using Unity.Logging.Sinks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Core.Scripts.OnlineService.Authentication
{
    /// <summary>
    /// Manages user authentication flows in Unity using Cognito service as a backend.
    /// Handles sign-in, sign-up, password reset, and account confirmation processes.
    /// </summary>
    public class UnityAuthentication : MonoBehaviour
    {
        #region Data Structures
        /// <summary>
        /// Enum defining possible UI display states
        /// </summary>
        private enum UIDisplay
        {
            SignIn,
            ForgotPassword,
            ResetPassword,
            SignUp,
            ConfirmAccount,
        }

        #endregion

        #region Constants

        /// <summary>
        /// Name of the OpenID Connect provider for Cognito
        /// </summary>
        private const string OidcName = "oidc-cognito";
        
        /// <summary>
        /// Regex pattern
        /// </summary>
        private const string Pattern = @"\[(.*?)\]";

        #endregion
        
        #region Fields and Properties
        
        [SerializeField] 
        private UIDocument uiDocument;
        [SerializeField]
        private float loadingSpeed = 1f;
        
        private readonly CognitoService _cognitoService = new();
        private readonly UnityService _unityService = new();
        
        private VisualElement _root;
        private VisualElement _loader;
        private CircularLoader _loading;
        
        #endregion
        
        #region Unity Lifecycle

        /// <summary>
        /// Initializes UI elements and logging when the component is enabled
        /// </summary>
        private void OnEnable()
        {
            InitializeUIDocument();
            ConfigureLogging();
        }

        /// <summary>
        /// Initializes the Unity authentication service
        /// </summary>
        private void Awake()
        {
            _unityService.InitializeAsync();
        }

        /// <summary>
        /// Configures all UI components and their interactions
        /// </summary>
        private void Start()
        {
            RegisterAllUIComponents();
        }

        /// <summary>
        /// 
        /// </summary>
        private void Update()
        {
            // Loading progress change when display is flex
            if (_loader.style.display == DisplayStyle.Flex)
            {
                _loading.progress += Time.deltaTime * loadingSpeed;
            }
        }

        private void OnDestroy()
        {
            UnregisterAllUIComponents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the UI document component if not already set
        /// </summary>
        private void InitializeUIDocument()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
            _root = uiDocument.rootVisualElement;
            _loader = _root.Q<VisualElement>("loader");
            _loading = _loader.Q<CircularLoader>("loading");
            
            // Hide loader display
            _loader.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Configures the logging system for authentication operations
        /// </summary>
        private void ConfigureLogging()
        {
            var logConfig = new LoggerConfig()
                .MinimumLevel.Debug()
                .OutputTemplate("{Level} - {Message}")
                .WriteTo.File("logs/unity-auth-output.txt", minLevel: LogLevel.Verbose)
                .WriteTo.UnityEditorConsole()
                .CreateLogger();
            Log.Logger = logConfig;
        }

        /// <summary>
        /// Configures all UI components for the authentication system
        /// </summary>
        private void RegisterAllUIComponents()
        {
            RegisterSignInEvent(_root);
            RegisterForgotPasswordEvent(_root);
            RegisterResetPasswordEvent(_root);
            RegisterSignUpEvent(_root);
            RegisterConfirmAccountEvent(_root);
        }
        
        private void UnregisterAllUIComponents()
        {
            UnRegisterSignInEvent(_root);
            UnregisterForgotPasswordEvent(_root);
            UnregisterResetPasswordEvent(_root);
            UnregisterSignUpEvent(_root);
            UnregisterConfirmAccountEvent(_root);
        }

        #endregion

        #region UI Configuration

        /// <summary>
        /// Configures the sign-in UI panel and its interactive elements
        /// </summary>
        /// <param name="root">Root visual element containing all UI components</param>
        private void RegisterSignInEvent(VisualElement root)
        {
            var signIn = root.Q<VisualElement>("sign-in");
            var usernameField = signIn.Q<TextField>("username");
            var usernameValidation = signIn.Q<Label>("username-validate");
            var passwordField = signIn.Q<TextField>("password");
            var passwordValidations = new[]
            {
                signIn.Q<Label>("password-validate"),
                signIn.Q<Label>("password-length-validate"),
                signIn.Q<Label>("password-special-validate"),
                signIn.Q<Label>("password-upper-case-validate"),
                signIn.Q<Label>("password-lower-case-validate"),
                signIn.Q<Label>("password-number-validate"),
            };
            var showPasswordToggle = signIn.Q<Toggle>("show-password");
            var rememberMeToggle = signIn.Q<Toggle>("remember-toggle");
            var forgotPasswordUIButton = signIn.Q<Button>("forgot-password-ui-button");
            var signInButton = signIn.Q<Button>("sign-in-button");
            var signUpUIButton = signIn.Q<Button>("sign-up-ui-button");
            
            // Load remembered account if available
            LoadRememberedAccount(rememberMeToggle, usernameField, passwordField);
            
            // Configure password visibility toggle
            showPasswordToggle.RegisterCallback<ChangeEvent<bool>, TextField>(ShowPasswordToggleHandler, passwordField);

            // Register event callbacks
            rememberMeToggle.RegisterCallback<ChangeEvent<bool>, (Toggle, TextField, TextField)>(
                SignInUIDisplayRememberMeToggleHandler, (rememberMeToggle, usernameField, passwordField));
            
            forgotPasswordUIButton.RegisterCallback<ClickEvent, (Toggle, TextField, Label, TextField, Label[])>(
                SignInUIDisplayForgotPasswordUIButtonHandler, (rememberMeToggle, usernameField, usernameValidation, passwordField, passwordValidations));
            
            signInButton.RegisterCallback<ClickEvent, (Toggle, TextField, Label, TextField, Label[])>(
                SignInUIDisplaySignInButtonHandler, (rememberMeToggle, usernameField, usernameValidation, passwordField, passwordValidations));
            
            signUpUIButton.RegisterCallback<ClickEvent, (Label, Label[])>(SignInUIDisplaySignUpUIButtonHandler, (usernameValidation, passwordValidations));
        }
        
        private void UnRegisterSignInEvent(VisualElement root)
        {
            var signIn = root.Q<VisualElement>("sign-in");
            var showPasswordToggle = signIn.Q<Toggle>("show-password");
            var rememberMeToggle = signIn.Q<Toggle>("remember-toggle");
            var forgotPasswordUIButton = signIn.Q<Button>("forgot-password-ui-button");
            var signInButton = signIn.Q<Button>("sign-in-button");
            var signUpUIButton = signIn.Q<Button>("sign-up-ui-button");
            
            // Configure password visibility toggle
            showPasswordToggle.UnregisterCallback<ChangeEvent<bool>, TextField>(ShowPasswordToggleHandler);

            // Register event callbacks
            rememberMeToggle.UnregisterCallback<ChangeEvent<bool>, (Toggle, TextField, TextField)>(SignInUIDisplayRememberMeToggleHandler);
            
            forgotPasswordUIButton.UnregisterCallback<ClickEvent, (Toggle, TextField, Label, TextField, Label[])>(SignInUIDisplayForgotPasswordUIButtonHandler);
            
            signInButton.UnregisterCallback<ClickEvent, (Toggle, TextField, Label, TextField, Label[])>(SignInUIDisplaySignInButtonHandler);
            
            signUpUIButton.UnregisterCallback<ClickEvent, (Label, Label[])>(SignInUIDisplaySignUpUIButtonHandler);
        }

        /// <summary>
        /// Configures the forgot password UI panel and its interactive elements
        /// </summary>
        /// <param name="root">Root visual element containing all UI components</param>
        private void RegisterForgotPasswordEvent(VisualElement root)
        {
            var forgotPassword = root.Q<VisualElement>("forgot-password");
            var emailField = forgotPassword.Q<TextField>("email");
            var emailValidate = forgotPassword.Q<Label>("email-validate");
            var resetPasswordButton = forgotPassword.Q<Button>("reset-password-ui-button");
            var backButton = forgotPassword.Q<Button>("back-button");
            
            resetPasswordButton.RegisterCallback<ClickEvent, (TextField, Label)>(
                ForgotPasswordUIDisplayResetPasswordButtonHandler, (emailField, emailValidate));
            
            backButton.RegisterCallback<ClickEvent, (TextField, Label)>(ForgotPasswordUIDisplayBackButtonHandler, (emailField, emailValidate));
        }
        
        private void UnregisterForgotPasswordEvent(VisualElement root)
        {
            var forgotPassword = root.Q<VisualElement>("forgot-password");
            var resetPasswordButton = forgotPassword.Q<Button>("reset-password-ui-button");
            var backButton = forgotPassword.Q<Button>("back-button");
            
            resetPasswordButton.UnregisterCallback<ClickEvent, (TextField, Label)>(
                ForgotPasswordUIDisplayResetPasswordButtonHandler);
            
            backButton.UnregisterCallback<ClickEvent, (TextField, Label)>(ForgotPasswordUIDisplayBackButtonHandler);
        }

        /// <summary>
        /// Configures the reset password UI panel and its interactive elements
        /// </summary>
        /// <param name="root">Root visual element containing all UI components</param>
        private void RegisterResetPasswordEvent(VisualElement root)
        {
            var resetPassword = root.Q<VisualElement>("reset-password");
            var codeField = resetPassword.Q<TextField>("code");
            var codeValidate = resetPassword.Q<Label>("code-validate");
            var passwordField = resetPassword.Q<TextField>("password");
            var passwordValidates = new[]
            {
                resetPassword.Q<Label>("password-validate"),
                resetPassword.Q<Label>("password-length-validate"),
                resetPassword.Q<Label>("password-special-validate"),
                resetPassword.Q<Label>("password-upper-case-validate"),
                resetPassword.Q<Label>("password-lower-case-validate"),
                resetPassword.Q<Label>("password-number-validate"),
            };
            var showPasswordToggle = resetPassword.Q<Toggle>("show-password");
            var confirmPasswordField = resetPassword.Q<TextField>("confirm-password");
            var confirmPasswordValidate = resetPassword.Q<Label>("confirm-password-validate");
            var showConfirmPasswordToggle = resetPassword.Q<Toggle>("show-confirm-password");
            var changePasswordButton = resetPassword.Q<Button>("change-password-button");
            var backButton = resetPassword.Q<Button>("back-button");
            
            // Configure password visibility toggles
            showPasswordToggle.RegisterCallback<ChangeEvent<bool>, TextField>(ShowPasswordToggleHandler, passwordField);
                
            showConfirmPasswordToggle.RegisterCallback<ChangeEvent<bool>, TextField>(ShowPasswordToggleHandler, confirmPasswordField);
            
            // Register event callbacks
            changePasswordButton.RegisterCallback<ClickEvent, (TextField, Label, TextField, Label[], TextField, Label)>(
                ResetPasswordUIDisplayChangePasswordButtonHandler,(codeField, codeValidate, passwordField, passwordValidates, confirmPasswordField, confirmPasswordValidate));
            
            backButton.RegisterCallback<ClickEvent, (TextField, Label, TextField, Label[], TextField, Label)>(
                ResetPasswordUIDisplayBackButtonHandler, (codeField, codeValidate, passwordField, passwordValidates, confirmPasswordField, confirmPasswordValidate));
        }
        
        /// <summary>
        /// Configures the reset password UI panel and its interactive elements
        /// </summary>
        /// <param name="root">Root visual element containing all UI components</param>
        private void UnregisterResetPasswordEvent(VisualElement root)
        {
            var resetPassword = root.Q<VisualElement>("reset-password");
            var showPasswordToggle = resetPassword.Q<Toggle>("show-password");
            var showConfirmPasswordToggle = resetPassword.Q<Toggle>("show-confirm-password");
            var changePasswordButton = resetPassword.Q<Button>("change-password-button");
            var backButton = resetPassword.Q<Button>("back-button");
            
            // Configure password visibility toggles
            showPasswordToggle.UnregisterCallback<ChangeEvent<bool>, TextField>(ShowPasswordToggleHandler);
                
            showConfirmPasswordToggle.UnregisterCallback<ChangeEvent<bool>, TextField>(ShowPasswordToggleHandler);
            
            // Register event callbacks
            changePasswordButton.UnregisterCallback<ClickEvent, (TextField, Label, TextField, Label[], TextField, Label)>(
                ResetPasswordUIDisplayChangePasswordButtonHandler);
            
            backButton.UnregisterCallback<ClickEvent, (TextField, Label, TextField, Label[], TextField, Label)>(
                ResetPasswordUIDisplayBackButtonHandler);
        }

        /// <summary>
        /// Configures the sign-up UI panel and its interactive elements
        /// </summary>
        /// <param name="root">Root visual element containing all UI components</param>
        private void RegisterSignUpEvent(VisualElement root)
        {
            var signUp = root.Q<VisualElement>("sign-up");
            var emailField = signUp.Q<TextField>("email");
            var emailValidate = signUp.Q<Label>("email-validate");
            var usernameField = signUp.Q<TextField>("username");
            var usernameValidate = signUp.Q<Label>("username-validate");
            var passwordField = signUp.Q<TextField>("password");
            var passwordValidates = new[]
            {
                signUp.Q<Label>("password-validate"),
                signUp.Q<Label>("password-length-validate"),
                signUp.Q<Label>("password-special-validate"),
                signUp.Q<Label>("password-upper-case-validate"),
                signUp.Q<Label>("password-lower-case-validate"),
                signUp.Q<Label>("password-number-validate"),
            };
            var showPasswordToggle = signUp.Q<Toggle>("show-password");
            var confirmPasswordField = signUp.Q<TextField>("confirm-password");
            var confirmPasswordValidate = signUp.Q<Label>("confirm-password-validate");
            var showConfirmPasswordToggle = signUp.Q<Toggle>("show-confirm-password");
            var signUpButton = signUp.Q<Button>("sign-up-button");
            var signInUIButton = signUp.Q<Button>("sign-in-ui-button");

            // Configure password visibility toggles
            showPasswordToggle.RegisterCallback<ChangeEvent<bool>, TextField>(ShowPasswordToggleHandler, passwordField);
                
            showConfirmPasswordToggle.RegisterCallback<ChangeEvent<bool>, TextField>(ShowPasswordToggleHandler, confirmPasswordField);
            
            // Register event callbacks
            signUpButton.RegisterCallback<ClickEvent, (TextField, Label, TextField, Label, TextField, Label[], TextField, Label)>(
                SignUpUIDisplaySignUpButtonHandler, (emailField, emailValidate, usernameField, usernameValidate, 
                    passwordField, passwordValidates, confirmPasswordField, confirmPasswordValidate));
            
            signInUIButton.RegisterCallback<ClickEvent, (TextField, Label, TextField, Label, TextField, Label[], TextField, Label)>(
                SignUpUIDisplaySignInUIButtonHandler, (emailField, emailValidate, usernameField, usernameValidate, 
                    passwordField, passwordValidates, confirmPasswordField, confirmPasswordValidate));
        }
        
        /// <summary>
        /// Configures the sign-up UI panel and its interactive elements
        /// </summary>
        /// <param name="root">Root visual element containing all UI components</param>
        private void UnregisterSignUpEvent(VisualElement root)
        {
            var signUp = root.Q<VisualElement>("sign-up");
            var showPasswordToggle = signUp.Q<Toggle>("show-password");
            var showConfirmPasswordToggle = signUp.Q<Toggle>("show-confirm-password");
            var signUpButton = signUp.Q<Button>("sign-up-button");
            var signInUIButton = signUp.Q<Button>("sign-in-ui-button");

            // Configure password visibility toggles
            showPasswordToggle.UnregisterCallback<ChangeEvent<bool>, TextField>(ShowPasswordToggleHandler);
                
            showConfirmPasswordToggle.UnregisterCallback<ChangeEvent<bool>, TextField>(ShowPasswordToggleHandler);
            
            // Register event callbacks
            signUpButton.UnregisterCallback<ClickEvent, (TextField, Label, TextField, Label, TextField, Label[], TextField, Label)>(
                SignUpUIDisplaySignUpButtonHandler);
            
            signInUIButton.UnregisterCallback<ClickEvent, (TextField, Label, TextField, Label, TextField, Label[], TextField, Label)>(
                SignUpUIDisplaySignInUIButtonHandler);
        }

        /// <summary>
        /// Configures the account confirmation UI panel and its interactive elements
        /// </summary>
        /// <param name="root">Root visual element containing all UI components</param>
        private void RegisterConfirmAccountEvent(VisualElement root)
        {
            var confirmAccount = root.Q<VisualElement>("confirm-account");
            var codeField = confirmAccount.Q<TextField>("code");
            var codeValidate = confirmAccount.Q<Label>("code-validate");
            var resendCodeButton = confirmAccount.Q<Button>("resend-code-button");
            var confirmAccountButton = confirmAccount.Q<Button>("confirm-account-button");
            var backButton = confirmAccount.Q<Button>("back-button");
            
            // Register event callbacks
            
            resendCodeButton.RegisterCallback<ClickEvent>(
                ConfirmAccountUIDisplayResendCodeButtonHandler);
                
            confirmAccountButton.RegisterCallback<ClickEvent, (TextField, Label)>(
                ConfirmAccountUIDisplayConfirmButtonHandler, (codeField, codeValidate));
                
            backButton.RegisterCallback<ClickEvent, (TextField, Label)>(
                ConfirmAccountUIDisplayBackButtonHandler, (codeField, codeValidate));
        }
        
        /// <summary>
        /// Configures the account confirmation UI panel and its interactive elements
        /// </summary>
        /// <param name="root">Root visual element containing all UI components</param>
        private void UnregisterConfirmAccountEvent(VisualElement root)
        {
            var confirmAccount = root.Q<VisualElement>("confirm-account");
            var resendCodeButton = confirmAccount.Q<Button>("resend-code-button");
            var confirmAccountButton = confirmAccount.Q<Button>("confirm-account-button");
            var backButton = confirmAccount.Q<Button>("back-button");
            
            // Register event callbacks
            
            resendCodeButton.UnregisterCallback<ClickEvent>(
                ConfirmAccountUIDisplayResendCodeButtonHandler);
                
            confirmAccountButton.UnregisterCallback<ClickEvent, (TextField, Label)>(
                ConfirmAccountUIDisplayConfirmButtonHandler);
                
            backButton.UnregisterCallback<ClickEvent, (TextField, Label)>(
                ConfirmAccountUIDisplayBackButtonHandler);
        }
        
        /// <summary>
        /// Switches between different UI panels based on the specified display state
        /// </summary>
        /// <param name="root">Root visual element containing all UI components</param>
        /// <param name="display">Desired UI panel to display</param>
        private void ConfigureUIDisplay(VisualElement root, UIDisplay display)
        {
            var signIn = root.Q<VisualElement>("sign-in");
            var forgotPassword = root.Q<VisualElement>("forgot-password");
            var resetPassword = root.Q<VisualElement>("reset-password");
            var signUp = root.Q<VisualElement>("sign-up");
            var confirmAccount = root.Q<VisualElement>("confirm-account");

            // Hide all panels first
            signIn.style.display = DisplayStyle.None;
            forgotPassword.style.display = DisplayStyle.None;
            resetPassword.style.display = DisplayStyle.None;
            signUp.style.display = DisplayStyle.None;
            confirmAccount.style.display = DisplayStyle.None;

            // Show only the requested panel
            switch (display)
            {
                case UIDisplay.SignIn:
                    signIn.style.display = DisplayStyle.Flex;
                    break;
                case UIDisplay.ForgotPassword:
                    forgotPassword.style.display = DisplayStyle.Flex;
                    break;
                case UIDisplay.ResetPassword:
                    resetPassword.style.display = DisplayStyle.Flex;
                    break;
                case UIDisplay.SignUp:
                    signUp.style.display = DisplayStyle.Flex;
                    break;
                case UIDisplay.ConfirmAccount:
                    confirmAccount.style.display = DisplayStyle.Flex;
                    break;
                default:
                    Log.Error($"Unknown UI display state: {display}");
                    signIn.style.display = DisplayStyle.Flex; // Default to sign in
                    break;
            }
        }

        /// <summary>
        /// Loads remembered account information if available
        /// </summary>
        private void LoadRememberedAccount(Toggle rememberMeToggle, TextField usernameField, TextField passwordField)
        {
            if (RememberAccount.LoadAccountData(out var data))
            {
                rememberMeToggle.value = data.active;
                usernameField.value = data.username;
                passwordField.value = data.password;
            }
        }

        #endregion

        #region UI Event Handlers

        #region General Event Handlers

        private void ShowPasswordToggleHandler(ChangeEvent<bool> evt, TextField passwordField)
        {
            passwordField.isPasswordField = !evt.newValue;
        }

        #endregion

        #region Sign In Event Handlers

        /// <summary>
        /// Handles the "Remember Me" toggle event to save or delete account data
        /// </summary>
        private void SignInUIDisplayRememberMeToggleHandler(ChangeEvent<bool> evt, (Toggle remember, TextField username, TextField password) data)
        {
            if (evt.newValue)
            {
                RememberAccount.SaveAccountData(new RememberAccount.RememberData
                {
                    active = data.remember.value,
                    username = data.username.value,
                    password = data.password.value
                });
            }
            else
            {
                RememberAccount.DeleteAccountData();
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="data"></param>
        private void SignInUIDisplayForgotPasswordUIButtonHandler(ClickEvent evt,
            (Toggle remember, TextField username, Label usernameValidate, TextField password, Label[] passwordValidates) data)
        {
            data.remember.value = false;
            data.username.value = string.Empty;
            data.usernameValidate.style.display = DisplayStyle.None;
            data.password.value = string.Empty;
            var passwordNullValidate = data.passwordValidates[0];
            var passwordLengthValidate = data.passwordValidates[1];
            var passwordSpecialValidate = data.passwordValidates[2];
            var passwordUpperValidate = data.passwordValidates[3];
            var passwordLowerValidate = data.passwordValidates[4];
            var passwordNumberValidate = data.passwordValidates[5];
            passwordNullValidate.style.display = DisplayStyle.None;
            passwordLengthValidate.style.display = DisplayStyle.None;
            passwordSpecialValidate.style.display = DisplayStyle.None;
            passwordUpperValidate.style.display = DisplayStyle.None;
            passwordLowerValidate.style.display = DisplayStyle.None;
            passwordNumberValidate.style.display = DisplayStyle.None;
            ConfigureUIDisplay(_root, UIDisplay.ForgotPassword);
        }
        
        /// <summary>
        /// Handles the sign-in button click to authenticate the user
        /// </summary>
        private void SignInUIDisplaySignInButtonHandler(ClickEvent evt, 
            (Toggle remember, TextField username, Label usernameValidate, TextField password, Label[] passwordValidates) data)
        {
            var userValidate = data.usernameValidate;
            var passwordNullValidate = data.passwordValidates[0];
            var passwordLengthValidate = data.passwordValidates[1];
            var passwordSpecialValidate = data.passwordValidates[2];
            var passwordUpperValidate = data.passwordValidates[3];
            var passwordLowerValidate = data.passwordValidates[4];
            var passwordNumberValidate = data.passwordValidates[5];
            
            // Validate inputs
            userValidate.style.display = ValidateUsername(data.username) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;
            
            passwordNullValidate.style.display = ValidateNullPassword(data.password) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;
            
            passwordLengthValidate.style.display = ValidateLengthPassword(data.password)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            
            passwordSpecialValidate.style.display = ValidateSpecialPassword(data.password)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            
            passwordUpperValidate.style.display = ValidateUppercasePassword(data.password)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            
            passwordLowerValidate.style.display = ValidateLowercasePassword(data.password)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            
            passwordNumberValidate.style.display = ValidateNumberPassword(data.password)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
            
            // Only proceed if all inputs are valid
            if (ValidateUsername(data.username) && ValidateNullPassword(data.password))
            {
                // Show loader when task start
                _loader.style.display = DisplayStyle.Flex;
                StartCoroutine(SignIn(data.remember, 
                    data.username.value, data.password.value));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="data"></param>
        private void SignInUIDisplaySignUpUIButtonHandler(ClickEvent evt, (Label usernameValidate, Label[] passwordValidates) data)
        {
            data.usernameValidate.style.display = DisplayStyle.None;
            var passwordNullValidate = data.passwordValidates[0];
            var passwordLengthValidate = data.passwordValidates[1];
            var passwordSpecialValidate = data.passwordValidates[2];
            var passwordUpperValidate = data.passwordValidates[3];
            var passwordLowerValidate = data.passwordValidates[4];
            var passwordNumberValidate = data.passwordValidates[5];
            passwordNullValidate.style.display = DisplayStyle.None;
            passwordLengthValidate.style.display = DisplayStyle.None;
            passwordSpecialValidate.style.display = DisplayStyle.None;
            passwordUpperValidate.style.display = DisplayStyle.None;
            passwordLowerValidate.style.display = DisplayStyle.None;
            passwordNumberValidate.style.display = DisplayStyle.None;
            ConfigureUIDisplay(_root, UIDisplay.SignUp);
        }

        #endregion

        #region Forgot Password Event Handlers

        /// <summary>
        /// Handles the reset password button click to initiate password reset process
        /// </summary>
        private void ForgotPasswordUIDisplayResetPasswordButtonHandler(ClickEvent evt, (TextField email, Label emailValidate) data)
        {
            if (ValidateEmail(data.email))
            {
                data.emailValidate.style.display = DisplayStyle.None;
                // Show loader when task start
                _loader.style.display = DisplayStyle.Flex;
                StartCoroutine(ResetPassword(data.email.value));
            }
            else
            {
                data.emailValidate.style.display = DisplayStyle.Flex;
            }
        }
        
        private void ForgotPasswordUIDisplayBackButtonHandler(ClickEvent evt,
            (TextField emailField, Label emailValidate) data)
        {
            data.emailField.value = string.Empty;
            data.emailValidate.style.display = DisplayStyle.None;
            ConfigureUIDisplay(_root, UIDisplay.SignIn);
        }

        #endregion

        #region Reset Password Event Handlers
        
        /// <summary>
        /// Handles the change password button click to confirm password change
        /// </summary>
        private void ResetPasswordUIDisplayChangePasswordButtonHandler(ClickEvent evt, 
            (TextField code, Label codeValidate, TextField password, Label[] passwordValidates, TextField confirmPassword,
                Label confirmPasswordValidate) data)
        {
            var forgotPassword = _root.Q<VisualElement>("forgot-password");
            var emailField = forgotPassword.Q<TextField>("email");
            // Get all validation message elements
            var codeValidate = data.codeValidate;
            var passwordNullValidate = data.passwordValidates[0];
            var passwordLengthValidate = data.passwordValidates[1];
            var passwordSpecialValidate = data.passwordValidates[2];
            var passwordUpperValidate = data.passwordValidates[3];
            var passwordLowerValidate = data.passwordValidates[4];
            var passwordNumberValidate = data.passwordValidates[5];
            var confirmPasswordValidate = data.confirmPasswordValidate;
            
            // Validate code
            codeValidate.style.display = ValidateCode(data.code) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;

            // Validate password requirements
            passwordNullValidate.style.display = ValidateNullPassword(data.password) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;

            passwordLengthValidate.style.display = ValidateLengthPassword(data.password) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;

            passwordSpecialValidate.style.display = ValidateSpecialPassword(data.password) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;

            passwordUpperValidate.style.display = ValidateUppercasePassword(data.password) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;

            passwordLowerValidate.style.display = ValidateLowercasePassword(data.password) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;

            passwordNumberValidate.style.display = ValidateNumberPassword(data.password) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;

            // Validate password confirmation
            if (ValidateConfirmPassword(data.password, data.confirmPassword))
            {
                confirmPasswordValidate.style.display = DisplayStyle.None;
                // Show loader when task start
                _loader.style.display = DisplayStyle.Flex;
                StartCoroutine(ChangePassword(emailField.value, data.code.value, data.password.value));
            }
            else
            {
                confirmPasswordValidate.style.display = DisplayStyle.Flex;
            }
        }

        private void ResetPasswordUIDisplayBackButtonHandler(ClickEvent evt,
            (TextField code, Label codeValidate, TextField password, Label[] passwordValidates, TextField
                confirmPassword,
                Label confirmPasswordValidate) data)
        {
            var forgotPassword = _root.Q<VisualElement>("forgot-password");
            var emailField = forgotPassword.Q<TextField>("email");
            // Get all validation message elements
            var codeValidate = data.codeValidate;
            var passwordNullValidate = data.passwordValidates[0];
            var passwordLengthValidate = data.passwordValidates[1];
            var passwordSpecialValidate = data.passwordValidates[2];
            var passwordUpperValidate = data.passwordValidates[3];
            var passwordLowerValidate = data.passwordValidates[4];
            var passwordNumberValidate = data.passwordValidates[5];
            var confirmPasswordValidate = data.confirmPasswordValidate;
            
            emailField.value = string.Empty;
            data.code.value = string.Empty;
            codeValidate.style.display = DisplayStyle.None;
            data.password.value = string.Empty;
            passwordNullValidate.style.display = DisplayStyle.None;
            passwordLengthValidate.style.display = DisplayStyle.None;
            passwordSpecialValidate.style.display = DisplayStyle.None;
            passwordUpperValidate.style.display = DisplayStyle.None;
            passwordLowerValidate.style.display = DisplayStyle.None;
            passwordNumberValidate.style.display = DisplayStyle.None;
            data.confirmPassword.value = string.Empty;
            confirmPasswordValidate.style.display = DisplayStyle.None;
            
            ConfigureUIDisplay(_root, UIDisplay.SignIn);
        }
        
        #endregion

        #region Sign Up Event Handlers

        /// <summary>
        /// Handles the sign-up button click to register a new user
        /// </summary>
        private void SignUpUIDisplaySignUpButtonHandler(ClickEvent evt, 
            (TextField email, Label emailValidate, TextField username, Label usernameValidate, TextField password, 
                Label[] passwordValidates, TextField confirmPassword, Label confirmPasswordValidate) data)
        {
            // Get all validation message elements
            var emailValidate = data.emailValidate;
            var usernameValidate = data.usernameValidate;
            var passwordNullValidate = data.passwordValidates[0];
            var passwordLengthValidate = data.passwordValidates[1];
            var passwordSpecialValidate = data.passwordValidates[2];
            var passwordUpperValidate = data.passwordValidates[3];
            var passwordLowerValidate = data.passwordValidates[4];
            var passwordNumberValidate = data.passwordValidates[5];
            var confirmPasswordValidate = data.confirmPasswordValidate;
            
            // Validate all inputs
            emailValidate.style.display = ValidateEmail(data.email) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;
                
            usernameValidate.style.display = ValidateUsername(data.username)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
                
            passwordNullValidate.style.display = ValidateNullPassword(data.password) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;
                
            passwordLengthValidate.style.display = ValidateLengthPassword(data.password) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;
                
            passwordSpecialValidate.style.display = ValidateSpecialPassword(data.password) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;
                
            passwordUpperValidate.style.display = ValidateUppercasePassword(data.password) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;
                
            passwordLowerValidate.style.display = ValidateLowercasePassword(data.password) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;
                
            passwordNumberValidate.style.display = ValidateNumberPassword(data.password) 
                ? DisplayStyle.None 
                : DisplayStyle.Flex;
                
            confirmPasswordValidate.style.display = ValidateConfirmPassword(data.password, data.confirmPassword)
                ? DisplayStyle.None 
                : DisplayStyle.Flex;
            
            // Only proceed if all validations pass
            bool allValid = ValidateEmail(data.email) &&
                           ValidateUsername(data.username) &&
                           ValidateNullPassword(data.password) &&
                           ValidateLengthPassword(data.password) &&
                           ValidateSpecialPassword(data.password) &&
                           ValidateUppercasePassword(data.password) &&
                           ValidateLowercasePassword(data.password) &&
                           ValidateNumberPassword(data.password) &&
                           ValidateConfirmPassword(data.password, data.confirmPassword);
                           
            if (allValid)
            {
                // Show loader when task start
                _loader.style.display = DisplayStyle.Flex;
                StartCoroutine(SignUp(data.username.value, data.password.value, data.email.value));
            }
        }

        private void SignUpUIDisplaySignInUIButtonHandler(ClickEvent evt, 
            (TextField email, Label emailValidate, TextField username, Label usernameValidate, TextField password, 
                Label[] passwordValidates, TextField confirmPassword, Label confirmPasswordValidate) data)
        {
            var emailValidate = data.emailValidate;
            var usernameValidate = data.usernameValidate;
            var passwordNullValidate = data.passwordValidates[0];
            var passwordLengthValidate = data.passwordValidates[1];
            var passwordSpecialValidate = data.passwordValidates[2];
            var passwordUpperValidate = data.passwordValidates[3];
            var passwordLowerValidate = data.passwordValidates[4];
            var passwordNumberValidate = data.passwordValidates[5];
            var confirmPasswordValidate = data.confirmPasswordValidate;
            
            data.email.value = string.Empty;
            emailValidate.style.display = DisplayStyle.None;
            data.username.value = string.Empty;
            usernameValidate.style.display = DisplayStyle.None;
            data.password.value = string.Empty;
            passwordNullValidate.style.display = DisplayStyle.None;
            passwordLengthValidate.style.display = DisplayStyle.None;
            passwordSpecialValidate.style.display = DisplayStyle.None;
            passwordUpperValidate.style.display = DisplayStyle.None;
            passwordLowerValidate.style.display = DisplayStyle.None;
            passwordNumberValidate.style.display = DisplayStyle.None;
            data.confirmPassword.value = string.Empty;
            confirmPasswordValidate.style.display = DisplayStyle.None;
            
            ConfigureUIDisplay(_root, UIDisplay.SignIn);
        }

        #endregion

        #region Confirm Account Event Handers

        /// <summary>
        /// Handles the resend code button click to send a new confirmation code
        /// </summary>
        private void ConfirmAccountUIDisplayResendCodeButtonHandler(ClickEvent evt)
        {
            var signUp = _root.Q<VisualElement>("sign-up");
            var usernameField = signUp.Q<TextField>("username");
            var usernameValidate = signUp.Q<Label>("username-validate");
            
            if (ValidateUsername(usernameField))
            {
                usernameValidate.style.display = DisplayStyle.None;
                StartCoroutine(ResendCode(usernameField.value));
            }
            else
            {
                usernameValidate.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// Handles the confirm account button click to verify a new account
        /// </summary>
        private void ConfirmAccountUIDisplayConfirmButtonHandler(ClickEvent evt, (TextField code, Label codeValidate) data)
        {
            var signUp = _root.Q<VisualElement>("sign-up");
            var usernameField = signUp.Q<TextField>("username");
            var usernameValidate = signUp.Q<Label>("username-validate");
            
            var codeValidate = data.codeValidate;
            
            bool isUsernameValid = ValidateUsername(usernameField);
            bool isCodeValid = ValidateCode(data.code);
            
            usernameValidate.style.display = isUsernameValid ? DisplayStyle.None : DisplayStyle.Flex;
            codeValidate.style.display = isCodeValid ? DisplayStyle.None : DisplayStyle.Flex;
            
            if (isUsernameValid && isCodeValid)
            {
                // Show loader when task start
                _loader.style.display = DisplayStyle.Flex;
                StartCoroutine(ConfirmAccount(usernameField.value, data.code.value));
            }
        }

        private void ConfirmAccountUIDisplayBackButtonHandler(ClickEvent evt, (TextField code, Label codeValidate) data)
        {
            var signUp = _root.Q<VisualElement>("sign-up");
            var usernameField = signUp.Q<TextField>("username");
            usernameField.value = string.Empty;
            var usernameValidate = signUp.Q<Label>("username-validate");
            usernameValidate.style.display = DisplayStyle.None;
            var emailField = signUp.Q<TextField>("email");
            emailField.value = string.Empty;
            var emailValidate = signUp.Q<Label>("email-validate");
            emailValidate.style.display = DisplayStyle.None;
            var passwordField = signUp.Q<TextField>("password");
            passwordField.value = string.Empty;
            var passwordNullValidate = signUp.Q<Label>("password-validate");
            passwordNullValidate.style.display = DisplayStyle.None;
            var passwordLengthValidate = signUp.Q<Label>("password-length-validate");
            passwordLengthValidate.style.display = DisplayStyle.None;
            var passwordSpecialValidate = signUp.Q<Label>("password-special-validate");
            passwordSpecialValidate.style.display = DisplayStyle.None;
            var passwordUpperValidate = signUp.Q<Label>("password-upper-validate");
            passwordUpperValidate.style.display = DisplayStyle.None;
            var passwordLowerValidate = signUp.Q<Label>("password-lower-validate");
            passwordLowerValidate.style.display = DisplayStyle.None;
            var passwordNumberValidate = signUp.Q<Label>("password-number-validate");
            passwordNumberValidate.style.display = DisplayStyle.None;
            var confirmPasswordField = signUp.Q<TextField>("confirm-password");
            confirmPasswordField.style.display = DisplayStyle.None;
            var confirmPasswordValidate = signUp.Q<Label>("confirm-password-validate");
            confirmPasswordValidate.style.display = DisplayStyle.None;
            
            var codeValidate = data.codeValidate;
            data.code.value = string.Empty;
            codeValidate.style.display = DisplayStyle.None;
            
            ConfigureUIDisplay(_root, UIDisplay.SignIn);
        }

        #endregion

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates that a username is not empty
        /// </summary>
        private bool ValidateUsername(TextField username)
        {
            return !string.IsNullOrEmpty(username.text);
        }

        /// <summary>
        /// Validates that a password is not empty
        /// </summary>
        private bool ValidateNullPassword(TextField password)
        {
            return !string.IsNullOrEmpty(password.text);
        }

        /// <summary>
        /// Validates that a password meets the minimum length requirement (8 characters)
        /// </summary>
        private bool ValidateLengthPassword(TextField password)
        {
            return Regex.IsMatch(password.value, @"^.{8,}$");
        }

        /// <summary>
        /// Validates that a password contains at least one number
        /// </summary>
        private bool ValidateNumberPassword(TextField password)
        {
            return Regex.IsMatch(password.value, @"(?=.*\d)");
        }

        /// <summary>
        /// Validates that a password contains at least one special character
        /// </summary>
        private bool ValidateSpecialPassword(TextField password)
        {
            return Regex.IsMatch(password.value, @"(?=.*[@$!%*?&])");
        }

        /// <summary>
        /// Validates that a password contains at least one uppercase letter
        /// </summary>
        private bool ValidateUppercasePassword(TextField password)
        {
            return Regex.IsMatch(password.value, @"(?=.*[A-Z])");
        }

        /// <summary>
        /// Validates that a password contains at least one lowercase letter
        /// </summary>
        private bool ValidateLowercasePassword(TextField password)
        {
            return Regex.IsMatch(password.value, @"(?=.*[a-z])");
        }

        /// <summary>
        /// Validates that a password and its confirmation match
        /// </summary>
        private bool ValidateConfirmPassword(TextField password, TextField confirmPassword)
        {
            return password.value == confirmPassword.value;
        }

        /// <summary>
        /// Validates that an email field contains a valid email address
        /// </summary>
        private bool ValidateEmail(TextField email)
        {
            return IsValidEmail(email.value);
        }

        /// <summary>
        /// Validates that a string is a valid email address format
        /// </summary>
        private bool IsValidEmail(string email)
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

        /// <summary>
        /// Validates that a confirmation code contains only digits
        /// </summary>
        private bool ValidateCode(TextField code)
        {
            return Regex.IsMatch(code.value, @"^\d+$");
        }

        #endregion

        #region Authentication Service Methods

        /// <summary>
        /// Signs in a user with Cognito and links with Unity authentication via OpenID Connect
        /// </summary>
        /// <param name="remember">Toggle indicating whether to remember login credentials</param>
        /// <param name="username">User's username or email</param>
        /// <param name="password">User's password</param>
        private IEnumerator SignIn(Toggle remember, string username, string password)
        {
            // Sign in with Cognito
            var task = _cognitoService.SignInAsync(username, password);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Log.Error($"Sign-in failed: {task.Exception?.Message}");
                yield break;
            }
            
            var result = task.Result;
            if (result != null)
            {
                // Save credentials if remember me is checked
                if (remember.value)
                {
                    RememberAccount.SaveAccountData(new RememberAccount.RememberData
                    {
                        active = remember.value,
                        username = username,
                        password = password
                    });
                }
                
                // Hide loader when task complete
                _loader.style.display = DisplayStyle.None;
                
                // Link with Unity authentication
                yield return _unityService.SignInWithOpenIdConnectAsync(OidcName, result.AuthenticationResult.IdToken);
            }
        }

        /// <summary>
        /// Initiates the password reset process by sending a confirmation code
        /// </summary>
        /// <param name="username">User's email or username</param>
        private IEnumerator ResetPassword(string username)
        {
            var task = _cognitoService.ForgotPasswordAsync(username);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Log.Error($"Reset password request failed: {task.Exception?.Message}");
                yield break;
            }
            
            var result = task.Result;
            if (result != null)
            {
                // Hide loader when task complete
                _loader.style.display = DisplayStyle.None;
                
                ConfigureUIDisplay(_root, UIDisplay.ResetPassword);
                
                var resetPassword = _root.Q<VisualElement>("reset-password");
                
                var informationLabel = resetPassword.Q<Label>("information-label");

                var sourceLabel = informationLabel.text;
                
                var resultLabel = Regex.Replace(sourceLabel, Pattern, match => $"[{username}]");
                
                informationLabel.text = resultLabel;
            }
        }

        /// <summary>
        /// Confirms a password reset using the verification code
        /// </summary>
        /// <param name="username">User's email or username</param>
        /// <param name="code">Verification code received by email</param>
        /// <param name="newPassword">New password to set</param>
        private IEnumerator ChangePassword(string username, string code, string newPassword)
        {
            var task = _cognitoService.ConfirmForgotPasswordAsync(username, code, newPassword);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Log.Error($"Change password failed: {task.Exception?.Message}");
                yield break;
            }
            
            var result = task.Result;
            if (result != null)
            {
                var forgotPassword = _root.Q<VisualElement>("forgot-password");
                var emailField = forgotPassword.Q<TextField>("email");
                emailField.value = string.Empty;
                
                var resetPassword = _root.Q<VisualElement>("reset-password");
                var codeField = resetPassword.Q<TextField>("code");
                codeField.value = string.Empty;
                var passwordField = resetPassword.Q<TextField>("password");
                passwordField.value = string.Empty;
                var confirmPassword = resetPassword.Q<TextField>("confirm-password");
                confirmPassword.value = string.Empty;
                
                // Hide loader when task complete
                _loader.style.display = DisplayStyle.None;
                
                ConfigureUIDisplay(_root, UIDisplay.SignIn);
            }
        }

        /// <summary>
        /// Registers a new user account
        /// </summary>
        /// <param name="username">Desired username</param>
        /// <param name="password">User's password</param>
        /// <param name="email">User's email address</param>
        private IEnumerator SignUp(string username, string password, string email)
        {
            var task = _cognitoService.SignUpAsync(username, password, email);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Log.Error($"Sign-up failed: {task.Exception?.Message}");
                yield break;
            }
            
            var result = task.Result;
            if (result != null)
            {
                // Hide loader when task complete
                _loader.style.display = DisplayStyle.None;
                
                ConfigureUIDisplay(_root, UIDisplay.ConfirmAccount);
                
                var confirmAccount = _root.Q<VisualElement>("confirm-account");
                var informationLabel = confirmAccount.Q<Label>("information-label");
                var sourceLabel = informationLabel.text;
                var resultLabel = Regex.Replace(sourceLabel, Pattern, match => $"[{username}]");
                informationLabel.text = resultLabel;
            }
        }

        /// <summary>
        /// Resends the account confirmation code to the user's email
        /// </summary>
        /// <param name="username">User's username</param>
        private IEnumerator ResendCode(string username)
        {
            var task = _cognitoService.ResendConfirmationCodeAsync(username);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Log.Error($"Resend code failed: {task.Exception?.Message}");
            }
        }

        /// <summary>
        /// Confirms a user account using the verification code
        /// </summary>
        /// <param name="username">User's username</param>
        /// <param name="code">Verification code received by email</param>
        private IEnumerator ConfirmAccount(string username, string code)
        {
            var task = _cognitoService.ConfirmSignUpAsync(username, code);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Log.Error($"Account confirmation failed: {task.Exception?.Message}");
                yield break;
            }
            
            var result = task.Result;
            if (result != null)
            {
                Log.Debug("Account successfully confirmed");
                var signUp = _root.Q<VisualElement>("sign-up");
                var emailField = signUp.Q<TextField>("email");
                emailField.value = string.Empty;
                var usernameField = signUp.Q<TextField>("username");
                usernameField.value = string.Empty;
                var passwordField = signUp.Q<TextField>("password");
                passwordField.value = string.Empty;
                var confirmPassword = signUp.Q<TextField>("confirm-password");
                confirmPassword.value = string.Empty;
                
                var confirmAccount = _root.Q<VisualElement>("confirm-account");
                var codeField = confirmAccount.Q<TextField>("code");
                codeField.value = string.Empty;
                
                // Hide loader when task complete
                _loader.style.display = DisplayStyle.None;
                
                ConfigureUIDisplay(_root, UIDisplay.SignIn);
            }
        }

        #endregion
    }
}