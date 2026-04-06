using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR_WIN
using Microsoft.Win32;
#endif

using EventHorizon.Editor;

/// <summary>
/// DataOrbit
/// Place this file inside an Editor folder.
/// Open via EventHorizon/DataOrbit
///
/// What this version supports:
/// - Normal PlayerPrefs string/int/float editing
/// - Windows registry discovery where possible
/// - Reading a JSON save container stored under a single PlayerPrefs key
/// - Reading external .json save files from common/default save locations
/// - Flattening nested save entries so they are visible and editable in the tool
///
/// Supported JSON container formats:
/// {
///   "Entries": [
///     { "Key": "Level Number", "Value": "{\"Value\":4}" },
///     { "Key": "Currency Wallet", "Value": "{\"Entries\":[...]}" }
///   ],
///   "Timestamp": "..."
/// }
/// </summary>
public sealed class DataOrbit : EditorWindow
{
    private const float FooterHeight = 8f;
    private const float NarrowLayoutBreakpoint = 920f;
    private const float CompactToolbarBreakpoint = 1180f;
    private static readonly string[] CommonContainerKeys = { "GDF_GameData", "GameData", "GDF_SaveSlot_0", "SaveSlot_0" };
    private const string CatalogEditorPrefKey = "PlayerPrefsPro.Catalog";
    private const string FavoritesEditorPrefKey = "PlayerPrefsPro.Favorites";
    private const string SelectedKeyEditorPrefKey = "PlayerPrefsPro.SelectedKey";
    private const string LeftPanelWidthPrefKey = "PlayerPrefsPro.LeftPanelWidth";
    private const string SettingsEditorPrefKey = "PlayerPrefsPro.Settings";

    private const float MinLeftPanelWidth = 280f;
    private const float MaxLeftPanelWidth = 560f;
    private static readonly string[] KnownPlayerPrefsPrefixes = { string.Empty, "GDF_" };
    private static readonly Color BackgroundColor = new Color(0.03f, 0.05f, 0.1f);
    private static readonly Color SurfaceColor = new Color(0.08f, 0.1f, 0.15f, 0.78f);
    private static readonly Color SurfaceAltColor = new Color(0.1f, 0.12f, 0.18f, 0.9f);
    private static readonly Color AccentGold = new Color(0.88f, 0.71f, 0.38f);
    private static readonly Color AccentBlue = new Color(0.22f, 0.35f, 0.58f);
    private static readonly Color TextColor = new Color(0.91f, 0.92f, 0.95f);
    private static readonly Color SubtleTextColor = new Color(0.58f, 0.63f, 0.72f);
    private static readonly Color FooterCyan = new Color(0.34f, 0.66f, 0.66f);
    private static readonly Color FooterGreen = new Color(0.19f, 0.69f, 0.43f);
    private static readonly Color FooterYellow = new Color(0.82f, 0.68f, 0.16f);
    private static readonly Color FooterRed = new Color(0.78f, 0.15f, 0.18f);
    private static readonly Color StarColor = new Color(0.95f, 0.95f, 0.98f, 0.85f);
    private const string KeysDeckArt =
@"        .          .
    .      /\         *
       *  /==\    .
         |::::|
      .  |::::|   .
         /|::|\      *
        /_|__|_\
          /\/\
         DATA ORBIT";

    private enum PrefValueType
    {
        String,
        Int,
        Float,
        Bool
    }

    private enum EntrySourceType
    {
        PlayerPrefs,
        PlayerPrefsJsonContainer,
        ExternalJsonFile
    }

    [Serializable]
    private sealed class EntryData
    {
        public string key;
        public PrefValueType type;
        public string stringValue;
        public int intValue;
        public float floatValue;
        public bool favorite;
        public string note;
        public long lastTouchedTicks;
        public EntrySourceType sourceType;
        public string sourceName;
        public string parentContainerKey;
        public string sourceFilePath;
        public bool isReadOnly;
        public bool isExpandedFromContainer;
        public string rawJsonValue;

        public string DisplayValue
        {
            get
            {
                return type switch
                {
                    PrefValueType.String => stringValue ?? string.Empty,
                    PrefValueType.Int => intValue.ToString(CultureInfo.InvariantCulture),
                    PrefValueType.Float => floatValue.ToString("0.#####", CultureInfo.InvariantCulture),
                    PrefValueType.Bool => boolValue ? "True" : "False",
                    _ => string.Empty
                };
            }
        }

        public bool boolValue;

        public EntryData Clone()
        {
            return new EntryData
            {
                key = key,
                type = type,
                stringValue = stringValue,
                intValue = intValue,
                floatValue = floatValue,
                boolValue = boolValue,
                favorite = favorite,
                note = note,
                lastTouchedTicks = lastTouchedTicks,
                sourceType = sourceType,
                sourceName = sourceName,
                parentContainerKey = parentContainerKey,
                sourceFilePath = sourceFilePath,
                isReadOnly = isReadOnly,
                isExpandedFromContainer = isExpandedFromContainer,
                rawJsonValue = rawJsonValue
            };
        }
    }

    [Serializable]
    private sealed class EntryListWrapper
    {
        public List<EntryData> items = new();
    }

    [Serializable]
    private sealed class FavoritesWrapper
    {
        public List<string> keys = new();
    }

    [Serializable]
    private sealed class SettingsData
    {
        public string jsonContainerKey = string.Empty;
        public string externalJsonPath = string.Empty;
        public bool autoScanPersistentDataPath = true;
        public bool autoParseJsonStringsInPlayerPrefs = true;
        public bool autoLoadExternalJsonFiles = true;
        public bool showReadOnlyEntries = true;
        public bool hideUnitySystemEntries = true;
    }

    [Serializable]
    private sealed class SnapshotFile
    {
        public string companyName;
        public string productName;
        public string unityVersion;
        public string exportedAtUtc;
        public List<EntryData> items = new();
    }

    [Serializable]
    private sealed class SaveContainerJson
    {
        public List<SaveContainerEntryJson> Entries = new();
        public string Timestamp;
    }

    [Serializable]
    private sealed class SaveContainerEntryJson
    {
        public string Key;
        public string Value;
    }

    [Serializable]
    private sealed class SimpleIntValueJson
    {
        public int Value;
    }

    [Serializable]
    private sealed class SimpleFloatValueJson
    {
        public float Value;
    }

    [Serializable]
    private sealed class SimpleBoolValueJson
    {
        public bool Value;
    }

    [Serializable]
    private sealed class SimpleStringValueJson
    {
        public string Value;
    }

    [Serializable]
    private sealed class CurrencyWalletJson
    {
        public List<CurrencyBalanceJson> Entries = new();
    }

    [Serializable]
    private sealed class CurrencyBalanceJson
    {
        public string CurrencyID;
        public int Balance;
    }

    private readonly List<EntryData> _entries = new();
    private readonly HashSet<string> _favorites = new();
    private readonly Dictionary<string, List<EntryData>> _containerChildrenByContainerKey = new();
    private readonly Dictionary<string, List<EntryData>> _fileChildrenByFilePath = new();

    private Vector2 _listScroll;
    private Vector2 _detailsScroll;
    private Vector2 _jsonTableScroll;
    private string _search = string.Empty;
    private string _selectedEntryId = string.Empty;
    private EntryData _editingCopy;
    private EntryData _newEntry = CreateNewEntry();
    private SettingsData _settings = new();
    private string _jsonTableHostEntryId = string.Empty;
    private readonly List<EntryData> _jsonTableEntries = new();

    private bool _showOnlyFavorites;
    private bool _showAddPanel = true;
    private bool _showStats = true;
    private bool _showDangerZone;
    private bool _showAdvanced;
    private bool _showSettings;
    private float _leftPanelWidth = 360f;
    private bool _resizing;

    [MenuItem("EventHorizon/DataOrbit")]
    public static void Open()
    {
        var window = GetWindow<DataOrbit>();
        window.titleContent = new GUIContent("DataOrbit");
        window.minSize = new Vector2(760f, 520f);
        window.Show();
    }

    [MenuItem("Tools/PlayerPrefs Pro", false, 40)]
    public static void OpenLegacy()
    {
        Open();
    }
    
    private static EntryData CreateNewEntry()
    {
        return new EntryData
        {
            key = string.Empty,
            type = PrefValueType.String,
            stringValue = string.Empty,
            intValue = 0,
            floatValue = 0f,
            boolValue = false,
            favorite = false,
            note = string.Empty,
            lastTouchedTicks = DateTime.UtcNow.Ticks,
            sourceType = EntrySourceType.PlayerPrefs,
            sourceName = "PlayerPrefs"
        };
    }

    private void OnEnable()
    {
        _leftPanelWidth = Mathf.Clamp(EditorPrefs.GetFloat(LeftPanelWidthPrefKey, 360f), MinLeftPanelWidth, MaxLeftPanelWidth);
        LoadSettings();
        LoadCatalog();
        LoadFavorites();
        LoadSelectedEntry();
        SyncFavoritesIntoEntries();
        FullRefresh();

        if (string.IsNullOrEmpty(_selectedEntryId) && _entries.Count > 0)
            _selectedEntryId = BuildEntryId(_entries[0]);

        RebuildEditingCopy();
    }

    private void OnDisable()
    {
        PersistCatalog();
        PersistFavorites();
        PersistSelectedEntry();
        PersistSettings();
        EditorPrefs.SetFloat(LeftPanelWidthPrefKey, _leftPanelWidth);
    }

    private void OnGUI()
    {
        Font previousFont = GUI.skin.font;
        GUI.skin.font = EventHorizonEditorFont.CascadiaMono;
        try
        {
            DrawBackground();
            float toolbarHeight = DrawToolbar();

            Rect contentRect = new Rect(
                8f,
                toolbarHeight + 6f,
                Mathf.Max(0f, position.width - 16f),
                Mathf.Max(0f, position.height - toolbarHeight - FooterHeight - 14f));

            if (contentRect.width > 0f && contentRect.height > 0f)
            {
                DrawMainLayout(contentRect);
            }

            if (GUI.changed)
                Repaint();
        }
        finally
        {
            GUI.skin.font = previousFont;
        }
    }

    private void DrawBackground()
    {
        Rect rect = new Rect(0f, 0f, position.width, position.height);
        EditorGUI.DrawRect(rect, BackgroundColor);
        DrawTopography(rect, 1.25f);
        DrawStars(rect, 1.2f);
        DrawWindowFrame(rect);
        DrawFooterStrip(rect);
    }

