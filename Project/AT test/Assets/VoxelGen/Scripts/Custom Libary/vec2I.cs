using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

//===================== Hold 2 integers in a Vector2  ====================//

namespace CusHolder
{

    //class that holds a vector 2 of integers
    [System.Serializable]
    public class vec2I
    {

        public int x, y;

        //vec2I defaults
        public vec2I()
        {
            x = 0;
            y = 0;
        }

        //vec2i default with other vec2i input
        public vec2I(vec2I input)
        {
            x = input.x;
            y = input.y;
        }

        //vec2i default with 2 ints as inputs
        public vec2I(int _x, int _y)
        {
            x = _x;
            y = _y;
        }

        //float defaults
        //vec2i default with vector2 input
        public vec2I(Vector2 input)
        {
            x = (int)input.x;
            y = (int)input.y;
        }

        //vec2i default with 2 floats inputs
        public vec2I(float _x, float _y)
        {
            x = (int)_x;
            y = (int)_y;
        }

        //vec2i default with 1 float input
        public vec2I(float _x)
        {
            y = 0;
        }

        //vec2i default with 1 int input
        public vec2I(int _x)
        {
            y = 0;
        }

        //functions
        //returns a new vec2i initialised to 0
        public static vec2I zero()
        {
            return new vec2I(0, 0);
        }

        //returns a new vec2i initialised to 1
        public static vec2I one()
        {
            return new vec2I(1, 1);
        }

        //returns a new vector2 initialised to this vec2i
        public Vector3 vec2()
        {
            return new Vector2(x, y);
        }

        //basic overrides
        //adds two vec2i together
        public static vec2I operator +(vec2I in1, vec2I in2)
        {
            return new vec2I(in1.x + in2.x, in1.y + in2.y);
        }

        //adds two vec2i together
        public static vec2I operator +(vec2I in1, Vector2 in2)
        {
            return new vec2I(in1.x + (int)in2.x, in1.y + (int)in2.y);
        }

        //adds vector2 and vec2i together
        public static Vector2 operator +(Vector2 in1, vec2I in2)
        {
            return new Vector2(in1.x + in2.x, in1.y + in2.y);
        }

        //takes away a vec2i from a vec2i
        public static vec2I operator -(vec2I in1, vec2I in2)
        {
            return new vec2I(in1.x - in2.x, in1.y - in2.y);
        }

        //takes away a vec2i from a vector2
        public static vec2I operator -(vec2I in1, Vector2 in2)
        {
            return new vec2I(in1.x - (int)in2.x, in1.y - (int)in2.y);
        }

        //takes away a vector2 from a vec2i
        public static Vector2 operator -(Vector2 in1, vec2I in2)
        {
            return new Vector2(in1.x - in2.x, in1.y - in2.y);
        }
    }

#if UNITY_EDITOR
	//custom property drawer for vec2I
	[CustomPropertyDrawer (typeof(vec2I))]
	public class vec2IDrawer : PropertyDrawer
	{

		//Draw the property inside the given rect
		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty (position, label, property);

			//Display a label on the inspector
			position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);

			//Store x,y into a temp vector2
			Vector2 temp = new Vector2 (property.FindPropertyRelative ("x").intValue,
				               property.FindPropertyRelative ("y").intValue);

			//Display vector2 to inspector
			temp = EditorGUI.Vector2Field (position, "", temp); 

			//Set vec2I x and y to inspector value
			property.FindPropertyRelative ("x").intValue = (int)temp.x;
			property.FindPropertyRelative ("y").intValue = (int)temp.y;

			EditorGUI.EndProperty ();
		}

	}
#endif

}