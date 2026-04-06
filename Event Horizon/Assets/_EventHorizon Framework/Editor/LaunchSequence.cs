using EventHorizon.Core;
using EventHorizon.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Handles = UnityEditor.Handles;

namespace EventHorizon.Editor
{
    /// <summary>
    /// Mission-control style scene setup for each EventHorizon module flow.
    /// Opens via EventHorizon > Launch Sequence in the menu bar.
    /// </summary>
    public class LaunchSequence : EditorWindow
    {
        private const float FooterHeight = 28f;

        private static readonly Color BackgroundColor = new Color(0.03f, 0.05f, 0.1f);
        private static readonly Color SurfaceColor = new Color(0.08f, 0.1f, 0.15f);
        private static readonly Color PanelColor = new Color(0.1f, 0.12f, 0.18f);
        private static readonly Color AccentColor = new Color(0.88f, 0.71f, 0.38f);
        private static readonly Color AccentBlue = new Color(0.22f, 0.35f, 0.58f);
        private static readonly Color FooterCyan = new Color(0.34f, 0.66f, 0.66f);
        private static readonly Color FooterGreen = new Color(0.19f, 0.69f, 0.43f);
        private static readonly Color FooterYellow = new Color(0.82f, 0.68f, 0.16f);
        private static readonly Color FooterRed = new Color(0.78f, 0.15f, 0.18f);
        private static readonly Color TextColor = new Color(0.91f, 0.92f, 0.95f);
        private static readonly Color SubtleTextColor = new Color(0.58f, 0.63f, 0.72f);
        private static readonly Color ReadyColor = new Color(0.48f, 0.95f, 0.75f);
        private static readonly Color WarningColor = new Color(1f, 0.68f, 0.3f);
        private static readonly Color RuntimeColor = new Color(0.72f, 0.62f, 1f);
        private static readonly Color StarColor = new Color(0.95f, 0.95f, 0.98f, 0.85f);

        [MenuItem("EventHorizon/Launch Sequence")]
        public static void Open()
        {
            var window = GetWindow<LaunchSequence>("LaunchSequence");
            window.minSize = new Vector2(380f, 420f);
        }

        private void OnGUI()
        {
            Font previousFont = GUI.skin.font;
            GUI.skin.font = EventHorizonEditorFont.CascadiaMono;
            try
            {
                DrawBackdrop();

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawCommandDeck();

                    EditorGUILayout.Space(6f);
                    DrawCoreFlow();
                    EditorGUILayout.Space(4f);
                    DrawUIFlow();
                    EditorGUILayout.Space(4f);
                    DrawRuntimeOnlyFlow("Sound", "Audio systems deploy their pool at runtime. No scene-side launch hardware required.");
                    EditorGUILayout.Space(4f);
                    DrawRuntimeOnlyFlow("Currency", "Economy systems are scriptable and orbital by default. No scene anchors required.");
                    EditorGUILayout.Space(4f);
                    DrawRuntimeOnlyFlow("Save", "Persistence systems operate from scriptable control assets. No scene anchors required.");
                    EditorGUILayout.Space(4f);
                    DrawRuntimeOnlyFlow("Logger", "SingularityConsole runs from its module asset. Register it early in your ModuleRegistry to control log colors and filtering.");
                    EditorGUILayout.Space(FooterHeight + 8f);
                }
            }
            finally
            {
                GUI.skin.font = previousFont;
            }
        }

        private void DrawBackdrop()
        {
            Rect rect = new Rect(0f, 0f, position.width, position.height);
            EditorGUI.DrawRect(rect, BackgroundColor);
            DrawTopography(rect);
            DrawStars(rect);
            EditorGUI.DrawRect(new Rect(0f, 0f, rect.width, 2f), AccentBlue);
            DrawFooter(rect);
        }

