﻿using System.Reflection;

namespace SmartReplace.Scripts.Editor
{
    /// <summary>
    /// Encapsulates a component's field
    /// Will also be displayed on the UI and later used for replacement.
    /// </summary>
    public class TransferableField
    {
        public FieldInfo Field;
        public bool IsActivated;
    }
}