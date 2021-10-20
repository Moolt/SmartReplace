namespace SmartReplace.Scripts.Editor
{
    /// <summary>
    /// Summarizes any changes made by the user on the UI by comparing two snapshots
    /// </summary>
    public class WindowValuesChangeSummary
    {
        public bool ShowSimilarObjectsChanged;
        public bool SimilarObjectCriteriaChanged;
        public bool PrefabsChanged;

        public WindowValuesChangeSummary ObtainChanges(
            WindowValuesChangeSnapshot oldSnapshot,
            WindowValuesChangeSnapshot newSnapshot)
        {
            ShowSimilarObjectsChanged = oldSnapshot.ShowSimilarObjects != newSnapshot.ShowSimilarObjects;
            var byNameChanged = oldSnapshot.SearchByName != newSnapshot.SearchByName;
            var byComponentChanged = oldSnapshot.SearchByComponents != newSnapshot.SearchByComponents;
            var multiSceneChanged = oldSnapshot.EnableMultiScene != newSnapshot.EnableMultiScene;
            SimilarObjectCriteriaChanged = byNameChanged || byComponentChanged || multiSceneChanged;
            var oldPrefabChanged = oldSnapshot.BrokenPrefab != newSnapshot.BrokenPrefab;
            var newPrefabChanged = oldSnapshot.FreshPrefab != newSnapshot.FreshPrefab;
            PrefabsChanged = oldPrefabChanged || newPrefabChanged;
            return this;
        }
    }
}