using System;
using UnityEngine;
using System.Linq;
using Wokarol.Utils;
using Random = UnityEngine.Random;




#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Wokarol
{
    [SelectionBase]
    public abstract class Dice : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private Transform model;
        [Space]
        [SerializeField] private int[] values;

        public Rigidbody Body => body;
        public abstract int FaceCount { get; }
        public abstract int FaceEdgeCount { get; }

        private void OnValidate()
        {
            if (values.Length != FaceCount)
                Array.Resize(ref values, FaceCount);
        }

        public abstract (Vector3 normal, Vector3 forward) GetLocalFaceCoordinates(int faceIndex);

        public void ForceValue(int targetValue)
        {
            var actualFaceIndex = GetIndexOfTopFace(considerModelRotation: false);
            var targetFaceIndex = Array.IndexOf(values, targetValue);

            if (targetFaceIndex == -1)
            {
                Debug.LogError($"{LogExtras.Prefix(this)} Could not force {LogExtras.Value(targetValue)} as the dice does not have it");
                return;
            }

            var actualFace = GetLocalFaceCoordinates(actualFaceIndex);
            var targetFace = GetLocalFaceCoordinates(targetFaceIndex);


            var actualfaceRotation = Quaternion.LookRotation(actualFace.forward, actualFace.normal);
            var targetfaceRotation = Quaternion.LookRotation(targetFace.forward, targetFace.normal);

            var correctionRotation = 
                actualfaceRotation * 
                Quaternion.AngleAxis(Random.Range(0, FaceEdgeCount) * (360f / FaceEdgeCount), Vector3.up) * 
                Quaternion.Inverse(targetfaceRotation);

            model.localRotation = correctionRotation;
        }

        public int CheckValue()
        {
            var index = GetIndexOfTopFace(considerModelRotation: true);
            return values[index];
        }

        private int GetIndexOfTopFace(bool considerModelRotation)
        {
            var closestNormalDot = -1f;
            var closestFaceIndex = -1;

            for (int i = 0; i < FaceCount; i++)
            {
                var (faceNormal, _) = GetLocalFaceCoordinates(i);

                if (considerModelRotation)
                {
                    faceNormal = model.localRotation * faceNormal;
                }

                var globalNormal = transform.TransformDirection(faceNormal);

                var dot = Vector3.Dot(globalNormal, Vector3.up);

                if (dot > closestNormalDot)
                {
                    closestNormalDot = dot;
                    closestFaceIndex = i;
                }
            }

            return closestFaceIndex;
        }


#if UNITY_EDITOR
        GUIStyle cachedStyle;

        private void OnDrawGizmosSelected()
        {
            var baseColor = Color.red;
            Handles.color = baseColor;

            Gizmos.matrix = transform.localToWorldMatrix;
            Handles.matrix = transform.localToWorldMatrix;

            for (int i = 0; i < FaceCount; i++)
            {
                var face = GetLocalFaceCoordinates(i);
                var labelStyle = cachedStyle ??= new GUIStyle(GUI.skin.label);

                labelStyle.normal.textColor = baseColor;
                labelStyle.hover.textColor = baseColor;
                labelStyle.active.textColor = baseColor;
                labelStyle.alignment = TextAnchor.MiddleCenter;
                labelStyle.fontSize = 24;

                Handles.color = baseColor * new Color(1, 1, 1, 0.2f);
                Handles.DrawLine(Vector3.zero, face.normal * 1.2f, 2);
                Handles.color = baseColor;

                Handles.color = Color.cyan * new Color(1, 1, 1, 0.4f);
                Handles.DrawLine(face.normal * 1.2f, face.normal * 1.2f + face.forward * 0.2f, 2);
                Handles.color = baseColor;

                Handles.Label(face.normal * 1.2f, $"{(char)('A' + i)} : {values[i]}", labelStyle);
            }
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Dice), true)]
    public class DiceEditor : Editor
    {
        private SerializedProperty valuesProp;
        private int forceValueInput = 1;

        private void OnEnable()
        {
            valuesProp = serializedObject.FindProperty("values");

            forceValueInput = 1;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "values");

            GUILayout.Space(10);

            Dice diceTarget = (Dice)target;
            int faceCount = diceTarget.FaceCount;

            EditorGUILayout.LabelField($"Face Values", EditorStyles.boldLabel);

            if (valuesProp.arraySize != faceCount)
            {
                valuesProp.arraySize = faceCount;
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < faceCount; i++)
            {
                char label = (char)('A' + i);

                SerializedProperty element = valuesProp.GetArrayElementAtIndex(i);

                GUIContent labelContent = new GUIContent($"{label}");

                EditorGUILayout.PropertyField(element, labelContent);
            }
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(15);
            EditorGUILayout.LabelField("Simulation Control", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Check Roll", GUILayout.Width(100)))
            {
                var rolledValue = diceTarget.CheckValue();

                Debug.Log($"{LogExtras.Prefix("Dice")} Rolled {LogExtras.Value(rolledValue)}");
            }

            EditorGUILayout.BeginHorizontal();

            int minValue = 1;
            int maxValue = faceCount;
            forceValueInput = EditorGUILayout.IntField("Target Value", forceValueInput);
            forceValueInput = Mathf.Clamp(forceValueInput, minValue, maxValue);

            if (GUILayout.Button("Force Roll", GUILayout.Width(100)))
            {
                diceTarget.ForceValue(forceValueInput);

                Debug.Log($"{LogExtras.Prefix("Dice")} Forcing {LogExtras.Value(diceTarget.name)} to value: {LogExtras.Value(forceValueInput)}");
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("The 'Force Roll' button is active only during Play Mode.", MessageType.Info);
            }
        }
    }
#endif
}
