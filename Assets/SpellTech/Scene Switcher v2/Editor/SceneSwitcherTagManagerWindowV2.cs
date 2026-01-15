

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpellTech.SceneSwitcherV2
{
    public class SceneSwitcherTagManagerWindowV2 : EditorWindow
    {
        private ListView list;
        private TextField input;

        private Button addBtn;
        private Button renameBtn;
        private Button deleteBtn;

        private System.Action onChanged;

        public static void Show(System.Action onChanged)
        {
            var w = GetWindow<SceneSwitcherTagManagerWindowV2>(true, "Tag Manager");
            w.minSize = new Vector2(320, 360);
            w.onChanged = onChanged;
            w.Refresh();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;

            var title = new Label("Global Tags");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 6;
            root.Add(title);

            list = new ListView
            {
                selectionType = SelectionType.Single,
                fixedItemHeight = 24
            };
            list.style.flexGrow = 1;

            list.makeItem = () =>
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;

                var label = new Label { name = "tag-name" };
                label.style.flexGrow = 1;
                row.Add(label);

                return row;
            };

            list.bindItem = (el, i) =>
            {
                var tag = (string)list.itemsSource[i];
                el.Q<Label>("tag-name").text = tag;
            };

            list.selectionChanged += (Selection) => UpdateButtons();
            root.Add(list);

            input = new TextField("Tag name");
            input.style.marginTop = 8;
            root.Add(input);

            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.marginTop = 8;

            addBtn = new Button(AddTag) { text = "Add" };
            renameBtn = new Button(RenameSelected) { text = "Rename" };
            deleteBtn = new Button(DeleteSelected) { text = "Delete" };

            addBtn.style.flexGrow = 1;
            renameBtn.style.flexGrow = 1;
            deleteBtn.style.flexGrow = 1;

            addBtn.style.marginRight = 6;
            renameBtn.style.marginRight = 6;

            btnRow.Add(addBtn);
            btnRow.Add(renameBtn);
            btnRow.Add(deleteBtn);

            root.Add(btnRow);

            var closeBtn = new Button(Close) { text = "Close" };
            closeBtn.style.marginTop = 8;
            root.Add(closeBtn);

            Refresh();
        }

        private void Refresh()
        {
            if (list == null) return;

            var tags = SceneSwitcherTagManagerV2.GetGlobalTags();
            tags.Remove(SceneSwitcherTagManagerV2.Untagged);

            list.itemsSource = tags;
            list.Rebuild();

            UpdateButtons();
        }

        private void UpdateButtons()
        {
            bool has = list != null && list.selectedIndex >= 0;
            renameBtn?.SetEnabled(has);
            deleteBtn?.SetEnabled(has);
        }

        private void AddTag()
        {
            var tag = input.value?.Trim();
            if (string.IsNullOrEmpty(tag)) return;

            SceneSwitcherTagManagerV2.AddGlobalTag(tag);
            input.value = "";

            onChanged?.Invoke();
            Refresh();
        }

        private void RenameSelected()
        {
            if (list == null || list.selectedIndex < 0) return;

            var oldTag = (string)list.itemsSource[list.selectedIndex];
            var newTag = input.value?.Trim();
            if (string.IsNullOrEmpty(newTag)) return;

            if (SceneSwitcherTagManagerV2.RenameGlobalTag(oldTag, newTag))
            {
                input.value = "";
                onChanged?.Invoke();
                Refresh();
            }
        }

        private void DeleteSelected()
        {
            if (list == null || list.selectedIndex < 0) return;

            var tag = (string)list.itemsSource[list.selectedIndex];

            if (!EditorUtility.DisplayDialog(
                    "Delete Tag",
                    $"Delete tag '{tag}'?\nScenes using this tag will be set to '{SceneSwitcherTagManagerV2.Untagged}'.",
                    "Delete",
                    "Cancel"))
                return;

            if (SceneSwitcherTagManagerV2.RemoveGlobalTag(tag))
            {
                onChanged?.Invoke();
                Refresh();
            }
        }
    }
}
