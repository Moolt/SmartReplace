using UnityEngine;

namespace SmartReplace.Scripts.Editor
{
    /// <summary>
    /// Manages snapshots of the UI and outputs a summary of all changes that were made
    /// </summary>
    public class WindowValuesChangeManager
    {
        private readonly WindowValuesChangeSummary changes;

        private WindowValuesChangeSnapshot oldSnap;
        private WindowValuesChangeSnapshot newSnap;

        public WindowValuesChangeManager()
        {
            changes = new WindowValuesChangeSummary();
            oldSnap = new WindowValuesChangeSnapshot();
            newSnap = new WindowValuesChangeSnapshot();
        }

        public WindowValuesChangeSummary UpdateSnapshot(
            GameObject freshPrefab,
            GameObject brokenPrefab,
            bool showSimilar,
            bool byName,
            bool byComponents,
            bool multiScene)
        {
            newSnap.Update(freshPrefab, brokenPrefab, showSimilar, byName, byComponents, multiScene);
            changes.ObtainChanges(oldSnap, newSnap);
            oldSnap = newSnap;
            return changes;
        }
    }
}