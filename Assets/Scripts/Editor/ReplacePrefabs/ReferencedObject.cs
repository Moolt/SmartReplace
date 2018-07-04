using UnityEngine;

namespace ReplacePrefab
{
    public class ReferencedObject : ExternalReference
    {
        public ReferencedObject(GetReplacementFor getReplacementFor) : base(getReplacementFor)
        {
        }

        public override void UpdateReference()
        {
            base.UpdateReference();
            SetValueFor<GameObject>(ReferencingComponentInstance, ReferencingFieldInSource, ReferencedObject, IsList, IndexInList);
        }
    }
}