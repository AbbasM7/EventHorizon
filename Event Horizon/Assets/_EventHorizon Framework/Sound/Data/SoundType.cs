namespace EventHorizon.Sound
{
    /// <summary>
    /// Categorizes audio cues for independent volume channel control.
    /// </summary>
    public enum SoundType
    {
        /// <summary>Sound effects — short, one-shot audio.</summary>
        SFX,

        /// <summary>Background music — typically looping.</summary>
        Music,

        /// <summary>UI feedback sounds — button clicks, transitions, etc.</summary>
        UI
    }
}
