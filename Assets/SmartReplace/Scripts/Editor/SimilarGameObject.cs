using UnityEngine;

namespace SmartReplace.Scripts.Editor
{
    /// <summary>
    /// Wrapper class for an object to store whether it's been selected in the UI
    /// </summary>
    public class SimilarGameObject
    {
        public GameObject SimilarObject;
        public bool IsActivated = true;
    }
}