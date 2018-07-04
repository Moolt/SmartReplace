using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReplaceablePrefab : MonoBehaviour {

    public List<GameObject> otherList;
    public GameObject[] otherListArray;
    public List<ReplaceablePrefab> otherListComponents;
    public ReplaceablePrefab[] otherListArrayComponents;
    public ReplaceablePrefab otherComponent;
    public GameObject otherObject;
    public Dictionary<GameObject, ReplaceablePrefab> asd = new Dictionary<GameObject, ReplaceablePrefab>();

    public int someIntValue = 0;
    public List<int> someIntList = new List<int>();

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
