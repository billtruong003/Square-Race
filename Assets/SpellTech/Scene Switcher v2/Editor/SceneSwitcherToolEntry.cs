#if SPELLTECH_CORE
using SpellTech.Core;
using UnityEditor;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace SpellTech.SceneSwitcherV2
{
    public class SceneSwitcherV2ToolEntry : ISpellTechTool
    {
        public string ToolName => "Scene Switcher v2";
        public string ToolDescription => "Quickly switch between, bookmark, and manage scenes in your project.";

        private SceneSwitcherToolWindowV2 _windowInstance;

        public void CreateUI(VisualElement root)
        {
            try
            {
                if (_windowInstance == null)
                {
                    _windowInstance = ScriptableObject.CreateInstance<SceneSwitcherToolWindowV2>();
                    _windowInstance.OnEnable();  
                }

                _windowInstance.CreateGUI();

                var children = _windowInstance.rootVisualElement.Children().ToList();
                foreach (var child in children)
                {
                    root.Add(child);
                }

                root.styleSheets.Add(_windowInstance.toolStyleSheet);
                
                
                // On testing and Debugging, it was found that GeometryChangedEvent was not firing as expected.
                root.RegisterCallback<GeometryChangedEvent>(evt => _windowInstance.OnGeometryChanged(evt));
                

                if (children.Count == 0)
                {
                    root.Add(new Label("Error: Failed to load Scene Switcher UI. Check console."));
                }
            }
            catch (System.Exception e)
            {
                root.Add(new Label($"Error loading tool: {e.Message}"));
                Debug.LogException(e);
            }
        }

    }
}
#endif