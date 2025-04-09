using System;
using System.Threading.Tasks;
using Unity.Logging;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace Game.Core.Scripts.OnlineService.Authentication
{
    /// <summary>
    /// Service class for handling unity authentication operations.
    /// Provides methods for user management, authentication, and session handling.
    /// </summary>
    public class UnityService
    {
        public UnityService()
        {
        }

        /// <summary>
        /// Start unity service
        /// </summary>
        public async void InitializeAsync()
        {
            try
            {
                await UnityServices.InitializeAsync();
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Log.Error(e);
#endif
            }
        }
        
        /// <summary>
        /// Sign in with unity openid 
        /// </summary>
        /// <param name="idProviderName">OICD</param>
        /// <param name="idToken">User token</param>
        public async Task SignInWithOpenIdConnectAsync(string idProviderName, string idToken)
        {
            try
            {
                await AuthenticationService.Instance.SignInWithOpenIdConnectAsync(idProviderName, idToken);
#if UNITY_EDITOR
                Log.Debug("SignIn is successful.");
#endif
            }
            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message
#if UNITY_EDITOR
                Log.Error(ex);
#endif
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
#if UNITY_EDITOR
                Log.Error(ex);
#endif
            }
        }
    }
}