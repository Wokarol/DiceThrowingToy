using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Runtime.CompilerServices;

#if !UNITY_2021_2_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif


namespace Wokarol.Editor.Tools
{
    public class RecentAssetsWindow : EditorWindow
    {
        const int maxElementsInRecentList = 15;
        const string randomGUID = "{AD8E9BB2-096D-413F-829F-E83E0A82FD65}";
        static string listPersistanceKey => $"{Application.productName}_{Application.companyName}_{randomGUID}";

        private List<SavedSceneOrPrefab> scenesAndPrefabs = new List<SavedSceneOrPrefab>();
        private Vector2 scrollPos;

        private static GUIStyle closeButtonStyle;
        private static GUIStyle choiceButtonStyle;
        private static GUIStyle emptyListLabelStyle;

        [MenuItem("Tools/Recent Objects/Window")]
        static void ShowWindow()
        {
            var window = GetWindow<RecentAssetsWindow>();
            window.titleContent = new GUIContent("Scenes & Prefabs");
            window.Show();
        }


        private void OnEnable()
        {
            EditorSceneManager.sceneClosed += EditorSceneManager_sceneClosed;
            PrefabStage.prefabStageClosing += PrefabStage_prefabStageClosing;
            PrefabStage.prefabStageOpened += PrefabStage_prefabStageOpened;

            var listJson = EditorPrefs.GetString(listPersistanceKey, JsonUtility.ToJson(new WrappedList()));
            scenesAndPrefabs = JsonUtility.FromJson<WrappedList>(listJson).list;
        }


        private void OnDisable()
        {
            EditorSceneManager.sceneClosed -= EditorSceneManager_sceneClosed;
            PrefabStage.prefabStageClosing -= PrefabStage_prefabStageClosing;
            PrefabStage.prefabStageOpened -= PrefabStage_prefabStageOpened;

            var listJson = JsonUtility.ToJson(new WrappedList() { list = scenesAndPrefabs });
            EditorPrefs.SetString(listPersistanceKey, listJson);
        }

        [MenuItem("Tools/Recent Objects/Clear Editor Prefs")]
        static void ClearPrefs()
        {
            EditorPrefs.DeleteKey(listPersistanceKey);
        }

        private void OnGUI()
        {
            PrepareStyles();

            RemoveNotExistingPaths();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            DrawPrefabsAndScenes();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            DrawClearButton();

        }

        private void DrawPrefabsAndScenes()
        {
            EditorGUILayout.LabelField("Pinned scenes and prefabs:");
            DrawList(true);

            EditorGUILayout.LabelField("Recent scenes and prefabs:");
            DrawList(false);
        }

        private static void PrepareStyles( )
        {
            closeButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset()
            };
            choiceButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft
            };

