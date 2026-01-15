using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.SceneManagement;

namespace SpellTech.SceneSwitcherV2
{
    public class SceneSwitcherToolWindowV2 : EditorWindow
    {
        // ================= DATA =================
        private List<SceneAsset> bookmarkedScenes = new();

        private VisualElement root;
        private VisualElement mainContainer;
        private VisualElement listContainer;

        private ListView sceneListView;
        private DropdownField modeDropdown;
        private DropdownField tagFilterDropdown;
        private TextField searchField;

        private Button loadButton;
        private Button additiveButton;
        private Button pingButton;
        private Button addTagButton;

        private Button addToBuildButton;
        private Button bookmarkSelectedButton;
        private Button addCurrentButton;
        private Button removeSelectedButton;
        private Button clearAllButton;
        private Button settingButton;
        private Button creditButton;
        private VisualElement emptyListHint;


        public StyleSheet toolStyleSheet;
        private const string BookmarkedKey = "SSV2_Bookmarks";
        // ================= STATIC HELPERS (Toolbar) =================
        /// <summary>
        /// Used by toolbar shortcuts without requiring an open window instance.
        /// </summary>
        public static List<SceneAsset> LoadBookmarkedScenesFromPrefs()
        {
            string raw = EditorPrefs.GetString(BookmarkedKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return new List<SceneAsset>();

            return raw.Split(';')
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SceneAsset>)
                .Where(s => s != null)
                .ToList();
        }

