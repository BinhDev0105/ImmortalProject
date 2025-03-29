using System;
using System.Collections.Generic;
using GameUtilities.Runtime.Collection;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace GameAssembly.Scripts.SceneManager
{
    [CreateAssetMenu(fileName = "NewSceneGroup", menuName = "GameAssembly/SceneManager/SceneGroup", order = 0)]
    [Serializable]
    public class SceneGroup : ScriptableObject
    {
        [DictionaryDrawer]
        public SerializableDictionary<int, SceneGroupData> playerAttributes;
    }

    [Serializable]
    public class SceneGroupData
    {
        public List<AssetLabelReference> list;
        public int a;
        public float b;
    }

    [Serializable]
    public enum SceneGroupType
    {
        AuthenticationScene = 0,
        LobbyScene = 1,
    }
}