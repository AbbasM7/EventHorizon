using System;
using System.Collections.Generic;
using UnityEngine;

namespace EventHorizon.Core
{
    public enum EventHorizonLogModule
    {
        General,
        Core,
        UI,
        Sound,
        Currency,
        Save,
        Scene,
        Editor,
        Example,
        Logger,
        Ads,
        Game,
        Bootstrap,
        Boosters,
        Board,
        Views,
        Input,
        Systems,
        Audio
    }

    [Serializable]
    public class SingularityConsoleChannel
    {
        [SerializeField] private EventHorizonLogModule _module;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private string _hexColor = "#FFFFFF";

        public EventHorizonLogModule Module => _module;
        public bool Enabled => _enabled;
        public string HexColor => _hexColor;

        public SingularityConsoleChannel(EventHorizonLogModule module, string hexColor)
        {
            _module = module;
            _enabled = true;
            _hexColor = hexColor;
        }
    }

    public static class SingularityConsole
    {
        private static readonly Dictionary<EventHorizonLogModule, SingularityConsoleChannel> DefaultChannels = new Dictionary<EventHorizonLogModule, SingularityConsoleChannel>
        {
            { EventHorizonLogModule.General, new SingularityConsoleChannel(EventHorizonLogModule.General, "#D8E6FF") },
            { EventHorizonLogModule.Core, new SingularityConsoleChannel(EventHorizonLogModule.Core, "#5CC8FF") },
            { EventHorizonLogModule.UI, new SingularityConsoleChannel(EventHorizonLogModule.UI, "#FF8AD8") },
            { EventHorizonLogModule.Sound, new SingularityConsoleChannel(EventHorizonLogModule.Sound, "#FFD166") },
            { EventHorizonLogModule.Currency, new SingularityConsoleChannel(EventHorizonLogModule.Currency, "#8CE99A") },
            { EventHorizonLogModule.Save, new SingularityConsoleChannel(EventHorizonLogModule.Save, "#A78BFA") },
            { EventHorizonLogModule.Scene, new SingularityConsoleChannel(EventHorizonLogModule.Scene, "#FF9F68") },
            { EventHorizonLogModule.Editor, new SingularityConsoleChannel(EventHorizonLogModule.Editor, "#7EE7FF") },
            { EventHorizonLogModule.Example, new SingularityConsoleChannel(EventHorizonLogModule.Example, "#F9F871") },
            { EventHorizonLogModule.Logger, new SingularityConsoleChannel(EventHorizonLogModule.Logger, "#FFFFFF") },
            { EventHorizonLogModule.Ads, new SingularityConsoleChannel(EventHorizonLogModule.Ads, "#3DDC97") },
            { EventHorizonLogModule.Game, new SingularityConsoleChannel(EventHorizonLogModule.Game, "#9FB3C8") },
            { EventHorizonLogModule.Bootstrap, new SingularityConsoleChannel(EventHorizonLogModule.Bootstrap, "#64DFC7") },
            { EventHorizonLogModule.Boosters, new SingularityConsoleChannel(EventHorizonLogModule.Boosters, "#FFB86C") },
            { EventHorizonLogModule.Board, new SingularityConsoleChannel(EventHorizonLogModule.Board, "#7CC6FE") },
            { EventHorizonLogModule.Views, new SingularityConsoleChannel(EventHorizonLogModule.Views, "#F497D6") },
            { EventHorizonLogModule.Input, new SingularityConsoleChannel(EventHorizonLogModule.Input, "#B8F2E6") },
            { EventHorizonLogModule.Systems, new SingularityConsoleChannel(EventHorizonLogModule.Systems, "#FF8E72") },
            { EventHorizonLogModule.Audio, new SingularityConsoleChannel(EventHorizonLogModule.Audio, "#FFE66D") }
        };

        private static SingularityConsoleModuleSO _boundModule;

        public static void Bind(SingularityConsoleModuleSO module)
        {
            _boundModule = module;
        }

        public static void Unbind(SingularityConsoleModuleSO module)
        {
            if (_boundModule == module)
            {
                _boundModule = null;
            }
        }

        public static void Log(object source, string message, UnityEngine.Object context = null)
        {
            Write(ResolveModule(source), message, LogType.Log, null, context);
        }

        public static void Log<T>(string message, UnityEngine.Object context = null)
        {
            Write(ResolveModule(typeof(T)), message, LogType.Log, null, context);
        }

        public static void LogWarning(object source, string message, UnityEngine.Object context = null)
        {
            Write(ResolveModule(source), message, LogType.Warning, null, context);
        }

        public static void LogWarning<T>(string message, UnityEngine.Object context = null)
        {
            Write(ResolveModule(typeof(T)), message, LogType.Warning, null, context);
        }

        public static void LogError(object source, string message, UnityEngine.Object context = null)
        {
            Write(ResolveModule(source), message, LogType.Error, null, context);
        }

        public static void LogError<T>(string message, UnityEngine.Object context = null)
        {
            Write(ResolveModule(typeof(T)), message, LogType.Error, null, context);
        }

        public static void LogGeneral(string message, string hexColor = null, UnityEngine.Object context = null)
        {
            Write(EventHorizonLogModule.General, message, LogType.Log, hexColor, context);
        }

        public static void Log(EventHorizonLogModule module, string message, UnityEngine.Object context = null)
        {
            Write(module, message, LogType.Log, null, context);
        }

