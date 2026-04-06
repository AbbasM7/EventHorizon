using UnityEditor;
using UnityEngine;

namespace EventHorizon.Editor
{
    internal static class EventHorizonEditorFont
    {
        private static Font _cachedFont;

        public static Font CascadiaMono
        {
            get
            {
                if (_cachedFont == null)
                {
                    _cachedFont = ResolveFont();
                }

                return _cachedFont;
            }
        }

        private static Font ResolveFont()
        {
            Font assetFont = FindProjectFontAsset();
            if (assetFont != null)
            {
                return assetFont;
            }

            return EditorStyles.standardFont;
        }

        private static Font FindProjectFontAsset()
        {
            string[] guids = AssetDatabase.FindAssets("Cascadia t:Font");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Font font = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (font != null)
                {
                    return font;
                }
            }

            guids = AssetDatabase.FindAssets("Mono t:Font");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.IndexOf("Cascadia", System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                Font font = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (font != null)
                {
                    return font;
                }
            }

            return null;
        }
    }
}
