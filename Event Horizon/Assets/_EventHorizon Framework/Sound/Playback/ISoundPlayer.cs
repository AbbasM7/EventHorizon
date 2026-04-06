namespace EventHorizon.Sound
{
    /// <summary>
    /// Contract for systems that play and control audio.
    /// </summary>
    public interface ISoundPlayer
    {
        /// <summary>
        /// Plays the specified sound cue.
        /// </summary>
        void PlayCue(SoundCueSO cue);

        /// <summary>
        /// Stops the specified sound cue if currently playing.
        /// </summary>
        void StopCue(SoundCueSO cue);

        /// <summary>
        /// Stops all currently playing sounds.
        /// </summary>
        void StopAll();

        /// <summary>
        /// Sets the master volume (0-1).
        /// </summary>
        void SetMasterVolume(float volume);

        /// <summary>
        /// Sets the music channel volume (0-1).
        /// </summary>
        void SetMusicVolume(float volume);

        /// <summary>
        /// Sets the SFX channel volume (0-1).
        /// </summary>
        void SetSFXVolume(float volume);
    }
}
