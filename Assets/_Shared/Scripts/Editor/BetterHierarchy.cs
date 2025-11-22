using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Wokarol.EditorExtensions
{
	[InitializeOnLoad]
	public static class BetterHierarchy
	{
		private const string toggleStyleName = "OL Toggle";
		private const string mixedToggleStyleName = "OL ToggleMixed";

		private static bool includeNotImportant = true;
		private const string includeNotImportantPrefsKey = "{E0EF3D35-59F0-4531-8040-7341E3093C84}";

		// ===============================================================================================

		private static readonly Dictionary<Type, string> iconOverrides = new Dictionary<Type, string>
		{
		};

		private static readonly HashSet<Type> importantList = new HashSet<Type>
		{
			typeof(Camera),
			typeof(Rigidbody2D),
			typeof(Rigidbody),
			typeof(TMPro.TMP_Text),
			typeof(Collider),
			typeof(Collider2D),
			typeof(Renderer),
			typeof(CanvasRenderer),
		};

		private static readonly HashSet<Type> blacklist = new HashSet<Type>
		{
			typeof(Transform),
			typeof(RectTransform),
			typeof(ParticleSystemRenderer),
		};


		// ===============================================================================================

		private static readonly Dictionary<Type, Texture> iconCacheByType = new Dictionary<Type, Texture>();
        private static readonly Dictionary<Texture, int> reusableUsedIconsCollection = new Dictionary<Texture, int>();
        private static readonly List<(Texture texture, bool important)> reusableIconsToDrawCollection = new List<(Texture icon, bool important)>();

        static BetterHierarchy()
		{
			EditorApplication.hierarchyWindowItemOnGUI = DrawItem;
			includeNotImportant = EditorPrefs.GetBool(includeNotImportantPrefsKey);
		}

		[MenuItem("Tools/Better Hierarchy/Toggle Non-Important")]
		public static void ToggleNonImportant()
		{
			includeNotImportant = !includeNotImportant;
			EditorPrefs.SetBool(includeNotImportantPrefsKey, includeNotImportant);
			EditorApplication.RepaintHierarchyWindow();
		}

		[MenuItem("Tools/Better Hierarchy/Clear Icon Cache")]
		public static void CLearIconCache()
		{
			iconCacheByType.Clear();
		}

		static void DrawItem(int instanceID, Rect rect)
		{
			// Gets object for given item
			GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

			if (go != null)
			{
				bool isHeader = go.name.StartsWith("---");

				bool shouldHaveActivityToggle = !isHeader || go.transform.childCount > 0;

				DrawComponentIcons(rect, go, out int numberOfIconsDrawn);

				if (shouldHaveActivityToggle)
				{
					DrawActivityToggle(rect, go);
				}
				if (isHeader)
				{
					DrawHeader(rect, go, shouldHaveActivityToggle, numberOfIconsDrawn);
				}
			}
		}

		private static void DrawHeader(Rect rect, GameObject go, bool cutLeft, int componentDrawCut)
		{
			// Creating highlight rect and style
			Rect highlightRect = new Rect(rect);
			highlightRect.width -= highlightRect.height;

			GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.fontStyle = FontStyle.Bold;
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.fontSize -= 1;
			highlightRect.height -= 1;
			highlightRect.y += 1;

			// Drawing background
			string colorHTML = EditorGUIUtility.isProSkin ? "#2D2D2D" : "#AAAAAA";
			ColorUtility.TryParseHtmlString(colorHTML, out Color headerColor);

			var headerRect = new Rect(highlightRect);
			headerRect.y -= 1;
			headerRect.xMin -= 28;
			headerRect.xMax += 28;

			var fullRect = new Rect(headerRect);

			if (PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab && componentDrawCut == 0)
			{
				headerRect.xMax -= 10;
			}

			if (componentDrawCut > 0)
			{
				headerRect.xMax -= 16;
				headerRect.xMax -= componentDrawCut * 16;
			}

			if (cutLeft)
			{
				headerRect.xMin += 28;
			}

			EditorGUI.DrawRect(headerRect, headerColor);

			// Offseting text
			highlightRect.height -= 2;

			// Drawing label
			EditorGUI.LabelField(highlightRect, go.name.Replace("---", "").ToUpperInvariant(), labelStyle);
		}

		private static void DrawComponentIcons(Rect rect, GameObject go, out int numberOfIconsDrawn)
		{

            reusableUsedIconsCollection.Clear();
            reusableIconsToDrawCollection.Clear();

			var usedIcons = reusableUsedIconsCollection;
			var iconsToDraw = reusableIconsToDrawCollection;
			var offset = 0;

            foreach (var component in go.GetComponents<Component>())
			{
				if (component == null)
					continue;

				Type type = component.GetType();

				if (type == typeof(CinemachineBrain))
				{
					offset++;
					continue;
				}

				if (blacklist.Contains(type))
					continue;

				Texture texture = GetIconFor(component, type);
				bool important = CheckTypeRecursive(type, importantList);

				if (!includeNotImportant && !important)
					continue;

				if (usedIcons.TryGetValue(texture, out int index))
				{
					var icon = iconsToDraw[index];
					icon.important |= important;
					iconsToDraw[index] = icon;
				}
				else
				{
					iconsToDraw.Add((texture, important));
					usedIcons.Add(texture, iconsToDraw.Count - 1);
				}
			}

			for (int i = 0; i < iconsToDraw.Count; i++)
			{
				(Texture texture, bool important) = iconsToDraw[i];
				Color tint = important
					? new Color(1, 1, 1, 1)
					: new Color(0.8f, 0.8f, 0.8f, 0.25f);
				GUI.DrawTexture(GetRightRectWithOffset(rect, i + offset), texture, ScaleMode.ScaleToFit, true, 0, tint * GUI.color, 0, 0);
			}

			numberOfIconsDrawn = iconsToDraw.Count;
		}

		private static bool CheckTypeRecursive(Type t, HashSet<Type> set)
		{
			if (set.Contains(t))
				return true;

			if (t.BaseType == null)
				return false;

			return CheckTypeRecursive(t.BaseType, set);
		}

		private static Texture GetIconFor(Component c, Type type)
		{
			if (iconCacheByType.TryGetValue(type, out var tex))
				return tex;

			var texture = iconOverrides.TryGetValue(type, out string icon)
				? EditorGUIUtility.IconContent(icon).image
				: EditorGUIUtility.ObjectContent(c, type).image;

			iconCacheByType[type] = texture;
			return texture;
		}

		private static void DrawActivityToggle(Rect rect, GameObject go)
		{
			// Gets style of toggle
			bool active = go.activeInHierarchy;
			Color lastGUIColor = GUI.color;

			GUIStyle toggleStyle = active
				? toggleStyleName
				: toggleStyleName; // Previously used to display mixed toggle

			// Sets rect for toggle
			var toggleRect = new Rect(rect);
			toggleRect.width = toggleRect.height;
			toggleRect.x -= 28;

			if (!active && go.activeSelf)
				GUI.color *= new Color(1, 1, 1, 0.3f);

			// Creates toggle
			bool state = GUI.Toggle(toggleRect, go.activeSelf, GUIContent.none, toggleStyle);
			GUI.color = lastGUIColor;


			// Sets game object's active state to result of toggle
			if (state != go.activeSelf)
			{
				Undo.RecordObject(go, $"{(state ? "Enabled" : "Disabled")}");
				go.SetActive(state);
				Undo.FlushUndoRecordObjects();
			}
		}

		static Rect GetRightRectWithOffset(Rect rect, int offset)
		{
			var newRect = new Rect(rect);
			newRect.width = newRect.height;
			newRect.x = rect.x + rect.width - (rect.height * offset) - 16;
			return newRect;
		}
	}
}