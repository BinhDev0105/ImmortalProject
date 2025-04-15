using System;
using System.Threading.Tasks;
using Game.Core.Scripts.GameSceneManager;
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
                Log.Error(e);
            }
        }
        
        /// <summary>
        /// Sign in with unity openid 
        /// </summary>
        /// <param name="idProviderName">OIDC</param>
        /// <param name="idToken">User token</param>
        public async Task<bool> SignInWithOpenIdConnectAsync(string idProviderName, string idToken)
        {
            try
            {
                await AuthenticationService.Instance.SignInWithOpenIdConnectAsync(idProviderName, idToken);

                await SceneLoader.Instance.LoadSceneGroup(1);
                
                return true;
            }
            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message

                Log.Error(ex);
                return false;
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                Log.Error(ex);
                return false;
            }
        }
    }
}