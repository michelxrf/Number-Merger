using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace NumberMerge.Audio
{
    public enum AudioChannel
    {
        Music,
        Sfx,
        Ui
    }

    // One AudioMixer with three groups (Music, Sfx, Ui). Each group's volume is exposed as a
    // parameter on the mixer (MusicVolume, SfxVolume, UiVolume); SetVolume writes it in decibels.
    public sealed class AudioManager : MonoBehaviour
    {
        private const string MusicVolumeParameter = "MusicVolume";
        private const string SfxVolumeParameter = "SfxVolume";
        private const string UiVolumeParameter = "UiVolume";
        private const float MinimumLinearVolume = 0.0001f;
        private const float SilenceDecibels = -80f;

        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private AudioMixerGroup _uiGroup;
        [SerializeField] private AudioClip _backgroundMusic;
        [Tooltip("Clips for gameplay and UI sound effects. Played through Play(SfxId).")]
        [SerializeField] private SfxLibrary _sfxLibrary;

        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private AudioSource _uiSource;
        private readonly List<AudioSource> _chainSources = new List<AudioSource>();

        public static AudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _musicSource = CreateSource(_musicGroup, true);
            _sfxSource = CreateSource(_sfxGroup, false);
            _uiSource = CreateSource(_uiGroup, false);

            if (_sfxLibrary == null)
            {
                Debug.LogWarning("AudioManager has no SfxLibrary assigned; sound effects will be silent.", this);
            }

            PlayMusic();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlayMusic()
        {
            if (_backgroundMusic == null)
            {
                Debug.LogWarning("AudioManager has no background music assigned.", this);
                return;
            }

            if (_musicSource.clip == _backgroundMusic && _musicSource.isPlaying)
            {
                return;
            }

            _musicSource.clip = _backgroundMusic;
            _musicSource.loop = true;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            _musicSource.Stop();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip != null)
            {
                _sfxSource.PlayOneShot(clip);
            }
        }

        public void PlayUi(AudioClip clip)
        {
            if (clip != null)
            {
                _uiSource.PlayOneShot(clip);
            }
        }

        // Plays a clip with a pitch raised by chainIndex * ChainPitchStep (index 0 = 1x). Uses its own
        // sources so changing a pitch never affects sounds that are still playing.
        public void PlayChained(SfxId id, int chainIndex)
        {
            if (_sfxLibrary == null)
            {
                return;
            }

            AudioClip clip = _sfxLibrary.GetRandomClip(id);
            if (clip == null)
            {
                return;
            }

            AudioSource source = GetIdleChainSource();
            source.pitch = 1f + Mathf.Max(0, chainIndex) * _sfxLibrary.ChainPitchStep;
            source.clip = clip;
            source.Play();
        }

        private AudioSource GetIdleChainSource()
        {
            foreach (AudioSource source in _chainSources)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            AudioSource created = CreateSource(_sfxGroup, false);
            _chainSources.Add(created);
            return created;
        }

        // Plays a random clip for the moment from the SfxLibrary. Button presses use the UI channel,
        // everything else uses the SFX channel. Silent if the library or the clip is missing.
        public void Play(SfxId id)
        {
            AudioClip clip = _sfxLibrary != null ? _sfxLibrary.GetRandomClip(id) : null;

            if (id == SfxId.ButtonPress)
            {
                PlayUi(clip);
            }
            else
            {
                PlaySfx(clip);
            }
        }

        // linearVolume is clamped to 0..1 and converted to decibels for the mixer.
        public void SetVolume(AudioChannel channel, float linearVolume)
        {
            string parameter = GetParameter(channel);
            float decibels = ToDecibels(Mathf.Clamp01(linearVolume));

            if (!_mixer.SetFloat(parameter, decibels))
            {
                Debug.LogError("AudioMixer does not expose parameter '" + parameter + "'.", this);
            }
        }

        public float GetVolume(AudioChannel channel)
        {
            if (!_mixer.GetFloat(GetParameter(channel), out float decibels))
            {
                return 0f;
            }

            return decibels <= SilenceDecibels ? 0f : Mathf.Pow(10f, decibels / 20f);
        }

        private AudioSource CreateSource(AudioMixerGroup group, bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.outputAudioMixerGroup = group;
            return source;
        }

        private static string GetParameter(AudioChannel channel)
        {
            switch (channel)
            {
                case AudioChannel.Music:
                    return MusicVolumeParameter;
                case AudioChannel.Sfx:
                    return SfxVolumeParameter;
                case AudioChannel.Ui:
                    return UiVolumeParameter;
                default:
                    return null;
            }
        }

        private static float ToDecibels(float linearVolume)
        {
            return linearVolume <= MinimumLinearVolume
                ? SilenceDecibels
                : Mathf.Log10(linearVolume) * 20f;
        }
    }
}
