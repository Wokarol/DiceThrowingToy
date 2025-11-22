using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class StupidColorSpaceFix : EditorWindow
{
    private Color colorLinear;
    private Color colorGamma;


    [MenuItem("Window/Color Converter")]
    public static void ShowWindow()
    {
        var window = EditorWindow.GetWindow(typeof(StupidColorSpaceFix));
        window.titleContent = new GUIContent("Color Converter");
    }

    private void OnEnable()
    {
        colorLinear = Color.white;
        colorGamma = colorLinear.gamma;
    }

    private void OnGUI()
    {
        var newLinear = EditorGUILayout.ColorField("Linear", colorLinear);
        if (newLinear != colorLinear)
        {
            colorLinear = newLinear;
            colorGamma = colorLinear.gamma;
        }

        var newGamma = EditorGUILayout.ColorField("Gamma", colorGamma);
        if (newGamma != colorGamma)
        {
            colorLinear = newGamma.linear;
            colorGamma = newGamma;
        }
    }
}
