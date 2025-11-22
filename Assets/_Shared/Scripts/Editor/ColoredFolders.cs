using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Wokarol.ColoredFolders
{
    [InitializeOnLoad]
    public class ColoredFolders
    {
        private const string projectPrefix = "Assets/_Project";
        private const string sharedPrefix = "Assets/_Shared";

        static ColoredFolders()
        {
            EditorApplication.projectWindowItemOnGUI -= ReplaceFolders;
            EditorApplication.projectWindowItemOnGUI += ReplaceFolders;
        }

        private static void ReplaceFolders(string guid, Rect selectionRect)
        {
            if (selectionRect.x < 15) return; // This excludes the assets at the root indent, which solves the issue of Two Column Layout in List mode
            if (selectionRect.width <= selectionRect.height) return; // This excludes non-wide assets, which solves the issue of Two Column Layout in Grid mode

            var folderPath = AssetDatabase.GUIDToAssetPath(guid);

            var isProjectFolder = FastStringCompare(folderPath, projectPrefix);
            var isSharedFolder = FastStringCompare(folderPath, sharedPrefix);

            if (isProjectFolder || isSharedFolder)
            {
                var colorMult = isProjectFolder
                    ? new Color(1, 1, 1, 0.4f)
                    : new Color(1, 1, 1, 0.2f);

                // Drawing primary bar
                var barWidth = 4;
                var barRect = new Rect(selectionRect)
                {
                    width = barWidth
                };
                barRect.position = new Vector2(6, barRect.position.y);

                var barColor = new Color(0 / 255f, 255 / 255f, 162 / 255f);

                EditorGUI.DrawRect(barRect, barColor * colorMult);

                // Drawing secondary bar
                var offset = isProjectFolder ? projectPrefix.Length : sharedPrefix.Length;
                barRect.position += Vector2.right * 14;

                var secondaryBarColor = (Color?)null;
                if (FastStringCompare(folderPath, "/Scenes", offset: offset)) secondaryBarColor = new Color(0 / 255f, 128 / 255f, 255 / 255f);
                if (FastStringCompare(folderPath, "/Scripts", offset: offset)) secondaryBarColor = new Color(228 / 255f, 1 / 255f, 255 / 255f);
                if (FastStringCompare(folderPath, "/Content", offset: offset)) secondaryBarColor = new Color(255 / 255f, 162 / 255f, 0 / 255f);
                if (FastStringCompare(folderPath, "/Packages", offset: offset)) secondaryBarColor = new Color(255 / 255f, 222 / 255f, 0 / 255f);

                if (secondaryBarColor != null)
                {
                    EditorGUI.DrawRect(barRect, secondaryBarColor.Value * colorMult);
                }
            }

        }

        private static bool FastStringCompare(string input, string prefix, int offset = 0)
        {
            if (input.Length < (prefix.Length + offset))
            {
                return false;
            }

            for (int i = 0; i < prefix.Length; i++)
            {
                if (input[i + offset] != prefix[i]) return false;
            }

            return true;
        }
    }
}
