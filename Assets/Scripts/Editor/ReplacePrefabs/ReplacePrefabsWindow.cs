using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ReplacePrefab
{
    public class ReplacePrefabsWindow : EditorWindow
    {
        public GameObject brokenPrefab;
        public GameObject freshPrefab;
        public bool keepReferencesToOldPrefab = true;

        private List<TransferableType> componentIntersection = new List<TransferableType>();
        private Vector2 scrollPosition;

        private List<SimilarGameObject> similarObjects = new List<SimilarGameObject>();
        private List<ReferencingComponent> referencingComponents = new List<ReferencingComponent>();
        private List<ReferencedObject> referencedObjects = new List<ReferencedObject>();
        private Dictionary<int, GameObject> replacementHistory = new Dictionary<int, GameObject>();

        public delegate List<T> GetHierarchyFor<T>(GameObject target);

        private WindowValuesChangeManager uiChangeManager = new WindowValuesChangeManager();

        private bool showHandles = true;
        private bool showSimilarObjects = true;
        private bool searchForSimilarObjects = false;
        private bool showComponentComparisons = true;
        private bool searchChilren = false;
        private bool searchByName = false;
        private bool searchByComponents = false;
        private bool enableMultiScene = false;

        private bool transferTransform = true;
        private bool transferPosition = true;
        private bool transferScale = true;
        private bool transferRotation = true;

        private bool isRunning = false;
        private bool hasErrorOccurred = false;

        private readonly int indentation = 15;

        [MenuItem("Window/Replace Prefab...")]
        static void Init()
        {
            ReplacePrefabsWindow editor = (ReplacePrefabsWindow)EditorWindow.GetWindow(typeof(ReplacePrefabsWindow));
            editor.titleContent = new GUIContent("Replace prefab");
            editor.Show();
        }

        #region GUI
        private void OnHierarchyChange()
        {
            similarObjects.RemoveAll(so => so.SimilarObject == null);
        }

        private void OnFocus()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        private void OnDestroy()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Space();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("Object input", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scene object", GUILayout.Width(100));
            brokenPrefab = EditorGUILayout.ObjectField(brokenPrefab, typeof(GameObject), true) as GameObject;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Fresh prefab", GUILayout.Width(100));
            freshPrefab = EditorGUILayout.ObjectField(freshPrefab, typeof(GameObject), false) as GameObject;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (CanCompare) searchForSimilarObjects = EditorGUILayout.Toggle(searchForSimilarObjects, GUILayout.Width(15));
            EditorGUILayout.LabelField("Search for similar objects in scene", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            if (CanCompare)
            {
                if (searchForSimilarObjects)
                {
                    searchByName = ShowToogleWithIndentation(searchByName, "By name", indentation);
                    searchByComponents = ShowToogleWithIndentation(searchByComponents, "By components", indentation);

                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("", GUILayout.Width(indentation));
                    showSimilarObjects = EditorGUILayout.Foldout(showSimilarObjects, "Similar objects");
                    EditorGUILayout.EndHorizontal();
                    if (showSimilarObjects)
                    {
                        foreach (SimilarGameObject similarObject in similarObjects)
                        {
                            similarObject.IsActivated = ShowToogleWithIndentation(similarObject.IsActivated, similarObject.SimilarObject.name, indentation * 2);
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Object input needed to find similar objects", MessageType.None);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Transfer component values", EditorStyles.boldLabel);

            if (CanCompare && componentIntersection.Count > 0)
            {
                showComponentComparisons = EditorGUILayout.Foldout(showComponentComparisons, "Component intersection");
                if (showComponentComparisons)
                {
                    foreach (TransferableType transferableType in componentIntersection)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(indentation));
                        transferableType.IsActivated = EditorGUILayout.Toggle(transferableType.IsActivated, GUILayout.Width(15));
                        transferableType.IsExpanded = EditorGUILayout.Foldout(transferableType.IsExpanded, transferableType.ComponentType.ToString());
                        EditorGUILayout.EndHorizontal();

                        if (transferableType.IsActivated && transferableType.IsExpanded)
                        {
                            foreach (TransferableField field in transferableType.Fields)
                            {
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("", GUILayout.Width(indentation * 3.2f));
                                field.IsActivated = EditorGUILayout.Toggle(field.IsActivated, GUILayout.Width(15));
                                EditorGUI.BeginDisabledGroup(!field.IsActivated);
                                EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(field.Field.Name));
                                EditorGUI.EndDisabledGroup();
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                }
            }

            if (!CanCompare)
            {
                EditorGUILayout.HelpBox("Object input needed to compare the object's components.", MessageType.None);
            }
            else if (componentIntersection.Count == 0)
            {
                EditorGUILayout.HelpBox("No intersecting components found.", MessageType.None);
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            transferTransform = EditorGUILayout.Toggle(transferTransform, GUILayout.Width(15));
            EditorGUILayout.LabelField("Transfer transform values", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            if (transferTransform)
            {
                transferPosition = ShowToogleWithIndentation(transferPosition, "Position", indentation);
                transferRotation = ShowToogleWithIndentation(transferRotation, "Rotation", indentation);
                transferScale = ShowToogleWithIndentation(transferScale, "Scale", indentation);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Other", EditorStyles.boldLabel);

            keepReferencesToOldPrefab = ShowToogleWithIndentation(keepReferencesToOldPrefab, "Transfer external references", indentation);
            enableMultiScene = ShowToogleWithIndentation(enableMultiScene, "Allow multi scene", indentation);

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(!CanReplace);
            if (GUILayout.Button("Replace"))
            {
                ReplaceAll();
            }
            EditorGUI.EndDisabledGroup();

            if (hasErrorOccurred)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("An error has occurred and not all objects have been replaced.\nSee the log for further details.", MessageType.Error);
                if (GUILayout.Button("I understand..."))
                {
                    hasErrorOccurred = false;
                }
            }

            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            OnPropertiesChanged();
        }

        private bool ShowToogleWithIndentation(bool inputValue, string text, int customIndentation)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(customIndentation));
            inputValue = EditorGUILayout.Toggle(inputValue, GUILayout.Width(15));
            EditorGUI.BeginDisabledGroup(!inputValue);
            EditorGUILayout.LabelField(text);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            return inputValue;
        }

        protected void OnSceneGUI(SceneView sceneView)
        {
            if (brokenPrefab == null || !showHandles) return;
            Handles.color = Color.green;
            Handles.SphereHandleCap(0, brokenPrefab.transform.position, Quaternion.identity, 0.5f, EventType.Repaint);
            Handles.color = Color.yellow;
            foreach (SimilarGameObject similarObject in similarObjects)
            {
                if (similarObject.IsActivated)
                {
                    Handles.SphereHandleCap(0, similarObject.SimilarObject.transform.position, Quaternion.identity, 0.2f, EventType.Repaint);
                }
            }
            Handles.color = Color.white;
        }
        #endregion

        private void CompareObjects()
        {
            List<Component> oldComponents = GameObjectHelper.GetComponentsInAllChildren<Component>(brokenPrefab);
            List<Component> newComponents = GameObjectHelper.GetComponentsInAllChildren<Component>(freshPrefab);

            var results = oldComponents.Distinct().Join(newComponents.Distinct(), o => o.GetType(), n => n.GetType(), (o, n) => o.GetType()).Distinct();

            componentIntersection = results.Select(r => new TransferableType() { ComponentType = r, IsActivated = r.Namespace != "UnityEngine" }).ToList();
            componentIntersection.ForEach(c => ObtainFields(c));
        }

        private void ObtainFields(TransferableType transferableType)
        {
            var fields = GetFieldsForType(transferableType.ComponentType);
            transferableType.Fields = fields.Select(f => new TransferableField() { Field = f, IsActivated = true }).ToList();
        }

        private List<FieldInfo> GetFieldsForType(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).ToList();

            //GetFields won't actually return all fields of the base types, even with FlattenHierarchy enabled
            //Manually add base type fields
            var baseType = type.BaseType;
            while (baseType != typeof(Component) && baseType != typeof(MonoBehaviour) && baseType != null)
            {
                var baseFields = baseType.GetFields(BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Public).ToList();
                fields.AddRange(baseFields);
                baseType = baseType.BaseType;
            }

            fields.RemoveAll(f => !f.IsPublic && !Attribute.IsDefined(f, typeof(SerializeField)));
            return fields;
        }

        private void ReplaceAll()
        {
            isRunning = true;
            if (keepReferencesToOldPrefab) FindExternalReferences();

            similarObjects.Where(o => o.IsActivated).Select(o => o.SimilarObject).ToList().ForEach(o => Replace(o));
            Replace(brokenPrefab);

            if (keepReferencesToOldPrefab) RestoreExternalReferences();

            CleanUp();
            isRunning = false;
            hasErrorOccurred = false;
        }

        private void Replace(GameObject oldObject)
        {
            var newlyCreatedPrefab = PrefabUtility.InstantiatePrefab(freshPrefab, oldObject.scene) as GameObject;
            if (transferTransform) ApplyTransformation(oldObject, newlyCreatedPrefab);
            TransferComponentValues(oldObject, newlyCreatedPrefab);
            newlyCreatedPrefab.name = oldObject.name;
            Undo.RecordObject(oldObject, "Replace object");

            MakeHistory(oldObject, newlyCreatedPrefab);

            DestroyImmediate(oldObject);

            if (oldObject == brokenPrefab)
            {
                brokenPrefab = null;
            }
            else
            {
                similarObjects?.RemoveAll(o => o.SimilarObject == oldObject);
            }
        }

        private void MakeHistory(GameObject oldObject, GameObject newObject)
        {
            var idHierarchy = GameObjectHelper.GetAllChildrenOf(oldObject, true).Select(go => go.GetInstanceID()).ToList();
            idHierarchy.ForEach(id => replacementHistory.Add(id, newObject));
        }

        private void ApplyTransformation(GameObject oldObject, GameObject newlyCreatedPrefab)
        {
            var targetTransform = oldObject.transform;
            if (transferPosition) newlyCreatedPrefab.transform.position = targetTransform.position;
            if (transferRotation) newlyCreatedPrefab.transform.rotation = targetTransform.rotation;
            if (transferScale) newlyCreatedPrefab.transform.localScale = targetTransform.localScale;
        }

        private void TransferComponentValues(GameObject oldObject, GameObject newlyCreatedPrefab)
        {
            componentIntersection.Where(v => v.IsActivated).ToList().ForEach(c => TransferSingleComponent(c, oldObject, newlyCreatedPrefab));
        }

        private void TransferSingleComponent(TransferableType type, GameObject oldObject, GameObject newlyCreatedPrefab)
        {
            Component oldComponent = GameObjectHelper.GetComponentInAllChildren(oldObject, type.ComponentType);
            Component newComponent = GameObjectHelper.GetComponentInAllChildren(newlyCreatedPrefab, type.ComponentType);

            try
            {
                type.Fields.Where(f => f.IsActivated).Select(fi => fi.Field).ToList().ForEach(mf => mf.SetValue(newComponent, mf.GetValue(oldComponent)));
            }
            catch
            {
                Debug.LogWarning("An error occurred when transfering values for " + type.ComponentType.Name);
            }
        }

        private bool CanCompare => brokenPrefab != null && freshPrefab != null;

        private bool CanReplace => CanCompare && componentIntersection.Count > 0;

        private void FindSimilarObjects()
        {
            if (!CanCompare)
            {
                Debug.LogWarning("Scene object and prefab must be set before searching for similar objects.");
                return;
            }

            var allObjects = FindAllObjects();
            allObjects.Remove(brokenPrefab);

            if (!searchChilren)
            {
                allObjects.RemoveAll(o => o.transform.parent != null);
            }

            if (searchByName)
            {
                FindSimilarObjectsByName(allObjects);
            }
            if (searchByComponents)
            {
                FindSimilarObjectsByComponents(allObjects);
            }

            similarObjects = allObjects.Select(ro => new SimilarGameObject() { SimilarObject = ro, IsActivated = true }).ToList();
        }

        private void FindSimilarObjectsByName(List<GameObject> objectsToBeFiltered)
        {
            List<string> separatedNames = GameObjectNameFragments(brokenPrefab.name, freshPrefab.name);
            objectsToBeFiltered.RemoveAll(o => !separatedNames.Any(n => o.name.Contains(n)));
        }

        private void FindSimilarObjectsByComponents(List<GameObject> objectsToBeFiltered)
        {
            if (componentIntersection.Count == 0)
            {
                CompareObjects();
            }

            var currentComponents = componentIntersection.Where(c => c.IsActivated).Select(c => c.ComponentType);
            var objectsToRemove = new List<GameObject>();

            foreach (GameObject gameObject in objectsToBeFiltered)
            {                
                var sceneObjectComponents = GameObjectHelper.GetComponentsInAllChildren<Component>(gameObject).Select(c => c.GetType()).ToList();
                var intersectingComponentsAmount = sceneObjectComponents.Join(currentComponents, a => a, b => b, (a, b) => a).Count();

                if (intersectingComponentsAmount == 0)
                {
                    objectsToRemove.Add(gameObject);
                }
            }

            objectsToBeFiltered.RemoveAll(o => objectsToRemove.Contains(o));
        }

        private List<string> GameObjectNameFragments(params string[] names)
        {
            var parsedNames = new List<string>();

            foreach (string name in names)
            {
                var nicifiedName = ObjectNames.NicifyVariableName(name);
                parsedNames.AddRange(nicifiedName.Split(' ').ToList());
            }

            parsedNames.RemoveAll(s => !s.All(Char.IsLetter));

            return parsedNames;
        }

        #region External References
        private void FindExternalReferences()
        {
            var allObjectsInScene = FindAllObjects();
            var allComponentsInScene = allObjectsInScene.SelectMany(s => GameObjectHelper.GetComponentsSafe<Component>(s)).Where(c => c.GetType().Namespace != "UnityEngine").ToList();

            var componentToFieldsMapping = new Dictionary<Type, List<FieldInfo>>();
            allComponentsInScene.Select(c => c.GetType()).Distinct().ToList().ForEach(t => componentToFieldsMapping.Add(t, GetFieldsForType(t)));

            var targetObjects = new List<GameObject>();
            targetObjects.Add(brokenPrefab);
            targetObjects.AddRange(similarObjects.Where(s => s.IsActivated).Select(s => s.SimilarObject));

            var objectHierarchyMapping = new Dictionary<GameObject, List<GameObject>>();
            var componentHierarchyMapping = new Dictionary<GameObject, List<Component>>();

            foreach (Component potentiallyTargetingComponent in allComponentsInScene)
            {
                foreach (FieldInfo potentialReference in componentToFieldsMapping[potentiallyTargetingComponent.GetType()])
                {
                    var componentFieldType = potentialReference.FieldType;
                    var baseParameter = new FindReferenceBaseParameter()
                    {
                        ComponentWithReference = potentiallyTargetingComponent,
                        ReferenceOnTarget = potentialReference,
                        TargetObjects = targetObjects
                    };

                    if (componentFieldType.IsAssignableFrom(typeof(GameObject)))
                    {
                        baseParameter.IsList = false;
                        FindReferencesOn<GameObject>(baseParameter, objectHierarchyMapping, GameObjectHelper.ObjectHierarchyFor);
                    }
                    if (componentFieldType.IsSubclassOf(typeof(Component)))
                    {
                        baseParameter.IsList = false;
                        FindReferencesOn<Component>(baseParameter, componentHierarchyMapping, GameObjectHelper.ComponentHierarchyFor);
                    }
                    if (IsListOf<GameObject>(componentFieldType))
                    {
                        baseParameter.IsList = true;
                        FindReferencesOn<GameObject>(baseParameter, objectHierarchyMapping, GameObjectHelper.ObjectHierarchyFor);
                    }
                    if (IsListOf<Component>(componentFieldType))
                    {
                        baseParameter.IsList = true;
                        FindReferencesOn<Component>(baseParameter, componentHierarchyMapping, GameObjectHelper.ComponentHierarchyFor);
                    }
                }
            }
        }

        private void FindReferencesOn<T>(FindReferenceBaseParameter param, Dictionary<GameObject, List<T>> hierarchyMapping, GetHierarchyFor<T> getHierarchyFor)
        {
            List<T> hierarchyAsList;
            var value = GetValueAsListFor<T>(param.ComponentWithReference, param.ReferenceOnTarget, param.IsList);

            foreach (GameObject targetObject in param.TargetObjects)
            {
                if (hierarchyMapping.ContainsKey(targetObject))
                {
                    hierarchyAsList = hierarchyMapping[targetObject];
                }
                else
                {
                    hierarchyAsList = getHierarchyFor(targetObject);
                    hierarchyMapping.Add(targetObject, hierarchyAsList);
                }

                for (int i = 0; i < value.Count; i++)
                {
                    if (hierarchyAsList.Any(o => o.Equals(value[i])))
                    {
                        if (typeof(T) == typeof(Component)) StoreReferenceForComponent(targetObject, param.ComponentWithReference, param.ReferenceOnTarget, value[i] as Component, param.IsList, i);
                        if (typeof(T) == typeof(GameObject)) StoreReferenceForObject(targetObject, param.ComponentWithReference, param.ReferenceOnTarget, value[i] as GameObject, param.IsList, i);
                    }
                }
            }
        }

        private List<T> GetValueAsListFor<T>(Component source, FieldInfo field, bool isList)
        {
            List<T> results = new List<T>();
            if (isList)
            {
                var fieldType = field.FieldType;

                if (fieldType.IsArray)
                {
                    var array = field.GetValue(source) as Array;
                    var arrayLength = array.Length;

                    for (int i = 0; i < arrayLength; i++)
                    {
                        results.Add((T)array.GetValue(i));
                    }
                }
                else
                {
                    var list = field.GetValue(source);

                    if(list == null)
                    {
                        return results;
                    }

                    var count = (int)fieldType.GetProperty("Count").GetValue(list);
                    var propertyItemInfo = fieldType.GetProperty("Item");

                    for (int i = 0; i < count; i++)
                    {
                        results.Add((T)propertyItemInfo.GetValue(list, new object[] { i }));
                    }
                }
            }
            else
            {
                results.Add((T)field.GetValue(source));
            }
            return results;
        }

        private void StoreReferenceForObject(GameObject referencedObject, Component referencingComponent, FieldInfo referencingField, GameObject value, bool isList, int index)
        {
            referencedObjects.Add(new ReferencedObject((o) => replacementHistory[o])
            {
                IsList = isList,
                IndexInList = index,
                IsActivated = true,
                ReferencingFieldInSource = referencingField,
                ReferencingComponentInstance = referencingComponent,
                ReferencedObjectID = referencedObject.GetInstanceID(),
                SourceObjectID = referencingComponent.gameObject.GetInstanceID()
            });
        }

        private void StoreReferenceForComponent(GameObject referencedObject, Component referencingComponent, FieldInfo referencingField, Component value, bool isList, int index)
        {
            referencingComponents.Add(new ReferencingComponent((o) => replacementHistory[o])
            {
                IsList = isList,
                IndexInList = index,
                IsActivated = true,
                ReferencedComponentType = value.GetType(),
                ReferencingFieldInSource = referencingField,
                ReferencingComponentInstance = referencingComponent,
                ReferencedObjectID = referencedObject.GetInstanceID(),
                SourceObjectID = referencingComponent.gameObject.GetInstanceID()
            });
        }

        private void RestoreExternalReferences()
        {
            referencedObjects.ForEach(ro => ro.UpdateReference());
            referencingComponents.ForEach(rc => rc.UpdateReference());
        }

        #endregion

        private void CleanUp()
        {
            referencedObjects.Clear();
            referencingComponents.Clear();
            similarObjects.Clear();
            componentIntersection.Clear();
            replacementHistory.Clear();
            searchForSimilarObjects = false;
        }

        private void OnPropertiesChanged()
        {
            var changes = UiChanges;

            if(brokenPrefab != null && !IsSceneObject(brokenPrefab))
            {
                Debug.LogError("The destination object has to be an object from the scene and cannot be a prefab.");
                brokenPrefab = null;
                return;
            }

            if (!CanCompare)
            {
                similarObjects.Clear();
                searchForSimilarObjects = false;
            }

            if (!CanReplace)
            {
                componentIntersection.Clear();
            }

            if (changes.PrefabsChanged && CanCompare)
            {
                if (searchForSimilarObjects)
                {
                    FindSimilarObjects();
                }
                CompareObjects();
            }

            if (changes.ShowSimilarObjectsChanged || changes.SimilarObjectCriteraChanged)
            {
                if (searchForSimilarObjects)
                {
                    FindSimilarObjects();
                }
                else
                {
                    similarObjects.Clear();
                }
            }

            //Will only be true if an error has occurred and the replacement has been interrupted
            if (isRunning)
            {
                CleanUp();
                isRunning = false;
                hasErrorOccurred = true;
            }
        }

        private bool IsSceneObject(GameObject gameObject) => gameObject != null && gameObject.scene != null && gameObject.scene.name != null;

        private WindowValuesChangeSummary UiChanges => uiChangeManager.UpdateSnapshot(freshPrefab, brokenPrefab, searchForSimilarObjects, searchByName, searchByComponents, enableMultiScene);

        private bool IsListOf<T>(Type type)
        {
            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    Type itemType;

                    itemType = type.IsArray ? type.GetElementType() : type.GetGenericArguments().Single();
                    return itemType == typeof(T) || itemType.IsSubclassOf(typeof(T));
                }
            }
            return false;
        }

        private List<GameObject> FindAllObjects()
        {
            var allObjects = GameObject.FindObjectsOfType<GameObject>().ToList();

            if (!enableMultiScene && brokenPrefab != null)
            {
                allObjects.RemoveAll(o => o.scene.name != brokenPrefab.scene.name);
            }

            return allObjects;
        }
    }
}