using UnityEngine;

namespace ReplacePrefab
{
    /// <summary>
    /// Represents all settings made by the user on the UI at a specific point of time
    /// </summary>
    public struct WindowValuesChangeSnapshop
    {
        public GameObject FreshPrefab;
        public GameObject BrokenPrefab;
        public bool ShowSimilarObjects;
        public bool SearchByName;
        public bool SearchByComponents;
        public bool EnableMultiscene;

        public WindowValuesChangeSnapshop Update(GameObject freshPrefab, GameObject brokenPrefab, bool showSimilar, bool byName, bool byComponents, bool multiscene)
        {
            FreshPrefab = freshPrefab;
            BrokenPrefab = brokenPrefab;
            ShowSimilarObjects = showSimilar;
            SearchByName = byName;
            SearchByComponents = byComponents;
            EnableMultiscene = multiscene;
            return this;
        }
    }
}