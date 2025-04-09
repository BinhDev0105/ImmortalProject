using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Game.Core.Runtime.Scripts.GameSceneManager
{
    public class SceneGroupManager
    {
        public event Action<string> OnSceneLoaded = delegate { };
        public event Action<string> OnSceneUnloaded = delegate { };
        public event Action OnSceneGroupLoaded = delegate { };
        
        public readonly AsyncOperationHandleGroup HandleGroup = new AsyncOperationHandleGroup(10);
        
        public SceneGroup ActiveSceneGroup;

        public async Task LoadSceneAsync(SceneGroup sceneGroup, IProgress<float> progress, bool reloadDuplicate = false)
        {
            ActiveSceneGroup = sceneGroup;
            var loadedScenes = new List<string>();
            
            await UnloadSceneAsync();
            
            int sceneCount = SceneManager.sceneCount;

            for (int i = 0; i < sceneCount; i++)
            {
                loadedScenes.Add(SceneManager.GetSceneAt(i).name);
            }
            
            var totalSceneToLoad = ActiveSceneGroup.Scenes.Count;
            
            var operationGroup = new AsyncOperationGroup(totalSceneToLoad);

            for (int i = 0; i < totalSceneToLoad; i++)
            {
                var sceneData = sceneGroup.Scenes[i];
                if (reloadDuplicate == false && loadedScenes.Contains(sceneData.Name))
                    continue;

                if (sceneData.Reference.State == SceneReferenceState.Regular)
                {
                    var operation = SceneManager.LoadSceneAsync(sceneData.Reference.Path, LoadSceneMode.Additive);
                    operationGroup.Operations.Add(operation);
                }
                else if (sceneData.Reference.State == SceneReferenceState.Addressable)
                {
                    var operationHandle = Addressables.LoadSceneAsync(sceneData.Reference.Path, LoadSceneMode.Additive);
                    HandleGroup.Handles.Add(operationHandle);
                }
                
                OnSceneLoaded?.Invoke(sceneData.Name);
            }

            while (!operationGroup.IsDone || !HandleGroup.IsDone)
            {
                progress?.Report((operationGroup.Progress + HandleGroup.Progress)/2f);
                await Task.Delay(100);
            }
            
            Scene activeScene = SceneManager.GetSceneByName(ActiveSceneGroup.FindSceneByType(SceneType.Active));

            if (activeScene.IsValid())
            {
                SceneManager.SetActiveScene(activeScene);
            }
            
            OnSceneGroupLoaded?.Invoke();
        }

        public async Task UnloadSceneAsync()
        {
            var scenes = new List<string>();
            var activeScene = SceneManager.GetActiveScene().name;
            
            int sceneCount = SceneManager.sceneCount;

            for (int i = sceneCount - 1; i > 0; i--)
            {
                var sceneAt = SceneManager.GetSceneAt(i);
                if (!sceneAt.isLoaded)
                    continue;
                
                var sceneName = sceneAt.name;
                if (sceneName.Equals(activeScene) || sceneName == "Bootstrapper")
                    continue;
                if (HandleGroup.Handles.Any(h => h.IsValid() && h.Result.Scene.name == sceneName))
                    continue;
                scenes.Add(sceneName);
            }
            
            var operationGroup = new AsyncOperationGroup(scenes.Count);

            foreach (var scene in scenes)
            {
                var operation = SceneManager.UnloadSceneAsync(scene);
                if (operation == null)
                    continue;
                
                operationGroup.Operations.Add(operation);
                
                OnSceneUnloaded?.Invoke(scene);
            }

            foreach (var handle in HandleGroup.Handles)
            {
                if (handle.IsValid())
                {
                    Addressables.UnloadSceneAsync(handle);
                }
            }
            HandleGroup.Handles.Clear();

            while (!operationGroup.IsDone)
            {
                await Task.Delay(100);
            }
            
            await Resources.UnloadUnusedAssets();
        }
    }

    public readonly struct AsyncOperationGroup
    {
        public readonly List<AsyncOperation> Operations;

        public float Progress => Operations.Count == 0 ? 0 : Operations.Average(op => op.progress);
        public bool IsDone => Operations.All(op => op.isDone);
        
        public AsyncOperationGroup(int initialCapacity)
        {
            Operations = new List<AsyncOperation>(initialCapacity);
        }
    }

    public readonly struct AsyncOperationHandleGroup
    {
        public readonly List<AsyncOperationHandle<SceneInstance>> Handles;

        public float Progress => Handles.Count == 0 ? 0 : Handles.Average(h => h.PercentComplete);
        public bool IsDone => Handles.Count == 0 || Handles.All(h => h.IsDone);

        public AsyncOperationHandleGroup(int initialCapacity)
        {
            Handles = new List<AsyncOperationHandle<SceneInstance>>(initialCapacity);
        }
    }
}