            emptyListLabelStyle ??= new GUIStyle(GUI.skin.label)
            {
                normal = new GUIStyleState()
                {
                    background = GUI.skin.label.normal.background,
                    scaledBackgrounds = GUI.skin.label.normal.scaledBackgrounds,
                    textColor = GUI.skin.label.normal.textColor * new Color(1, 1, 1, 0.5f),
                }
            };
            emptyListLabelStyle.hover = emptyListLabelStyle.normal;
        }

        private void DrawClearButton()
        {
            GUI.backgroundColor = new Color(1, 0, 0, 0.4f);
            if (GUILayout.Button(new GUIContent("CLEAR ALL"), GUILayout.Height(24)))
            {
                scenesAndPrefabs.Clear();
                Repaint();
            }
            GUI.backgroundColor = Color.white;
        }

        private void RemoveNotExistingPaths()
        {
            scenesAndPrefabs.RemoveAll(s =>
            {
                return string.IsNullOrEmpty(s.Name) || string.IsNullOrEmpty(s.Path) || (AssetDatabase.LoadAssetAtPath<Object>(s.Path) == null);
            });
        }

        private void DrawList(bool drawPinned)
        {
            Event e = Event.current;

            var elementsToDraw = scenesAndPrefabs.Where(s => s.IsPinned == drawPinned);
            if (!elementsToDraw.Any())
            {
                GUILayout.Label("  --- empty ---", emptyListLabelStyle);
                return;
            }

            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            foreach (var s in elementsToDraw.Reverse())
            {
                bool isSceneOrPrefabCurrentlyOpen = false;
                if (stage == null)
                {
                    for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                    {
                        if (EditorSceneManager.GetSceneAt(i).path == s.Path)
                            isSceneOrPrefabCurrentlyOpen = true;
                    }
                }
                else
                {
                    isSceneOrPrefabCurrentlyOpen = stage.assetPath == s.Path;
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(isSceneOrPrefabCurrentlyOpen);

                var icon = AssetDatabase.GetCachedIcon(s.Path);
                var lastIconSize = EditorGUIUtility.GetIconSize();
                EditorGUIUtility.SetIconSize(new Vector2(16, 16));
                if (GUILayout.Button(new GUIContent(s.Name, icon, s.Path), choiceButtonStyle, GUILayout.Height(19)))
                {
                    bool shouldBeSaved = !e.shift || s.IsPrefab;

                    bool saved = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    if (!saved) continue;

                    if (s.IsPrefab)
                    {
                        AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(s.Path));
                    }
                    else
                    {
                        if (e.shift)
                        {
                            EditorSceneManager.OpenScene(s.Path, OpenSceneMode.Additive);
                        }
                        else
                        {
                            EditorSceneManager.OpenScene(s.Path);
                        }
                    }
                }
                EditorGUI.EndDisabledGroup();

                var lockButtonIcon = s.IsPinned ? "Locked" : "Unlocked";
                if (GUILayout.Button(EditorGUIUtility.IconContent(lockButtonIcon), closeButtonStyle, GUILayout.Width(19), GUILayout.Height(19)))
                {
                    ChangePinTo(s.Path, !s.IsPinned);
                    Repaint();
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent("d_clear"), closeButtonStyle, GUILayout.Width(19), GUILayout.Height(19)))
                {
                    scenesAndPrefabs.Remove(s);
                    Repaint();
                }

                EditorGUIUtility.SetIconSize(lastIconSize);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void EditorSceneManager_sceneClosed(Scene scene)
        {
            if (Application.isPlaying) return;

            AddRecentItem(scene.path, scene.name, false);

            Repaint();
        }

        private void PrefabStage_prefabStageClosing(PrefabStage obj)
        {
            AddRecentItem(obj.assetPath, obj.prefabContentsRoot.name, true);

            Repaint();
        }

        private void PrefabStage_prefabStageOpened(PrefabStage obj)
        {
            int sceneCount = SceneManager.loadedSceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                AddRecentItem(scene.path, scene.name, false);
            }

            Repaint();
        }

        private void AddRecentItem(string path, string name, bool prefab)
        {
            for (int i = 0; i < scenesAndPrefabs.Count; i++)
            {
                if (scenesAndPrefabs[i].Path == path)
                {
                    // If the entry is already in the list, move it to top
                    var foundEntry = scenesAndPrefabs[i];
                    scenesAndPrefabs.RemoveAt(i);
                    scenesAndPrefabs.Add(foundEntry);
                    return;
                }
            }

            var s = new SavedSceneOrPrefab()
            {
                Path = path,
                Name = name,
                IsPrefab = prefab,
                IsPinned = false,
            };

            scenesAndPrefabs.Add(s);

            // If the limit is reached, we remove first element that is not pinned
            int pinnedItems = scenesAndPrefabs.Where(a => a.IsPinned).Count();
            int scenesAndPrefabsMaxCount = maxElementsInRecentList + pinnedItems;

            if (scenesAndPrefabs.Count > scenesAndPrefabsMaxCount)
                scenesAndPrefabs.Remove(scenesAndPrefabs.First(a => !a.IsPinned));
        }

        private void ChangePinTo(string path, bool pinned)
        {
            for (int i = 0; i < scenesAndPrefabs.Count; i++)
            {
                if (scenesAndPrefabs[i].Path == path)
                {
                    var s = scenesAndPrefabs[i];
                    s.IsPinned = pinned;
                    scenesAndPrefabs[i] = s;
                    break;
                }
            }
        }

        [System.Serializable]
        struct SavedSceneOrPrefab
        {
            public string Path;
            public string Name;
            public bool IsPrefab;
            public bool IsPinned;
        }

        [System.Serializable]
        struct WrappedList
        {
            public List<SavedSceneOrPrefab> list;
        }
    }
}