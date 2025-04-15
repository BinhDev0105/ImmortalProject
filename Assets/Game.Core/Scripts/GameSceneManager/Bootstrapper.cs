using Game.Core.Runtime.Scripts.Utility;
using Unity.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core.Scripts.GameSceneManager
{
    public class Bootstrapper : PersistentSingleton<Bootstrapper>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static async void Init()
        {
#if UNITY_EDITOR
            Log.Debug("Bootstrapper...");
#endif
            await SceneManager.LoadSceneAsync("Bootstrapper", LoadSceneMode.Single);
        }
    }
}