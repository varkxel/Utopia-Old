using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;

namespace Utopia
{
	[CustomPropertyDrawer(typeof(Curve))]
	internal class CurveField : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
			if(GUI.Button(position, "Edit"))
			{
				// Open curve editor
				CurveEditor.Create(property, property.boxedValue as Curve, new GUIContent($"Curve Editor - {property.displayName}"));
			}

			EditorGUI.EndProperty();
		}
	}

	internal class CurveEditor : EditorWindow
	{
		private Texture2D texture;

		public SerializedProperty property;
		public Curve curve;

		public static void Create(SerializedProperty property, Curve curve, GUIContent label)
		{
			CurveEditor editorWindow = GetWindow<CurveEditor>();
			editorWindow.titleContent = label;
			editorWindow.property = property;
			editorWindow.curve = curve;
			editorWindow.Show();
		}

		public void GenerateTexture(int2 dimensions)
		{
			int size = dimensions.x * dimensions.y;

			texture = new Texture2D
			(
				dimensions.x, dimensions.y,
				DefaultFormat.LDR,
				TextureCreationFlags.DontUploadUponCreate
			);
			
			curve.Initialise();
			float minX = curve.x.Min();
			float maxX = curve.x.Max();
			float minY = curve.y.Min();
			float maxY = curve.y.Max();

			Curve.RawData data = curve.GetRawData();
			Color32[] colours = new Color32[size];
			for(int i = 0; i < dimensions.x; i++)
			{
				float x = unlerp(0, dimensions.x, i);
				float y = Curve.Evaluate(lerp(minX, maxX, x), data);
				y = unlerp(minY, maxY, y);
				int yInt = (int) (y * dimensions.y);
				yInt = clamp(yInt, 0, dimensions.y - 1);

				int index = i + yInt * dimensions.x;
				colours[index] = new Color32(0, 255, 0, 255);
			}
			texture.SetPixels32(colours);
			texture.Apply();
			
			curve.Dispose();
		}

		private void OnGUI()
		{
			const float ratio = 0.65f;
			
			EditorGUILayout.BeginHorizontal();
			Rect previewRect = EditorGUILayout.GetControlRect(false, position.height, GUILayout.Width(position.width * ratio));
			int2 size = new int2(position.size);
			size.x = (int) (size.x * ratio);
			
			GenerateTexture(size);
			EditorGUI.DrawPreviewTexture(previewRect, texture);
			
			EditorGUILayout.BeginVertical();
			for(int i = 0; i < curve._x.Length; i++)
			{
				EditorGUILayout.LabelField($"Keyframe {i}", EditorStyles.boldLabel);
	
				EditorGUI.indentLevel++;
				Vector2 keyPos = new Vector2(curve._x[i], curve._y[i]);
				keyPos = EditorGUILayout.Vector2Field("Position", keyPos);
				curve._x[i] = keyPos.x;
				curve._y[i] = keyPos.y;

				EditorGUILayout.LabelField("Tangents", EditorStyles.boldLabel);
				
				EditorGUI.indentLevel++;
				EditorGUILayout.BeginHorizontal();
				curve._tangentIn[i] = EditorGUILayout.FloatField("In", curve._tangentIn[i]);
				curve._tangentOut[i] = EditorGUILayout.FloatField("Out", curve._tangentOut[i]);
				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel--;
				
				EditorGUI.indentLevel--;
			}
			if(GUILayout.Button("Add Keyframe"))
			{
				float[] x = new float[curve._x.Length + 1];
				curve._x.CopyTo(x, 0);
				x[^1] = x[^2] + 1.0f;
				curve._x = x;

				float[] y = new float[curve._y.Length + 1];
				curve._y.CopyTo(y, 0);
				y[^1] = y[^2];
				curve._y = y;

				float[] tIn = new float[curve._tangentIn.Length + 1];
				curve._tangentIn.CopyTo(tIn, 0);
				tIn[^1] = 1.0f;
				curve._tangentIn = tIn;

				float[] tOut = new float[curve._tangentOut.Length + 1];
				curve._tangentOut.CopyTo(tOut, 0);
				tOut[^1] = 1.0f;
				curve._tangentOut = tOut;
			}
			
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();

			property.boxedValue = curve;
			property.serializedObject.ApplyModifiedProperties();
		}
	}
}