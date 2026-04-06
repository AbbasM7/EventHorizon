using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace EventHorizon.Editor
{
    public class ObservationDeck : EditorWindow
    {
        private const float FooterHeight = 16f;
        private const float NarrowLayoutBreakpoint = 860f;
        private const float CompactLayoutWidth = 980f;
        private const float CompactLayoutHeight = 700f;
        private const float SplitSpacing = 6f;

        private static readonly Color BackgroundColor = new Color(0.03f, 0.05f, 0.1f);
        private static readonly Color SurfaceColor = new Color(0.08f, 0.1f, 0.15f);
        private static readonly Color FooterCyan = new Color(0.34f, 0.66f, 0.66f);
        private static readonly Color FooterGreen = new Color(0.19f, 0.69f, 0.43f);
        private static readonly Color FooterYellow = new Color(0.82f, 0.68f, 0.16f);
        private static readonly Color FooterRed = new Color(0.78f, 0.15f, 0.18f);
        private static readonly Color AccentBlue = new Color(0.22f, 0.35f, 0.58f);
        private static readonly Color AccentGold = new Color(0.88f, 0.71f, 0.38f);
        private static readonly Color AccentOrange = new Color(0.91f, 0.39f, 0.2f);
        private static readonly Color AccentRed = new Color(0.79f, 0.12f, 0.22f);
        private static readonly Color StarColor = new Color(0.95f, 0.95f, 0.98f, 0.85f);
        private static readonly Color AccentColor = new Color(0.88f, 0.71f, 0.38f);
        private static readonly Color AccentMuted = new Color(0.19f, 0.24f, 0.33f);
        private static readonly Color TextColor = new Color(0.91f, 0.92f, 0.95f);
        private static readonly Color SubtleTextColor = new Color(0.58f, 0.63f, 0.72f);
        private static readonly Color WarningColor = new Color(0.88f, 0.71f, 0.38f);
        private static readonly Color ErrorColor = new Color(0.91f, 0.39f, 0.2f);
        private static readonly Color SelectedRowColor = new Color(0.16f, 0.2f, 0.29f);
        private static readonly Color RowColor = new Color(0.1f, 0.12f, 0.18f);
        private static readonly string[] DetailTabs = { "Trace", "Details" };
        private static readonly Regex StackTraceRegex = new Regex(@"^(?<method>.+?)\s+\(at\s+(?<path>.*?):(?<line>\d+)\)$", RegexOptions.Compiled);
        private static readonly string SpaceArt =
@"   .   *      .      /\       .
      .    *        /==\   .    
   .       .       |::::|     * 
      ORBITAL OBSERVATION DECK";

        private readonly Dictionary<string, bool> _categoryFilters = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private readonly List<ObservationDeckLogEntry> _visibleEntries = new List<ObservationDeckLogEntry>();

        private Vector2 _logScroll;
        private Vector2 _detailScroll;
        private string _searchText = string.Empty;
        private int _selectedIndex = -1;
        private int _detailTabIndex;
        private bool _collapseDuplicates;
        private double _lastAnimationTick;
        private int _rocketFrameIndex;
        private float _compactSplitRatio = 0.55f;
        private bool _draggingCompactSplitter;

        [MenuItem("EventHorizon/Observation Deck")]
        public static void Open()
        {
            var window = GetWindow<ObservationDeck>("ObservationDeck");
            window.minSize = new Vector2(920f, 520f);
        }

        private void OnEnable()
        {
            ObservationDeckLogStore.OnLogsChanged -= Repaint;
            ObservationDeckLogStore.OnLogsChanged += Repaint;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            SyncCategories();
        }

        private void OnDisable()
        {
            ObservationDeckLogStore.OnLogsChanged -= Repaint;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnGUI()
        {
            Font previousFont = GUI.skin.font;
            GUI.skin.font = EventHorizonEditorFont.CascadiaMono;
            try
            {
                DrawBackground();
                SyncCategories();
                RebuildVisibleEntries();
                bool useCompactLayout = position.width < CompactLayoutWidth || position.height < CompactLayoutHeight;
                bool useStackedLayout = position.width < NarrowLayoutBreakpoint;

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawHeader(useCompactLayout);
                    EditorGUILayout.Space(8f);
                    DrawToolbar();
                    EditorGUILayout.Space(6f);
                    DrawMainContent(useCompactLayout, useStackedLayout);
                    EditorGUILayout.Space(FooterHeight + 8f);
                }
            }
            finally
            {
                GUI.skin.font = previousFont;
            }
        }

        private void DrawMainContent(bool useCompactLayout, bool useStackedLayout)
        {
            if (useCompactLayout)
            {
                DrawCompactContent();
                return;
            }

            if (useStackedLayout)
            {
                DrawLogFeed(true);
                EditorGUILayout.Space(SplitSpacing);
                DrawInspectorPanel(true);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLogFeed(false);
                EditorGUILayout.Space(SplitSpacing);
                DrawInspectorPanel(false);
            }
        }

        private void DrawCompactContent()
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                float leftWidth = Mathf.Clamp((position.width - 24f) * _compactSplitRatio, 260f, position.width - 240f);

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(leftWidth), GUILayout.ExpandHeight(true)))
                {
                    DrawLogFeed(true);
                }

                Rect splitterRect = GUILayoutUtility.GetRect(4f, 4f, GUILayout.Width(4f), GUILayout.ExpandHeight(true));
                Rect boundsRect = new Rect(0f, splitterRect.y, Mathf.Max(1f, position.width - 24f), splitterRect.height);
                DrawVerticalSplitter(splitterRect, boundsRect);

                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    DrawCompactTracePanel();
                }
            }
        }

        private void DrawBackground()
        {
            Rect rect = new Rect(0f, 0f, position.width, position.height);
            EditorGUI.DrawRect(rect, BackgroundColor);
            DrawTopography(rect);
            DrawStars(rect);
            DrawWindowFrame(rect);
            DrawStarfieldFooter(rect);
        }

        private void DrawHeader(bool compact)
        {
            GUIStyle artStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                richText = false,
                fontSize = 10,
                alignment = TextAnchor.UpperLeft
            };
            artStyle.font = EventHorizonEditorFont.CascadiaMono;
            artStyle.clipping = TextClipping.Overflow;

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                richText = true
            };

            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                richText = true
            };

            Rect panelRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            var previousColor = GUI.color;
            GUI.color = TextColor;
            DrawAsciiBlock(SpaceArt, artStyle);
            GUI.color = previousColor;

            bool narrowHeader = compact || position.width < 1180f;

            GUI.contentColor = TextColor;
            if (narrowHeader)
            {
                EditorGUILayout.LabelField("OBSERVATIONDECK // SINGULARITY LOG MONITOR", titleStyle);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Search", GUILayout.Width(44f));
                    _searchText = EditorGUILayout.TextField(_searchText, GUILayout.MinWidth(120f));
                    _collapseDuplicates = GUILayout.Toggle(_collapseDuplicates, "Collapse", GUILayout.Width(78f));
                    if (GUILayout.Button("Clear", GUILayout.Width(72f)))
                    {
                        ObservationDeckLogStore.Clear();
                        _selectedIndex = -1;
                    }
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("OBSERVATIONDECK // SINGULARITY LOG MONITOR", titleStyle);
                    GUILayout.Label("Search", GUILayout.Width(44f));
                    _searchText = EditorGUILayout.TextField(_searchText, GUILayout.Width(180f));
                    _collapseDuplicates = GUILayout.Toggle(_collapseDuplicates, "Collapse", GUILayout.Width(78f));
                    if (GUILayout.Button("Clear", GUILayout.Width(72f)))
                    {
                        ObservationDeckLogStore.Clear();
                        _selectedIndex = -1;
                    }
                }
            }
            EditorGUILayout.LabelField(
                "Live mission console for EventHorizon logs. Filter by category, search transmissions, and inspect the selected log's call trace in the starboard panel.",
                subtitleStyle);
            GUI.contentColor = Color.white;
            EditorGUILayout.EndVertical();
        }

        private static void DrawAsciiBlock(string ascii, GUIStyle style)
        {
            string[] lines = ascii.Split(new[] { '\n' }, StringSplitOptions.None);
            float lineHeight = Mathf.Max(style.fontSize + 2f, 13f);
            float longest = 0f;
            for (int i = 0; i < lines.Length; i++)
            {
                float width = style.CalcSize(new GUIContent(lines[i])).x;
                if (width > longest)
                {
                    longest = width;
                }
            }

            Rect rect = GUILayoutUtility.GetRect(longest + 8f, lineHeight * lines.Length + 1f, GUILayout.ExpandWidth(true));
            for (int i = 0; i < lines.Length; i++)
            {
                Rect lineRect = new Rect(rect.x, rect.y + i * lineHeight, rect.width, lineHeight);
                GUI.Label(lineRect, lines[i], style);
            }
        }

        private void DrawToolbar()
        {
            Rect panelRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawToolbarWave();
            DrawCategoryFiltersFlow();
            EditorGUILayout.EndVertical();
        }

        private void DrawCategoryFiltersFlow()
        {
            List<string> categories = _categoryFilters.Keys.OrderBy(key => key).ToList();
            float availableWidth = position.width - 32f;
            float estimatedWidth = 112f;
            int perRow = Mathf.Max(1, Mathf.FloorToInt(availableWidth / estimatedWidth));

            for (int start = 0; start < categories.Count; start += perRow)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    int end = Mathf.Min(start + perRow, categories.Count);
                    for (int i = start; i < end; i++)
                    {
                        string category = categories[i];
                        bool enabled = _categoryFilters[category];
                        Color chipColor = GetCategoryColor(category);
                        bool toggled = DrawCategoryFilterButton(category, enabled, chipColor);

                        if (toggled != enabled)
                        {
                            _categoryFilters[category] = toggled;
                        }
                    }
                }
            }
        }

        private void DrawLogFeed(bool fullWidth)
        {
            GUILayoutOption widthOption = fullWidth
                ? GUILayout.ExpandWidth(true)
                : GUILayout.Width(Mathf.Max(320f, (position.width - 24f - SplitSpacing) * 0.58f));

            Rect panelRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox, widthOption, GUILayout.ExpandHeight(true));
            GUILayout.Label($"Signal Feed  {_visibleEntries.Count} entries", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll);
            for (int i = 0; i < _visibleEntries.Count; i++)
            {
                ObservationDeckLogEntry entry = _visibleEntries[i];
                DrawLogRow(entry, i);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawLogRow(ObservationDeckLogEntry entry, int visibleIndex)
        {
            GUIStyle messageStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                normal = { textColor = visibleIndex == _selectedIndex ? Color.white : TextColor },
                fontSize = 11,
                richText = false
            };

            float availableMessageWidth = Mathf.Max(120f, EditorGUIUtility.currentViewWidth - 190f);
            float messageHeight = messageStyle.CalcHeight(new GUIContent(entry.Message), availableMessageWidth);
            float rowHeight = Mathf.Max(62f, 34f + messageHeight);
            Rect rowRect = EditorGUILayout.GetControlRect(false, rowHeight);
            bool isSelected = visibleIndex == _selectedIndex;
            Color categoryColor = GetCategoryColor(entry.Category);
            Color backgroundColor = isSelected ? SelectedRowColor : RowColor;

            DrawRoundedPanel(rowRect, backgroundColor, new Color(1f, 1f, 1f, 0.05f), 14f);
            DrawRoundedPanel(new Rect(rowRect.x, rowRect.y + 1f, 5f, rowRect.height - 2f), categoryColor, categoryColor, 3.5f);

            if (isSelected)
            {
                DrawRoundedPanel(new Rect(rowRect.x + 2f, rowRect.y + 2f, rowRect.width - 4f, 2f), AccentColor, AccentColor, 1.5f);
                DrawRoundedPanel(new Rect(rowRect.x + 2f, rowRect.yMax - 4f, rowRect.width - 4f, 2f), AccentColor, AccentColor, 1.5f);
            }

            Rect contentRect = new Rect(rowRect.x + 12f, rowRect.y + 6f, rowRect.width - 24f, rowRect.height - 12f);
            Rect messageRect = new Rect(contentRect.x, contentRect.y + 24f, contentRect.width, rowHeight - 34f);
            Rect chipRect = new Rect(contentRect.x, contentRect.y, 74f, 18f);
            Rect timeRect = new Rect(chipRect.xMax + 8f, contentRect.y, 86f, 18f);
            Rect typeRect = new Rect(timeRect.xMax + 8f, contentRect.y, 64f, 18f);

            DrawCategoryChip(chipRect, entry.Category, categoryColor);
            DrawMetaLabel(timeRect, entry.Timestamp.ToString("HH:mm:ss.fff"), SubtleTextColor, TextAnchor.MiddleLeft);
            DrawMetaLabel(typeRect, entry.LogType.ToString().ToUpperInvariant(), GetLogTypeColor(entry.LogType), TextAnchor.MiddleLeft);

            GUI.Label(messageRect, entry.Message, messageStyle);
            EditorGUIUtility.AddCursorRect(rowRect, MouseCursor.Link);

            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                _selectedIndex = visibleIndex;
                _detailTabIndex = 0;
                Event.current.Use();
                Repaint();
            }

            EditorGUILayout.Space(3f);
        }

        private void DrawInspectorPanel(bool fullWidth)
        {
            GUILayoutOption widthOption = fullWidth
                ? GUILayout.ExpandWidth(true)
                : GUILayout.Width(Mathf.Max(280f, (position.width - 24f - SplitSpacing) * 0.42f));

            Rect panelRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox, widthOption, GUILayout.ExpandHeight(true));
            GUILayout.Label("Starboard Analysis", EditorStyles.boldLabel);

            ObservationDeckLogEntry entry = GetSelectedEntry();
            if (entry == null)
            {
                DrawRocketAscii();
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox("Select a transmission from the signal feed to inspect its origin frame and telemetry summary.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            DrawRocketAscii();
            EditorGUILayout.Space(6f);
            _detailTabIndex = GUILayout.Toolbar(_detailTabIndex, DetailTabs);
            EditorGUILayout.Space(6f);

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            if (_detailTabIndex == 0)
            {
                DrawTraceTab(entry);
            }
            else
            {
                DrawDetailsTab(entry);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawCompactTracePanel()
        {
            Rect panelRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.Label("Trace", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            ObservationDeckLogEntry entry = GetSelectedEntry();
            if (entry == null)
            {
                EditorGUILayout.HelpBox("Select a transmission to inspect its call path.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            List<ObservationDeckTraceFrame> frames = ParseTraceFrames(entry.StackTrace);
            if (frames.Count == 0)
            {
                EditorGUILayout.HelpBox("No source frame was captured for this transmission.", MessageType.None);
            }
            else
            {
                for (int i = 0; i < frames.Count; i++)
                {
                    DrawTraceFrame(frames[i], i);
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawTraceTab(ObservationDeckLogEntry entry)
        {
            GUILayout.Label("Call Path", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            List<ObservationDeckTraceFrame> frames = ParseTraceFrames(entry.StackTrace);
            if (frames.Count == 0)
            {
                EditorGUILayout.HelpBox("No source frame was captured for this transmission.", MessageType.None);
                return;
            }

            for (int i = 0; i < frames.Count; i++)
            {
                ObservationDeckTraceFrame frame = frames[i];
                DrawTraceFrame(frame, i);
            }
        }

        private void DrawDetailsTab(ObservationDeckLogEntry entry)
        {
            GUILayout.Label("Transmission Details", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);
            ObservationDeckTraceFrame primaryFrame = ParseTraceFrames(entry.StackTrace).FirstOrDefault();
            EditorGUILayout.LabelField("Category", entry.Category);
            EditorGUILayout.LabelField("Severity", entry.LogType.ToString());
            EditorGUILayout.LabelField("Time", entry.Timestamp.ToString("HH:mm:ss.fff"));
            if (!string.IsNullOrWhiteSpace(primaryFrame.ScriptName))
            {
                EditorGUILayout.LabelField("Source", $"{primaryFrame.MethodName}  ->  {primaryFrame.ScriptName}:{primaryFrame.LineNumber}");
            }
            EditorGUILayout.Space(8f);
            GUILayout.Label("Message", EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(entry.Message, EditorStyles.wordWrappedMiniLabel, GUILayout.MinHeight(54f));
        }

        private void SyncCategories()
        {
            foreach (ObservationDeckLogEntry entry in ObservationDeckLogStore.Entries)
            {
                if (!_categoryFilters.ContainsKey(entry.Category))
                {
                    _categoryFilters.Add(entry.Category, true);
                }
            }
        }

        private void RebuildVisibleEntries()
        {
            _visibleEntries.Clear();
            HashSet<string> seen = _collapseDuplicates ? new HashSet<string>(StringComparer.Ordinal) : null;

            foreach (ObservationDeckLogEntry entry in ObservationDeckLogStore.Entries)
            {
                if (!_categoryFilters.TryGetValue(entry.Category, out bool enabled) || !enabled)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(_searchText))
                {
                    bool matches = entry.Message.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0
                        || entry.Category.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0
                        || entry.StackTrace.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!matches)
                    {
                        continue;
                    }
                }

                if (seen != null)
                {
                    string key = $"{entry.Category}|{entry.Message}|{entry.StackTrace}|{entry.LogType}";
                    if (!seen.Add(key))
                    {
                        continue;
                    }
                }

                _visibleEntries.Add(entry);
            }

            _visibleEntries.Sort((left, right) => right.Timestamp.CompareTo(left.Timestamp));

            if (_selectedIndex >= _visibleEntries.Count)
            {
                _selectedIndex = _visibleEntries.Count - 1;
            }
        }

        private ObservationDeckLogEntry GetSelectedEntry()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _visibleEntries.Count)
            {
                return null;
            }

            return _visibleEntries[_selectedIndex];
        }

        private static Color GetCategoryColor(string category)
        {
            switch (category.ToUpperInvariant())
            {
                case "CORE":
                    return AccentBlue;
                case "UI":
                    return new Color(0.49f, 0.33f, 0.72f);
                case "SOUND":
                    return AccentGold;
                case "CURRENCY":
                    return new Color(0.55f, 0.74f, 0.4f);
                case "SAVE":
                    return AccentRed;
                case "SCENE":
                    return AccentOrange;
                case "EDITOR":
                    return new Color(0.56f, 0.73f, 0.92f);
                case "EXAMPLE":
                    return new Color(0.92f, 0.82f, 0.5f);
                case "LOGGER":
                    return Color.white;
                case "GAME":
                    return new Color(0.62f, 0.7f, 0.78f);
                case "BOOTSTRAP":
                    return new Color(0.39f, 0.87f, 0.78f);
                case "BOOSTERS":
                    return new Color(0.98f, 0.72f, 0.42f);
                case "BOARD":
                    return new Color(0.49f, 0.78f, 0.99f);
                case "VIEWS":
                    return new Color(0.96f, 0.59f, 0.84f);
                case "INPUT":
                    return new Color(0.72f, 0.95f, 0.9f);
                case "SYSTEMS":
                    return new Color(1f, 0.56f, 0.45f);
                case "AUDIO":
                    return new Color(1f, 0.9f, 0.43f);
                default:
                    return TextColor;
            }
        }

        private static Color GetLogTypeColor(LogType logType)
        {
            switch (logType)
            {
                case LogType.Warning:
                    return WarningColor;
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    return ErrorColor;
                default:
                    return TextColor;
            }
        }

        private static string ToHtmlColor(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGB(color)}";
        }

        private static GUIStyle StyledSubtleLabel()
        {
            return new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = SubtleTextColor }
            };
        }

        private static void DrawMetaLabel(Rect rect, string text, Color color, TextAnchor alignment)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = alignment,
                normal = { textColor = color }
            };

            GUI.Label(rect, text, style);
        }

        private static void DrawCategoryChip(Rect rect, string category, Color color)
        {
            EditorGUI.DrawRect(rect, color);
            GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = BackgroundColor }
            };

            GUI.Label(rect, category, style);
        }

        private static List<ObservationDeckTraceFrame> ParseTraceFrames(string stackTrace)
        {
            List<ObservationDeckTraceFrame> frames = new List<ObservationDeckTraceFrame>();
            string[] lines = stackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                Match match = StackTraceRegex.Match(lines[i]);
                if (!match.Success)
                {
                    continue;
                }

                string path = match.Groups["path"].Value.Replace('\\', '/');
                if (path.Contains("UnityEngine.Debug", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (path.Contains("SingularityConsole.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string methodName = SimplifyMethodName(match.Groups["method"].Value);
                string scriptName = System.IO.Path.GetFileName(path);
                if (!int.TryParse(match.Groups["line"].Value, out int lineNumber))
                {
                    continue;
                }

                frames.Add(new ObservationDeckTraceFrame(methodName, scriptName, path, lineNumber));
            }

            frames.Reverse();
            return frames;
        }

        private void OnEditorUpdate()
        {
            if (focusedWindow != this)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup - _lastAnimationTick > 0.18d)
            {
                _rocketFrameIndex = (_rocketFrameIndex + 1) % 3;
                _lastAnimationTick = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void DrawRocketAscii()
        {
            string[] frames =
            {
@"            <color=#FFFFFF>/\</color>
            <color=#FFFFFF>/  \</color>
            <color=#FFFFFF>/____\</color>
            <color=#FFFFFF>|    |</color>
            <color=#34558F>|====|</color>
            <color=#E0B560>|====|</color>
            <color=#FFFFFF>/|====|\</color>
            <color=#34558F>/_|====|_\</color>
            <color=#34558F>/__|====|__\</color>
            <color=#FFFFFF>/_/  \_\</color>
            <color=#FFFFFF>V  /\  V</color>
            <color=#FFFFFF>V /  \ V</color>
           <color=#9AA4B5>  ~  ~~  ~</color>
           <color=#E0B560> \ |||| /</color>
            <color=#E85F2F>\||||||/</color>
             <color=#C91F38>\||||/</color>",
@"            <color=#FFFFFF>/\</color>
            <color=#FFFFFF>/  \</color>
            <color=#FFFFFF>/____\</color>
            <color=#FFFFFF>|    |</color>
            <color=#34558F>|====|</color>
            <color=#E0B560>|====|</color>
            <color=#FFFFFF>/|====|\</color>
            <color=#34558F>/_|====|_\</color>
            <color=#34558F>/__|====|__\</color>
            <color=#FFFFFF>/_/  \_\</color>
            <color=#FFFFFF>V  /\  V</color>
            <color=#FFFFFF>V /  \ V</color>
           <color=#9AA4B5> ~  ~  ~~ </color>
            <color=#E0B560>\ |||||| /</color>
            <color=#E85F2F>\||||||/</color>
            <color=#C91F38> \||||/</color>",
@"            <color=#FFFFFF>/\</color>
            <color=#FFFFFF>/  \</color>
            <color=#FFFFFF>/____\</color>
            <color=#FFFFFF>|    |</color>
            <color=#34558F>|====|</color>
            <color=#E0B560>|====|</color>
            <color=#FFFFFF>/|====|\</color>
            <color=#34558F>/_|====|_\</color>
            <color=#34558F>/__|====|__\</color>
            <color=#FFFFFF>/_/  \_\</color>
            <color=#FFFFFF>V  /\  V</color>
            <color=#FFFFFF>V /  \ V</color>
           <color=#9AA4B5>  ~~  ~  ~</color>
            <color=#E0B560>\ |||| /</color>
            <color=#E85F2F>\||||||/</color>
             <color=#C91F38>\||||/</color>"
            };

            GUIStyle rocketStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 11,
                richText = true
            };
            rocketStyle.font = EventHorizonEditorFont.CascadiaMono;
            rocketStyle.clipping = TextClipping.Overflow;

            DrawRichAsciiBlock(frames[_rocketFrameIndex], rocketStyle, 204f);
        }

        private static void DrawRichAsciiBlock(string ascii, GUIStyle style, float height)
        {
            string[] lines = ascii.Split(new[] { '\n' }, StringSplitOptions.None);
            float lineHeight = Mathf.Max(style.fontSize + 3f, 14f);
            Rect rect = GUILayoutUtility.GetRect(10f, height, GUILayout.ExpandWidth(true));
            float totalHeight = lineHeight * lines.Length;
            float startY = rect.y + Mathf.Max(0f, (rect.height - totalHeight) * 0.5f);

            for (int i = 0; i < lines.Length; i++)
            {
                Rect lineRect = new Rect(rect.x, startY + i * lineHeight, rect.width, lineHeight);
                GUI.Label(lineRect, lines[i], style);
            }
        }

        private void DrawTraceFrame(ObservationDeckTraceFrame frame, int index)
        {
            Rect frameRect = EditorGUILayout.GetControlRect(false, 54f);
            DrawRoundedPanel(frameRect, new Color(0.12f, 0.14f, 0.2f), new Color(1f, 1f, 1f, 0.05f), 14f);
            DrawRoundedPanel(new Rect(frameRect.x, frameRect.y + 1f, 4f, frameRect.height - 2f), AccentBlue, AccentBlue, 3f);

            Rect contentRect = new Rect(frameRect.x + 10f, frameRect.y + 6f, frameRect.width - 20f, frameRect.height - 12f);
            GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, 16f), $"Hop {index + 1}", StyledSubtleLabel());
            GUI.Label(new Rect(contentRect.x, contentRect.y + 16f, contentRect.width, 18f), frame.MethodName, EditorStyles.boldLabel);
            GUI.Label(new Rect(contentRect.x, contentRect.y + 33f, contentRect.width, 16f), $"{frame.ScriptName}:{frame.LineNumber}", StyledSubtleLabel());

            EditorGUIUtility.AddCursorRect(frameRect, MouseCursor.Link);
            if (Event.current.type == EventType.MouseDown && frameRect.Contains(Event.current.mousePosition))
            {
                OpenTraceFrame(frame);
                Event.current.Use();
            }

            EditorGUILayout.Space(4f);
        }

        private void DrawVerticalSplitter(Rect rect, Rect bounds)
        {
            EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.08f));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
            Event evt = Event.current;

            if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition))
            {
                _draggingCompactSplitter = true;
                evt.Use();
            }

            if (_draggingCompactSplitter && evt.type == EventType.MouseDrag)
            {
                _compactSplitRatio = Mathf.Clamp((evt.mousePosition.x - bounds.x) / bounds.width, 0.3f, 0.7f);
                Repaint();
                evt.Use();
            }

            if (_draggingCompactSplitter && (evt.type == EventType.MouseUp || evt.rawType == EventType.MouseUp))
            {
                _draggingCompactSplitter = false;
                evt.Use();
            }
        }

        private static string SimplifyMethodName(string rawMethodName)
        {
            string methodName = rawMethodName.Replace(":", ".");
            int genericMarker = methodName.IndexOf('<');
            if (genericMarker >= 0)
            {
                return methodName.Substring(0, genericMarker);
            }

            return methodName;
        }

        private static void OpenTraceFrame(ObservationDeckTraceFrame frame)
        {
            if (string.IsNullOrWhiteSpace(frame.AssetPath))
            {
                return;
            }

            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(frame.AssetPath);
            if (script != null)
            {
                AssetDatabase.OpenAsset(script, frame.LineNumber);
                return;
            }

            InternalEditorUtility.OpenFileAtLineExternal(frame.AssetPath, frame.LineNumber);
        }

        private static void DrawTopography(Rect rect)
        {
            Handles.BeginGUI();
            Color lineColor = new Color(1f, 1f, 1f, 0.09f);
            for (int layer = 0; layer < 10; layer++)
            {
                Vector3[] points = new Vector3[48];
                float amplitude = 14f + layer * 3f;
                float y = rect.y + 32f + layer * 58f;
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

        private bool DrawCategoryFilterButton(string category, bool enabled, Color chipColor)
        {
            Color previousBackground = GUI.backgroundColor;
            Color previousContent = GUI.contentColor;
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { textColor = TextColor },
                hover = { textColor = TextColor },
                active = { textColor = TextColor },
                focused = { textColor = TextColor },
                onNormal = { textColor = TextColor },
                onHover = { textColor = TextColor },
                onActive = { textColor = TextColor },
                onFocused = { textColor = TextColor }
            };
            GUI.backgroundColor = enabled ? chipColor : AccentMuted;
            GUI.contentColor = TextColor;
            bool toggled = GUILayout.Toggle(enabled, category, buttonStyle, GUILayout.Height(24f));
            Rect rect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), chipColor);
            GUI.backgroundColor = previousBackground;
            GUI.contentColor = previousContent;
            return toggled;
        }

        private static void DrawToolbarWave()
        {
            Rect waveRect = EditorGUILayout.GetControlRect(false, 12f);
            Handles.BeginGUI();
            Vector3[] points = new Vector3[28];
            for (int i = 0; i < points.Length; i++)
            {
                float x = waveRect.x + (waveRect.width / (points.Length - 1)) * i;
                float y = waveRect.center.y + Mathf.Sin(i * 0.55f) * 3f;
                points[i] = new Vector3(x, y, 0f);
            }

            Handles.color = new Color(0.88f, 0.71f, 0.38f, 0.55f);
            Handles.DrawAAPolyLine(2f, points);
            Handles.EndGUI();
        }

        private static void DrawStarfieldFooter(Rect rect)
        {
            float footerHeight = 8f;
            float footerY = rect.yMax - footerHeight;
            float segment = rect.width / 4f;

            EditorGUI.DrawRect(new Rect(rect.x, footerY, segment, footerHeight), FooterCyan);
            EditorGUI.DrawRect(new Rect(rect.x + segment, footerY, segment, footerHeight), FooterGreen);
            EditorGUI.DrawRect(new Rect(rect.x + segment * 2f, footerY, segment, footerHeight), FooterYellow);
            EditorGUI.DrawRect(new Rect(rect.x + segment * 3f, footerY, rect.width - segment * 3f, footerHeight), FooterRed);
        }

        private static void DrawWindowFrame(Rect rect)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), AccentGold);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), AccentGold);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2f, rect.height), AccentGold);
            EditorGUI.DrawRect(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), AccentGold);
        }


        private static void DrawRoundedPanel(Rect rect, Color fillColor, Color outlineColor, float radius)
        {
            Handles.BeginGUI();
            Handles.color = fillColor;
            DrawRoundedShape(rect, radius);

            if (outlineColor.a > 0f)
            {
                Handles.color = outlineColor;
                DrawRoundedOutline(rect, radius);
            }

            Handles.EndGUI();
        }

        private static void DrawRoundedShape(Rect rect, float radius)
        {
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            Vector3[] points = BuildRoundedRectPoints(rect, radius);
            Handles.DrawAAConvexPolygon(points);
        }

        private static void DrawRoundedOutline(Rect rect, float radius)
        {
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            Vector3[] points = BuildRoundedRectPoints(rect, radius);
            Vector3[] closedPoints = new Vector3[points.Length + 1];

            for (int i = 0; i < points.Length; i++)
            {
                closedPoints[i] = points[i];
            }

            closedPoints[closedPoints.Length - 1] = points[0];
            Handles.DrawAAPolyLine(1.25f, closedPoints);
        }

        private static Vector3[] BuildRoundedRectPoints(Rect rect, float radius)
        {
            if (radius <= 0.01f)
            {
                return new[]
                {
                    new Vector3(rect.xMin, rect.yMin),
                    new Vector3(rect.xMax, rect.yMin),
                    new Vector3(rect.xMax, rect.yMax),
                    new Vector3(rect.xMin, rect.yMax)
                };
            }

            const int CornerSegments = 8;
            List<Vector3> points = new List<Vector3>(CornerSegments * 4);

            AddCorner(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, -90f, 0f, CornerSegments);
            AddCorner(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, CornerSegments);
            AddCorner(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, CornerSegments);
            AddCorner(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, CornerSegments);

            return points.ToArray();
        }

        private static void AddCorner(List<Vector3> points, Vector2 center, float radius, float startAngle, float endAngle, int segments)
        {
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float radians = Mathf.Deg2Rad * Mathf.Lerp(startAngle, endAngle, t);
                points.Add(new Vector3(center.x + Mathf.Cos(radians) * radius, center.y + Mathf.Sin(radians) * radius));
            }
        }

        private static void DrawStars(Rect rect)
        {
            Handles.BeginGUI();
            Vector2[] stars =
            {
                new Vector2(38f, 34f), new Vector2(92f, 60f), new Vector2(248f, 42f), new Vector2(332f, 88f),
                new Vector2(512f, 34f), new Vector2(648f, 78f), new Vector2(774f, 44f), new Vector2(884f, 110f),
                new Vector2(104f, 182f), new Vector2(214f, 236f), new Vector2(436f, 188f), new Vector2(586f, 248f),
                new Vector2(726f, 194f), new Vector2(848f, 286f), new Vector2(78f, 344f), new Vector2(304f, 388f),
                new Vector2(482f, 332f), new Vector2(690f, 402f), new Vector2(854f, 366f), new Vector2(932f, 458f)
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
