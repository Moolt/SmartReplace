using UnityEngine;

namespace ReplacePrefab
{
    /// <summary>
    /// Will restore a reference to a replaced object
    /// </summary>
    public class ReferencedObject : ExternalReference
    {
        public ReferencedObject(GetReplacementFor getReplacementFor) : base(getReplacementFor)
        {
        }

        public override void UpdateReference()
        {
            base.UpdateReference();
            //Value will always be the parent object
            SetValueFor<GameObject>(ReferencingComponentInstance, ReferencingFieldInSource, ReferencedObject, IsList, IndexInList);
        }
    }
}