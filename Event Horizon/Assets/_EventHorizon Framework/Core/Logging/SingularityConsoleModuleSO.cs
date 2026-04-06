using System;
using System.Collections.Generic;
using UnityEngine;

namespace EventHorizon.Core
{
    [CreateAssetMenu(menuName = "EventHorizon/Logger/Singularity Console", order = 10)]
    public class SingularityConsoleModuleSO : ModuleBase
    {
        [Header("Channel Controls")]
        [Tooltip("Per-module log settings used by SingularityConsole.")]
        [SerializeField] private List<SingularityConsoleChannel> _channels = new List<SingularityConsoleChannel>();

        private Dictionary<EventHorizonLogModule, SingularityConsoleChannel> _channelLookup;

        public SingularityConsoleChannel GetChannel(EventHorizonLogModule module)
        {
            EnsureChannels();

            if (_channelLookup.TryGetValue(module, out SingularityConsoleChannel channel))
            {
                return channel;
            }

            return null;
        }

        protected override void OnInitialize()
        {
            EnsureChannels();
            SingularityConsole.Bind(this);
        }

        protected override void OnDispose()
        {
            SingularityConsole.Unbind(this);
        }

        private void OnValidate()
        {
            EnsureChannels();
        }

        private void EnsureChannels()
        {
            if (_channels == null)
            {
                _channels = new List<SingularityConsoleChannel>();
            }

            foreach (EventHorizonLogModule module in Enum.GetValues(typeof(EventHorizonLogModule)))
            {
                if (FindChannel(module) == null)
                {
                    _channels.Add(new SingularityConsoleChannel(module, GetDefaultHexColor(module)));
                }
            }

            _channelLookup = new Dictionary<EventHorizonLogModule, SingularityConsoleChannel>();
            for (int i = 0; i < _channels.Count; i++)
            {
                if (_channels[i] == null) continue;
                _channelLookup[_channels[i].Module] = _channels[i];
            }
        }

        private SingularityConsoleChannel FindChannel(EventHorizonLogModule module)
        {
            for (int i = 0; i < _channels.Count; i++)
            {
                if (_channels[i] != null && _channels[i].Module == module)
                {
                    return _channels[i];
                }
            }

            return null;
        }

        private static string GetDefaultHexColor(EventHorizonLogModule module)
        {
            switch (module)
            {
                case EventHorizonLogModule.Core:
                    return "#5CC8FF";
                case EventHorizonLogModule.UI:
                    return "#FF8AD8";
                case EventHorizonLogModule.Sound:
                    return "#FFD166";
                case EventHorizonLogModule.Currency:
                    return "#8CE99A";
                case EventHorizonLogModule.Save:
                    return "#A78BFA";
                case EventHorizonLogModule.Scene:
                    return "#FF9F68";
                case EventHorizonLogModule.Editor:
                    return "#7EE7FF";
                case EventHorizonLogModule.Example:
                    return "#F9F871";
                case EventHorizonLogModule.Logger:
                    return "#FFFFFF";
                case EventHorizonLogModule.Ads:
                    return "#3DDC97";
                case EventHorizonLogModule.Game:
                    return "#9FB3C8";
                case EventHorizonLogModule.Bootstrap:
                    return "#64DFC7";
                case EventHorizonLogModule.Boosters:
                    return "#FFB86C";
                case EventHorizonLogModule.Board:
                    return "#7CC6FE";
                case EventHorizonLogModule.Views:
                    return "#F497D6";
                case EventHorizonLogModule.Input:
                    return "#B8F2E6";
                case EventHorizonLogModule.Systems:
                    return "#FF8E72";
                case EventHorizonLogModule.Audio:
                    return "#FFE66D";
                default:
                    return "#D8E6FF";
            }
        }
    }
}
