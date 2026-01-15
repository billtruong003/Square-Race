#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SpellTech.SceneSwitcher.Pro
{
    [FilePath("UserSettings/SpellTechSceneSwitcherV3.asset", FilePathAttribute.Location.ProjectFolder)]
    public class SceneSwitcherProfile : ScriptableSingleton<SceneSwitcherProfile>
    {
        [Serializable]
        public class SceneEntry
        {
            public string Guid;
            public string Name;
            public string Path;
            public bool IsPinned;
            public Color Tint = Color.clear;
            public long LastAccessed;
            public bool IsValid => !string.IsNullOrEmpty(Guid) && !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(Guid));
        }

        [SerializeField] private List<SceneEntry> _entries = new List<SceneEntry>();
        [SerializeField] private bool _includeBuildSettingsScenes = true;
        [SerializeField] private int _maxRecentScenes = 15;

        public List<SceneEntry> Entries => _entries;
        public bool IncludeBuildScenes => _includeBuildSettingsScenes;

        public void RegisterScene(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return;

            var entry = _entries.FirstOrDefault(e => e.Guid == guid);
            if (entry == null)
            {
                entry = new SceneEntry { Guid = guid, Path = path, Name = Path.GetFileNameWithoutExtension(path) };
                _entries.Add(entry);
            }

            entry.LastAccessed = DateTime.UtcNow.Ticks;
            entry.Path = path;

            CleanList();
            Save(true);
        }

        public void TogglePin(string guid)
        {
            var entry = _entries.FirstOrDefault(e => e.Guid == guid);
            if (entry != null) { entry.IsPinned = !entry.IsPinned; Save(true); }
        }

        public void SetColor(string guid, Color c)
        {
            var entry = _entries.FirstOrDefault(e => e.Guid == guid);
            if (entry != null) { entry.Tint = c; Save(true); }
        }

        private void CleanList()
        {
            var pinned = _entries.Where(x => x.IsPinned).ToList();
            var recent = _entries.Where(x => !x.IsPinned).OrderByDescending(x => x.LastAccessed).Take(_maxRecentScenes).ToList();
            _entries.Clear();
            _entries.AddRange(pinned);
            _entries.AddRange(recent);
        }
    }

    public static class SceneHelper
    {
        public static void Open(string path, bool additive)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                SceneSwitcherProfile.instance.RegisterScene(path);
                EditorSceneManager.OpenScene(path, additive ? OpenSceneMode.Additive : OpenSceneMode.Single);
            }
        }

        public static List<SceneSwitcherProfile.SceneEntry> GetFiltered(string search)
        {
            var list = new List<SceneSwitcherProfile.SceneEntry>(SceneSwitcherProfile.instance.Entries);
            if (SceneSwitcherProfile.instance.IncludeBuildScenes)
            {
                foreach (var s in EditorBuildSettings.scenes)
                {
                    if (s.enabled && !string.IsNullOrEmpty(s.path))
                    {
                        var g = AssetDatabase.AssetPathToGUID(s.path);
                        if (!list.Any(x => x.Guid == g))
                            list.Add(new SceneSwitcherProfile.SceneEntry { Guid = g, Path = s.path, Name = Path.GetFileNameWithoutExtension(s.path) });
                    }
                }
            }

            var q = search?.ToLowerInvariant() ?? "";
            return list.Where(x => x.IsValid && x.Name.ToLowerInvariant().Contains(q))
                       .OrderByDescending(x => x.IsPinned)
                       .ThenByDescending(x => x.LastAccessed)
                       .ToList();
        }
    }

    public class ScenePopup : PopupWindowContent
    {
        private string _search = "";
        private Vector2 _scroll;
        private List<SceneSwitcherProfile.SceneEntry> _list;
        private GUIStyle _itemStyle, _selectedStyle;
        private int _index = 0;
        private bool _close;

        public override Vector2 GetWindowSize() => new Vector2(300, 350);

        public override void OnOpen()
        {
            _search = "";
            Refresh();
        }

        public override void OnClose() { }

        private void Refresh()
        {
            _list = SceneHelper.GetFiltered(_search);
            _index = Mathf.Clamp(_index, 0, Mathf.Max(0, _list.Count - 1));
        }

        public override void OnGUI(Rect rect)
        {
            if (_close) { editorWindow.Close(); return; }
            if (_itemStyle == null) SetupStyles();

            DrawHeader();
            DrawList();
            HandleInput();
        }

        private void SetupStyles()
        {
            _itemStyle = new GUIStyle(GUI.skin.label) { padding = new RectOffset(8, 8, 4, 4), alignment = TextAnchor.MiddleLeft, richText = true, fontSize = 12 };
            _selectedStyle = new GUIStyle(_itemStyle);
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.24f, 0.49f, 0.9f, 0.5f));
            tex.Apply();
            _selectedStyle.normal.background = tex;
            _selectedStyle.normal.textColor = Color.white;
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUI.SetNextControlName("Search");
            var n = GUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            if (n != _search) { _search = n; Refresh(); }

            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus"), EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                var s = SceneManager.GetActiveScene();
                if (!string.IsNullOrEmpty(s.path))
                {
                    SceneSwitcherProfile.instance.RegisterScene(s.path);
                    SceneSwitcherProfile.instance.TogglePin(AssetDatabase.AssetPathToGUID(s.path));
                    Refresh();
                }
            }
            GUILayout.EndHorizontal();
            EditorGUI.FocusTextInControl("Search");
        }

        private void DrawList()
        {
            _scroll = GUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _list.Count; i++)
            {
                var entry = _list[i];
                var r = GUILayoutUtility.GetRect(GUIContent.none, _itemStyle, GUILayout.Height(24));

                if (Event.current.type == EventType.Repaint)
                {
                    (i == _index ? _selectedStyle : _itemStyle).Draw(r, GUIContent.none, false, false, false, false);

                    if (entry.Tint != Color.clear)
                        EditorGUI.DrawRect(new Rect(r.x, r.y, 3, r.height), entry.Tint);

                    var icon = AssetDatabase.GetCachedIcon(entry.Path);
                    if (icon) GUI.DrawTexture(new Rect(r.x + 5, r.y + 4, 16, 16), icon);

                    GUI.Label(new Rect(r.x + 26, r.y, r.width - 50, r.height), entry.Name, i == _index ? _selectedStyle : _itemStyle);

                    if (entry.IsPinned)
                        GUI.DrawTexture(new Rect(r.width - 20, r.y + 4, 16, 16), EditorGUIUtility.IconContent("Favorite Icon").image);
                }

                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    if (Event.current.button == 0)
                    {
                        _index = i;
                        SceneHelper.Open(entry.Path, Event.current.control);
                        _close = true;
                    }
                    else if (Event.current.button == 1)
                    {
                        DoContext(entry);
                    }
                }
            }
            GUILayout.EndScrollView();
        }

        private void HandleInput()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            if (e.keyCode == KeyCode.DownArrow) { _index = Mathf.Min(_index + 1, _list.Count - 1); e.Use(); }
            if (e.keyCode == KeyCode.UpArrow) { _index = Mathf.Max(_index - 1, 0); e.Use(); }
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                if (_index >= 0 && _index < _list.Count) { SceneHelper.Open(_list[_index].Path, e.control); _close = true; }
                e.Use();
            }
        }

        private void DoContext(SceneSwitcherProfile.SceneEntry e)
        {
            var m = new GenericMenu();
            m.AddItem(new GUIContent("Pin / Unpin"), e.IsPinned, () => { SceneSwitcherProfile.instance.TogglePin(e.Guid); Refresh(); });
            m.AddItem(new GUIContent("Color/None"), false, () => SceneSwitcherProfile.instance.SetColor(e.Guid, Color.clear));
            m.AddItem(new GUIContent("Color/Red"), false, () => SceneSwitcherProfile.instance.SetColor(e.Guid, Color.red));
            m.AddItem(new GUIContent("Color/Blue"), false, () => SceneSwitcherProfile.instance.SetColor(e.Guid, Color.cyan));
            m.AddItem(new GUIContent("Color/Green"), false, () => SceneSwitcherProfile.instance.SetColor(e.Guid, Color.green));
            m.ShowAsContext();
        }
    }

    [InitializeOnLoad]
    public static class ToolbarUiInjector
    {
        private static readonly Type ToolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static VisualElement _root;
        private static VisualElement _container;
        private static TextElement _label;
        private static Image _icon;

        static ToolbarUiInjector()
        {
            EditorApplication.delayCall += Initialize;
            EditorApplication.update += EnsureAttached;
        }

        private static void EnsureAttached()
        {
            if (_root == null || _root.Q("SpellTechSceneSwitcherV3") == null) Initialize();
        }

        private static void Initialize()
        {
            var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
            if (toolbars.Length == 0) return;

            _root = ToolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(toolbars[0]) as VisualElement;
            if (_root == null) return;

            var playZone = _root.Q("ToolbarZonePlayMode");
            if (playZone == null) return;

            if (_root.Q("SpellTechSceneSwitcherV3") != null) return;

            _container = CreateButton();

            // Insert after play buttons
            int index = playZone.parent.IndexOf(playZone);
            playZone.parent.Insert(index + 1, _container);

            UpdateLabel(SceneManager.GetActiveScene(), OpenSceneMode.Single);
            EditorSceneManager.sceneOpened += UpdateLabel;
        }

        private static VisualElement CreateButton()
        {
            var btn = new VisualElement
            {
                name = "SpellTechSceneSwitcherV3",
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.Center,
                    height = 20,
                    marginRight = 6,
                    marginLeft = 6,
                    paddingLeft = 4,
                    paddingRight = 4,
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3,
                    alignSelf = Align.Center
                }
            };

            // Quan trọng: Class này giúp nó giống hệt style của Unity Toolbar (hover, press effect)
            btn.AddToClassList("unity-toolbar-button");
            btn.AddToClassList("unity-editor-toolbar-element");

            _icon = new Image
            {
                image = EditorGUIUtility.IconContent("d_SceneAsset Icon").image,
                style = { width = 16, height = 16, marginRight = 2 }
            };

            _label = new TextElement
            {
                text = "Scenes",
                style =
                {
                    unityTextAlign = TextAnchor.MiddleLeft,
                    marginLeft = 2,
                    marginRight = 2,
                    color = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.9f, 0.9f) : Color.black,
                    fontSize = 12,
                    width = 100,
                    overflow = Overflow.Hidden
                }
            };

            var arrow = new Image
            {
                image = EditorGUIUtility.IconContent("icon dropdown").image,
                style = { width = 12, height = 12, marginLeft = 2, opacity = 0.7f },
                tintColor = EditorGUIUtility.isProSkin ? Color.white : Color.black
            };

            btn.Add(_icon);
            btn.Add(_label);
            btn.Add(arrow);

            btn.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0)
                    UnityEditor.PopupWindow.Show(btn.worldBound, new ScenePopup());
            });

            return btn;
        }

        private static void UpdateLabel(Scene scene, OpenSceneMode mode)
        {
            if (_label != null)
            {
                string name = string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name;
                _label.text = name;
                _label.tooltip = string.IsNullOrEmpty(scene.path) ? "Unsaved Scene" : scene.path;
            }
        }
    }
}
#endif