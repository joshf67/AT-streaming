using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TNT : MonoBehaviour {

	public float explosionTime = 2;
	public float explosionDistance = 2;
	
	// Update is called once per frame
	void Update () {
		if (explosionTime < 0) {
			GameObject.FindObjectOfType<VoxelCreation> ().explode (transform.position, explosionDistance);
		} else {
			explosionTime -= Time.deltaTime;
		}
	}

	public void trigger() {
		if (explosionTime > 0) {
			GameObject.FindObjectOfType<VoxelCreation> ().explode (transform.position, explosionDistance);
		}
	}
}
