// SFXClipGroup.cs
// ScriptableObject that holds a group of audio clips for randomized playback
// Created: December 15, 2025

using UnityEngine;

namespace DLYH.Audio
{
    /// <summary>
    /// A group of audio clips that can be played randomly for variety.
    /// Use this for actions like keyboard clicks where you want variation.
    /// </summary>
    [CreateAssetMenu(fileName = "New SFX Clip Group", menuName = "DLYH/Audio/SFX Clip Group")]
    public class SFXClipGroup : ScriptableObject
    {
        [Header("Audio Clips")]
        [SerializeField, Tooltip("Array of clips to randomly select from")]
        private AudioClip[] _clips;

        [Header("Playback Settings")]
        [SerializeField, Range(0f, 1f), Tooltip("Base volume for this clip group")]
        private float _baseVolume = 1f;

        [SerializeField, Range(0f, 0.3f), Tooltip("Random volume variation (+/-)")]
        private float _volumeVariation = 0.1f;

        [SerializeField, Range(0.5f, 2f), Tooltip("Base pitch for this clip group")]
        private float _basePitch = 1f;

        [SerializeField, Range(0f, 0.2f), Tooltip("Random pitch variation (+/-)")]
        private float _pitchVariation = 0.05f;

        /// <summary>
        /// Get a random clip from this group
        /// </summary>
        public AudioClip GetRandomClip()
        {
            if (_clips == null || _clips.Length == 0)
            {
                Debug.LogWarning($"[SFXClipGroup] No clips in group: {name}");
                return null;
            }

            return _clips[Random.Range(0, _clips.Length)];
        }

        /// <summary>
        /// Get a random volume with variation applied
        /// </summary>
        public float GetRandomVolume()
        {
            return _baseVolume + Random.Range(-_volumeVariation, _volumeVariation);
        }

        /// <summary>
        /// Get a random pitch with variation applied
        /// </summary>
        public float GetRandomPitch()
        {
            return _basePitch + Random.Range(-_pitchVariation, _pitchVariation);
        }

        /// <summary>
        /// Number of clips in this group
        /// </summary>
        public int ClipCount => _clips?.Length ?? 0;

        /// <summary>
        /// Base volume setting
        /// </summary>
        public float BaseVolume => _baseVolume;
    }
}