        private void DrawCommandDeck()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                richText = true
            };

            GUIStyle bodyStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                richText = true,
                normal = { textColor = SubtleTextColor }
            };

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var previousColor = GUI.color;
                GUI.color = TextColor;
                EditorGUILayout.LabelField("LAUNCHSEQUENCE // ORBITAL SCENE COMMAND", titleStyle);
                GUI.color = previousColor;

                EditorGUILayout.LabelField(
                    "Mission control scan for EventHorizon modules. Confirm each system below, then fire the launch actions to deploy any missing scene infrastructure.",
                    bodyStyle);

                Rect stars = EditorGUILayout.GetControlRect(false, 10f);
                EditorGUI.DrawRect(new Rect(stars.x, stars.y + 4f, stars.width, 1f), new Color(0.2f, 0.3f, 0.5f, 0.8f));
                EditorGUI.DrawRect(new Rect(stars.x + 32f, stars.y + 1f, 3f, 3f), ReadyColor);
                EditorGUI.DrawRect(new Rect(stars.x + 144f, stars.y + 6f, 2f, 2f), AccentColor);
                EditorGUI.DrawRect(new Rect(stars.x + 236f, stars.y + 2f, 4f, 4f), RuntimeColor);
            }
        }

        private void DrawCoreFlow()
        {
            var existing = FindFirstObjectByType<ModuleBootstrapper>();
            bool ready = existing != null;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawFlowHeader("Core Thrusters", ready);

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField("Launch hardware:", StyledMiniLabel());
                    DrawRequirement("ModuleBootstrapper", ready);

                    if (!ready)
                    {
                        if (DrawActionButton("Ignite Core"))
                            SetupCore();
                    }
                    else
                    {
                        EditorGUILayout.ObjectField("Bootstrapper", existing, typeof(ModuleBootstrapper), true);
                    }
                }
            }
        }

        private static void SetupCore()
        {
            var existing = FindFirstObjectByType<ModuleBootstrapper>();
            if (existing != null)
            {
                SingularityConsole.Log<LaunchSequence>("ModuleBootstrapper already online. Skipping deployment.");
                return;
            }

            var go = new GameObject("Control Center");
            go.AddComponent<ModuleBootstrapper>();
            Undo.RegisterCreatedObjectUndo(go, "Create EventHorizon Control Center");
            Selection.activeGameObject = go;
            SingularityConsole.Log<LaunchSequence>("Control Center deployed. Assign your ModuleRegistry asset in the Inspector.");
        }

        private void DrawUIFlow()
        {
            var existing = FindFirstObjectByType<UIRoot>();
            bool ready = existing != null;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawFlowHeader("Bridge Displays", ready);

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField("Launch hardware:", StyledMiniLabel());
                    DrawRequirement("Canvas", ready);
                    DrawRequirement("UIRoot (on bridge panel)", ready);

                    if (!ready)
                    {
                        if (DrawActionButton("Deploy UI Array"))
                            SetupUI();
                    }
                    else
                    {
                        EditorGUILayout.ObjectField("UIRoot", existing, typeof(UIRoot), true);
                    }
                }
            }
        }

        private static void SetupUI()
        {
            var existing = FindFirstObjectByType<UIRoot>();
            if (existing != null)
            {
                SingularityConsole.Log<LaunchSequence>("UIRoot already online. Skipping deployment.");
                return;
            }

            var canvasGO = new GameObject("UICanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create EventHorizon UI Canvas");

            var panelGO = new GameObject("ViewContainer");
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelGO.transform.SetParent(canvasGO.transform, false);
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var uiRoot = panelGO.AddComponent<UIRoot>();

            var so = new SerializedObject(uiRoot);
            so.FindProperty("_uiRoot").objectReferenceValue = panelRect;
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = panelGO;
            SingularityConsole.Log<LaunchSequence>("UICanvas deployed with ViewContainer/UIRoot. Assign your UIModuleSO asset in the Inspector.");
        }

        private static void DrawRuntimeOnlyFlow(string moduleName, string note)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawFlowHeader(moduleName, true, "Orbital autopilot");
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.HelpBox(note, MessageType.None);
                }
            }
        }

        private static void DrawFlowHeader(string label, bool ready, string overrideStatus = null)
        {
            string status = overrideStatus ?? (ready ? "Ready" : "Stand by");
            Color statusColor = overrideStatus != null ? RuntimeColor
                : ready ? ReadyColor
                : WarningColor;

            using (new EditorGUILayout.HorizontalScope())
            {
                var previousColor = GUI.color;
                GUI.color = TextColor;
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(140f));
                GUI.color = previousColor;

                GUI.color = statusColor;
                EditorGUILayout.LabelField(status, StyledMiniLabel());
                GUI.color = previousColor;
            }

            Rect lineRect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(lineRect, new Color(0.32f, 0.5f, 0.82f, 0.35f));
        }

        private static void DrawRequirement(string name, bool met)
        {
            string icon = met ? "[OK]" : "[--]";
            Color color = met ? ReadyColor : WarningColor;
            var previousColor = GUI.color;
            GUI.color = color;
            EditorGUILayout.LabelField($"  {icon}  {name}", StyledMiniLabel());
            GUI.color = previousColor;
        }

        private static bool DrawActionButton(string label)
        {
            var previousBackgroundColor = GUI.backgroundColor;
            var previousContentColor = GUI.contentColor;
            GUI.backgroundColor = PanelColor;
            GUI.contentColor = AccentColor;
            bool clicked = GUILayout.Button(label, GUILayout.Height(28f));
            GUI.backgroundColor = previousBackgroundColor;
            GUI.contentColor = previousContentColor;
            return clicked;
        }

        private static GUIStyle StyledMiniLabel()
        {
            return new GUIStyle(EditorStyles.miniLabel)
            {
                richText = true,
                normal = { textColor = TextColor }
            };
        }

        private static void DrawFooter(Rect rect)
        {
            float bandHeight = FooterHeight / 4f;
            float y = rect.yMax - FooterHeight;

            EditorGUI.DrawRect(new Rect(0f, y, rect.width, bandHeight), FooterCyan);
            EditorGUI.DrawRect(new Rect(0f, y + bandHeight, rect.width, bandHeight), FooterGreen);
            EditorGUI.DrawRect(new Rect(0f, y + (bandHeight * 2f), rect.width, bandHeight), FooterYellow);
            EditorGUI.DrawRect(new Rect(0f, y + (bandHeight * 3f), rect.width, rect.yMax - (y + (bandHeight * 3f))), FooterRed);
        }

        private static void DrawTopography(Rect rect)
        {
            Handles.BeginGUI();
            Color lineColor = new Color(1f, 1f, 1f, 0.09f);
            for (int layer = 0; layer < 8; layer++)
            {
                Vector3[] points = new Vector3[40];
                float amplitude = 12f + layer * 3f;
                float y = rect.y + 28f + layer * 56f;
                for (int i = 0; i < points.Length; i++)
                {
                    float x = rect.x + (rect.width / (points.Length - 1)) * i;
                    float wave = Mathf.Sin((x * 0.015f) + layer * 0.85f) * amplitude;
                    float drift = Mathf.Cos((x * 0.007f) + layer * 0.6f) * (amplitude * 0.45f);
                    points[i] = new Vector3(x, y + wave + drift, 0f);
                }

                Handles.color = lineColor;
                Handles.DrawAAPolyLine(2f, points);
            }
            Handles.EndGUI();
        }

        private static void DrawStars(Rect rect)
        {
            Handles.BeginGUI();
            Vector2[] stars =
            {
                new Vector2(38f, 34f), new Vector2(92f, 60f), new Vector2(248f, 42f), new Vector2(332f, 88f),
                new Vector2(512f, 34f), new Vector2(648f, 78f), new Vector2(774f, 44f), new Vector2(884f, 110f),
                new Vector2(104f, 182f), new Vector2(214f, 236f), new Vector2(436f, 188f), new Vector2(586f, 248f),
                new Vector2(726f, 194f), new Vector2(848f, 286f), new Vector2(78f, 344f), new Vector2(304f, 388f)
            };

            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i].x > rect.width || stars[i].y > rect.height)
                {
                    continue;
                }

                float size = (i % 3 == 0) ? 3f : 2f;
                Handles.color = StarColor;
                Handles.DrawSolidDisc(stars[i], Vector3.forward, size * 0.5f);

                if (i % 4 == 0)
                {
                    Handles.color = new Color(0.95f, 0.95f, 0.98f, 0.28f);
                    Handles.DrawLine(stars[i] + new Vector2(-4f, 0f), stars[i] + new Vector2(4f, 0f));
                    Handles.DrawLine(stars[i] + new Vector2(0f, -4f), stars[i] + new Vector2(0f, 4f));
                }
            }

            Handles.EndGUI();
        }
    }
}
