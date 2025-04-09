using System;
using System.Threading.Tasks;
using Unity.Logging;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable InconsistentNaming

namespace Game.Core.Runtime.Scripts.GameSceneManager
{
    public class SceneLoader : MonoBehaviour
    {
        public UIDocument UIDocument;
        public float fillSpeed = 0.5f;
        public SceneGroup[] SceneGroups;
        
        private ProgressBar _loadingBar;
        private bool _isLoading;

        private readonly SceneGroupManager _manager = new SceneGroupManager();

        private void OnEnable()
        {
            if (UIDocument == null)
            {
                UIDocument = GetComponent<UIDocument>();
            }

            _loadingBar = UIDocument.rootVisualElement.Q<ProgressBar>("loading-bar");
            Log.Debug("Loading scene groups...");
            Log.Debug($"High value: {_loadingBar.highValue}");
        }

        private void Awake()
        {
            _manager.OnSceneLoaded += sceneName => Log.Debug($"Load scene {sceneName}");
            _manager.OnSceneUnloaded += sceneName => Log.Debug($"Unload scene {sceneName}");
            _manager.OnSceneGroupLoaded += () => Log.Debug($"Scene group loaded");
        }

        private async void Start()
        {
            try
            {
                await LoadSceneGroup(0);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void Update()
        {
            if (!_isLoading) 
                return;
            
            float currentValue = _loadingBar.value;
            float valueDifference = Mathf.Abs(currentValue - _loadingBar.highValue);
            
            float dynamicFillSpeed = valueDifference * fillSpeed;
            
            _loadingBar.value = Mathf.Lerp(currentValue, _loadingBar.highValue, dynamicFillSpeed * Time.deltaTime);
            _loadingBar.title = $"{_loadingBar.value*100:F0}%";
            _loadingBar.style.color = _loadingBar.value >= _loadingBar.highValue / 2 ? Color.white : Color.black;
        }

        private async Task LoadSceneGroup(int index)
        {
            _loadingBar.value = 0;
            _loadingBar.highValue = 1f;
            if (index < 0 || index >= SceneGroups.Length)
            {
                Log.Error($"Invalid scene group index: {index}");
                return;
            }
            
            LoadingProgress progress = new LoadingProgress();
            progress.ProgressChanged += target => _loadingBar.highValue = Mathf.Max(target, _loadingBar.highValue);
            
            EnableLoadingBar();
            await _manager.LoadSceneAsync(SceneGroups[index], progress);
            EnableLoadingBar(false);
        }

        private void EnableLoadingBar(bool enable = true)
        {
            _isLoading = enable;
            _loadingBar.visible = enable;
        }
    }

    public class LoadingProgress : IProgress<float>
    {
        public event Action<float> ProgressChanged;

        private const float radio = 1f;

        public void Report(float value)
         => ProgressChanged?.Invoke(value/radio);
    }
}