using System;
using System.Collections.Generic;

namespace ReplacePrefab
{
    public class TransferableType
    {
        public Type ComponentType;
        public bool IsActivated;
        public List<TransferableField> Fields;
        public bool IsExpanded = false;
    }
}