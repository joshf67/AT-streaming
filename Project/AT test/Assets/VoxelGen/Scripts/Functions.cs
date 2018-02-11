using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Functions : ScriptableObject {

	public static Vector3 vec3Times(Vector3 one, Vector3 two) {
		return new Vector3 (one.x * two.x, one.y * two.y, one.z * two.z);
	}

	public static Vector3 vec3Div(Vector3 one, Vector3 two) {
		return new Vector3 (one.x / two.x, one.y / two.y, one.z / two.z);
	}

}