using System.Collections.Generic;
using DG.Tweening;
using SaveSystem;
using UnityEngine;
using UnityEngine.Audio;
using Zenject;

namespace AudioSystem
{
    public sealed class GameAudio : MonoBehaviour
    {
        private const string MasterVolumeParameter = "MasterVolume";
        private const string SfxVolumeParameter = "SfxVolume";
        private const string MusicVolumeParameter = "MusicVolume";

        [Header("Library")]
        [SerializeField] private AudioCueLibrary cueLibrary;

        [Header("Mixer")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup musicGroup;

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfx2DSource;
        [SerializeField] [Min(1)] private int sfxPoolSize = 12;
        [SerializeField] private bool dontDestroyOnLoad = true;

        private readonly Dictionary<string, AudioCueDefinition> _cues = new(System.StringComparer.Ordinal);
        private readonly Dictionary<string, float> _lastPlayTimes = new(System.StringComparer.Ordinal);
        private readonly List<AudioSource> _sfxPool = new();
        private LocalSettingsService _settingsService;
        private Tween _musicFadeTween;
        private int _nextPoolIndex;

        public static GameAudio Instance { get; private set; }

        [Inject]
        private void Construct([InjectOptional] LocalSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            RebuildCueLookup();
            EnsureSources();
            ApplySavedVolumes();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            _musicFadeTween?.Kill();
        }

        public void PlaySfx(string cueId)
        {
            if (!TryGetCue(cueId, AudioBus.Sfx, out AudioCueDefinition cue))
                return;

            AudioClip clip = cue.GetRandomClip();
            if (clip == null)
                return;

            AudioSource source = sfx2DSource != null ? sfx2DSource : GetPooledSfxSource();
            ConfigureSfxSource(source, cue, Vector3.zero, false);
            source.PlayOneShot(clip, cue.Volume);
        }

        public void PlaySfxAt(string cueId, Vector3 position)
        {
            if (!TryGetCue(cueId, AudioBus.Sfx, out AudioCueDefinition cue))
                return;

            AudioClip clip = cue.GetRandomClip();
            if (clip == null)
                return;

            AudioSource source = GetPooledSfxSource();
            ConfigureSfxSource(source, cue, position, true);
            source.clip = clip;
            source.volume = cue.Volume;
            source.Play();
        }

        public void PlayMusic(string cueId, float fadeDuration = 0.5f)
        {
            if (!TryGetCue(cueId, AudioBus.Music, out AudioCueDefinition cue))
                return;

            AudioClip clip = cue.GetRandomClip();
            if (clip == null || musicSource == null)
                return;

            _musicFadeTween?.Kill();

            if (musicSource.clip == clip && musicSource.isPlaying)
                return;

            if (fadeDuration <= 0f || !musicSource.isPlaying)
            {
                StartMusicClip(clip, cue);
                return;
            }

            _musicFadeTween = musicSource
                .DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    StartMusicClip(clip, cue);
                    _musicFadeTween = musicSource.DOFade(cue.Volume, fadeDuration).SetUpdate(true);
                });
        }

