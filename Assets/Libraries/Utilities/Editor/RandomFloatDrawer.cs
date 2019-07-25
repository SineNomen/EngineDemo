
using UnityEngine;
using UnityEditor;

namespace Sojourn.Utility {
	[CustomPropertyDrawer(typeof(RandomFloat))]
	public class RandomFloatDrawer : PropertyDrawer {
		// Draw the property inside the given rect
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			SerializedProperty min = property.FindPropertyRelative("Min");
			SerializedProperty max = property.FindPropertyRelative("Max");
			SerializedProperty low = property.FindPropertyRelative("Low");
			SerializedProperty high = property.FindPropertyRelative("High");
			float minValue = min.floatValue;
			float maxValue = max.floatValue;
			// Now draw the property as a Slider or an IntSlider based on whether it's a float or integer.
			Rect rect = position;
			rect.width = EditorGUIUtility.labelWidth;

			EditorGUI.LabelField(rect, property.displayName);

			float labelWidth = 30.0f;
			float spacer = 15.0f;
			//everything but field is a constant size, the field width will fill the rest f the space
			float fieldWidth = ((position.width - rect.width) - spacer - (2 * labelWidth)) / 2.0f;

			rect.position = new Vector2(rect.position.x + rect.width, rect.position.y);
			rect.width = labelWidth;
			EditorGUI.LabelField(rect, "Min");

			rect.x += labelWidth;
			rect.width = fieldWidth;
			float newMin = EditorGUI.FloatField(rect, min.floatValue);
			min.floatValue = Mathf.Max(newMin, low.floatValue);

			rect.position = new Vector2(rect.position.x + rect.width + spacer, rect.position.y);
			rect.width = labelWidth;
			EditorGUI.LabelField(rect, "Max");

			rect.x += labelWidth;
			rect.width = fieldWidth;
			float newMax = EditorGUI.FloatField(rect, max.floatValue);
			max.floatValue = Mathf.Min(newMax, high.floatValue);
		}
	}
}