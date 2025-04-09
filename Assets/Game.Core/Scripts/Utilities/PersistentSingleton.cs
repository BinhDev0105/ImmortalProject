using UnityEngine.Serialization;

namespace Game.Core.Runtime.Scripts.Utility
{
    using UnityEngine;

    public class PersistentSingleton<T> : MonoBehaviour where T : Component {
        [Tooltip("if this is true, this singleton will auto detach if it finds itself parented on awake")]
        public bool unParentOnAwake = true;

        public static bool HasInstance => _instance != null;
        public static T Current => _instance;

        private static T _instance;

        public static T Instance {
            get {
                if (_instance == null) {
                    _instance = FindFirstObjectByType<T>();
                    if (_instance == null) {
                        GameObject obj = new GameObject
                        {
                            name = typeof(T).Name + "AutoCreated"
                        };
                        _instance = obj.AddComponent<T>();
                    }
                }

                return _instance;
            }
        }

        private void Awake() => InitializeSingleton();

        private void InitializeSingleton() {
            if (!Application.isPlaying) {
                return;
            }

            if (unParentOnAwake) {
                transform.SetParent(null);
            }

            if (_instance == null) {
                _instance = this as T;
                DontDestroyOnLoad(transform.gameObject);
                enabled = true;
            } else {
                if (this != _instance) {
                    Destroy(this.gameObject);
                }
            }
        }
    }
}