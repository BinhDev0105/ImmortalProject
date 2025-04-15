using System;
using System.Collections.Generic;
using System.Linq;
using Eflatun.SceneReference;

// ReSharper disable InconsistentNaming

namespace Game.Core.Scripts.GameSceneManager
{
    [Serializable]
    public class SceneGroup
    {
        public string GroupName = "New Scene Group";
        public List<SceneData> Scenes;
        
        public string FindSceneByType(SceneType sceneType)
            => Scenes.FirstOrDefault(scene => scene.SceneType == sceneType)?.Reference.Name;
    }
    
    [Serializable]
    public class SceneData
    {
        public SceneReference Reference;
        public string Name => Reference.Name;
        public SceneType SceneType;
    }
        
    public enum SceneType
    {
        Active = 0,
        Authentication = 1,
        MainMenu = 2,
        UI = 3,
        HUB = 4,
        Cinematic = 5,
        Environment = 6,
    }
}