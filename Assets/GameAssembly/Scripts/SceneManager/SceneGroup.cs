using System;
using System.Collections.Generic;
using GameUtilities.Runtime.Collection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using SerializedScriptableObject = GameUtilities.Runtime.SerializedScriptableObject;

namespace GameAssembly.Scripts.SceneManager
{
    [CreateAssetMenu(fileName = "NewSceneGroup", menuName = "GameAssembly/SceneManager/SceneGroup", order = 0)]
    [Serializable]
    public class SceneGroup : ScriptableObject
    {
        [DictionaryDrawer]
        public GameUtilities.Runtime.Collection.Dictionary<int, SceneGroupData> playerAttributes;
    }

    [Serializable]
    public class SceneGroupData
    {
        public List<AssetLabelReference> list;
    }

    [Serializable]
    public enum SceneGroupType
    {
        AuthenticationScene = 0,
        LobbyScene = 1,
    }
}