        public void StopMusic(float fadeDuration = 0.5f)
        {
            if (musicSource == null)
                return;

            _musicFadeTween?.Kill();

            if (fadeDuration <= 0f)
            {
                musicSource.Stop();
                musicSource.clip = null;
                return;
            }

            _musicFadeTween = musicSource
                .DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    musicSource.Stop();
                    musicSource.clip = null;
                });
        }

        public void SetMasterVolume(float volume)
        {
            SetVolume(MasterVolumeParameter, volume);
            if (_settingsService?.Current != null)
                _settingsService.Current.masterVolume = Mathf.Clamp01(volume);
        }

        public void SetSfxVolume(float volume)
        {
            SetVolume(SfxVolumeParameter, volume);
            if (_settingsService?.Current != null)
                _settingsService.Current.sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetMusicVolume(float volume)
        {
            SetVolume(MusicVolumeParameter, volume);
            if (_settingsService?.Current != null)
                _settingsService.Current.musicVolume = Mathf.Clamp01(volume);
        }

        public void SaveVolumes()
        {
            _settingsService?.Save();
        }

        private bool TryGetCue(string cueId, AudioBus expectedBus, out AudioCueDefinition cue)
        {
            cue = null;

            if (string.IsNullOrWhiteSpace(cueId) || !_cues.TryGetValue(cueId, out cue))
                return false;

            if (cue.Bus != expectedBus)
                return false;

            if (cue.MinimumInterval > 0f
                && _lastPlayTimes.TryGetValue(cueId, out float lastPlayTime)
                && Time.unscaledTime - lastPlayTime < cue.MinimumInterval)
            {
                return false;
            }

            _lastPlayTimes[cueId] = Time.unscaledTime;
            return true;
        }

        private void RebuildCueLookup()
        {
            _cues.Clear();

            if (cueLibrary == null)
                return;

            foreach (AudioCueDefinition cue in cueLibrary.Cues)
            {
                if (cue == null || string.IsNullOrWhiteSpace(cue.Id))
                    continue;

                _cues[cue.Id] = cue;
            }
        }

        private void EnsureSources()
        {
            musicSource = EnsureSource(musicSource, "MusicSource", musicGroup, false);
            sfx2DSource = EnsureSource(sfx2DSource, "Sfx2DSource", sfxGroup, false);

            while (_sfxPool.Count < sfxPoolSize)
            {
                AudioSource source = EnsureSource(null, $"SfxSource_{_sfxPool.Count:00}", sfxGroup, false);
                _sfxPool.Add(source);
            }
        }

        private AudioSource EnsureSource(AudioSource source, string objectName, AudioMixerGroup mixerGroup, bool spatial)
        {
            if (source == null)
            {
                GameObject sourceObject = new(objectName);
                sourceObject.transform.SetParent(transform);
                sourceObject.transform.localPosition = Vector3.zero;
                source = sourceObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = spatial ? 1f : 0f;
            source.outputAudioMixerGroup = mixerGroup;
            return source;
        }

        private AudioSource GetPooledSfxSource()
        {
            if (_sfxPool.Count == 0)
                return EnsureSource(null, "SfxSource_Runtime", sfxGroup, true);

            AudioSource source = _sfxPool[_nextPoolIndex];
            _nextPoolIndex = (_nextPoolIndex + 1) % _sfxPool.Count;
            return source;
        }

        private void ConfigureSfxSource(AudioSource source, AudioCueDefinition cue, Vector3 position, bool useWorldPosition)
        {
            if (source == null)
                return;

            source.Stop();
            source.clip = null;
            source.loop = false;
            source.outputAudioMixerGroup = sfxGroup;
            source.spatialBlend = useWorldPosition ? cue.SpatialBlend : 0f;
            source.pitch = 1f + Random.Range(-cue.RandomPitchRange, cue.RandomPitchRange);

            if (useWorldPosition)
                source.transform.position = position;
        }

        private void StartMusicClip(AudioClip clip, AudioCueDefinition cue)
        {
            musicSource.Stop();
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.pitch = 1f;
            musicSource.volume = cue.Volume;
            musicSource.outputAudioMixerGroup = musicGroup;
            musicSource.Play();
        }

        private void ApplySavedVolumes()
        {
            LocalSettingsSaveData settings = _settingsService?.Current;
            SetVolume(MasterVolumeParameter, settings?.masterVolume ?? 1f);
            SetVolume(SfxVolumeParameter, settings?.sfxVolume ?? 1f);
            SetVolume(MusicVolumeParameter, settings?.musicVolume ?? 1f);
        }

        private void SetVolume(string exposedParameter, float volume)
        {
            if (mixer == null)
                return;

            mixer.SetFloat(exposedParameter, LinearToDecibels(Mathf.Clamp01(volume)));
        }

        private static float LinearToDecibels(float volume)
        {
            return volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20f;
        }
    }
}
