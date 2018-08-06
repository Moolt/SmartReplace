using System;
using UnityEngine;

namespace ReplacePrefab
{
    /// <summary>
    /// Will restore a reference to a component on a replaced object
    /// </summary>
    public class ReferencingComponent : ExternalReference
    {
        public Type ReferencedComponentType;

        public ReferencingComponent(GetReplacementFor getReplacementFor) : base(getReplacementFor)
        {
        }

        public override void UpdateReference()
        {
            base.UpdateReference();
            var referencedComponent = GameObjectHelper.GetComponentInAllChildren(ReferencedObject, ReferencedComponentType);
            SetValueFor<Component>(ReferencingComponentInstance, ReferencingFieldInSource, referencedComponent, IsList, IndexInList);
        }
    }
}