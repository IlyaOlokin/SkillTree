using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioSystem
{
    [CreateAssetMenu(menuName = "SkillTree/Audio/Audio Cue Library", fileName = "AudioCueLibrary")]
    public sealed class AudioCueLibrary : ScriptableObject
    {
        [SerializeField] private List<AudioCueDefinition> cues = new();

        public IReadOnlyList<AudioCueDefinition> Cues => cues;
    }

    [Serializable]
    public sealed class AudioCueDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private AudioBus bus = AudioBus.Sfx;
        [SerializeField] private AudioClip[] clips;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.5f;
        [SerializeField] [Range(-1f, 1f)] private float randomPitchRange = 0.04f;
        [SerializeField] [Min(0f)] private float minimumInterval = 0.03f;
        [SerializeField] [Range(0f, 1f)] private float spatialBlend;

        public string Id => id;
        public AudioBus Bus => bus;
        public AudioClip[] Clips => clips;
        public float Volume => volume;
        public float RandomPitchRange => randomPitchRange;
        public float MinimumInterval => minimumInterval;
        public float SpatialBlend => spatialBlend;

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0)
                return null;

            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }
    }
}
