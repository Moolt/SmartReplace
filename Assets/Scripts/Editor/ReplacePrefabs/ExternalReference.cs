using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ReplacePrefab
{
    public abstract class ExternalReference
    {
        public delegate GameObject GetReplacementFor(int objectID);
        private GetReplacementFor GetInstance;
        private Component referencingComponent;
        private Type referencingComponentType;

        protected GameObject ReferencedObject => GetInstance(ReferencedObjectID);

        public int ReferencedObjectID;
        public int SourceObjectID;

        public Component ReferencingComponentInstance {
            set {
                referencingComponent = value;
                referencingComponentType = referencingComponent.GetType();
            }
            get {
                //Gameobject with referencing component might itself have been replaced
                if (referencingComponent == null || referencingComponent.gameObject == null)
                {
                    var sourceObject = GetInstance(SourceObjectID);
                    referencingComponent = GameObjectHelper.GetComponentInAllChildren(sourceObject, referencingComponentType);
                }
                return referencingComponent;
            }
        }

        protected void SetValueFor<T>(Component source, FieldInfo field, T value, bool isList, int index)
        {
            if (isList)
            {
                var fieldType = field.FieldType;

                if (fieldType.IsArray)
                {
                    var array = field.GetValue(source) as Array;
                    array.SetValue(value, index);
                }
                else
                {
                    var list = field.GetValue(source);
                    var count = (int)fieldType.GetProperty("Count").GetValue(list);
                    var propertyItemInfo = fieldType.GetProperty("Item");

                    propertyItemInfo.SetValue(list, value, new object[] { index });
                }
            }
            else
            {
                field.SetValue(source, value);
            }
        }

        public FieldInfo ReferencingFieldInSource;

        public bool IsList;
        public int IndexInList;

        public bool IsActivated = true;

        public virtual void UpdateReference()
        {
            Undo.RecordObject(ReferencingComponentInstance, "Updating external references");
        }

        public ExternalReference(GetReplacementFor getReplacementFor)
        {
            GetInstance = getReplacementFor;
        }
    }
}