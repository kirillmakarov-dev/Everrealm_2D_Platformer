using UnityEngine;
using UnityEngine.Audio;

namespace LetterHunter.Audio
{
    [DisallowMultipleComponent]
    public sealed class SoundManager : MonoBehaviour
    {
        private const string MasterPreference = "audio.master";
        private const string MusicPreference = "audio.music";
        private const string SfxPreference = "audio.sfx";

        [Header("Mixer")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private string masterVolumeParameter = "MasterVolume";
        [SerializeField] private string musicVolumeParameter = "MusicVolume";
        [SerializeField] private string sfxVolumeParameter = "SfxVolume";
        [SerializeField] private string uiVolumeParameter = "UiVolume";

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Clips")]
        [SerializeField] private AudioClip sceneMusic;
        [SerializeField] private AudioClip shotClip;
        [SerializeField] private AudioClip impactClip;
        [SerializeField] private AudioClip buttonHoverClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip menuOpenClip;
        [SerializeField] private AudioClip menuCloseClip;

        [Header("One-shot levels")]
        [Range(0f, 1f), SerializeField] private float shotLevel = 0.65f;
        [Range(0f, 1f), SerializeField] private float impactLevel = 0.8f;
        [Range(0f, 1f), SerializeField] private float uiLevel = 0.65f;

        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 0.65f;
        public float SfxVolume { get; private set; } = 0.8f;

        private void Awake()
        {
            MasterVolume = PlayerPrefs.GetFloat(MasterPreference, 1f);
            MusicVolume = PlayerPrefs.GetFloat(MusicPreference, 0.65f);
            SfxVolume = PlayerPrefs.GetFloat(SfxPreference, 0.8f);
            ApplyMixerVolumes();
            StartSceneMusic();
        }

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            SetMixerVolume(masterVolumeParameter, MasterVolume);
            SavePreference(MasterPreference, MasterVolume);
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            SetMixerVolume(musicVolumeParameter, MusicVolume);
            SavePreference(MusicPreference, MusicVolume);
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            SetMixerVolume(sfxVolumeParameter, SfxVolume);
            SetMixerVolume(uiVolumeParameter, SfxVolume);
            SavePreference(SfxPreference, SfxVolume);
        }

        public void PlayShot(AudioClip overrideClip = null) =>
            Play(sfxSource, overrideClip != null ? overrideClip : shotClip, shotLevel);
        public void PlayImpact(AudioClip overrideClip = null) =>
            Play(sfxSource, overrideClip != null ? overrideClip : impactClip, impactLevel);
        public void PlayButtonHover() => Play(uiSource, buttonHoverClip, uiLevel);
        public void PlayButtonClick() => Play(uiSource, buttonClickClip, uiLevel);
        public void PlayMenuOpen() => Play(uiSource, menuOpenClip, uiLevel);
        public void PlayMenuClose() => Play(uiSource, menuCloseClip, uiLevel);

        private void StartSceneMusic()
        {
            if (musicSource == null || sceneMusic == null)
                return;

            musicSource.clip = sceneMusic;
            musicSource.loop = true;
            if (!musicSource.isPlaying)
                musicSource.Play();
        }

        private void ApplyMixerVolumes()
        {
            SetMixerVolume(masterVolumeParameter, MasterVolume);
            SetMixerVolume(musicVolumeParameter, MusicVolume);
            SetMixerVolume(sfxVolumeParameter, SfxVolume);
            SetMixerVolume(uiVolumeParameter, SfxVolume);
        }

        private void SetMixerVolume(string parameter, float normalizedValue)
        {
            if (mixer == null || string.IsNullOrWhiteSpace(parameter))
                return;

            var decibels = normalizedValue <= 0.0001f ? -80f : Mathf.Log10(normalizedValue) * 20f;
            mixer.SetFloat(parameter, decibels);
        }

        private static void Play(AudioSource source, AudioClip clip, float level)
        {
            if (source != null && clip != null)
                source.PlayOneShot(clip, Mathf.Clamp01(level));
        }

        private static void SavePreference(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }
    }
}
