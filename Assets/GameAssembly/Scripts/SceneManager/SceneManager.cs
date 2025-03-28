using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace GameAssembly.Scripts.SceneManager
{
    public class SceneManager : MonoBehaviour
    {
        public UIDocument document;
        public float loadingProgress = 0f;
        public SceneGroup sceneGroup;
        
        private VisualElement _root;
        private ProgressBar _loadingBar;
        
        private const float MinProgress = 0f;
        private const float MaxProgress = 100f;

        private void OnEnable()
        {
            _root = document.rootVisualElement;
            _loadingBar = _root.Q<ProgressBar>("LoadingBar");
            //_loadingBar.style.display = DisplayStyle.None;
            _loadingBar.value = loadingProgress;
            _loadingBar.lowValue = MinProgress;
            _loadingBar.highValue = MaxProgress;
        }

        private void Awake()
        {
            // var labelReferences = sceneGroup.Data[0].labels;
            // var sceneCount = 0;
            // foreach (var label in labelReferences)
            // {
            //     Addressables.LoadSceneAsync(label, LoadSceneMode.Additive).Completed += (asyncOperationHandle) =>
            //     {
            //         if (asyncOperationHandle.Status != AsyncOperationStatus.Succeeded) return;
            //         sceneCount++;
            //         _loadingBar.value = Mathf.Clamp(((float)sceneCount / labelReferences.Count)*100f,0f,100f);
            //     };
            // }
        }
    }
}