    private float DrawToolbar()
    {
        bool compact = position.width < CompactToolbarBreakpoint;
        Rect toolbarRect = EditorGUILayout.BeginVertical(EditorStyles.toolbar);
        EditorGUI.DrawRect(toolbarRect, new Color(SurfaceColor.r, SurfaceColor.g, SurfaceColor.b, 0.96f));
        if (compact)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(6f);
                GUILayout.Label("DataOrbit", ToolbarTitleStyle(), GUILayout.Width(140f));
                GUILayout.Space(8f);
                _search = ToolbarSearchField(_search, GUILayout.MinWidth(120f));
                GUILayout.Space(4f);
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    FullRefresh();
                if (GUILayout.Button("Discover", EditorStyles.toolbarButton, GUILayout.Width(75f)))
                    DiscoverKeys();
                GUILayout.Space(4f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(6f);
                _showOnlyFavorites = GUILayout.Toggle(_showOnlyFavorites, new GUIContent("Fav"), EditorStyles.toolbarButton, GUILayout.Width(52f));
                _showStats = GUILayout.Toggle(_showStats, new GUIContent("Stats"), EditorStyles.toolbarButton, GUILayout.Width(55f));
                _showAdvanced = GUILayout.Toggle(_showAdvanced, new GUIContent("Adv"), EditorStyles.toolbarButton, GUILayout.Width(48f));
                _showSettings = GUILayout.Toggle(_showSettings, new GUIContent("Cfg"), EditorStyles.toolbarButton, GUILayout.Width(48f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Export", EditorStyles.toolbarButton, GUILayout.Width(65f)))
                    ExportSnapshot();
                if (GUILayout.Button("Import", EditorStyles.toolbarButton, GUILayout.Width(65f)))
                    ImportSnapshot();
                if (GUILayout.Button("Delete All", EditorStyles.toolbarButton, GUILayout.Width(78f)))
                    DeleteAllWithConfirmation();
                GUILayout.Space(4f);
            }
        }
        else
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(6);
                GUILayout.Label("DataOrbit", ToolbarTitleStyle(), GUILayout.Width(140f));

                GUILayout.Space(8f);
                _search = ToolbarSearchField(_search, GUILayout.MinWidth(180f), GUILayout.MaxWidth(340f));

                GUILayout.Space(8f);
                _showOnlyFavorites = GUILayout.Toggle(_showOnlyFavorites, new GUIContent("Favorites"), EditorStyles.toolbarButton, GUILayout.Width(80f));
                _showStats = GUILayout.Toggle(_showStats, new GUIContent("Stats"), EditorStyles.toolbarButton, GUILayout.Width(55f));
                _showAdvanced = GUILayout.Toggle(_showAdvanced, new GUIContent("Advanced"), EditorStyles.toolbarButton, GUILayout.Width(80f));
                _showSettings = GUILayout.Toggle(_showSettings, new GUIContent("Settings"), EditorStyles.toolbarButton, GUILayout.Width(75f));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    FullRefresh();

                if (GUILayout.Button("Discover", EditorStyles.toolbarButton, GUILayout.Width(75f)))
                    DiscoverKeys();

                if (GUILayout.Button("Export", EditorStyles.toolbarButton, GUILayout.Width(65f)))
                    ExportSnapshot();

                if (GUILayout.Button("Import", EditorStyles.toolbarButton, GUILayout.Width(65f)))
                    ImportSnapshot();

                if (GUILayout.Button("Delete All", EditorStyles.toolbarButton, GUILayout.Width(78f)))
                    DeleteAllWithConfirmation();

                GUILayout.Space(4f);
            }
        }
        EditorGUILayout.EndVertical();
        return compact ? 46f : 24f;
    }

    private void DrawMainLayout(Rect rect)
    {
        if (rect.width < NarrowLayoutBreakpoint)
        {
            float topHeight = Mathf.Clamp(rect.height * 0.52f, 220f, rect.height - 220f);
            Rect topRect = new Rect(rect.x, rect.y, rect.width, topHeight);
            Rect bottomRect = new Rect(rect.x, topRect.yMax + 8f, rect.width, Mathf.Max(0f, rect.height - topHeight - 8f));
            DrawPanel(topRect, "Keys", DrawLeftPanel);
            DrawPanel(bottomRect, "Inspector", DrawRightPanel);
            return;
        }

        float leftWidth = Mathf.Clamp(_leftPanelWidth, MinLeftPanelWidth, Mathf.Max(MinLeftPanelWidth, rect.width - 340f));
        Rect leftRect = new Rect(rect.x, rect.y, leftWidth, rect.height);
        Rect splitterRect = new Rect(leftRect.xMax + 2f, rect.y, 4f, rect.height);
        Rect rightRect = new Rect(splitterRect.xMax + 2f, rect.y, rect.width - leftWidth - 8f, rect.height);

        DrawPanel(leftRect, "Keys", DrawLeftPanel);
        DrawSplitter(splitterRect);
        DrawPanel(rightRect, "Inspector", DrawRightPanel);
    }

    private void DrawPanel(Rect rect, string title, Action<Rect> innerDrawer)
    {
        DrawPanelChrome(rect, title == "Keys");

        Rect headerRect = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 34f);
        EditorGUI.DrawRect(headerRect, SurfaceAltColor);
        GUI.Label(new Rect(headerRect.x + 12f, headerRect.y + 7f, headerRect.width - 24f, 20f), title, HeaderStyle());

        Rect bodyRect = new Rect(rect.x + 8f, rect.y + 42f, rect.width - 16f, rect.height - 50f);
        innerDrawer(bodyRect);
    }

    private void DrawLeftPanel(Rect rect)
    {
        GUILayout.BeginArea(rect);

        if (position.width >= NarrowLayoutBreakpoint)
        {
            DrawKeysDeckHeader();
            GUILayout.Space(6f);
        }

        if (_showStats)
            DrawStatsBar();

        if (_showSettings)
        {
            GUILayout.Space(6f);
            DrawSettingsPanel();
        }

        GUILayout.Space(6f);
        DrawEntryList();

        GUILayout.Space(8f);
        _showAddPanel = EditorGUILayout.BeginFoldoutHeaderGroup(_showAddPanel, "Quick Add / Edit Data");
        if (_showAddPanel)
            DrawAddPanel();
        EditorGUILayout.EndFoldoutHeaderGroup();

        GUILayout.EndArea();
    }

    private void DrawKeysDeckHeader()
    {
        GUIStyle artStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            richText = false,
            fontSize = 10,
            alignment = TextAnchor.UpperLeft
        };
        artStyle.font = EventHorizonEditorFont.CascadiaMono;
        artStyle.clipping = TextClipping.Overflow;
        artStyle.normal.textColor = TextColor;

        using (new EditorGUILayout.VerticalScope(CardStyle()))
        {
            DrawAsciiBlock(KeysDeckArt, artStyle);
        }
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

        Rect rect = GUILayoutUtility.GetRect(longest + 8f, lineHeight * lines.Length + 4f, GUILayout.ExpandWidth(true));
        for (int i = 0; i < lines.Length; i++)
        {
            Rect lineRect = new Rect(rect.x, rect.y + i * lineHeight, rect.width, lineHeight);
            GUI.Label(lineRect, lines[i], style);
        }
    }

    private void DrawStatsBar()
    {
        int total = GetFilteredEntriesForList().Count;
        int playerPrefs = _entries.Count(x => x.sourceType == EntrySourceType.PlayerPrefs);
        int container = _entries.Count(x => x.sourceType == EntrySourceType.PlayerPrefsJsonContainer);
        int files = _entries.Count(x => x.sourceType == EntrySourceType.ExternalJsonFile);
        int favorites = _entries.Count(x => x.favorite);

        using (new EditorGUILayout.VerticalScope(CardStyle()))
        {
            GUILayout.Label("Overview", SectionTitleStyle());
            using (new EditorGUILayout.HorizontalScope())
            {
                StatPill($"Shown {total}");
                StatPill($"Prefs {playerPrefs}");
                StatPill($"Container {container}");
                StatPill($"Files {files}");
                StatPill($"★ {favorites}");
            }
        }
    }

    private void DrawSettingsPanel()
    {
        using (new EditorGUILayout.VerticalScope(CardStyle()))
        {
            GUILayout.Label("Data Sources", SectionTitleStyle());
            EditorGUILayout.HelpBox("Use this if your game stores many values inside one JSON save key or in external .json files.", MessageType.None);

            _settings.jsonContainerKey = EditorGUILayout.TextField(new GUIContent("Container Key", "PlayerPrefs key that stores your save JSON."), _settings.jsonContainerKey ?? string.Empty);
            _settings.externalJsonPath = EditorGUILayout.TextField(new GUIContent("JSON File / Folder", "Optional custom file or folder path to scan for .json saves."), _settings.externalJsonPath ?? string.Empty);
            _settings.autoScanPersistentDataPath = EditorGUILayout.ToggleLeft("Auto scan Application.persistentDataPath-equivalent locations", _settings.autoScanPersistentDataPath);
            _settings.autoParseJsonStringsInPlayerPrefs = EditorGUILayout.ToggleLeft("Auto parse PlayerPrefs strings that look like save containers", _settings.autoParseJsonStringsInPlayerPrefs);
            _settings.autoLoadExternalJsonFiles = EditorGUILayout.ToggleLeft("Auto load external .json save files", _settings.autoLoadExternalJsonFiles);
            _settings.showReadOnlyEntries = EditorGUILayout.ToggleLeft("Show external file entries", _settings.showReadOnlyEntries);
            _settings.hideUnitySystemEntries = EditorGUILayout.ToggleLeft("Hide Unity/editor system keys", _settings.hideUnitySystemEntries);

            if (position.width < NarrowLayoutBreakpoint)
            {
                if (GUILayout.Button("Browse Path"))
                {
                    string folder = EditorUtility.OpenFolderPanel("Pick save folder", string.IsNullOrWhiteSpace(_settings.externalJsonPath) ? Application.dataPath : _settings.externalJsonPath, string.Empty);
                    if (!string.IsNullOrWhiteSpace(folder))
                        _settings.externalJsonPath = folder;
                }

                if (GUILayout.Button("Auto Detect Container Key"))
                    AutoDetectContainerKey();

                if (GUILayout.Button("Refresh Sources", GUILayout.Height(26f)))
                    FullRefresh();

                if (GUILayout.Button("Open Persistent Path Hints", GUILayout.Height(26f)))
                    RevealSavePathHints();
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Browse Path"))
                    {
                        string folder = EditorUtility.OpenFolderPanel("Pick save folder", string.IsNullOrWhiteSpace(_settings.externalJsonPath) ? Application.dataPath : _settings.externalJsonPath, string.Empty);
                        if (!string.IsNullOrWhiteSpace(folder))
                            _settings.externalJsonPath = folder;
                    }

                    if (GUILayout.Button("Auto Detect Container Key"))
                        AutoDetectContainerKey();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Refresh Sources", GUILayout.Height(26f)))
                        FullRefresh();

                    if (GUILayout.Button("Open Persistent Path Hints", GUILayout.Height(26f)))
                        RevealSavePathHints();
                }
            }
        }
    }

    private void DrawEntryList()
    {
        using (new EditorGUILayout.VerticalScope(CardStyle(), GUILayout.ExpandHeight(true)))
        {
            GUILayout.Label("Visible Entries", SectionTitleStyle());
            GUILayout.Space(2f);

            List<EntryData> list = GetFilteredEntriesForList();
            if (list.Count == 0 && position.width >= NarrowLayoutBreakpoint)
            {
                DrawKeysDeckEmptyState();
                GUILayout.Space(6f);
            }

            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.ExpandHeight(true));

            if (list.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No entries found.\n\nTry these:\n1. Press Discover\n2. Set your Container Key\n3. Enable external JSON loading\n4. Press Refresh Sources",
                    MessageType.Info);
            }
            else
            {
                foreach (EntryData entry in list)
                    DrawEntryRow(entry);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawKeysDeckEmptyState()
    {
        GUIStyle artStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            richText = false,
            fontSize = 10,
            alignment = TextAnchor.UpperLeft
        };
        artStyle.font = EventHorizonEditorFont.CascadiaMono;
        artStyle.clipping = TextClipping.Overflow;
        artStyle.normal.textColor = new Color(TextColor.r, TextColor.g, TextColor.b, 0.7f);

        DrawAsciiBlock(KeysDeckArt, artStyle);
    }

    private List<EntryData> GetFilteredEntriesForList()
    {
        IEnumerable<EntryData> filtered = _entries;

        if (!_settings.showReadOnlyEntries)
            filtered = filtered.Where(e => !e.isReadOnly);

        if (_settings.hideUnitySystemEntries)
            filtered = filtered.Where(e => !IsUnitySystemEntry(e));

        if (_showOnlyFavorites)
            filtered = filtered.Where(e => e.favorite);

        if (!string.IsNullOrWhiteSpace(_search))
        {
            string needle = _search.Trim();
            filtered = filtered.Where(e =>
                (e.key ?? string.Empty).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (e.note ?? string.Empty).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (e.DisplayValue ?? string.Empty).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (e.sourceName ?? string.Empty).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (e.parentContainerKey ?? string.Empty).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (e.sourceFilePath ?? string.Empty).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (e.rawJsonValue ?? string.Empty).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        return filtered
            .OrderByDescending(e => e.favorite)
            .ThenBy(e => e.sourceType)
            .ThenBy(e => e.key, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void DrawEntryRow(EntryData entry)
    {
        bool selected = string.Equals(_selectedEntryId, BuildEntryId(entry), StringComparison.Ordinal);
        Rect rowRect = EditorGUILayout.GetControlRect(false, 56f);
        GUI.Box(rowRect, GUIContent.none, selected ? SelectedRowStyle() : RowStyle());

        Rect starRect = new Rect(rowRect.x + 8f, rowRect.y + 15f, 24f, 24f);
        Rect keyRect = new Rect(rowRect.x + 36f, rowRect.y + 6f, rowRect.width - 170f, 18f);
        Rect infoRect = new Rect(rowRect.x + 36f, rowRect.y + 25f, rowRect.width - 150f, 16f);
        Rect sourceRect = new Rect(rowRect.x + 36f, rowRect.y + 39f, rowRect.width - 150f, 14f);
        Rect typeRect = new Rect(rowRect.xMax - 96f, rowRect.y + 8f, 84f, 18f);
        Rect modeRect = new Rect(rowRect.xMax - 96f, rowRect.y + 29f, 84f, 18f);

        if (GUI.Button(starRect, entry.favorite ? "★" : "☆", FavoriteButtonStyle()))
        {
            entry.favorite = !entry.favorite;
            if (entry.favorite) _favorites.Add(entry.key); else _favorites.Remove(entry.key);
            PersistFavorites();
        }

        if (GUI.Button(new Rect(rowRect.x, rowRect.y, rowRect.width, rowRect.height), GUIContent.none, GUIStyle.none))
            SelectEntry(entry);

        GUI.Label(keyRect, entry.key, KeyLabelStyle());
        GUI.Label(infoRect, Truncate(entry.DisplayValue, 56), SecondaryLabelStyle());
        GUI.Label(sourceRect, BuildSourceLabel(entry), TinyLabelStyle());
        GUI.Label(typeRect, entry.type.ToString(), TypeTagStyle());
        GUI.Label(modeRect, entry.isReadOnly ? "ReadOnly" : "Writable", SourceTagStyle(entry));
    }

    private string BuildSourceLabel(EntryData entry)
    {
        return entry.sourceType switch
        {
            EntrySourceType.PlayerPrefs => $"Source: PlayerPrefs",
            EntrySourceType.PlayerPrefsJsonContainer => $"Source: Container [{entry.parentContainerKey}]",
            EntrySourceType.ExternalJsonFile => $"Source: File [{Path.GetFileName(entry.sourceFilePath ?? string.Empty)}]",
            _ => "Source: Unknown"
        };
    }

    private void DrawAddPanel()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.HelpBox("This section writes direct PlayerPrefs keys. EventHorizon save-slot children can be edited from the inspector when they come from a PlayerPrefs JSON container.", MessageType.None);
            _newEntry.key = EditorGUILayout.TextField("Key", _newEntry.key);
            _newEntry.type = (PrefValueType)EditorGUILayout.EnumPopup("Type", _newEntry.type);
            DrawValueEditor(_newEntry, false);
            _newEntry.note = EditorGUILayout.TextField("Note", _newEntry.note ?? string.Empty);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear", GUILayout.Width(90f)))
                    _newEntry = CreateNewEntry();

                if (GUILayout.Button("Save Key", GUILayout.Width(120f)))
                    SaveNewOrOverwrite(_newEntry);
            }
        }
    }

    private void DrawRightPanel(Rect rect)
    {
        GUILayout.BeginArea(rect);
        _detailsScroll = EditorGUILayout.BeginScrollView(_detailsScroll);

        EntryData live = GetSelectedEntry();
        if (live == null)
        {
            EditorGUILayout.HelpBox("Select a key from the left panel, or add a new one.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
            return;
        }

        using (new EditorGUILayout.VerticalScope(CardStyle()))
        {
            GUILayout.Label("Selected Entry", SectionTitleStyle());

            if (_editingCopy == null || _editingCopy.key != live.key)
                _editingCopy = live.Clone();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Key");
            GUI.enabled = false;
            EditorGUILayout.TextField(_editingCopy.key);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(live.parentContainerKey))
                EditorGUILayout.LabelField("Container", live.parentContainerKey);

            bool canEditValue = !live.isReadOnly;
            GUI.enabled = canEditValue;
            _editingCopy.type = (PrefValueType)EditorGUILayout.EnumPopup("Type", _editingCopy.type);
            DrawValueEditor(_editingCopy, true);
            GUI.enabled = true;

            if (!live.isReadOnly &&
                (live.sourceType == EntrySourceType.PlayerPrefsJsonContainer || live.sourceType == EntrySourceType.ExternalJsonFile) &&
                _editingCopy.type == PrefValueType.String &&
                LooksLikeJson(_editingCopy.rawJsonValue))
            {
                GUILayout.Space(4f);
                EditorGUILayout.LabelField("Raw JSON Override", EditorStyles.boldLabel);
                _editingCopy.rawJsonValue = EditorGUILayout.TextArea(_editingCopy.rawJsonValue ?? string.Empty, GUILayout.MinHeight(90f));
            }

            _editingCopy.note = EditorGUILayout.TextField("Note", _editingCopy.note ?? string.Empty);
            _editingCopy.favorite = EditorGUILayout.Toggle("Favorite", _editingCopy.favorite);

            if (live.isReadOnly)
            {
                EditorGUILayout.HelpBox("This entry is read-only because it came from an external JSON file.", MessageType.Info);
            }
            else if (live.sourceType == EntrySourceType.PlayerPrefsJsonContainer)
            {
                EditorGUILayout.HelpBox("This entry came from a JSON container stored inside PlayerPrefs. Use 'Apply To Container' to write it back into the parent container key.", MessageType.Info);
            }
            else if (live.sourceType == EntrySourceType.ExternalJsonFile)
            {
                EditorGUILayout.HelpBox("This entry came from an external JSON save file. Saving will write the updated value back into that file.", MessageType.Info);
            }

            GUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Revert", GUILayout.Height(28f)))
                    _editingCopy = live.Clone();

                if (GUILayout.Button("Duplicate", GUILayout.Height(28f)))
                    DuplicateEntry(live);

                GUI.enabled = !live.isReadOnly;
                if (live.sourceType == EntrySourceType.PlayerPrefs)
                {
                    if (GUILayout.Button("Save Changes", GUILayout.Height(28f)))
                        SaveEditedEntry(live, _editingCopy);
                }
                else if (live.sourceType == EntrySourceType.PlayerPrefsJsonContainer)
                {
                    if (GUILayout.Button("Apply To Container", GUILayout.Height(28f)))
                        SaveEditedContainerEntry(live, _editingCopy);
                }
                else if (live.sourceType == EntrySourceType.ExternalJsonFile)
                {
                    if (GUILayout.Button("Save To File", GUILayout.Height(28f)))
                        SaveEditedExternalFileEntry(live, _editingCopy);
                }
                else
                {
                    GUILayout.Button("Read Only", GUILayout.Height(28f));
                }
                GUI.enabled = true;
            }
        }

        if (CanShowJsonTable(live))
        {
            GUILayout.Space(8f);
            DrawJsonContainerTable(live);
        }

        GUILayout.Space(8f);

        using (new EditorGUILayout.VerticalScope(CardStyle()))
        {
            GUILayout.Label("Utilities", SectionTitleStyle());
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy Key"))
                    EditorGUIUtility.systemCopyBuffer = live.key;

                if (GUILayout.Button("Copy Value"))
                    EditorGUIUtility.systemCopyBuffer = live.DisplayValue;

                if (GUILayout.Button("Ping Save Location"))
                    RevealStorageLocation();
            }

            if (!string.IsNullOrEmpty(live.rawJsonValue))
            {
                GUILayout.Space(4f);
                EditorGUILayout.LabelField("Raw JSON", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(live.rawJsonValue, GUILayout.MinHeight(70f));
            }

            if (_showAdvanced)
            {
                GUILayout.Space(4f);
                EditorGUILayout.LabelField("Last touched", new DateTime(live.lastTouchedTicks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                EditorGUILayout.LabelField("Value preview", live.DisplayValue);
                EditorGUILayout.LabelField("Type-safe getter", GetGetterPreview(live));
            }
        }

        GUILayout.Space(8f);

        _showDangerZone = EditorGUILayout.BeginFoldoutHeaderGroup(_showDangerZone, "Danger Zone");
        if (_showDangerZone)
        {
            using (new EditorGUILayout.VerticalScope(CardStyle()))
            {
                EditorGUILayout.HelpBox("These actions cannot be undone unless you exported a snapshot first.", MessageType.Warning);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = !live.isReadOnly;
                    if (GUILayout.Button(GetDeleteButtonLabel(live), GUILayout.Height(28f)))
                    {
                        if (live.sourceType == EntrySourceType.PlayerPrefsJsonContainer)
                            DeleteContainerEntry(live);
                        else if (live.sourceType == EntrySourceType.ExternalJsonFile)
                            DeleteExternalFileEntry(live);
                        else
                            DeleteEntry(live.key);
                    }
                    GUI.enabled = true;

                    if (GUILayout.Button("Delete Missing Keys From Catalog", GUILayout.Height(28f)))
                        CleanupCatalogAgainstPrefs();
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawJsonContainerTable(EntryData live)
    {
        EnsureJsonTableState(live);

        using (new EditorGUILayout.VerticalScope(CardStyle()))
        {
            GUILayout.Label("JSON Table Editor", SectionTitleStyle());
            EditorGUILayout.HelpBox("This view turns the selected JSON save key into editable rows. Saving this table writes the JSON back into the selected PlayerPrefs key.", MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Row", GUILayout.Height(24f)))
                {
                    _jsonTableEntries.Add(CreateNewTableEntry());
                }

                if (GUILayout.Button("Reload From JSON", GUILayout.Height(24f)))
                {
                    LoadJsonTableState(live);
                }

                if (GUILayout.Button("Save Table To Key", GUILayout.Height(24f)))
                {
                    SaveJsonTableToPlayerPrefs(live);
                }
            }

            GUILayout.Space(6f);
            _jsonTableScroll = EditorGUILayout.BeginScrollView(_jsonTableScroll, GUILayout.MinHeight(180f), GUILayout.MaxHeight(360f));
            for (int i = 0; i < _jsonTableEntries.Count; i++)
            {
                DrawJsonTableRow(_jsonTableEntries[i], i);
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawValueEditor(EntryData entry, bool editingExisting)
    {
        switch (entry.type)
        {
            case PrefValueType.String:
                entry.stringValue = EditorGUILayout.TextField("Value", entry.stringValue ?? string.Empty);
                break;
            case PrefValueType.Int:
                entry.intValue = EditorGUILayout.IntField("Value", entry.intValue);
                break;
            case PrefValueType.Float:
                entry.floatValue = EditorGUILayout.FloatField("Value", entry.floatValue);
                break;
            case PrefValueType.Bool:
                entry.boolValue = EditorGUILayout.Toggle("Value", entry.boolValue);
                break;
        }

        if (_showAdvanced && editingExisting)
        {
            switch (entry.type)
            {
                case PrefValueType.String:
                    EditorGUILayout.LabelField("Length", (entry.stringValue ?? string.Empty).Length.ToString());
                    break;
                case PrefValueType.Int:
                    EditorGUILayout.LabelField("Hex", $"0x{entry.intValue:X}");
                    break;
                case PrefValueType.Float:
                    EditorGUILayout.LabelField("Raw", entry.floatValue.ToString("R", CultureInfo.InvariantCulture));
                    break;
                case PrefValueType.Bool:
                    EditorGUILayout.LabelField("Raw", entry.boolValue ? "true" : "false");
                    break;
            }
        }
    }

    private bool CanShowJsonTable(EntryData entry)
    {
        return entry != null
               && entry.sourceType == EntrySourceType.PlayerPrefs
               && entry.type == PrefValueType.String
               && LooksLikeSaveContainerJson(entry.stringValue);
    }

    private void EnsureJsonTableState(EntryData live)
    {
        string entryId = BuildEntryId(live);
        if (_jsonTableHostEntryId == entryId)
            return;

        LoadJsonTableState(live);
    }

    private void LoadJsonTableState(EntryData live)
    {
        _jsonTableHostEntryId = BuildEntryId(live);
        _jsonTableEntries.Clear();

        if (live == null || string.IsNullOrWhiteSpace(live.stringValue))
            return;

        SaveContainerJson container;
        try
        {
            container = JsonUtility.FromJson<SaveContainerJson>(live.stringValue);
        }
        catch
        {
            return;
        }

        if (container?.Entries == null)
            return;

        for (int i = 0; i < container.Entries.Count; i++)
        {
            SaveContainerEntryJson item = container.Entries[i];
            if (item == null || string.IsNullOrWhiteSpace(item.Key))
                continue;

            EntryData entry = BuildEntryFromContainerItem(item, EntrySourceType.PlayerPrefsJsonContainer, live.key, live.key, null, false);
            entry.sourceType = EntrySourceType.PlayerPrefsJsonContainer;
            entry.sourceName = live.key;
            entry.parentContainerKey = live.key;
            entry.isReadOnly = false;
            _jsonTableEntries.Add(entry);
        }
    }

    private void DrawJsonTableRow(EntryData entry, int index)
    {
        using (new EditorGUILayout.VerticalScope(RowStyle()))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                entry.key = EditorGUILayout.TextField("Key", entry.key ?? string.Empty);
                entry.type = (PrefValueType)EditorGUILayout.EnumPopup("Type", entry.type, GUILayout.Width(150f));
            }

            switch (entry.type)
            {
                case PrefValueType.String:
                    entry.stringValue = EditorGUILayout.TextField("Value", entry.stringValue ?? string.Empty);
                    if (LooksLikeJson(entry.rawJsonValue))
                    {
                        entry.rawJsonValue = EditorGUILayout.TextArea(entry.rawJsonValue ?? string.Empty, GUILayout.MinHeight(54f));
                    }
                    else
                    {
                        entry.rawJsonValue = null;
                    }
                    break;
                case PrefValueType.Int:
                    entry.intValue = EditorGUILayout.IntField("Value", entry.intValue);
                    break;
                case PrefValueType.Float:
                    entry.floatValue = EditorGUILayout.FloatField("Value", entry.floatValue);
                    break;
                case PrefValueType.Bool:
                    entry.boolValue = EditorGUILayout.Toggle("Value", entry.boolValue);
                    break;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove", GUILayout.Width(90f)))
                {
                    _jsonTableEntries.RemoveAt(index);
                    GUIUtility.ExitGUI();
                }
            }
        }
    }

    private void SaveJsonTableToPlayerPrefs(EntryData live)
    {
        if (live == null)
            return;

        SaveContainerJson container = new SaveContainerJson
        {
            Timestamp = DateTime.UtcNow.ToString("O"),
            Entries = _jsonTableEntries
                .Where(e => !string.IsNullOrWhiteSpace(e.key))
                .Select(e => new SaveContainerEntryJson
                {
                    Key = e.key,
                    Value = BuildRawValueForContainer(e)
                })
                .ToList()
        };

        string json = JsonUtility.ToJson(container, true);
        PlayerPrefs.SetString(live.key, json);
        PlayerPrefs.Save();

        live.stringValue = json;
        live.rawJsonValue = json;
        live.lastTouchedTicks = DateTime.UtcNow.Ticks;
        FullRefresh();
    }

    private static EntryData CreateNewTableEntry()
    {
        return new EntryData
        {
            key = "New Key",
            type = PrefValueType.String,
            stringValue = string.Empty,
            intValue = 0,
            floatValue = 0f,
            boolValue = false,
            favorite = false,
            note = string.Empty,
            sourceType = EntrySourceType.PlayerPrefsJsonContainer,
            sourceName = "JSON Table",
            isReadOnly = false
        };
    }

    private void StatPill(string label)
    {
        GUILayout.Label(label, StatPillStyle(), GUILayout.Height(22f));
    }

    private void DrawSplitter(Rect rect)
    {
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        EditorGUI.DrawRect(rect, AccentGold);

        Event e = Event.current;
        switch (e.type)
        {
            case EventType.MouseDown:
                if (rect.Contains(e.mousePosition))
                {
                    _resizing = true;
                    e.Use();
                }
                break;
            case EventType.MouseDrag:
                if (_resizing)
                {
                    _leftPanelWidth = Mathf.Clamp(e.mousePosition.x, MinLeftPanelWidth, Mathf.Min(MaxLeftPanelWidth, position.width - 340f));
                    Repaint();
                    e.Use();
                }
                break;
            case EventType.MouseUp:
                if (_resizing)
                {
                    _resizing = false;
                    e.Use();
                }
                break;
        }
    }

    private void FullRefresh()
    {
        LoadCatalog();
        SyncFavoritesIntoEntries();
#if UNITY_EDITOR_WIN
        TryDiscoverFromWindowsRegistry();
#endif
        RefreshValuesFromPlayerPrefs();
        ParseConfiguredAndDetectedJsonSources();
        if (!string.IsNullOrEmpty(_selectedEntryId) && GetSelectedEntry() == null)
            _selectedEntryId = _entries.Count > 0 ? BuildEntryId(_entries[0]) : string.Empty;

        if (string.IsNullOrEmpty(_selectedEntryId))
        {
            _editingCopy = null;
            _jsonTableHostEntryId = string.Empty;
            _jsonTableEntries.Clear();
        }

        PersistCatalog();
        RebuildEditingCopy();
        Repaint();
    }

    private void ParseConfiguredAndDetectedJsonSources()
    {
        RemoveGeneratedJsonEntries();

        string resolvedContainerKey = ResolveConfiguredOrCommonContainerKey();
        if (!string.IsNullOrWhiteSpace(resolvedContainerKey))
        {
            string json = PlayerPrefs.GetString(resolvedContainerKey, string.Empty);
            AddEntriesFromSaveContainerJson(json, EntrySourceType.PlayerPrefsJsonContainer, resolvedContainerKey, null, false);
        }

        if (_settings.autoParseJsonStringsInPlayerPrefs)
        {
            foreach (EntryData entry in _entries.Where(e => e.sourceType == EntrySourceType.PlayerPrefs).ToList())
            {
                if (entry.type != PrefValueType.String)
                    continue;

                if (string.IsNullOrWhiteSpace(entry.stringValue))
                    continue;

                if (LooksLikeSaveContainerJson(entry.stringValue))
                    AddEntriesFromSaveContainerJson(entry.stringValue, EntrySourceType.PlayerPrefsJsonContainer, entry.key, null, false);
            }
        }

        if (_settings.autoLoadExternalJsonFiles)
        {
            foreach (string filePath in GetCandidateJsonFiles())
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    AddEntriesFromSaveContainerJson(json, EntrySourceType.ExternalJsonFile, Path.GetFileName(filePath), filePath, false);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }

    private void RemoveGeneratedJsonEntries()
    {
        _entries.RemoveAll(e => e.sourceType == EntrySourceType.PlayerPrefsJsonContainer || e.sourceType == EntrySourceType.ExternalJsonFile);
        _containerChildrenByContainerKey.Clear();
        _fileChildrenByFilePath.Clear();
    }

    private void AddEntriesFromSaveContainerJson(string json, EntrySourceType sourceType, string sourceNameOrContainerKey, string sourceFilePath, bool readOnly)
    {
        if (string.IsNullOrWhiteSpace(json) || !LooksLikeJson(json))
            return;

        SaveContainerJson container;
        try
        {
            container = JsonUtility.FromJson<SaveContainerJson>(json);
        }
        catch
        {
            return;
        }

        if (container?.Entries == null || container.Entries.Count == 0)
            return;

        string parentKey = sourceType == EntrySourceType.PlayerPrefsJsonContainer ? sourceNameOrContainerKey : null;

        foreach (SaveContainerEntryJson item in container.Entries)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Key))
                continue;

            EntryData entry = BuildEntryFromContainerItem(item, sourceType, sourceNameOrContainerKey, parentKey, sourceFilePath, readOnly);
            _entries.Add(entry);

            if (sourceType == EntrySourceType.PlayerPrefsJsonContainer)
            {
                if (!_containerChildrenByContainerKey.TryGetValue(sourceNameOrContainerKey, out List<EntryData> children))
                {
                    children = new List<EntryData>();
                    _containerChildrenByContainerKey[sourceNameOrContainerKey] = children;
                }
                children.Add(entry);
            }
            else if (sourceType == EntrySourceType.ExternalJsonFile)
            {
                if (!_fileChildrenByFilePath.TryGetValue(sourceFilePath, out List<EntryData> children))
                {
                    children = new List<EntryData>();
                    _fileChildrenByFilePath[sourceFilePath] = children;
                }
                children.Add(entry);
            }
        }
    }

    private EntryData BuildEntryFromContainerItem(SaveContainerEntryJson item, EntrySourceType sourceType, string sourceNameOrContainerKey, string parentContainerKey, string sourceFilePath, bool readOnly)
    {
        EntryData entry = new EntryData
        {
            key = item.Key,
            type = PrefValueType.String,
            stringValue = item.Value ?? string.Empty,
            favorite = _favorites.Contains(item.Key),
            lastTouchedTicks = DateTime.UtcNow.Ticks,
            sourceType = sourceType,
            sourceName = sourceNameOrContainerKey,
            parentContainerKey = parentContainerKey,
            sourceFilePath = sourceFilePath,
            isReadOnly = readOnly,
            isExpandedFromContainer = true,
            rawJsonValue = item.Value
        };

        string raw = item.Value ?? string.Empty;
        if (TryParseSimpleIntWrapper(raw, out int intValue))
        {
            entry.type = PrefValueType.Int;
            entry.intValue = intValue;
            entry.stringValue = raw;
            return entry;
        }

        if (TryParseSimpleFloatWrapper(raw, out float wrappedFloatValue))
        {
            entry.type = PrefValueType.Float;
            entry.floatValue = wrappedFloatValue;
            entry.stringValue = raw;
            return entry;
        }

        if (TryParseSimpleBoolWrapper(raw, out bool boolValue))
        {
            entry.type = PrefValueType.Bool;
            entry.boolValue = boolValue;
            entry.stringValue = raw;
            return entry;
        }

        if (TryParseSimpleStringWrapper(raw, out string wrappedStringValue))
        {
            entry.type = PrefValueType.String;
            entry.stringValue = wrappedStringValue ?? string.Empty;
            return entry;
        }

        if (TryParseSimpleFloat(raw, out float floatValue))
        {
            entry.type = PrefValueType.Float;
            entry.floatValue = floatValue;
            entry.stringValue = raw;
            return entry;
        }

        if (TryParseCurrencyWallet(raw, out string walletSummary))
        {
            entry.type = PrefValueType.String;
            entry.stringValue = walletSummary;
            return entry;
        }

        entry.type = PrefValueType.String;
        entry.stringValue = raw;
        return entry;
    }

    private bool TryParseSimpleIntWrapper(string json, out int value)
    {
        value = default;
        if (!LooksLikeJson(json))
            return false;

        try
        {
            SimpleIntValueJson wrapper = JsonUtility.FromJson<SimpleIntValueJson>(json);
            if (wrapper != null && json.IndexOf("\"Value\"", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                value = wrapper.Value;
                return true;
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }

    private bool TryParseSimpleFloatWrapper(string json, out float value)
    {
        value = default;
        if (!LooksLikeJson(json))
            return false;

        try
        {
            SimpleFloatValueJson wrapper = JsonUtility.FromJson<SimpleFloatValueJson>(json);
            if (wrapper != null && json.IndexOf("\"Value\"", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                value = wrapper.Value;
                return true;
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }

    private bool TryParseSimpleBoolWrapper(string json, out bool value)
    {
        value = default;
        if (!LooksLikeJson(json))
            return false;

        try
        {
            SimpleBoolValueJson wrapper = JsonUtility.FromJson<SimpleBoolValueJson>(json);
            if (wrapper != null && json.IndexOf("\"Value\"", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                value = wrapper.Value;
                return true;
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }

    private bool TryParseSimpleStringWrapper(string json, out string value)
    {
        value = null;
        if (!LooksLikeJson(json))
            return false;

        try
        {
            SimpleStringValueJson wrapper = JsonUtility.FromJson<SimpleStringValueJson>(json);
            if (wrapper != null && json.IndexOf("\"Value\"", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                value = wrapper.Value;
                return true;
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }

    private bool TryParseSimpleFloat(string raw, out float value)
    {
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private bool TryParseCurrencyWallet(string json, out string summary)
    {
        summary = null;
        if (!LooksLikeJson(json))
            return false;

        try
        {
            CurrencyWalletJson wallet = JsonUtility.FromJson<CurrencyWalletJson>(json);
            if (wallet?.Entries == null || wallet.Entries.Count == 0)
                return false;

            summary = string.Join(", ", wallet.Entries.Select(e => $"{e.CurrencyID}: {e.Balance}"));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool LooksLikeSaveContainerJson(string value)
    {
        return LooksLikeJson(value) && value.IndexOf("\"Entries\"", StringComparison.OrdinalIgnoreCase) >= 0 && value.IndexOf("\"Key\"", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool LooksLikeJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string trimmed = value.Trim();
        return (trimmed.StartsWith("{") && trimmed.EndsWith("}")) || (trimmed.StartsWith("[") && trimmed.EndsWith("]"));
    }

    private void AutoDetectContainerKey()
    {
        EntryData detected = _entries
            .Where(e => e.sourceType == EntrySourceType.PlayerPrefs && e.type == PrefValueType.String && LooksLikeSaveContainerJson(e.stringValue))
            .OrderByDescending(e => e.lastTouchedTicks)
            .FirstOrDefault();

        if (detected == null)
        {
            EditorUtility.DisplayDialog("Auto detect", "No PlayerPrefs string key that looks like your save container was found in the current catalog.\n\nPress Discover first or manually type the key name.", "OK");
            return;
        }

        _settings.jsonContainerKey = detected.key;
        PersistSettings();
        FullRefresh();
    }

    private static string ResolveExistingPlayerPrefsKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        if (PlayerPrefs.HasKey(key))
            return key;

        for (int i = 0; i < KnownPlayerPrefsPrefixes.Length; i++)
        {
            string candidate = KnownPlayerPrefsPrefixes[i] + key;
            if (PlayerPrefs.HasKey(candidate))
                return candidate;
        }

        return null;
    }

    private string ResolveConfiguredOrCommonContainerKey()
    {
        string resolved = ResolveExistingPlayerPrefsKey(_settings.jsonContainerKey);
        if (!string.IsNullOrWhiteSpace(resolved))
            return resolved;

        for (int i = 0; i < CommonContainerKeys.Length; i++)
        {
            resolved = ResolveExistingPlayerPrefsKey(CommonContainerKeys[i]);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                _settings.jsonContainerKey = CommonContainerKeys[i];
                PersistSettings();
                return resolved;
            }
        }

        EntryData detected = _entries
            .Where(e => e.sourceType == EntrySourceType.PlayerPrefs && e.type == PrefValueType.String && LooksLikeSaveContainerJson(e.stringValue))
            .OrderByDescending(e => e.lastTouchedTicks)
            .FirstOrDefault();

        if (detected == null)
            return null;

        _settings.jsonContainerKey = detected.key;
        PersistSettings();
        return ResolveExistingPlayerPrefsKey(detected.key);
    }

    private static bool IsUnitySystemEntry(EntryData entry)
    {
        if (entry == null || entry.sourceType != EntrySourceType.PlayerPrefs)
            return false;

        string key = entry.key ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (KnownPlayerPrefsPrefixes.Any(prefix => !string.IsNullOrEmpty(prefix) && key.StartsWith(prefix, StringComparison.Ordinal)))
            return false;

        string[] hiddenPrefixes =
        {
            "unity.", "Unity", "Screenmanager", "DeckBase", "SceneView", "GameView", "Inspector",
            "Profiler", "RecentlyUsed", "LastSceneManagerSetup", "Search.", "ShaderGraph", "BuildProfile"
        };

        for (int i = 0; i < hiddenPrefixes.Length; i++)
        {
            if (key.StartsWith(hiddenPrefixes[i], StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static SaveContainerJson LoadSaveContainerFromFile(string filePath)
    {
        try
        {
            return JsonUtility.FromJson<SaveContainerJson>(File.ReadAllText(filePath));
        }
        catch
        {
            return null;
        }
    }

    private static string GetDeleteButtonLabel(EntryData live)
    {
        return live.sourceType switch
        {
            EntrySourceType.PlayerPrefsJsonContainer => "Delete From Container",
            EntrySourceType.ExternalJsonFile => "Delete From File",
            _ => "Delete Selected"
        };
    }

    private void SaveNewOrOverwrite(EntryData source)
    {
        if (string.IsNullOrWhiteSpace(source.key))
        {
            EditorUtility.DisplayDialog("Invalid key", "Please enter a key name.", "OK");
            return;
        }

        EntryData existing = _entries.FirstOrDefault(e => e.key == source.key && e.sourceType == EntrySourceType.PlayerPrefs);
        if (existing != null)
        {
            bool overwrite = EditorUtility.DisplayDialog("Overwrite existing key?",
                $"A PlayerPrefs key named '{source.key}' already exists in the catalog. Overwrite it?", "Overwrite", "Cancel");
            if (!overwrite)
                return;
        }

        EntryData write = source.Clone();
        write.sourceType = EntrySourceType.PlayerPrefs;
        write.sourceName = "PlayerPrefs";
        write.isReadOnly = false;
        WriteToPlayerPrefs(write);
        UpsertEntry(write);
            SelectEntry(write);
        _newEntry = CreateNewEntry();
        FullRefresh();
    }

    private void SaveEditedEntry(EntryData live, EntryData edited)
    {
        if (live == null || edited == null)
            return;

        live.type = edited.type;
        live.stringValue = edited.stringValue;
        live.intValue = edited.intValue;
        live.floatValue = edited.floatValue;
        live.boolValue = edited.boolValue;
        live.note = edited.note;
        live.favorite = edited.favorite;
        live.lastTouchedTicks = DateTime.UtcNow.Ticks;

        if (live.favorite) _favorites.Add(live.key); else _favorites.Remove(live.key);

        WriteToPlayerPrefs(live);
        PersistCatalog();
        PersistFavorites();
        RebuildEditingCopy();
        FullRefresh();
    }

    private void SaveEditedContainerEntry(EntryData live, EntryData edited)
    {
        if (live == null || edited == null || string.IsNullOrWhiteSpace(live.parentContainerKey))
            return;

        if (!PlayerPrefs.HasKey(live.parentContainerKey))
        {
            EditorUtility.DisplayDialog("Container missing", $"The parent container key '{live.parentContainerKey}' was not found in PlayerPrefs.", "OK");
            return;
        }

        string json = PlayerPrefs.GetString(live.parentContainerKey, string.Empty);
        SaveContainerJson container;
        try
        {
            container = JsonUtility.FromJson<SaveContainerJson>(json);
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Invalid container JSON", ex.Message, "OK");
            return;
        }

        if (container?.Entries == null)
        {
            EditorUtility.DisplayDialog("Invalid container", "The parent container has no Entries array.", "OK");
            return;
        }

        SaveContainerEntryJson target = container.Entries.FirstOrDefault(x => x != null && x.Key == live.key);
        if (target == null)
        {
            EditorUtility.DisplayDialog("Entry missing", $"Could not find child entry '{live.key}' inside '{live.parentContainerKey}'.", "OK");
            return;
        }

        target.Value = BuildRawValueForContainer(edited);
        PlayerPrefs.SetString(live.parentContainerKey, JsonUtility.ToJson(container, true));
        PlayerPrefs.Save();

        live.type = edited.type;
        live.intValue = edited.intValue;
        live.floatValue = edited.floatValue;
        live.boolValue = edited.boolValue;
        live.stringValue = edited.stringValue;
        live.rawJsonValue = target.Value;
        live.note = edited.note;
        live.favorite = edited.favorite;
        live.lastTouchedTicks = DateTime.UtcNow.Ticks;

        PersistFavorites();
        FullRefresh();
    }

    private void SaveEditedExternalFileEntry(EntryData live, EntryData edited)
    {
        if (live == null || edited == null || string.IsNullOrWhiteSpace(live.sourceFilePath) || !File.Exists(live.sourceFilePath))
        {
            EditorUtility.DisplayDialog("File missing", "The source JSON file could not be found.", "OK");
            return;
        }

        SaveContainerJson container = LoadSaveContainerFromFile(live.sourceFilePath);
        if (container?.Entries == null)
        {
            EditorUtility.DisplayDialog("Invalid file", "The source file does not contain a valid save container.", "OK");
            return;
        }

        SaveContainerEntryJson target = container.Entries.FirstOrDefault(x => x != null && x.Key == live.key);
        if (target == null)
        {
            EditorUtility.DisplayDialog("Entry missing", $"Could not find child entry '{live.key}' inside '{live.sourceFilePath}'.", "OK");
            return;
        }

        target.Value = BuildRawValueForContainer(edited);
        container.Timestamp = DateTime.UtcNow.ToString("O");
        File.WriteAllText(live.sourceFilePath, JsonUtility.ToJson(container, true));

        live.type = edited.type;
        live.intValue = edited.intValue;
        live.floatValue = edited.floatValue;
        live.boolValue = edited.boolValue;
        live.stringValue = edited.stringValue;
        live.rawJsonValue = target.Value;
        live.note = edited.note;
        live.favorite = edited.favorite;
        live.lastTouchedTicks = DateTime.UtcNow.Ticks;

        PersistFavorites();
        FullRefresh();
    }

    private string BuildRawValueForContainer(EntryData entry)
    {
        switch (entry.type)
        {
            case PrefValueType.Int:
                return JsonUtility.ToJson(new SimpleIntValueJson { Value = entry.intValue });
            case PrefValueType.Float:
                return JsonUtility.ToJson(new SimpleFloatValueJson { Value = entry.floatValue });
            case PrefValueType.Bool:
                return JsonUtility.ToJson(new SimpleBoolValueJson { Value = entry.boolValue });
            case PrefValueType.String:
            default:
                if (!string.IsNullOrWhiteSpace(entry.rawJsonValue) && LooksLikeJson(entry.rawJsonValue))
                    return entry.rawJsonValue;

                return JsonUtility.ToJson(new SimpleStringValueJson { Value = entry.stringValue ?? string.Empty });
        }
    }

    private void DuplicateEntry(EntryData source)
    {
        string duplicatedKey = source.key + "_Copy";
        int suffix = 1;
        while (_entries.Any(e => e.key == duplicatedKey && e.sourceType == EntrySourceType.PlayerPrefs))
        {
            duplicatedKey = source.key + "_Copy" + suffix;
            suffix++;
        }

        EntryData copy = source.Clone();
        copy.key = duplicatedKey;
        copy.favorite = false;
        copy.lastTouchedTicks = DateTime.UtcNow.Ticks;
        copy.sourceType = EntrySourceType.PlayerPrefs;
        copy.sourceName = "PlayerPrefs";
        copy.parentContainerKey = null;
        copy.sourceFilePath = null;
        copy.isReadOnly = false;
        copy.isExpandedFromContainer = false;

        WriteToPlayerPrefs(copy);
        UpsertEntry(copy);
        SelectEntry(copy);
        FullRefresh();
    }

    private void DeleteContainerEntry(EntryData live)
    {
        if (live == null || string.IsNullOrWhiteSpace(live.parentContainerKey))
            return;

        if (!EditorUtility.DisplayDialog("Delete from container?", $"Delete '{live.key}' from container '{live.parentContainerKey}'?", "Delete", "Cancel"))
            return;

        string json = PlayerPrefs.GetString(live.parentContainerKey, string.Empty);
        SaveContainerJson container;
        try
        {
            container = JsonUtility.FromJson<SaveContainerJson>(json);
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Invalid container JSON", ex.Message, "OK");
            return;
        }

        if (container?.Entries == null)
            return;

        container.Entries.RemoveAll(x => x != null && x.Key == live.key);
        PlayerPrefs.SetString(live.parentContainerKey, JsonUtility.ToJson(container, true));
        PlayerPrefs.Save();

        _selectedEntryId = string.Empty;
        FullRefresh();
    }

    private void DeleteExternalFileEntry(EntryData live)
    {
        if (live == null || string.IsNullOrWhiteSpace(live.sourceFilePath) || !File.Exists(live.sourceFilePath))
            return;

        if (!EditorUtility.DisplayDialog("Delete from file?", $"Delete '{live.key}' from file '{Path.GetFileName(live.sourceFilePath)}'?", "Delete", "Cancel"))
            return;

        SaveContainerJson container = LoadSaveContainerFromFile(live.sourceFilePath);
        if (container?.Entries == null)
            return;

        container.Entries.RemoveAll(x => x != null && x.Key == live.key);
        container.Timestamp = DateTime.UtcNow.ToString("O");
        File.WriteAllText(live.sourceFilePath, JsonUtility.ToJson(container, true));

        _selectedEntryId = string.Empty;
        FullRefresh();
    }

    private void DeleteEntry(string key)
    {
        if (!EditorUtility.DisplayDialog("Delete key?", $"Delete PlayerPrefs key '{key}'?", "Delete", "Cancel"))
            return;

        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();

        int index = _entries.FindIndex(e => e.key == key && e.sourceType == EntrySourceType.PlayerPrefs);
        if (index >= 0)
            _entries.RemoveAt(index);

        _favorites.Remove(key);
        PersistCatalog();
        PersistFavorites();

        _selectedEntryId = _entries.Count > 0 ? BuildEntryId(_entries[0]) : string.Empty;
        RebuildEditingCopy();
        FullRefresh();
    }

    private void DeleteAllWithConfirmation()
    {
        if (!EditorUtility.DisplayDialog("Delete all PlayerPrefs?",
                "This will call PlayerPrefs.DeleteAll() and clear the local PlayerPrefs catalog for this project. Continue?",
                "Delete All", "Cancel"))
            return;

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        _entries.Clear();
        _favorites.Clear();
        _selectedEntryId = string.Empty;
        _editingCopy = null;
        PersistCatalog();
        PersistFavorites();
        FullRefresh();
    }

    private void CleanupCatalogAgainstPrefs()
    {
        List<EntryData> missing = new();
        foreach (EntryData entry in _entries)
        {
            if (entry.sourceType != EntrySourceType.PlayerPrefs)
                continue;

            if (!HasKey(entry))
                missing.Add(entry);
        }

        if (missing.Count == 0)
        {
            EditorUtility.DisplayDialog("Cleanup", "No missing PlayerPrefs keys were found.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Remove missing keys?", $"Remove {missing.Count} missing PlayerPrefs keys from the catalog?", "Remove", "Cancel"))
            return;

        foreach (EntryData item in missing)
        {
            _entries.Remove(item);
            _favorites.Remove(item.key);
        }

        PersistCatalog();
        PersistFavorites();
        RebuildEditingCopy();
        FullRefresh();
    }

    private bool HasKey(EntryData entry)
    {
        return entry.sourceType == EntrySourceType.PlayerPrefs && PlayerPrefs.HasKey(entry.key);
    }

    private void RefreshValuesFromPlayerPrefs()
    {
        List<EntryData> missing = new();

        foreach (EntryData entry in _entries.Where(e => e.sourceType == EntrySourceType.PlayerPrefs).ToList())
        {
            if (!PlayerPrefs.HasKey(entry.key))
            {
                missing.Add(entry);
                continue;
            }

            switch (entry.type)
            {
                case PrefValueType.String:
                    entry.stringValue = PlayerPrefs.GetString(entry.key, entry.stringValue ?? string.Empty);
                    break;
                case PrefValueType.Int:
                    entry.intValue = PlayerPrefs.GetInt(entry.key, entry.intValue);
                    break;
                case PrefValueType.Float:
                    entry.floatValue = PlayerPrefs.GetFloat(entry.key, entry.floatValue);
                    break;
                case PrefValueType.Bool:
                    entry.boolValue = PlayerPrefs.GetInt(entry.key, entry.boolValue ? 1 : 0) != 0;
                    break;
            }
        }

        for (int i = 0; i < missing.Count; i++)
        {
            _entries.Remove(missing[i]);
            _favorites.Remove(missing[i].key);
        }

        PersistCatalog();
        PersistFavorites();
    }

    private void DiscoverKeys()
    {
        int before = _entries.Count(x => x.sourceType == EntrySourceType.PlayerPrefs);

#if UNITY_EDITOR_WIN
        TryDiscoverFromWindowsRegistry();
#endif

        RefreshValuesFromPlayerPrefs();
        ParseConfiguredAndDetectedJsonSources();
        PersistCatalog();

        int after = _entries.Count(x => x.sourceType == EntrySourceType.PlayerPrefs);
        int discovered = after - before;
        EditorUtility.DisplayDialog("Discover complete", discovered > 0
            ? $"Discovered {discovered} PlayerPrefs keys. JSON sources were also refreshed."
            : "No new PlayerPrefs keys were discovered.\n\nIf your save is under one JSON key, set that key in Container Key and press Refresh Sources.", "OK");

        if (string.IsNullOrEmpty(_selectedEntryId) && _entries.Count > 0)
            SelectEntry(_entries[0]);
    }

#if UNITY_EDITOR_WIN
    private void TryDiscoverFromWindowsRegistry()
    {
        string[] candidatePaths =
        {
            $"Software\\{PlayerSettings.companyName}\\{PlayerSettings.productName}",
            $"Software\\Unity\\UnityEditor\\{PlayerSettings.companyName}\\{PlayerSettings.productName}"
        };

        foreach (string path in candidatePaths)
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(path);
            if (key == null)
                continue;

            foreach (string valueName in key.GetValueNames())
            {
                object raw = key.GetValue(valueName);
                if (raw == null)
                    continue;

                EntryData entry = ParseRegistryValue(valueName, raw);
                if (entry == null)
                    continue;

                UpsertEntry(entry);
            }
        }
    }

    private EntryData ParseRegistryValue(string valueName, object raw)
    {
        if (raw is int intValue)
        {
            return new EntryData
            {
                key = CleanRegistryKeyName(valueName),
                type = PrefValueType.Int,
                intValue = intValue,
                sourceType = EntrySourceType.PlayerPrefs,
                sourceName = "PlayerPrefs",
                lastTouchedTicks = DateTime.UtcNow.Ticks
            };
        }

        if (raw is byte[] bytes && bytes.Length == 4)
        {
            float floatValue = BitConverter.ToSingle(bytes, 0);
            return new EntryData
            {
                key = CleanRegistryKeyName(valueName),
                type = PrefValueType.Float,
                floatValue = floatValue,
                sourceType = EntrySourceType.PlayerPrefs,
                sourceName = "PlayerPrefs",
                lastTouchedTicks = DateTime.UtcNow.Ticks
            };
        }

        if (raw is string stringValue)
        {
            return new EntryData
            {
                key = CleanRegistryKeyName(valueName),
                type = PrefValueType.String,
                stringValue = stringValue,
                sourceType = EntrySourceType.PlayerPrefs,
                sourceName = "PlayerPrefs",
                lastTouchedTicks = DateTime.UtcNow.Ticks
            };
        }

        return null;
    }

    private static string CleanRegistryKeyName(string valueName)
    {
        const string suffix = "_h193410979";
        return valueName != null && valueName.EndsWith(suffix, StringComparison.Ordinal)
            ? valueName.Substring(0, valueName.Length - suffix.Length)
            : valueName;
    }
#endif

    private IEnumerable<string> GetCandidateJsonFiles()
    {
        HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(_settings.externalJsonPath))
        {
            if (File.Exists(_settings.externalJsonPath) && Path.GetExtension(_settings.externalJsonPath).Equals(".json", StringComparison.OrdinalIgnoreCase))
                paths.Add(_settings.externalJsonPath);
            else if (Directory.Exists(_settings.externalJsonPath))
            {
                foreach (string file in Directory.GetFiles(_settings.externalJsonPath, "*.json", SearchOption.AllDirectories))
                    paths.Add(file);
            }
        }

        if (_settings.autoScanPersistentDataPath)
        {
            foreach (string folder in GetLikelySaveFolders())
            {
                if (!Directory.Exists(folder))
                    continue;

                foreach (string file in Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories))
                    paths.Add(file);
            }
        }

        return paths;
    }

    private IEnumerable<string> GetLikelySaveFolders()
    {
        HashSet<string> folders = new(StringComparer.OrdinalIgnoreCase);

#if UNITY_EDITOR_WIN
        string appDataLocalLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", PlayerSettings.companyName, PlayerSettings.productName);
        folders.Add(appDataLocalLow);
        string appDataLocal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), PlayerSettings.companyName, PlayerSettings.productName);
        folders.Add(appDataLocal);
#elif UNITY_EDITOR_OSX
        folders.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", PlayerSettings.companyName, PlayerSettings.productName));
#elif UNITY_EDITOR_LINUX
        folders.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config", "unity3d", PlayerSettings.companyName, PlayerSettings.productName));
#endif

        return folders;
    }

    private string GetPersistentDataPathHint()
    {
        return string.Join(" | ", GetLikelySaveFolders().Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private void RevealSavePathHints()
    {
        string message = string.Join("\n", GetLikelySaveFolders().Select(x => "- " + x));
        EditorUtility.DisplayDialog("Likely save folders", string.IsNullOrWhiteSpace(message) ? "No save folder hints available for this platform." : message, "OK");
    }

    private void ExportSnapshot()
    {
        string path = EditorUtility.SaveFilePanel("Export PlayerPrefs snapshot", Application.dataPath,
            $"playerprefs_snapshot_{PlayerSettings.productName}_{DateTime.Now:yyyyMMdd_HHmmss}", "json");

        if (string.IsNullOrEmpty(path))
            return;

        SnapshotFile snapshot = new SnapshotFile
        {
            companyName = PlayerSettings.companyName,
            productName = PlayerSettings.productName,
            unityVersion = Application.unityVersion,
            exportedAtUtc = DateTime.UtcNow.ToString("O"),
            items = _entries.Select(e => e.Clone()).OrderBy(e => e.key, StringComparer.OrdinalIgnoreCase).ToList()
        };

        File.WriteAllText(path, JsonUtility.ToJson(snapshot, true));
        EditorUtility.RevealInFinder(path);
    }

    private void ImportSnapshot()
    {
        string path = EditorUtility.OpenFilePanel("Import PlayerPrefs snapshot", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return;

        SnapshotFile snapshot;
        try
        {
            snapshot = JsonUtility.FromJson<SnapshotFile>(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Import failed", ex.Message, "OK");
            return;
        }

        if (snapshot == null || snapshot.items == null)
        {
            EditorUtility.DisplayDialog("Import failed", "The selected file is empty or invalid.", "OK");
            return;
        }

        bool overwrite = EditorUtility.DisplayDialog("Import snapshot",
            $"Import {snapshot.items.Count} keys from snapshot? Existing direct PlayerPrefs keys with the same name will be overwritten.",
            "Import", "Cancel");
        if (!overwrite)
            return;

        foreach (EntryData item in snapshot.items.Where(x => x.sourceType == EntrySourceType.PlayerPrefs))
        {
            WriteToPlayerPrefs(item);
            UpsertEntry(item.Clone());
        }

        PersistCatalog();
        RefreshValuesFromPlayerPrefs();
        if (_entries.Count > 0 && string.IsNullOrEmpty(_selectedEntryId))
            SelectEntry(_entries[0]);

        FullRefresh();
    }

    private void RevealStorageLocation()
    {
#if UNITY_EDITOR_WIN
        EditorUtility.DisplayDialog("Windows storage",
            $"PlayerPrefs registry:\nHKCU\\Software\\{PlayerSettings.companyName}\\{PlayerSettings.productName}\nHKCU\\Software\\Unity\\UnityEditor\\{PlayerSettings.companyName}\\{PlayerSettings.productName}\n\nLikely file save folders:\n{string.Join("\n", GetLikelySaveFolders())}",
            "OK");
#elif UNITY_EDITOR_OSX
        string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) +
                      $"/Library/Preferences/unity.{PlayerSettings.companyName}.{PlayerSettings.productName}.plist";
        EditorUtility.DisplayDialog("macOS storage", path + "\n\nLikely file save folders:\n" + string.Join("\n", GetLikelySaveFolders()), "OK");
#elif UNITY_EDITOR_LINUX
        string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal) +
                      $"/.config/unity3d/{PlayerSettings.companyName}/{PlayerSettings.productName}";
        EditorUtility.DisplayDialog("Linux storage", path + "\n\nLikely file save folders:\n" + string.Join("\n", GetLikelySaveFolders()), "OK");
#else
        EditorUtility.DisplayDialog("Storage", "Storage location reveal is not implemented for this platform.", "OK");
#endif
    }

    private void WriteToPlayerPrefs(EntryData data)
    {
        switch (data.type)
        {
            case PrefValueType.String:
                PlayerPrefs.SetString(data.key, data.stringValue ?? string.Empty);
                break;
            case PrefValueType.Int:
                PlayerPrefs.SetInt(data.key, data.intValue);
                break;
            case PrefValueType.Float:
                PlayerPrefs.SetFloat(data.key, data.floatValue);
                break;
            case PrefValueType.Bool:
                PlayerPrefs.SetInt(data.key, data.boolValue ? 1 : 0);
                break;
        }

        data.lastTouchedTicks = DateTime.UtcNow.Ticks;
        PlayerPrefs.Save();
    }

    private void UpsertEntry(EntryData incoming)
    {
        int index = _entries.FindIndex(e => e.key == incoming.key && e.sourceType == incoming.sourceType && e.parentContainerKey == incoming.parentContainerKey && e.sourceFilePath == incoming.sourceFilePath);
        incoming.favorite = incoming.favorite || _favorites.Contains(incoming.key);
        if (incoming.favorite)
            _favorites.Add(incoming.key);

        if (index >= 0)
            _entries[index] = incoming;
        else
            _entries.Add(incoming);

        PersistCatalog();
        PersistFavorites();
    }

    private void SelectEntry(EntryData entry)
    {
        _selectedEntryId = entry != null ? BuildEntryId(entry) : string.Empty;
        RebuildEditingCopy();
        PersistSelectedEntry();
    }

    private static string BuildEntryId(EntryData entry)
    {
        if (entry == null)
            return string.Empty;

        return string.Join("::",
            entry.sourceType,
            entry.parentContainerKey ?? string.Empty,
            entry.sourceFilePath ?? string.Empty,
            entry.sourceName ?? string.Empty,
            entry.key ?? string.Empty);
    }

    private EntryData GetSelectedEntry()
    {
        return _entries.FirstOrDefault(e => BuildEntryId(e) == _selectedEntryId);
    }

    private void RebuildEditingCopy()
    {
        EntryData live = GetSelectedEntry();
        _editingCopy = live?.Clone();
    }

    private void SyncFavoritesIntoEntries()
    {
        foreach (EntryData entry in _entries)
            entry.favorite = _favorites.Contains(entry.key);
    }

    private void LoadCatalog()
    {
        _entries.Clear();
        string json = EditorPrefs.GetString(CatalogEditorPrefKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return;

        try
        {
            EntryListWrapper wrapper = JsonUtility.FromJson<EntryListWrapper>(json);
            if (wrapper?.items != null)
                _entries.AddRange(wrapper.items.Where(x => !string.IsNullOrWhiteSpace(x.key) && x.sourceType == EntrySourceType.PlayerPrefs));
        }
        catch
        {
            // ignored
        }
    }

    private void PersistCatalog()
    {
        EntryListWrapper wrapper = new EntryListWrapper
        {
            items = _entries.Where(e => e.sourceType == EntrySourceType.PlayerPrefs).OrderBy(e => e.key, StringComparer.OrdinalIgnoreCase).ToList()
        };
        EditorPrefs.SetString(CatalogEditorPrefKey, JsonUtility.ToJson(wrapper));
    }

    private void LoadFavorites()
    {
        _favorites.Clear();
        string json = EditorPrefs.GetString(FavoritesEditorPrefKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return;

        try
        {
            FavoritesWrapper wrapper = JsonUtility.FromJson<FavoritesWrapper>(json);
            if (wrapper?.keys != null)
                foreach (string key in wrapper.keys)
                    if (!string.IsNullOrWhiteSpace(key))
                        _favorites.Add(key);
        }
        catch
        {
            // ignored
        }
    }

    private void PersistFavorites()
    {
        FavoritesWrapper wrapper = new FavoritesWrapper { keys = _favorites.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList() };
        EditorPrefs.SetString(FavoritesEditorPrefKey, JsonUtility.ToJson(wrapper));
    }

    private void LoadSelectedEntry()
    {
        _selectedEntryId = EditorPrefs.GetString(SelectedKeyEditorPrefKey, string.Empty);
    }

    private void PersistSelectedEntry()
    {
        EditorPrefs.SetString(SelectedKeyEditorPrefKey, _selectedEntryId ?? string.Empty);
    }

    private void LoadSettings()
    {
        string json = EditorPrefs.GetString(SettingsEditorPrefKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            _settings = new SettingsData();
            return;
        }

        try
        {
            _settings = JsonUtility.FromJson<SettingsData>(json) ?? new SettingsData();
        }
        catch
        {
            _settings = new SettingsData();
        }
    }

    private void PersistSettings()
    {
        EditorPrefs.SetString(SettingsEditorPrefKey, JsonUtility.ToJson(_settings));
    }

    private string GetGetterPreview(EntryData entry)
    {
        return entry.type switch
        {
            PrefValueType.String => $"PlayerPrefs.GetString(\"{entry.key}\")",
            PrefValueType.Int => $"PlayerPrefs.GetInt(\"{entry.key}\")",
            PrefValueType.Float => $"PlayerPrefs.GetFloat(\"{entry.key}\")",
            PrefValueType.Bool => $"PlayerPrefs.GetInt(\"{entry.key}\") != 0",
            _ => string.Empty
        };
    }

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value;
        return value.Substring(0, max - 1) + "…";
    }

    private static GUIStyle ToolbarTitleStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(0, 0, 1, 0)
        };
        style.normal.textColor = AccentGold;
        return style;
    }

    private static GUIStyle PanelStyle()
    {
        GUIStyle style = new GUIStyle("HelpBox")
        {
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(0, 0, 0, 0)
        };
        style.normal.background = MakeTex(2, 2, SurfaceColor);
        return style;
    }

    private static GUIStyle HeaderStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft
        };
        style.normal.textColor = AccentGold;
        return style;
    }

    private static GUIStyle CardStyle()
    {
        GUIStyle style = new GUIStyle("HelpBox")
        {
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(0, 0, 0, 0)
        };
        style.normal.background = MakeTex(2, 2, SurfaceColor);
        return style;
    }

    private static GUIStyle SectionTitleStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            margin = new RectOffset(0, 0, 0, 6)
        };
        style.normal.textColor = TextColor;
        return style;
    }

    private static GUIStyle RowStyle()
    {
        GUIStyle style = new GUIStyle("HelpBox")
        {
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 4)
        };
        style.normal.background = MakeTex(2, 2, SurfaceAltColor);
        return style;
    }

    private static GUIStyle SelectedRowStyle()
    {
        GUIStyle style = new GUIStyle(RowStyle());
        style.normal.background = MakeTex(2, 2, new Color(0.16f, 0.2f, 0.29f));
        return style;
    }

    private static GUIStyle FavoriteButtonStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
            fixedWidth = 24,
            fixedHeight = 24
        };
        style.normal.textColor = FooterYellow;
        return style;
    }

    private static GUIStyle KeyLabelStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11
        };
        style.normal.textColor = TextColor;
        return style;
    }

    private static GUIStyle SecondaryLabelStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 10,
            clipping = TextClipping.Clip
        };
        style.normal.textColor = TextColor;
        return style;
    }

    private static GUIStyle TinyLabelStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 9,
            clipping = TextClipping.Clip
        };
        style.normal.textColor = SubtleTextColor;
        return style;
    }

    private static GUIStyle TypeTagStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.miniButtonMid)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 9,
            fontStyle = FontStyle.Bold
        };
        style.normal.background = MakeTex(2, 2, AccentBlue);
        style.normal.textColor = BackgroundColor;
        return style;
    }

    private static GUIStyle SourceTagStyle(EntryData entry)
    {
        GUIStyle style = new GUIStyle(EditorStyles.miniButton);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 9;
        style.fontStyle = FontStyle.Bold;
        style.normal.background = MakeTex(2, 2, entry.isReadOnly ? FooterRed : FooterGreen);
        style.normal.textColor = BackgroundColor;
        return style;
    }

    private static GUIStyle StatPillStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.miniButton)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10,
            padding = new RectOffset(10, 10, 3, 3),
            margin = new RectOffset(0, 6, 0, 0)
        };
        style.normal.background = MakeTex(2, 2, SurfaceAltColor);
        style.normal.textColor = TextColor;
        return style;
    }

    private static void DrawPanelChrome(Rect rect, bool emphasizeAtmosphere = false)
    {
        EditorGUI.DrawRect(rect, SurfaceColor);
        DrawPanelTopography(rect, emphasizeAtmosphere ? 0.2f : 0.1f);
        DrawPanelStars(rect, emphasizeAtmosphere ? 0.24f : 0.12f);
        if (emphasizeAtmosphere)
        {
            DrawKeysAsciiBackground(rect);
        }
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1.5f), AccentGold);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1.5f, rect.width, 1.5f), AccentGold);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1.5f, rect.height), AccentGold);
        EditorGUI.DrawRect(new Rect(rect.xMax - 1.5f, rect.y, 1.5f, rect.height), AccentGold);
    }

    private static void DrawWindowFrame(Rect rect)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), AccentGold);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), AccentGold);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2f, rect.height), AccentGold);
        EditorGUI.DrawRect(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), AccentGold);
    }

    private static void DrawFooterStrip(Rect rect)
    {
        float footerY = rect.yMax - FooterHeight;
        float segment = rect.width / 4f;
        EditorGUI.DrawRect(new Rect(rect.x, footerY, segment, FooterHeight), FooterCyan);
        EditorGUI.DrawRect(new Rect(rect.x + segment, footerY, segment, FooterHeight), FooterGreen);
        EditorGUI.DrawRect(new Rect(rect.x + segment * 2f, footerY, segment, FooterHeight), FooterYellow);
        EditorGUI.DrawRect(new Rect(rect.x + segment * 3f, footerY, rect.width - segment * 3f, FooterHeight), FooterRed);
    }

    private static void DrawTopography(Rect rect, float alphaMultiplier = 1f)
    {
        Handles.BeginGUI();
        Color lineColor = new Color(1f, 1f, 1f, 0.09f * alphaMultiplier);
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

    private static void DrawStars(Rect rect, float alphaMultiplier = 1f)
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
            Vector2 starPosition = rect.position + stars[i];
            if (starPosition.x > rect.xMax || starPosition.y > rect.yMax)
                continue;

            float size = i % 3 == 0 ? 3f : 2f;
            Handles.color = new Color(StarColor.r, StarColor.g, StarColor.b, StarColor.a * alphaMultiplier);
            Handles.DrawSolidDisc(starPosition, Vector3.forward, size * 0.5f);

            if (i % 4 == 0)
            {
                Handles.color = new Color(0.95f, 0.95f, 0.98f, 0.28f * alphaMultiplier);
                Handles.DrawLine(starPosition + new Vector2(-4f, 0f), starPosition + new Vector2(4f, 0f));
                Handles.DrawLine(starPosition + new Vector2(0f, -4f), starPosition + new Vector2(0f, 4f));
            }
        }

        Handles.EndGUI();
    }

    private static void DrawPanelTopography(Rect rect, float alphaMultiplier)
    {
        Handles.BeginGUI();
        Color lineColor = new Color(1f, 1f, 1f, 0.18f * alphaMultiplier);
        for (int layer = 0; layer < 7; layer++)
        {
            Vector3[] points = new Vector3[32];
            float amplitude = 10f + layer * 2.5f;
            float y = rect.y + 44f + layer * Mathf.Max(22f, rect.height / 8.5f);
            for (int i = 0; i < points.Length; i++)
            {
                float normalized = i / (float)(points.Length - 1);
                float x = Mathf.Lerp(rect.x + 6f, rect.xMax - 6f, normalized);
                float wave = Mathf.Sin((normalized * 8.5f) + layer * 0.7f) * amplitude;
                float drift = Mathf.Cos((normalized * 4.25f) + layer * 0.45f) * (amplitude * 0.45f);
                points[i] = new Vector3(x, y + wave + drift, 0f);
            }

            Handles.color = lineColor;
            Handles.DrawAAPolyLine(2.2f, points);
        }
        Handles.EndGUI();
    }

    private static void DrawPanelStars(Rect rect, float alphaMultiplier)
    {
        Handles.BeginGUI();
        int starCount = 28;
        for (int i = 0; i < starCount; i++)
        {
            float x = rect.x + 18f + (rect.width - 36f) * ((i * 37 % 100) / 100f);
            float y = rect.y + 18f + (rect.height - 36f) * ((i * 53 % 100) / 100f);
            Vector2 star = new Vector2(x, y);
            float size = i % 3 == 0 ? 2.4f : 1.6f;

            Handles.color = new Color(StarColor.r, StarColor.g, StarColor.b, 1f * alphaMultiplier);
            Handles.DrawSolidDisc(star, Vector3.forward, size * 0.5f);

            if (i % 4 == 0)
            {
                Handles.color = new Color(0.95f, 0.95f, 0.98f, 0.34f * alphaMultiplier);
                Handles.DrawLine(star + new Vector2(-4f, 0f), star + new Vector2(4f, 0f));
                Handles.DrawLine(star + new Vector2(0f, -4f), star + new Vector2(0f, 4f));
            }
        }
        Handles.EndGUI();
    }

    private static void DrawKeysAsciiBackground(Rect rect)
    {
        Rect artRect = new Rect(rect.x + 18f, rect.y + rect.height * 0.36f, rect.width - 32f, Mathf.Min(190f, rect.height * 0.42f));
        GUIStyle artStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize = 12,
            alignment = TextAnchor.UpperRight,
            richText = false
        };
        artStyle.font = EventHorizonEditorFont.CascadiaMono;
        artStyle.normal.textColor = new Color(TextColor.r, TextColor.g, TextColor.b, 0.42f);

        string[] lines = KeysDeckArt.Split(new[] { '\n' }, StringSplitOptions.None);
        float lineHeight = Mathf.Max(14f, artStyle.fontSize + 3f);
        float startY = artRect.y + Mathf.Max(0f, (artRect.height - lineHeight * lines.Length) * 0.5f);

        for (int i = 0; i < lines.Length; i++)
        {
            Rect lineRect = new Rect(artRect.x, startY + i * lineHeight, artRect.width, lineHeight);
            GUI.Label(lineRect, lines[i], artStyle);
        }
    }

    private static Texture2D MakeTex(int width, int height, Color color)
    {
        Color[] colors = Enumerable.Repeat(color, width * height).ToArray();
        Texture2D tex = new Texture2D(width, height);
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }

    private static string ToolbarSearchField(string value, params GUILayoutOption[] options)
    {
#if UNITY_2021_1_OR_NEWER
        return GUILayout.TextField(value ?? string.Empty, GUI.skin.FindStyle("ToolbarSearchTextField"), options);
#else
        return GUILayout.TextField(value ?? string.Empty, EditorStyles.toolbarTextField, options);
#endif
    }
}
