using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Wokarol.Editor.Tools
{
    public class VersionPromptBeforeBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder { get; }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (EditorUtility.DisplayDialog("Are you sure?", $"Do you want to build a project with following data:\n\nName: \t\t{Application.productName}\nCompany: \t{Application.companyName}\nVersion:    \t{Application.version}", "Yes", "No"))
            {
                return;
            }
            else
            {
                throw new System.Exception("User canceled the operation");
            }
        }
    }
}
