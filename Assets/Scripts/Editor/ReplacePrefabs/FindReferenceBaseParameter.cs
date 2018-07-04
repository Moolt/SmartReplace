using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ReplacePrefab
{
    public struct FindReferenceBaseParameter
    {
        public List<GameObject> TargetObjects;
        public Component ComponentWithReference;
        public FieldInfo ReferenceOnTarget;
        public bool IsList;
    }
}