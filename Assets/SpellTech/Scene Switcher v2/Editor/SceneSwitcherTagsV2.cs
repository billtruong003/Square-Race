using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace SpellTech.SceneSwitcherV2
{
    public static class SceneSwitcherTagManagerV2
    {
        private const string GlobalTagsKey = "SSV2_GlobalTags_Data";
        private const string SceneMappingKey = "SSV2_SceneMapping_Data";
        public const string Untagged = "Untagged";

        public static List<string> GetGlobalTags()
        {
            string data = EditorPrefs.GetString(GlobalTagsKey, Untagged);
            var tags = data.Split('|').ToList();
            if (!tags.Contains(Untagged)) tags.Insert(0, Untagged);
            return tags;
        }

        public static void AddGlobalTag(string tag)
        {
            if (string.IsNullOrEmpty(tag) || tag == Untagged) return;
            var tags = GetGlobalTags();
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
                EditorPrefs.SetString(GlobalTagsKey, string.Join("|", tags));
            }
        }

        // ================= NEW: REMOVE / RENAME =================
        public static bool RemoveGlobalTag(string tag)
        {
            if (string.IsNullOrEmpty(tag) || tag == Untagged) return false;

            var tags = GetGlobalTags();
            if (!tags.Contains(tag)) return false;

            tags.Remove(tag);
            EditorPrefs.SetString(GlobalTagsKey, string.Join("|", tags));

            // Any scene using removed tag -> Untagged
            ReplaceTagInMapping(tag, Untagged);
            return true;
        }

        public static bool RenameGlobalTag(string oldTag, string newTag)
        {
            if (string.IsNullOrEmpty(oldTag) || oldTag == Untagged) return false;
            if (string.IsNullOrWhiteSpace(newTag)) return false;

            newTag = newTag.Trim();
            if (newTag == Untagged) return false;

            var tags = GetGlobalTags();
            if (!tags.Contains(oldTag)) return false;

            // Avoid duplicates
            if (tags.Contains(newTag)) return false;

            int idx = tags.IndexOf(oldTag);
            tags[idx] = newTag;
            EditorPrefs.SetString(GlobalTagsKey, string.Join("|", tags));

            // Update scene mapping: oldTag -> newTag
            ReplaceTagInMapping(oldTag, newTag);
            return true;
        }

        private static void ReplaceTagInMapping(string fromTag, string toTag)
        {
            string mapping = EditorPrefs.GetString(SceneMappingKey, "{}");
            var dict = JsonUtility.FromJson<TagMap>(mapping) ?? new TagMap();

            for (int i = 0; i < dict.Values.Count; i++)
            {
                if (dict.Values[i] == fromTag)
                    dict.Values[i] = toTag;
            }

            EditorPrefs.SetString(SceneMappingKey, JsonUtility.ToJson(dict));
        }

        // ================= SCENE TAG =================
        public static string GetSceneTag(string guid)
        {
            string mapping = EditorPrefs.GetString(SceneMappingKey, "{}");
            var dict = JsonUtility.FromJson<TagMap>(mapping) ?? new TagMap();
            return dict.Get(guid);
        }

        public static void SetSceneTag(string guid, string tag)
        {
            string mapping = EditorPrefs.GetString(SceneMappingKey, "{}");
            var dict = JsonUtility.FromJson<TagMap>(mapping) ?? new TagMap();
            dict.Set(guid, tag);
            EditorPrefs.SetString(SceneMappingKey, JsonUtility.ToJson(dict));
        }

        [System.Serializable]
        private class TagMap
        {
            public List<string> Keys = new List<string>();
            public List<string> Values = new List<string>();

            public string Get(string k)
            {
                int i = Keys.IndexOf(k);
                return i != -1 ? Values[i] : Untagged;
            }

            public void Set(string k, string v)
            {
                int i = Keys.IndexOf(k);
                if (i != -1) Values[i] = v;
                else { Keys.Add(k); Values.Add(v); }
            }
        }
    }
}
