using UnityEngine;

namespace SmartReplace.Scripts.Editor
{
    /// <summary>
    /// Represents all settings made by the user on the UI at a specific point of time
    /// </summary>
    public struct WindowValuesChangeSnapshot
    {
        public GameObject FreshPrefab;
        public GameObject BrokenPrefab;
        public bool ShowSimilarObjects;
        public bool SearchByName;
        public bool SearchByComponents;
        public bool EnableMultiScene;

        public WindowValuesChangeSnapshot Update(
            GameObject freshPrefab, 
            GameObject brokenPrefab, 
            bool showSimilar,
            bool byName, 
            bool byComponents, 
            bool multiScene)
        {
            FreshPrefab = freshPrefab;
            BrokenPrefab = brokenPrefab;
            ShowSimilarObjects = showSimilar;
            SearchByName = byName;
            SearchByComponents = byComponents;
            EnableMultiScene = multiScene;
            return this;
        }
    }
}