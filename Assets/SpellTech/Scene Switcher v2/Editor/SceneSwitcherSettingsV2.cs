#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Compilation;

namespace SpellTech.SceneSwitcherV2
{
    /// <summary>
    /// Shared settings for Scene Switcher V2.
    /// Kept as a simple static wrapper around EditorPrefs so other scripts
    /// (toolbar integration, etc.) can compile without referencing windows.
    /// </summary>
    public static class SceneSwitcherSettings
    {
        private const string DebugKey = "SSV2_Debug";
        private const string ToolbarEnabledKey = "SSV2_ToolbarEnabled";

        public static bool IsDebugLoggingEnabled
        {
            get => EditorPrefs.GetBool(DebugKey, false);
            set => EditorPrefs.SetBool(DebugKey, value);
        }

        public static bool IsToolbarShortcutEnabled
        {
            get => EditorPrefs.GetBool(ToolbarEnabledKey, true);
            set => EditorPrefs.SetBool(ToolbarEnabledKey, value);
        }
    }

    public class SettingsWindowV2 : EditorWindow
    {
        public static void ShowWindow()
        {
            var wnd = GetWindow<SettingsWindowV2>(true, "V2 Settings");
            wnd.minSize = new Vector2(300, 190);
            wnd.maxSize = new Vector2(300, 190);
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;

            var title = new Label("Switcher V2 Preferences")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 13,
                    marginBottom = 10
                }
            };
            root.Add(title);

            var debugToggle = new Toggle("Enable Verbose Logging")
            {
                value = SceneSwitcherSettings.IsDebugLoggingEnabled
            };
            debugToggle.RegisterValueChangedCallback(evt =>
            {
                SceneSwitcherSettings.IsDebugLoggingEnabled = evt.newValue;
            });
            root.Add(debugToggle);

            // NEW: Toolbar enable/disable
            var toolbarToggle = new Toggle("Enable Toolbar Shortcut")
            {
                value = SceneSwitcherSettings.IsToolbarShortcutEnabled,
                tooltip = "Show the Scene Switcher dropdown in Unity's top toolbar."
            };
            toolbarToggle.RegisterValueChangedCallback(evt =>
            {
                SceneSwitcherSettings.IsToolbarShortcutEnabled = evt.newValue;
            });
            root.Add(toolbarToggle);

            var info = new Label("Toolbar changes may require script recompile or editor restart to apply.")
            {
                style =
                {
                    fontSize = 9,
                    unityFontStyleAndWeight = FontStyle.Italic,
                    whiteSpace = WhiteSpace.Normal,
                    marginTop = 4,
                    marginBottom = 8
                }
            };
            root.Add(info);

            // Optional: one-click recompile
            var recompileBtn = new Button(() => CompilationPipeline.RequestScriptCompilation())
            {
                text = "Recompile Scripts",
                tooltip = "Forces Unity to recompile scripts (can refresh toolbar registration)."
            };
            root.Add(recompileBtn);

            var autoSaveToggle = new Toggle("Auto-Save Tags on Focus Lost") { value = true };
            autoSaveToggle.SetEnabled(false);
            root.Add(autoSaveToggle);
        }
    }


    public class CreditsWindowV2 : EditorWindow
    {
        private struct ContributorV2
        {
            public string Name;
            public string Role;
            public string URL;
            public string AvatarName;
        }

        public static void ShowWindow()
        {
            var window = GetWindow<CreditsWindowV2>(true, "Credits", true);
            window.minSize = window.maxSize = new Vector2(320, 420);
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            ApplyBaseStyles(root);

            var scrollView = new ScrollView();
            root.Add(scrollView);

            var header = new VisualElement { name = "credits-header" };
            header.style.alignItems = Align.Center;
            header.style.paddingBottom = 15;
            header.Add(new Label("SCENE SWITCHER V2") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 16, color = new Color(0.3f, 0.7f, 1f) } });
            header.Add(new Label("Version 2.0.0 Professional") { style = { fontSize = 10, opacity = 0.6f } });
            scrollView.Add(header);

            var contributors = new List<ContributorV2>
            {
                new ContributorV2 { Name = "BillTheDev", Role = "The Boy", URL = "https://youtube.com/@billthedev", AvatarName = "BillTheDev" },
                new ContributorV2 { Name = "NDDEVGAME", Role = "The Bird", URL = "https://youtube.com/@nddevgame", AvatarName = "NDDEVGAME" },
                new ContributorV2 { Name = "SoraTheDev", Role = "Waifu", URL = "https://youtube.com/@sorathedev6739", AvatarName = "SoraTheDev" }
            };

            foreach (var person in contributors)
            {
                scrollView.Add(CreateContributorCard(person));
            }

            var footer = new Button(() => Application.OpenURL("https://spelltech.vn"))
            {
                text = "Visit SpellTech Website",
                name = "footer-button"
            };
            footer.style.marginTop = 20;
            root.Add(footer);
        }

        private VisualElement CreateContributorCard(ContributorV2 data)
        {
            var card = new VisualElement();
            card.AddToClassList("contributor-card-v2");
            card.style.flexDirection = FlexDirection.Row;

            card.style.marginBottom = 5;
            card.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);

            var avatar = new Image
            {
                image = LoadAvatar(data.AvatarName),
                scaleMode = ScaleMode.ScaleToFit
            };
            avatar.style.width = 44;
            avatar.style.height = 44;
            avatar.style.marginRight = 12;

            card.Add(avatar);

            var info = new VisualElement { style = { flexGrow = 1 } };
            info.Add(new Label(data.Name) { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 13 } });
            info.Add(new Label(data.Role) { style = { fontSize = 11, opacity = 0.7f } });
            card.Add(info);

            var linkBtn = new Button(() => Application.OpenURL(data.URL)) { text = "Profile" };
            linkBtn.style.width = 60;
            linkBtn.style.height = 24;
            card.Add(linkBtn);

            return card;
        }

        private Texture LoadAvatar(string name)
        {
            string path = Path.Combine(SceneSwitcherToolWindowV2.scriptFolder, "IMAGE", $"{name}.png");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return tex != null ? tex : EditorGUIUtility.IconContent("d_UserFixed").image;
        }

        private void ApplyBaseStyles(VisualElement root)
        {

            root.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
        }
    }

}
#endif