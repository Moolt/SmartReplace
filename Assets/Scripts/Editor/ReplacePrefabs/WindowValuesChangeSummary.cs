namespace ReplacePrefab
{
    /// <summary>
    /// Summarizes any changes made by the user on the UI by comparing two snapshots
    /// </summary>
    public class WindowValuesChangeSummary
    {
        public bool ShowSimilarObjectsChanged = false;
        public bool SimilarObjectCriteraChanged = false;
        public bool PrefabsChanged = false;

        public WindowValuesChangeSummary ObtainChanges(WindowValuesChangeSnapshop oldSnapshot, WindowValuesChangeSnapshop newSnapshot)
        {
            ShowSimilarObjectsChanged = oldSnapshot.ShowSimilarObjects != newSnapshot.ShowSimilarObjects;
            bool byNameChanged = oldSnapshot.SearchByName != newSnapshot.SearchByName;
            bool byComponentChanged = oldSnapshot.SearchByComponents != newSnapshot.SearchByComponents;
            bool multisceneChanged = oldSnapshot.EnableMultiscene != newSnapshot.EnableMultiscene;
            SimilarObjectCriteraChanged = byNameChanged || byComponentChanged || multisceneChanged;
            bool oldPrefabChanged = oldSnapshot.BrokenPrefab != newSnapshot.BrokenPrefab;
            bool newPrefabChanged = oldSnapshot.FreshPrefab != newSnapshot.FreshPrefab;
            PrefabsChanged = oldPrefabChanged || newPrefabChanged;
            return this;
        }
    }
}