        /// <summary>
        /// Toolbar shortcut: add the currently active scene to bookmarks.
        /// </summary>
        public static void AddCurrentSceneToBookmarksStatic()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path)) return;

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            if (sceneAsset == null) return;

            var current = LoadBookmarkedScenesFromPrefs();
            string newGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sceneAsset));
            if (string.IsNullOrEmpty(newGuid)) return;

            bool already = current.Any(s =>
            {
                if (s == null) return false;
                string g = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(s));
                return !string.IsNullOrEmpty(g) && g == newGuid;
            });

            if (already) return;

            current.Add(sceneAsset);

            var guids = current
                .Where(s => s != null)
                .Select(s => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(s)))
                .Where(g => !string.IsNullOrEmpty(g))
                .Distinct();

            EditorPrefs.SetString(BookmarkedKey, string.Join(";", guids));

            if (EditorPrefs.GetBool("SSV2_Debug", false))
            {
                Debug.Log($"SSV2: Bookmarked active scene: {scene.path}");
            }
        }

        private const float MinWidthForHorizontalLayout = 520f;

        private bool isHorizontalLayout = true;

        internal static string scriptFolder;


        // ================= MENU =================
        [MenuItem("Tools/SpellTech/Scene Switcher V2")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<SceneSwitcherToolWindowV2>("Scene Switcher V2");
            wnd.minSize = new Vector2(360, 320);
        }

        // ================= UNITY =================
        public void OnEnable()
        {
            LoadBookmarks();
        }

        public void CreateGUI()
        {
            root = rootVisualElement;
            ResolvePaths();

            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                Path.Combine(scriptFolder, "SceneSwitcherToolV2.uxml"));
            toolStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                Path.Combine(scriptFolder, "SceneSwitcherToolV2.uss"));

            uxml.CloneTree(root);
            root.styleSheets.Add(toolStyleSheet);   
            BindUI();
            SetupHeader();
            SetupListView();
            RegisterCallbacks();
            SetupIcons(root); 
            SetupDragAndDrop();

            root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            UpdateLayoutClasses();
            RefreshList();
        }

        // ================= PATH =================
        private void ResolvePaths()
        {
            var script = MonoScript.FromScriptableObject(this);
            scriptFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(script));
        }

        // ================= UI =================
        private void BindUI()
        {
            mainContainer = root.Q("main-container-v2");
            listContainer = root.Q("list-container-v2");

            sceneListView = root.Q<ListView>("scene-list-v2");
            modeDropdown = root.Q<DropdownField>("mode-dropdown-v2");
            tagFilterDropdown = root.Q<DropdownField>("tag-filter-dropdown-v2");
            searchField = root.Q<TextField>("search-field-v2");

            loadButton = root.Q<Button>("load-button-v2");
            additiveButton = root.Q<Button>("additive-button-v2");
            pingButton = root.Q<Button>("ping-button-v2");
            addToBuildButton = root.Q<Button>("add-to-build-button-v2");
            addTagButton = root.Q<Button>("add-tag-global-btn");
            addCurrentButton = root.Q<Button>("add-current-button-v2");
            removeSelectedButton = root.Q<Button>("remove-selected-button-v2");
            clearAllButton = root.Q<Button>("clear-all-button-v2");
            emptyListHint = root.Q<VisualElement>("drag-hint");
            settingButton = root.Q<Button>("settings-button-v2");
            creditButton = root.Q<Button>("credits-button-v2");
            bookmarkSelectedButton = root.Q<Button>("bookmark-selected-button-v2");
        }

        private void SetupHeader()
        {
            modeDropdown.choices = new List<string> { "Bookmarks", "Project Scenes", "Scenes In Build" };
            modeDropdown.index = 0;

            tagFilterDropdown.choices = BuildTagFilter();
            tagFilterDropdown.index = 0;

            root.Q<Image>("search-icon-v2").image =
                EditorGUIUtility.IconContent("d_Search Icon").image;
        }

        // ================= LIST VIEW =================
        private void SetupListView()
        {
            sceneListView.fixedItemHeight = 32;
            sceneListView.selectionType = SelectionType.Single;

            sceneListView.makeItem = () =>
    {
        var item = new VisualElement();
        item.AddToClassList("scene-item-v2");

        // ⚠️ RẤT QUAN TRỌNG
        item.pickingMode = PickingMode.Position;

        // 🔍 DEBUG CLICK
        item.RegisterCallback<MouseDownEvent>(evt =>
        {

        });

        var left = new VisualElement { name = "item-left" };
        var icon = new Image { name = "item-icon" };
                var label = new Label { name = "item-name" };
                label.AddToClassList("scene-name-v2");

        left.Add(icon);
        left.Add(label);

        var right = new VisualElement { name = "item-right" };
        var popup = new PopupField<string>(
            SceneSwitcherTagManagerV2.GetGlobalTags(),
            SceneSwitcherTagManagerV2.Untagged
        )
        {
            name = "item-tag-popup"
        };

        right.Add(popup);

        item.Add(left);
        item.Add(right);

        return item;
    };
            sceneListView.bindItem = (el, i) =>
            {
                var scene = sceneListView.itemsSource[i] as SceneAsset;
                if (!scene) return;

                el.userData = scene;

                var path = AssetDatabase.GetAssetPath(scene);
                var guid = AssetDatabase.AssetPathToGUID(path);

                el.Q<Image>("item-icon").image =
                    EditorGUIUtility.IconContent("d_SceneAsset Icon").image;

                var nameLabel = el.Q<Label>("item-name");
                nameLabel.text = Path.GetFileNameWithoutExtension(path);
                nameLabel.AddToClassList("in-build-scene-text-v2");
                nameLabel.EnableInClassList("in-build-scene-text-v2", IsSceneInBuild(path));
                

                //Debug.Log($"[V2 DEBUG] Binding item: {nameLabel.text}, IsSceneInBuild(path)={IsSceneInBuild(path)}");

                var popup = el.Q<PopupField<string>>("item-tag-popup");

                popup.choices = SceneSwitcherTagManagerV2.GetGlobalTags();
                popup.SetValueWithoutNotify(
                    SceneSwitcherTagManagerV2.GetSceneTag(guid)
                );

                popup.RegisterValueChangedCallback(evt =>
                {
                    SceneSwitcherTagManagerV2.SetSceneTag(guid, evt.newValue);
                    RefreshList();
                });
            };

            sceneListView.selectionChanged += selection =>
            {


                UpdateButtons();
            };

            sceneListView.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2 && evt.button == 0)
                    LoadSelected(OpenSceneMode.Single);
            });
        }

        // ================= CALLBACKS =================
        private void RegisterCallbacks()
        {
            searchField.RegisterValueChangedCallback(_ => RefreshList());
            modeDropdown.RegisterValueChangedCallback(_ => RefreshList());
            tagFilterDropdown.RegisterValueChangedCallback(_ => RefreshList());

            addTagButton.clicked += OnAddNewTag;
            loadButton.clicked += () => LoadSelected(OpenSceneMode.Single);
            additiveButton.clicked += () => LoadSelected(OpenSceneMode.Additive);

            settingButton.clicked += () =>
            {
                SettingsWindowV2.ShowWindow();
            };
            creditButton.clicked += () =>
            {
                CreditsWindowV2.ShowWindow();
            };

            pingButton.clicked += PingSelected;
            if (addToBuildButton != null)
                addToBuildButton.clicked += AddSelectedToBuildSettings;
            addCurrentButton.clicked += AddCurrentSceneToBookmarks;
            removeSelectedButton.clicked += RemoveSelectedFromBookmarks;
            clearAllButton.clicked += ClearAllBookmarks;
            bookmarkSelectedButton.clicked += () =>
            {
                if (sceneListView.selectedItem is SceneAsset s)
                {
                    AddSceneToBookmarks(s, assignCurrentFilterTag: true);
                }
            };
        }

        private void SetupIcons(VisualElement root)
        {
            const float iconSize = 16f;

            SetIcon(root, "search-icon-v2", "d_Search Icon", iconSize);

            SetIcon(root, "load-icon-v2", "d_PlayButton", iconSize);
            SetIcon(root, "additive-icon-v2", "d_Toolbar Plus", iconSize);
            SetIcon(root, "ping-icon-v2", "d_FolderOpened Icon", iconSize);

            // Build Settings
            SetIcon(root, "add-to-build-icon-v2", "d_Scene", iconSize);

            SetIcon(root, "add-current-icon-v2", "d_Toolbar Plus More", iconSize);
            SetIcon(root, "remove-selected-icon-v2", "d_Toolbar Minus", iconSize);
            SetIcon(root, "remove-all-icon-v2", "d_TreeEditor.Trash", iconSize);

            SetIcon(root, "settings-icon-v2", "d_SettingsIcon", iconSize);
            SetIcon(root, "credits-icon-v2", "d_Help", iconSize);
            SetIcon(root, "bookmark-selected-icon-v2", "d_Favorite", iconSize);
        }


        // ================= LAYOUT =================
        public void OnGeometryChanged(GeometryChangedEvent evt)
        {
            bool horizontal = evt.newRect.width > MinWidthForHorizontalLayout;
            if (horizontal != isHorizontalLayout)
            {
                isHorizontalLayout = horizontal;
                UpdateLayoutClasses();
            }
        }
        
        private void UpdateLayoutClasses()
        {
            mainContainer.EnableInClassList("wide-layout-v2", isHorizontalLayout);
            mainContainer.EnableInClassList("narrow-layout-v2", !isHorizontalLayout);
        }

        // ================= DATA =================
        private void RefreshList()
        {
            string q = searchField.value?.ToLower() ?? "";
            string tag = tagFilterDropdown.value;
            bool bookmarks = modeDropdown.index == 0;

            List<SceneAsset> source;
            if (modeDropdown.index == 0)
            {
                source = bookmarkedScenes;
            }
            else if (modeDropdown.index == 1)
            {
                source = AssetDatabase.FindAssets("t:Scene")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<SceneAsset>)
                    .Where(s => s != null)
                    .ToList();
            }
            else // Scenes In Build
            {
                source = EditorBuildSettings.scenes
                    .Where(s => s != null && s.enabled && !string.IsNullOrEmpty(s.path))
                    .Select(s => AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path))
                    .Where(s => s != null)
                    .ToList();
            }

            sceneListView.itemsSource = source
                .Where(s =>
                {
                    string name = s.name.ToLower();
                    string t = SceneSwitcherTagManagerV2.GetSceneTag(
                        AssetDatabase.AssetPathToGUID(
                            AssetDatabase.GetAssetPath(s)));

                    return (string.IsNullOrEmpty(q) || name.Contains(q)) &&
                           (tag == "All Tags" || t == tag);
                })
                .ToList();


            emptyListHint.style.display = sceneListView.itemsSource.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            sceneListView.Rebuild();
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            bool hasSelection = sceneListView.selectedIndex >= 0;
            loadButton.SetEnabled(hasSelection);
            additiveButton.SetEnabled(hasSelection);
            pingButton.SetEnabled(hasSelection);

            bookmarkSelectedButton.SetEnabled(hasSelection && !bookmarkedScenes.Contains(sceneListView.selectedItem as SceneAsset));

            if (addToBuildButton != null)
            {
                bool canAddToBuild = false;
                if (hasSelection && sceneListView.selectedItem is SceneAsset s && s != null)
                {
                    string p = AssetDatabase.GetAssetPath(s);
                    canAddToBuild = !string.IsNullOrEmpty(p) && !IsSceneInBuild(p);
                }
                addToBuildButton.SetEnabled(canAddToBuild);
            }

            bool isBookmarksMode = modeDropdown.index == 0;
            addCurrentButton.SetEnabled(isBookmarksMode);
            clearAllButton.SetEnabled(isBookmarksMode && bookmarkedScenes != null && bookmarkedScenes.Count > 0);
            removeSelectedButton.SetEnabled(isBookmarksMode && hasSelection);
        }

        // ================= ACTIONS =================
        private void LoadSelected(OpenSceneMode mode)
        {
            if (sceneListView.selectedItem is SceneAsset s &&
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(
                    AssetDatabase.GetAssetPath(s), mode);
            }
        }

        private void PingSelected()
        {
            if (sceneListView.selectedItem is SceneAsset s)
                EditorGUIUtility.PingObject(s);
        }

        private void AddSelectedToBuildSettings()
        {   

            
            if (sceneListView.selectedItem is not SceneAsset scene || scene == null)
                return;

            string path = AssetDatabase.GetAssetPath(scene);
            if (string.IsNullOrEmpty(path)) return;

            // Already in build -> do nothing
            if (IsSceneInBuild(path))
                return;

            var list = EditorBuildSettings.scenes.ToList();
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();

            RefreshList();
        }

        // ================= BOOKMARK =================
        private void LoadBookmarks()
        {
            bookmarkedScenes = EditorPrefs.GetString(BookmarkedKey, "")
                .Split(';')
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SceneAsset>)
                .Where(s => s != null)
                .ToList();
        }


        private void SaveBookmarks()
        {
            var guids = (bookmarkedScenes ?? new List<SceneAsset>())
                .Where(s => s != null)
                .Select(s => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(s)))
                .Where(g => !string.IsNullOrEmpty(g))
                .Distinct();

            EditorPrefs.SetString(BookmarkedKey, string.Join(";", guids));
        }

        private void AddSceneToBookmarks(SceneAsset scene, bool assignCurrentFilterTag)
        {
            if (scene == null) return;

            if (bookmarkedScenes == null)
                bookmarkedScenes = new List<SceneAsset>();

            if (!bookmarkedScenes.Contains(scene))
            {
                bookmarkedScenes.Add(scene);
                SaveBookmarks();
            }

            if (assignCurrentFilterTag)
            {
                var selectedTag = tagFilterDropdown.value;
                if (!string.IsNullOrEmpty(selectedTag) && selectedTag != "All Tags")
                {
                    var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(scene));
                    SceneSwitcherTagManagerV2.SetSceneTag(guid, selectedTag);
                }
            }

            RefreshList();
        }

        private void AddCurrentSceneToBookmarks()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
                return;

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(activeScene.path);
            AddSceneToBookmarks(sceneAsset, assignCurrentFilterTag: true);
        }

        private void RemoveSelectedFromBookmarks()
        {
            if (modeDropdown.index != 0) return;
            if (sceneListView.selectedItem is not SceneAsset scene) return;

            bookmarkedScenes.Remove(scene);
            SaveBookmarks();
            RefreshList();
        }

        private void ClearAllBookmarks()
        {
            if (modeDropdown.index != 0) return;

            bookmarkedScenes.Clear();
            SaveBookmarks();
            RefreshList();
        }

        // ================= DRAG & DROP =================
        private void SetupDragAndDrop()
        {
            // Drop into list area => add to bookmarks (+ auto-tag if a tag is currently selected)
            listContainer.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (HasSceneInDrag())
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            });

            listContainer.RegisterCallback<DragPerformEvent>(evt =>
            {
                var scenes = GetDraggedScenes();
                if (scenes.Count == 0) return;

                DragAndDrop.AcceptDrag();
                foreach (var s in scenes)
                    AddSceneToBookmarks(s, assignCurrentFilterTag: true);
            });

            // Drop onto the tag filter dropdown => assign the currently selected tag (no bookmark change)
            tagFilterDropdown.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (HasSceneInDrag() && tagFilterDropdown.value != "All Tags")
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            });

            tagFilterDropdown.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (tagFilterDropdown.value == "All Tags") return;

                var scenes = GetDraggedScenes();
                if (scenes.Count == 0) return;

                DragAndDrop.AcceptDrag();
                string tag = tagFilterDropdown.value;

                foreach (var s in scenes)
                {
                    var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(s));
                    SceneSwitcherTagManagerV2.SetSceneTag(guid, tag);
                }

                RefreshList();
            });
        }

        private static bool HasSceneInDrag()
        {
            if (DragAndDrop.objectReferences != null)
            {
                foreach (var o in DragAndDrop.objectReferences)
                {
                    if (o is SceneAsset) return true;
                }
            }

            if (DragAndDrop.paths != null)
            {
                foreach (var p in DragAndDrop.paths)
                {
                    if (!string.IsNullOrEmpty(p) && p.EndsWith(".unity"))
                        return true;
                }
            }

            return false;
        }

        private static List<SceneAsset> GetDraggedScenes()
        {
            var result = new List<SceneAsset>();

            if (DragAndDrop.objectReferences != null)
            {
                foreach (var o in DragAndDrop.objectReferences)
                {
                    if (o is SceneAsset s && s != null)
                        result.Add(s);
                }
            }

            if (DragAndDrop.paths != null)
            {
                foreach (var p in DragAndDrop.paths)
                {
                    if (string.IsNullOrEmpty(p) || !p.EndsWith(".unity"))
                        continue;

                    var s = AssetDatabase.LoadAssetAtPath<SceneAsset>(p);
                    if (s != null) result.Add(s);
                }
            }

            return result.Distinct().ToList();
        }

        // ================= BUILD SETTINGS =================
        private static bool IsSceneInBuild(string scenePath)
        {   
            if (string.IsNullOrEmpty(scenePath)) return false;
            var scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                var s = scenes[i];
                if (s != null && s.path == scenePath)
                {   

                    return true;
                }
                    
            }
            return false;
        }


        // ================= TAG =================
        private List<string> BuildTagFilter()
        {
            var list = new List<string> { "All Tags" };
            list.AddRange(SceneSwitcherTagManagerV2.GetGlobalTags());
            return list;
        }

        private void OnAddNewTag()
        {
           SceneSwitcherTagManagerWindowV2.Show(() =>
            {
                RefreshTagUI();
                RefreshList();
            });
        }
        private void RefreshTagUI()
        {
            // Update filter dropdown
            tagFilterDropdown.choices = BuildTagFilter();
            tagFilterDropdown.index = 0;

            // Update popup choices in visible items
            sceneListView.Query<PopupField<string>>("item-tag-popup")
                .ForEach(popup =>
                {
                    popup.choices = SceneSwitcherTagManagerV2.GetGlobalTags();
                });
        }   

        private void SetIcon(
            VisualElement root,
            string elementName,
            string unityIconName,
            float size)
        {
            var img = root.Q<Image>(elementName);
            if (img == null)
            {
                Debug.LogWarning($"[V2 ICON] Image not found: {elementName}");
                return;
            }

            img.image = EditorGUIUtility.IconContent(unityIconName).image;
            img.style.width = size;
            img.style.height = size;
        }


    }
    
}