using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameAssembly.Scripts.UnityServiceManager.Lobby
{
    public class LobbyManager : MonoBehaviour
    {
        public UIDocument document;
        private VisualElement _root;
        private Label _username;

        private void OnEnable()
        {
            _root = document.rootVisualElement;
            _username = _root.Q<Label>("Username");
        }

        private async void Awake()
        {
            try
            {
                await GetUsersName("username");
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogException(e);
#endif
            }
        }

        private async Task GetUsersName(string key)
        {
            try
            {
                var hashSet = new HashSet<string> { key };
                var results = await CloudSaveService.Instance.Data.Player.LoadAsync(
                    hashSet
                );
                results.TryGetValue(key, out var item);
                if (item != null) _username.text = item.Value.GetAsString();
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogException(e);
#endif
            }
        }
    }
}
