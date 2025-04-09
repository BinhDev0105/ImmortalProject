using Game.Core.Runtime.Scripts.Utility;
using Unity.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core.Runtime.Scripts.GameSceneManager
{
    public class Bootstrapper : PersistentSingleton<Bootstrapper>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static async void Init()
        {
            Log.Debug("Bootstrapper...");
            await SceneManager.LoadSceneAsync("Bootstrapper", LoadSceneMode.Single);
        }
    }
}