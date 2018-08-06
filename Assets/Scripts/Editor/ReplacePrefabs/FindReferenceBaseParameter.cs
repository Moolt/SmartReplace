using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ReplacePrefab
{
    /// <summary>
    /// Used for the search of external references and stores common information needed for references on
    /// GameObject, Component, List<GameObject>, List<Component> etc.
    /// </summary>
    public struct FindReferenceBaseParameter
    {
        public List<GameObject> TargetObjects;
        public Component ComponentWithReference;
        public FieldInfo ReferenceOnTarget;
        public bool IsList;
    }
}