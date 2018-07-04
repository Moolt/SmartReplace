using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ReplacePrefab
{
    public static class GameObjectHelper
    {
        public static List<GameObject> ObjectHierarchyFor(GameObject target)
        {
            return GetAllChildrenOf(target, true);
        }

        public static List<Component> ComponentHierarchyFor(GameObject target)
        {
            return GetComponentsInAllChildren<Component>(target);
        }

        public static List<GameObject> GetAllChildrenOf(GameObject parent, bool includeParent)
        {
            var children = new List<GameObject>();

            if (includeParent)
            {
                children.Add(parent);
            }

            foreach (Transform child in parent.transform)
            {
                GetAllChildrenOf(children, child.gameObject);
            }

            return children;
        }

        private static void GetAllChildrenOf(List<GameObject> children, GameObject parent)
        {
            children.Add(parent);

            foreach (Transform child in parent.transform)
            {
                GetAllChildrenOf(children, child.gameObject);
            }
        }

        public static List<T> GetComponentsInAllChildren<T>(GameObject gameObject)
        {
            var children = GetAllChildrenOf(gameObject, true);
            var components = new List<T>();
            children.ForEach(c => components.AddRange(c.GetComponents<T>()));
            RemoveNullReferences(components, gameObject);

            return components;
        }

        public static List<Component> GetComponentsInAllChildren(GameObject gameObject, Type type)
        {
            var children = GetAllChildrenOf(gameObject, true);
            var components = new List<Component>();
            children.ForEach(c => components.AddRange(c.GetComponents(type)));
            RemoveNullReferences(components, gameObject);
            return components;
        }

        public static List<T> GetComponentsSafe<T>(GameObject gameObject)
        {
            var components = gameObject.GetComponents<T>().ToList();
            RemoveNullReferences(components, gameObject);
            return components;
        }

        private static void RemoveNullReferences<T>(List<T> list, GameObject parent)
        {
            if (list.Any(c => c == null))
            {
                Debug.LogWarning(string.Format("Replace prefab warning: {0} contains missing scripts.", parent.name));
            }

            list.RemoveAll(c => c == null);
        }

        public static T GetComponentInAllChildren<T>(GameObject gameObject)
        {
            return GetComponentsInAllChildren<T>(gameObject).FirstOrDefault();
        }

        public static Component GetComponentInAllChildren(GameObject gameObject, Type type)
        {
            return GetComponentsInAllChildren(gameObject, type).FirstOrDefault();
        }
    }
}