using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

//===================== Hold 2 integers in a Vector3  ====================//

namespace CusHolder
{

    //class that holds a vector 3 of integers
    [System.Serializable]
    public class vec3I : vec2I
    {

        public int z;

        //vec3I defaults
        public vec3I() : base()
        {
            z = 0;
        }

        //vec3i default with other vec3i input
        public vec3I(vec3I input) : base(input.x, input.y)
        {
            z = input.z;
        }

        //vec3i default with 3 ints as input
        public vec3I(int _x, int _y, int _z) : base(_x, _y)
        {
            z = _z;
        }

        //vec3i default with vector3 input
        public vec3I(Vector3 input) : base(input.x, input.y)
        {
            z = (int)input.z;
        }

        //vec3i default with 3 floats as input
        public vec3I(float _x, float _y, float _z) : base(_x, _y)
        {
            z = (int)_z;
        }

        //vec3i default with 2 floats as input
        public vec3I(float _x, float _y) : base(_x, _y)
        {
            z = 0;
        }

        //vec3i default with 2 ints as input
        public vec3I(int _x, int _y) : base(_x, _y)
        {
            z = 0;
        }

        //vec3i default with 1 float as input
        public vec3I(float _x) : base(_x)
        {
            y = 0;
            z = 0;
        }

        //vec3i default with 1 int as input
        public vec3I(int _x) : base(_x)
        {
            y = 0;
            z = 0;
        }

        //functions
        //not sure how to make this just .blabla yet so

        //returns a new vec3i initialised to 0
        new public static vec3I zero()
        {
            return new vec3I(0, 0, 0);
        }

        //returns a new vec3i initialised to 1
        new public static vec3I one()
        {
            return new vec3I(1, 1, 1);
        }

        //returns a new vector3 initialised to this vec3i's variables
        public Vector3 vec3()
        {
            return new Vector3(x, y, z);
        }

        //basic overrides
        //adds two vec3i together
        public static vec3I operator +(vec3I in1, vec3I in2)
        {
            return new vec3I(in1.x + in2.x, in1.y + in2.y, in1.z + in2.z);
        }

        //adds vec3i and vector3 together
        public static vec3I operator +(vec3I in1, Vector3 in2)
        {
            return new vec3I(in1.x + (int)in2.x, in1.y + (int)in2.y, in1.z + (int)in2.z);
        }

        //adds vector3 and vec3i together
        public static Vector3 operator +(Vector3 in1, vec3I in2)
        {
            return new Vector3(in1.x + in2.x, in1.y + in2.y, in1.z + in2.z);
        }

        //take away vec3i by vec3i
        public static vec3I operator -(vec3I in1, vec3I in2)
        {
            return new vec3I(in1.x - in2.x, in1.y - in2.y, in1.z - in2.z);
        }

        //take away vec3i by vector3
        public static vec3I operator -(vec3I in1, Vector3 in2)
        {
            return new vec3I(in1.x - (int)in2.x, in1.y - (int)in2.y, in1.z - (int)in2.z);
        }

        //take vector3 vec3i by vec3i
        public static Vector3 operator -(Vector3 in1, vec3I in2)
        {
            return new Vector3(in1.x - in2.x, in1.y - in2.y, in1.z - in2.z);
        }

    }

#if UNITY_EDITOR
	//custom property drawer for vec3I
	[CustomPropertyDrawer (typeof(vec3I))]
	public class vec3IDrawer : PropertyDrawer
	{

		//Draw the property inside the given rect
		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty (position, label, property);

			//Display a label on the inspector
			position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);

			//Store x,y,z into a temp vector3
			Vector3 temp = new Vector3 (property.FindPropertyRelative ("x").intValue,
				                property.FindPropertyRelative ("y").intValue, property.FindPropertyRelative ("z").intValue);

			//Display vector2 to inspector
			temp = EditorGUI.Vector3Field (position, "", temp); 

			//Set vec3I x, y and z to inspector value
			property.FindPropertyRelative ("x").intValue = (int)temp.x;
			property.FindPropertyRelative ("y").intValue = (int)temp.y;
			property.FindPropertyRelative ("z").intValue = (int)temp.z;

			EditorGUI.EndProperty ();
		}

	}
#endif

}