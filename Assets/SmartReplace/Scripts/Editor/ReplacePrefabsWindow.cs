using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SmartReplace.Scripts.Editor
{
    public class ReplacePrefabsWindow : EditorWindow
    {
        // Object that should be replaced
        public GameObject brokenPrefab;

        // Prefab that should replace brokenPrefab
        public GameObject freshPrefab;

        // Always true
        public bool keepReferencesToOldPrefab = true;

        // List of all components, the brokenPrefab and freshPrefab have in common
        private List<TransferableType> componentIntersection = new List<TransferableType>();

        // UI
        private Vector2 scrollPosition;

        // List of objects with similar names / components to the brokenPrefab
        private List<SimilarGameObject> similarObjects = new List<SimilarGameObject>();

        // Components with references to any of the objects to be replaced
        private readonly List<ReferencingComponent> referencingComponents = new List<ReferencingComponent>();

        // References from objects in the scene to objects being replaced
        private readonly List<ReferencedObject> referencedObjects = new List<ReferencedObject>();

        // Mapping from old object id's to the new object instances
        private readonly Dictionary<int, GameObject> replacementHistory = new Dictionary<int, GameObject>();

        private readonly WindowValuesChangeManager uiChangeManager = new WindowValuesChangeManager();

        private delegate List<T> GetHierarchyFor<T>(GameObject target);

        private const bool ShowHandles = true; // Always true, activates scene handles
        private const bool SearchChildren = false; // Always false
        private const int Indentation = 15;

        private bool showSimilarObjects = true;
        private bool searchForSimilarObjects;
        private bool showComponentComparisons = true;
        private bool searchByName;
        private bool searchByComponents;
        private bool enableMultiScene;

        private bool transferTransform = true;
        private bool transferPosition = true;
        private bool transferScale = true;
        private bool transferRotation = true;

        private bool isRunning;
        private bool hasErrorOccurred;

        [MenuItem("Tools/Replace Prefab...")]
        private static void Init()
        {
            var editor = (ReplacePrefabsWindow)GetWindow(typeof(ReplacePrefabsWindow));
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
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        // Draws the UI
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
            if (CanCompare)
                searchForSimilarObjects = EditorGUILayout.Toggle(searchForSimilarObjects, GUILayout.Width(15));
            EditorGUILayout.LabelField("Search for similar objects in scene", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            if (CanCompare)
            {
                if (searchForSimilarObjects)
                {
                    searchByName = ShowToggleWithIndentation(searchByName, "By name", Indentation);
                    searchByComponents = ShowToggleWithIndentation(searchByComponents, "By components", Indentation);

                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("", GUILayout.Width(Indentation));
                    showSimilarObjects = EditorGUILayout.Foldout(showSimilarObjects, "Similar objects");
                    EditorGUILayout.EndHorizontal();
                    if (showSimilarObjects)
                    {
                        foreach (var similarObject in similarObjects)
                        {
                            similarObject.IsActivated = ShowToggleWithIndentation(similarObject.IsActivated,
                                similarObject.SimilarObject.name, Indentation * 2);
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
                    foreach (var transferableType in componentIntersection)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(Indentation));
                        transferableType.IsActivated =
                            EditorGUILayout.Toggle(transferableType.IsActivated, GUILayout.Width(15));
                        transferableType.IsExpanded = EditorGUILayout.Foldout(transferableType.IsExpanded,
                            transferableType.ComponentType.ToString());
                        EditorGUILayout.EndHorizontal();

                        if (transferableType.IsActivated && transferableType.IsExpanded)
                        {
                            foreach (var field in transferableType.Fields)
                            {
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("", GUILayout.Width(Indentation * 3.2f));
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
                transferPosition = ShowToggleWithIndentation(transferPosition, "Position", Indentation);
                transferRotation = ShowToggleWithIndentation(transferRotation, "Rotation", Indentation);
                transferScale = ShowToggleWithIndentation(transferScale, "Scale", Indentation);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Other", EditorStyles.boldLabel);

            keepReferencesToOldPrefab =
                ShowToggleWithIndentation(keepReferencesToOldPrefab, "Transfer external references", Indentation);
            enableMultiScene = ShowToggleWithIndentation(enableMultiScene, "Allow multi scene", Indentation);

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
                EditorGUILayout.HelpBox(
                    "An error has occurred and not all objects have been replaced.\nSee the log for further details.",
                    MessageType.Error);
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

        private bool ShowToggleWithIndentation(bool inputValue, string text, int customIndentation)
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

        // Draw handles to visualize replacement candidates
        private void OnSceneGUI(SceneView sceneView)
        {
            if (brokenPrefab == null || !ShowHandles) return;
            Handles.color = Color.green;
            Handles.SphereHandleCap(0, brokenPrefab.transform.position, Quaternion.identity, 0.5f, EventType.Repaint);
            Handles.color = Color.yellow;
            foreach (var similarObject in similarObjects)
            {
                if (similarObject.IsActivated)
                {
                    Handles.SphereHandleCap(0, similarObject.SimilarObject.transform.position, Quaternion.identity,
                        0.2f, EventType.Repaint);
                }
            }

            Handles.color = Color.white;
        }

        #endregion

        // Will compare the components of brokenPrefab with the ones on freshPrefab
        // Every intersecting component will be encapsulated as TransferableType and stored in a list
        private void CompareObjects()
        {
            var oldComponents = GameObjectHelper.GetComponentsInAllChildren<Component>(brokenPrefab);
            var newComponents = GameObjectHelper.GetComponentsInAllChildren<Component>(freshPrefab);

            var results = oldComponents.Distinct()
                .Join(newComponents.Distinct(), o => o.GetType(), n => n.GetType(), (o, n) => o.GetType()).Distinct();

            componentIntersection = results.Select(r => new TransferableType()
                { ComponentType = r, IsActivated = r.Namespace != "UnityEngine" }).ToList();
            componentIntersection.ForEach(c => ObtainFields(c));
        }

        // Will search for all fields on a component, encapsulate them in a TransferableField instance and return them as list
        private void ObtainFields(TransferableType transferableType)
        {
            var fields = GetFieldsForType(transferableType.ComponentType);
            transferableType.Fields =
                fields.Select(f => new TransferableField() { Field = f, IsActivated = true }).ToList();
        }

        // Find all fields (variables) of a specific type and returns their fieldInfo as a list
        private List<FieldInfo> GetFieldsForType(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Default |
                                        BindingFlags.DeclaredOnly | BindingFlags.NonPublic |
                                        BindingFlags.FlattenHierarchy).ToList();

            // GetFields won't actually return all fields of the base types, even with FlattenHierarchy enabled
            // Manually add base type fields
            var baseType = type.BaseType;
            while (baseType != typeof(Component) && baseType != typeof(MonoBehaviour) && baseType != null)
            {
                var baseFields = baseType.GetFields(BindingFlags.Instance | BindingFlags.Default |
                                                    BindingFlags.DeclaredOnly | BindingFlags.NonPublic |
                                                    BindingFlags.FlattenHierarchy | BindingFlags.Public).ToList();
                fields.AddRange(baseFields);
                baseType = baseType.BaseType;
            }

            fields.RemoveAll(f => !f.IsPublic && !Attribute.IsDefined(f, typeof(SerializeField)));
            return fields;
        }

        // Called by the replace button
        // Will execute the replacement for the brokenPrefab and all similar objects, if wanted by the user
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

        // Replaces a single instance of a given object
        private void Replace(GameObject oldObject)
        {
            // Instantiate new prefab
            var newlyCreatedPrefab = PrefabUtility.InstantiatePrefab(freshPrefab, oldObject.scene) as GameObject;
            // Apply old transformation
            if (transferTransform) ApplyTransformation(oldObject, newlyCreatedPrefab);
            TransferComponentValues(oldObject, newlyCreatedPrefab);
            newlyCreatedPrefab.name = oldObject.name;
            // Needed to invalidate the scene
            Undo.RecordObject(oldObject, "Replace object");
            // Create a lookup table for the old and new instance
            MakeHistory(oldObject, newlyCreatedPrefab);

            DestroyImmediate(oldObject);
            // Cleanup
            if (oldObject == brokenPrefab)
            {
                brokenPrefab = null;
            }
            else
            {
                similarObjects?.RemoveAll(o => o.SimilarObject == oldObject);
            }
        }

        // Creates a lookup table with the old objects id being the key and the new instance being the value
        private void MakeHistory(GameObject oldObject, GameObject newObject)
        {
            var idHierarchy = GameObjectHelper.GetAllChildrenOf(oldObject, true).Select(go => go.GetInstanceID())
                .ToList();
            idHierarchy.ForEach(id => replacementHistory.Add(id, newObject));
        }

        private void ApplyTransformation(GameObject oldObject, GameObject newlyCreatedPrefab)
        {
            var targetTransform = oldObject.transform;
            if (transferPosition) newlyCreatedPrefab.transform.position = targetTransform.position;
            if (transferRotation) newlyCreatedPrefab.transform.rotation = targetTransform.rotation;
            if (transferScale) newlyCreatedPrefab.transform.localScale = targetTransform.localScale;
        }

        // Transfers all selected components from the component intersection from the old to the new instance
        private void TransferComponentValues(GameObject oldObject, GameObject newlyCreatedPrefab)
        {
            componentIntersection.Where(v => v.IsActivated).ToList()
                .ForEach(c => TransferSingleComponent(c, oldObject, newlyCreatedPrefab));
        }

        // Transfers all selected values from an old object's component to the corresponding new component
        private void TransferSingleComponent(TransferableType type, GameObject oldObject, GameObject newlyCreatedPrefab)
        {
            var oldComponent = GameObjectHelper.GetComponentInAllChildren(oldObject, type.ComponentType);
            var newComponent = GameObjectHelper.GetComponentInAllChildren(newlyCreatedPrefab, type.ComponentType);

            try
            {
                type.Fields.Where(f => f.IsActivated).Select(fi => fi.Field).ToList()
                    .ForEach(mf => mf.SetValue(newComponent, mf.GetValue(oldComponent)));
            }
            catch
            {
                Debug.LogWarning("An error occurred when transfering values for " + type.ComponentType.Name);
            }
        }

        // Comparison only possible, if old and new prefab are known
        private bool CanCompare => brokenPrefab != null && freshPrefab != null;

        private bool CanReplace => CanCompare && componentIntersection.Count > 0;

        // Searches for objects in the scene with similar names or components to the brokenPrefab
        private void FindSimilarObjects()
        {
            if (!CanCompare)
            {
                Debug.LogWarning("Scene object and prefab must be set before searching for similar objects.");
                return;
            }

            var allObjects = FindAllObjects();
            allObjects.Remove(brokenPrefab);

            // Remove child objects from the list
            if (!SearchChildren)
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

            // Store the objects in a list to show them in the UI
            // GameObject will also be wrapped inside of a SimilarGameObject to store whether it's been selected in the UI
            similarObjects = allObjects.Select(ro => new SimilarGameObject() { SimilarObject = ro, IsActivated = true })
                .ToList();
        }

        // Removes objects from the list, that do not have similar names to brokenPrefab
        private void FindSimilarObjectsByName(List<GameObject> objectsToBeFiltered)
        {
            var separatedNames = GameObjectNameFragments(brokenPrefab.name, freshPrefab.name);
            objectsToBeFiltered.RemoveAll(o => !separatedNames.Any(n => o.name.Contains(n)));
        }

        // Removes objects from the list, that do not share at lease one common component with brokenPrefab
        private void FindSimilarObjectsByComponents(List<GameObject> objectsToBeFiltered)
        {
            if (componentIntersection.Count == 0)
            {
                CompareObjects();
            }

            var currentComponents = componentIntersection.Where(c => c.IsActivated).Select(c => c.ComponentType);
            var objectsToRemove = new List<GameObject>();

            foreach (var gameObject in objectsToBeFiltered)
            {
                var sceneObjectComponents = GameObjectHelper.GetComponentsInAllChildren<Component>(gameObject)
                    .Select(c => c.GetType()).ToList();
                var intersectingComponentsAmount =
                    sceneObjectComponents.Join(currentComponents, a => a, b => b, (a, b) => a).Count();

                if (intersectingComponentsAmount == 0)
                {
                    objectsToRemove.Add(gameObject);
                }
            }

            objectsToBeFiltered.RemoveAll(o => objectsToRemove.Contains(o));
        }

        // Used by FindSimilarObjectsByName to separate a name into separate, comparable fragments
        private List<string> GameObjectNameFragments(params string[] names)
        {
            var parsedNames = new List<string>();

            foreach (var name in names)
            {
                var nicifiedName = ObjectNames.NicifyVariableName(name);
                parsedNames.AddRange(nicifiedName.Split(' ').ToList());
            }

            parsedNames.RemoveAll(s => !s.All(Char.IsLetter));

            return parsedNames;
        }

        #region External References

        // Searches all components in the scene for references onto the object being replaced
        // All references will be saved and restored after replacement
        private void FindExternalReferences()
        {
            var allObjectsInScene = FindAllObjects();
            var allComponentsInScene = allObjectsInScene
                .SelectMany(s => GameObjectHelper.GetComponentsSafe<Component>(s))
                .Where(c => c.GetType().Namespace != "UnityEngine").ToList();

            var componentToFieldsMapping = new Dictionary<Type, List<FieldInfo>>();
            allComponentsInScene.Select(c => c.GetType()).Distinct().ToList()
                .ForEach(t => componentToFieldsMapping.Add(t, GetFieldsForType(t)));

            // All objects that may potentially be referenced by other objects
            var targetObjects = new List<GameObject>();
            targetObjects.Add(brokenPrefab);
            targetObjects.AddRange(similarObjects.Where(s => s.IsActivated).Select(s => s.SimilarObject));

            // Stores the children (value) of a gameObject (key) as list
            // Is a lookup table filled by the FindByReferencesOn function to save some processing time
            var objectHierarchyMapping = new Dictionary<GameObject, List<GameObject>>();
            // Stores the components (value) of a gameObject (key) as list
            var componentHierarchyMapping = new Dictionary<GameObject, List<Component>>();

            // Iterate through every variable of every component in the scene to check whether they may be referencing one of the replaced objects
            foreach (var potentiallyTargetingComponent in allComponentsInScene)
            {
                foreach (var potentialReference in componentToFieldsMapping[
                    potentiallyTargetingComponent.GetType()])
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
                        FindReferencesOn(baseParameter, objectHierarchyMapping,
                            GameObjectHelper.ObjectHierarchyFor);
                    }

                    if (componentFieldType.IsSubclassOf(typeof(Component)))
                    {
                        baseParameter.IsList = false;
                        FindReferencesOn(baseParameter, componentHierarchyMapping,
                            GameObjectHelper.ComponentHierarchyFor);
                    }

                    if (IsListOf<GameObject>(componentFieldType))
                    {
                        baseParameter.IsList = true;
                        FindReferencesOn(baseParameter, objectHierarchyMapping,
                            GameObjectHelper.ObjectHierarchyFor);
                    }

                    if (IsListOf<Component>(componentFieldType))
                    {
                        baseParameter.IsList = true;
                        FindReferencesOn(baseParameter, componentHierarchyMapping,
                            GameObjectHelper.ComponentHierarchyFor);
                    }
                }
            }
        }

        // Will look at a variable of a given component an checks, whether it references a GameObject or Component present in the hierarchyMapping
        // Stores any found references
        private void FindReferencesOn<T>(
            FindReferenceBaseParameter param,
            Dictionary<GameObject, List<T>> hierarchyMapping,
            GetHierarchyFor<T> getHierarchyFor)
        {
            List<T> hierarchyAsList;
            // Current value of the component's variable
            // May be a list. For simplicity, a list is always assumed
            var value = GetValueAsListFor<T>(param.ComponentWithReference, param.ReferenceOnTarget, param.IsList);

            foreach (var targetObject in param.TargetObjects)
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

                for (var i = 0; i < value.Count; i++)
                {
                    if (hierarchyAsList.Any(o => o.Equals(value[i])))
                    {
                        if (typeof(T) == typeof(Component))
                            StoreReferenceForComponent(targetObject, param.ComponentWithReference,
                                param.ReferenceOnTarget, value[i] as Component, param.IsList, i);
                        if (typeof(T) == typeof(GameObject))
                            StoreReferenceForObject(targetObject, param.ComponentWithReference, param.ReferenceOnTarget,
                                value[i] as GameObject, param.IsList, i);
                    }
                }
            }
        }

        // Returns the value of a component's variable as a list
        // Non enumerable types, arrays and lists will all be returned as a list of T
        private List<T> GetValueAsListFor<T>(Component source, FieldInfo field, bool isList)
        {
            var results = new List<T>();
            if (isList)
            {
                var fieldType = field.FieldType;

                if (fieldType.IsArray)
                {
                    var array = field.GetValue(source) as Array;
                    var arrayLength = array.Length;

                    for (var i = 0; i < arrayLength; i++)
                    {
                        results.Add((T)array.GetValue(i));
                    }
                }
                else
                {
                    var list = field.GetValue(source);

                    if (list == null)
                    {
                        return results;
                    }

                    var count = (int)fieldType.GetProperty("Count").GetValue(list);
                    var propertyItemInfo = fieldType.GetProperty("Item");

                    for (var i = 0; i < count; i++)
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

        // Stores a reference to a replaced object for later update
        private void StoreReferenceForObject(GameObject referencedObject, Component referencingComponent,
            FieldInfo referencingField, GameObject value, bool isList, int index)
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

        // Stores a reference to a replaced component for later update
        private void StoreReferenceForComponent(GameObject referencedObject, Component referencingComponent,
            FieldInfo referencingField, Component value, bool isList, int index)
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

        // Updates all references, so that all replaced objects will now be referenced
        private void RestoreExternalReferences()
        {
            referencedObjects.ForEach(ro => ro.UpdateReference());
            referencingComponents.ForEach(rc => rc.UpdateReference());
        }

        #endregion

        // Clears all temporary lists and prepares the window for further use
        private void CleanUp()
        {
            referencedObjects.Clear();
            referencingComponents.Clear();
            similarObjects.Clear();
            componentIntersection.Clear();
            replacementHistory.Clear();
            searchForSimilarObjects = false;
        }

        // Updates the UI when changes have been made
        private void OnPropertiesChanged()
        {
            var changes = UiChanges;

            // Show error, when a prefab is dropped in the brokenPrefab section
            if (brokenPrefab != null && !IsSceneObject(brokenPrefab))
            {
                Debug.LogError("The destination object has to be an object from the scene and cannot be a prefab.");
                brokenPrefab = null;
                return;
            }

            // Clear similar objects list if brokenPrefab or freshPrefab are null
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

            if (changes.ShowSimilarObjectsChanged || changes.SimilarObjectCriteriaChanged)
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

            // Will only be true if an error has occurred and the replacement has been interrupted
            if (!isRunning)
            {
                return;
            }

            CleanUp();
            isRunning = false;
            hasErrorOccurred = true;
        }

        // True if the given object is not null and inside of any scene
        private bool IsSceneObject(GameObject gameObject) => gameObject != null && gameObject.scene.name != null;

        // Returns a summary of all changes made by the user, so the UI can react accordingly
        private WindowValuesChangeSummary UiChanges => uiChangeManager.UpdateSnapshot(freshPrefab, brokenPrefab,
            searchForSimilarObjects, searchByName, searchByComponents, enableMultiScene);

        // True if type is an enumerable of T
        private bool IsListOf<T>(Type type)
        {
            foreach (var interfaceType in type.GetInterfaces())
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

        // Will return a list of all objects present in the scene view
        // Will remove objects that are not in the same scene as brokenPrefab if enableMultiScene is disabled
        private List<GameObject> FindAllObjects()
        {
            var allObjects = FindObjectsOfType<GameObject>().ToList();

            if (!enableMultiScene && brokenPrefab != null)
            {
                allObjects.RemoveAll(o => o.scene.name != brokenPrefab.scene.name);
            }

            return allObjects;
        }
    }
}