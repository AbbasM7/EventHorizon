using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace EventHorizon.Editor
{
    [InitializeOnLoad]
    public static class ObservationDeckLogStore
    {
        private const int MaxEntries = 1024;
        private static readonly List<ObservationDeckLogEntry> EntriesInternal = new List<ObservationDeckLogEntry>(MaxEntries);
        private static readonly Regex RichTextRegex = new Regex("<.*?>", RegexOptions.Compiled);
        private static readonly Regex CategoryRegex = new Regex(@"\[(?<category>[A-Z]+)\]", RegexOptions.Compiled);

        public static event Action OnLogsChanged;

        public static IReadOnlyList<ObservationDeckLogEntry> Entries => EntriesInternal;

        static ObservationDeckLogStore()
        {
            Application.logMessageReceived -= HandleLogMessageReceived;
            Application.logMessageReceived += HandleLogMessageReceived;
        }

        public static void Clear()
        {
            EntriesInternal.Clear();
            OnLogsChanged?.Invoke();
        }

        private static void HandleLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            string plainMessage = RichTextRegex.Replace(condition ?? string.Empty, string.Empty);
            string category = ResolveCategory(plainMessage);
            string message = ExtractMessageBody(plainMessage);

            EntriesInternal.Add(new ObservationDeckLogEntry(
                DateTime.Now,
                category,
                condition ?? string.Empty,
                message,
                stackTrace ?? string.Empty,
                type));

            if (EntriesInternal.Count > MaxEntries)
            {
                EntriesInternal.RemoveAt(0);
            }

            OnLogsChanged?.Invoke();
        }

        private static string ResolveCategory(string plainMessage)
        {
            Match match = CategoryRegex.Match(plainMessage);
            if (match.Success)
            {
                return match.Groups["category"].Value;
            }

            return "GENERAL";
        }

        private static string ExtractMessageBody(string plainMessage)
        {
            int endOfCategory = plainMessage.IndexOf(']');
            if (endOfCategory >= 0 && endOfCategory + 1 < plainMessage.Length)
            {
                return plainMessage.Substring(endOfCategory + 1).Trim();
            }

            return plainMessage.Trim();
        }
    }

    [Serializable]
    public class ObservationDeckLogEntry
    {
        public DateTime Timestamp { get; }
        public string Category { get; }
        public string RichMessage { get; }
        public string Message { get; }
        public string StackTrace { get; }
        public LogType LogType { get; }

        public ObservationDeckLogEntry(DateTime timestamp, string category, string richMessage, string message, string stackTrace, LogType logType)
        {
            Timestamp = timestamp;
            Category = category;
            RichMessage = richMessage;
            Message = message;
            StackTrace = stackTrace;
            LogType = logType;
        }
    }

    [Serializable]
    public struct ObservationDeckTraceFrame
    {
        public string MethodName { get; }
        public string ScriptName { get; }
        public string AssetPath { get; }
        public int LineNumber { get; }

        public ObservationDeckTraceFrame(string methodName, string scriptName, string assetPath, int lineNumber)
        {
            MethodName = methodName;
            ScriptName = scriptName;
            AssetPath = assetPath;
            LineNumber = lineNumber;
        }
    }
}
