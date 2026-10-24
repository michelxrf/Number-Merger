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

        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private AudioSource _uiSource;

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
