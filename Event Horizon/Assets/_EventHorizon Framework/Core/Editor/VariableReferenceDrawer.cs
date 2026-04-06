using UnityEditor;
using UnityEngine;

namespace EventHorizon.Core.Editor
{
    /// <summary>
    /// Custom property drawer for VariableReference fields.
    /// Renders a dropdown toggle between constant and variable modes,
    /// then draws the appropriate field inline.
    /// </summary>
    [CustomPropertyDrawer(typeof(FloatReference))]
    [CustomPropertyDrawer(typeof(IntReference))]
    [CustomPropertyDrawer(typeof(BoolReference))]
    [CustomPropertyDrawer(typeof(StringReference))]
    [CustomPropertyDrawer(typeof(Vector3Reference))]
    public class VariableReferenceDrawer : PropertyDrawer
    {
        private static readonly string[] _popupOptions = { "Constant", "Variable" };
        private static GUIStyle _popupStyle;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (_popupStyle == null)
            {
                _popupStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"))
                {
                    imagePosition = ImagePosition.ImageOnly
                };
            }

            label = EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label);

            EditorGUI.BeginChangeCheck();

            SerializedProperty useConstant = property.FindPropertyRelative("_useConstant");
            SerializedProperty constantValue = property.FindPropertyRelative("_constantValue");
            SerializedProperty variable = property.FindPropertyRelative("_variable");

            Rect buttonRect = new Rect(position);
            buttonRect.yMin += _popupStyle.margin.top;
            buttonRect.width = _popupStyle.fixedWidth + _popupStyle.margin.right;
            position.xMin = buttonRect.xMax;

            int result = EditorGUI.Popup(buttonRect, useConstant.boolValue ? 0 : 1, _popupOptions, _popupStyle);
            useConstant.boolValue = result == 0;

            EditorGUI.PropertyField(position, useConstant.boolValue ? constantValue : variable, GUIContent.none);

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }
    }
}
