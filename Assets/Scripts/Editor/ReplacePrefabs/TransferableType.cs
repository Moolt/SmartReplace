using System;
using System.Collections.Generic;

namespace ReplacePrefab
{
    /// <summary>
    /// A component with transferable values.
    /// Will also be displayed on the UI and later used for replacement.
    /// </summary>
    public class TransferableType
    {
        public Type ComponentType;
        public bool IsActivated;
        public List<TransferableField> Fields;
        public bool IsExpanded = false;
    }
}