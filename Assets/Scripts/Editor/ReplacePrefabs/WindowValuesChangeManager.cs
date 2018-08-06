using UnityEngine;

namespace ReplacePrefab
{
    /// <summary>
    /// Manages snapshots of the UI and outputs a summary of all changes that were made
    /// </summary>
    public class WindowValuesChangeManager
    {
        private WindowValuesChangeSummary changes;
        private WindowValuesChangeSnapshop oldSnap;
        private WindowValuesChangeSnapshop newSnap;

        public WindowValuesChangeManager()
        {
            changes = new WindowValuesChangeSummary();
            oldSnap = new WindowValuesChangeSnapshop();
            newSnap = new WindowValuesChangeSnapshop();
        }

        public WindowValuesChangeSummary UpdateSnapshot(GameObject freshPrefab, GameObject brokenPrefab, bool showSimilar, bool byName, bool byComponents, bool multiscene)
        {
            newSnap.Update(freshPrefab, brokenPrefab, showSimilar, byName, byComponents, multiscene);
            changes.ObtainChanges(oldSnap, newSnap);
            oldSnap = newSnap;
            return changes;
        }
    }
}