using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SmartReplace.Scripts.Editor
{
    /// <summary>
    /// Encapsulates a variable of a component referencing the object being replaced.
    /// Will be executed after replacement to update the reference to the replaced object / component.
    /// </summary>
    public abstract class ExternalReference
    {
        public delegate GameObject GetReplacementFor(int objectID);

        private readonly GetReplacementFor GetInstance; //Returns the replaced object that previously had the given id
        private Component referencingComponent; //The component that stores a reference to the object being replaced
        private Type referencingComponentType;

        protected GameObject ReferencedObject => GetInstance(ReferencedObjectID);

        public int ReferencedObjectID;
        public int SourceObjectID;

        public Component ReferencingComponentInstance
        {
            set
            {
                referencingComponent = value;
                referencingComponentType = referencingComponent.GetType();
            }
            get
            {
                // Gameobject with referencing component might itself have been replaced
                if (referencingComponent == null || referencingComponent.gameObject == null)
                {
                    var sourceObject = GetInstance(SourceObjectID);
                    referencingComponent =
                        GameObjectHelper.GetComponentInAllChildren(sourceObject, referencingComponentType);
                }

                return referencingComponent;
            }
        }

        // Writes a value into a component's field
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
                try
                {
                    field.SetValue(source, value);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                    Debug.LogError($"Failed to set value {value}");
                }
            }

            // Set dirty so the value doesn't get lost on play
            EditorUtility.SetDirty(source);
        }

        public FieldInfo ReferencingFieldInSource;

        public bool IsList;
        public int IndexInList;

        public bool IsActivated = true;

        public virtual void UpdateReference()
        {
            //Undo.RecordObject(ReferencingComponentInstance, "Updating external references");
        }

        public ExternalReference(GetReplacementFor getReplacementFor)
        {
            GetInstance = getReplacementFor;
        }
    }
}