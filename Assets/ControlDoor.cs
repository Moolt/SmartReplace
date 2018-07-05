using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlDoor : MonoBehaviour {

    private bool isOpened = false;
    public Animator animator;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public bool ToggleDoor()
    {
        isOpened = !isOpened;
        animator.SetBool("active", isOpened);
        return isOpened;
    }
}