        public static void LogWarning(EventHorizonLogModule module, string message, UnityEngine.Object context = null)
        {
            Write(module, message, LogType.Warning, null, context);
        }

        public static void LogError(EventHorizonLogModule module, string message, UnityEngine.Object context = null)
        {
            Write(module, message, LogType.Error, null, context);
        }

        public static EventHorizonLogModule ResolveModule(object source)
        {
            if (source == null)
            {
                return EventHorizonLogModule.General;
            }

            return ResolveModule(source.GetType());
        }

        public static EventHorizonLogModule ResolveModule(Type sourceType)
        {
            if (sourceType == null)
            {
                return EventHorizonLogModule.General;
            }

            string typeNamespace = sourceType.Namespace ?? string.Empty;

            if (typeNamespace.StartsWith("EventHorizon.Core", StringComparison.Ordinal))
                return sourceType.Name.Contains("SingularityConsole", StringComparison.Ordinal) ? EventHorizonLogModule.Logger : EventHorizonLogModule.Core;

            if (typeNamespace.StartsWith("EventHorizon.UI", StringComparison.Ordinal))
                return EventHorizonLogModule.UI;

            if (typeNamespace.StartsWith("EventHorizon.Sound", StringComparison.Ordinal))
                return EventHorizonLogModule.Sound;

            if (typeNamespace.StartsWith("EventHorizon.Currency", StringComparison.Ordinal))
                return EventHorizonLogModule.Currency;

            if (typeNamespace.StartsWith("EventHorizon.Save", StringComparison.Ordinal))
                return EventHorizonLogModule.Save;

            if (typeNamespace.StartsWith("EventHorizon.Scene", StringComparison.Ordinal))
                return EventHorizonLogModule.Scene;

            if (typeNamespace.StartsWith("EventHorizon.Editor", StringComparison.Ordinal))
                return EventHorizonLogModule.Editor;

            if (typeNamespace.StartsWith("EventHorizon.Example", StringComparison.Ordinal))
                return EventHorizonLogModule.Example;

            if (typeNamespace.StartsWith("EventHorizon.Ads", StringComparison.Ordinal))
                return EventHorizonLogModule.Ads;

            if (typeNamespace.StartsWith("ArrowOut", StringComparison.Ordinal))
                return ResolveArrowOutModule(sourceType);

            return EventHorizonLogModule.General;
        }

        private static EventHorizonLogModule ResolveArrowOutModule(Type sourceType)
        {
            string typeName = sourceType.Name;

            if (typeName.Contains("Bootstrap", StringComparison.Ordinal)
                || typeName.Contains("Setup", StringComparison.Ordinal)
                || typeName.Contains("Initializer", StringComparison.Ordinal))
                return EventHorizonLogModule.Bootstrap;

            if (typeName.Contains("Booster", StringComparison.Ordinal))
                return EventHorizonLogModule.Boosters;

            if (typeName.Contains("Input", StringComparison.Ordinal))
                return EventHorizonLogModule.Input;

            if (typeName.Contains("Audio", StringComparison.Ordinal))
                return EventHorizonLogModule.Audio;

            if (typeName.EndsWith("System", StringComparison.Ordinal))
                return EventHorizonLogModule.Systems;

            if (typeName.Contains("Board", StringComparison.Ordinal)
                || typeName.Contains("Grid", StringComparison.Ordinal)
                || typeName.Contains("Cell", StringComparison.Ordinal))
                return EventHorizonLogModule.Board;

            if (typeName.EndsWith("View", StringComparison.Ordinal)
                || typeName.Contains("Renderer", StringComparison.Ordinal)
                || typeName.Contains("Camera", StringComparison.Ordinal))
                return EventHorizonLogModule.Views;

            return EventHorizonLogModule.Game;
        }

        private static void Write(EventHorizonLogModule module, string message, LogType logType, string customHexColor, UnityEngine.Object context)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            SingularityConsoleChannel channel = _boundModule != null
                ? _boundModule.GetChannel(module)
                : GetDefaultChannel(module);

            if (channel != null && !channel.Enabled)
            {
                return;
            }

            string color = string.IsNullOrWhiteSpace(customHexColor)
                ? channel != null ? channel.HexColor : GetDefaultChannel(module).HexColor
                : customHexColor;

            string formattedMessage = FormatMessage(module, color, message);

            switch (logType)
            {
                case LogType.Warning:
                    if (context != null)
                        Debug.LogWarning(formattedMessage, context);
                    else
                        Debug.LogWarning(formattedMessage);
                    break;
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    if (context != null)
                        Debug.LogError(formattedMessage, context);
                    else
                        Debug.LogError(formattedMessage);
                    break;
                default:
                    if (context != null)
                        Debug.Log(formattedMessage, context);
                    else
                        Debug.Log(formattedMessage);
                    break;
            }
        }

        private static string FormatMessage(EventHorizonLogModule module, string color, string message)
        {
            string label = module.ToString().ToUpperInvariant();
            return $"<color={color}><b>[{label}]</b></color> {message}";
        }

        private static SingularityConsoleChannel GetDefaultChannel(EventHorizonLogModule module)
        {
            if (DefaultChannels.TryGetValue(module, out SingularityConsoleChannel channel))
            {
                return channel;
            }

            return DefaultChannels[EventHorizonLogModule.General];
        }
    }
}
