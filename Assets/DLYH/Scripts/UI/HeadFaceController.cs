// HeadFaceController.cs
// Controls the facial expression on guillotine heads based on game state
// Created: December 18, 2025

using UnityEngine;
using UnityEngine.UI;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Controls the facial expression displayed on a guillotine head.
    /// Expressions change based on the owner's danger level and opponent's danger level.
    /// </summary>
    public class HeadFaceController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Face Image")]
        [SerializeField, Tooltip("The Image component that displays the face")]
        private Image _faceImage;

        [Header("Direction Settings")]
        [SerializeField, Tooltip("True if this head should look left (face's perspective), false for right")]
        private bool _lookLeft = true;

        [Header("Faces - Positive States (when doing well / opponent struggling)")]
        [SerializeField] private Sprite _happyFace;
        [SerializeField] private Sprite _smugFace;
        [SerializeField] private Sprite _evilSmileFace;

        [Header("Faces - Neutral States")]
        [SerializeField] private Sprite _neutralFace;
        [SerializeField] private Sprite _concernedFace;

        [Header("Faces - Negative States (when in danger)")]
        [SerializeField] private Sprite _worriedFace;
        [SerializeField] private Sprite _scaredFace;
        [SerializeField] private Sprite _terrorFace;

        [Header("Faces - Execution")]
        [SerializeField] private Sprite _horrorFace;
        [SerializeField] private Sprite _victoryFace;

        [Header("Thresholds (percentage of miss limit)")]
        [SerializeField, Range(0f, 1f)] private float _concernedThreshold = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _worriedThreshold = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _scaredThreshold = 0.75f;
        [SerializeField, Range(0f, 1f)] private float _terrorThreshold = 0.9f;

        #endregion

        #region Private Fields

        private bool _isInitialized;
        private float _ownDangerPercent;
        private float _opponentDangerPercent;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the face controller.
        /// </summary>
        /// <param name="lookLeft">True if face should look left (face's perspective)</param>
        public void Initialize(bool lookLeft)
        {
            _lookLeft = lookLeft;
            _isInitialized = true;

            // Apply horizontal flip if needed
            UpdateFaceDirection();

            // Start with neutral/happy face
            SetFace(_happyFace ?? _neutralFace);
        }

        /// <summary>
        /// Update the face based on both players' danger levels.
        /// </summary>
        /// <param name="ownMisses">This head's owner's current misses</param>
        /// <param name="ownMissLimit">This head's owner's miss limit</param>
        /// <param name="opponentMisses">Opponent's current misses</param>
        /// <param name="opponentMissLimit">Opponent's miss limit</param>
        public void UpdateFace(int ownMisses, int ownMissLimit, int opponentMisses, int opponentMissLimit)
        {
            if (!_isInitialized) return;

            _ownDangerPercent = ownMissLimit > 0 ? (float)ownMisses / ownMissLimit : 0f;
            _opponentDangerPercent = opponentMissLimit > 0 ? (float)opponentMisses / opponentMissLimit : 0f;

            Sprite newFace = DetermineFace();
            SetFace(newFace);
        }

        /// <summary>
        /// Set the execution face - horror for the one being executed, evil smile for the winner.
        /// </summary>
        /// <param name="isBeingExecuted">True if this head is being executed</param>
        public void SetExecutionFace(bool isBeingExecuted)
        {
            if (isBeingExecuted)
            {
                SetFace(_horrorFace ?? _terrorFace);
            }
            else
            {
                SetFace(_victoryFace ?? _evilSmileFace ?? _smugFace);
            }
        }

        /// <summary>
        /// Reset to default happy face.
        /// </summary>
        public void ResetFace()
        {
            SetFace(_happyFace ?? _neutralFace);
        }

        #endregion

        #region Private Methods

        private Sprite DetermineFace()
        {
            // If we're in high danger, show fear regardless of opponent state
            if (_ownDangerPercent >= _terrorThreshold)
            {
                return _terrorFace ?? _scaredFace ?? _worriedFace;
            }
            if (_ownDangerPercent >= _scaredThreshold)
            {
                return _scaredFace ?? _worriedFace;
            }
            if (_ownDangerPercent >= _worriedThreshold)
            {
                return _worriedFace ?? _concernedFace;
            }
            if (_ownDangerPercent >= _concernedThreshold)
            {
                return _concernedFace ?? _neutralFace;
            }

            // If opponent is in danger and we're safe, show positive emotions
            if (_opponentDangerPercent >= _terrorThreshold)
            {
                return _evilSmileFace ?? _smugFace ?? _happyFace;
            }
            if (_opponentDangerPercent >= _scaredThreshold)
            {
                return _smugFace ?? _happyFace;
            }
            if (_opponentDangerPercent >= _worriedThreshold)
            {
                return _happyFace ?? _neutralFace;
            }

            // Default to neutral/slightly happy
            return _neutralFace ?? _happyFace;
        }

        private void SetFace(Sprite face)
        {
            if (_faceImage != null && face != null)
            {
                _faceImage.sprite = face;
            }
        }

        private void UpdateFaceDirection()
        {
            if (_faceImage == null) return;

            // Flip the face horizontally if needed
            // If lookLeft is true, we want normal scale (face looking left from face's perspective)
            // If lookLeft is false, flip to look right
            Vector3 scale = _faceImage.transform.localScale;
            scale.x = _lookLeft ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            _faceImage.transform.localScale = scale;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        [ContextMenu("Test - Show Happy")]
        private void TestHappy() => SetFace(_happyFace);

        [ContextMenu("Test - Show Worried")]
        private void TestWorried() => SetFace(_worriedFace);

        [ContextMenu("Test - Show Terror")]
        private void TestTerror() => SetFace(_terrorFace);

        [ContextMenu("Test - Show Horror (Execution)")]
        private void TestHorror() => SetFace(_horrorFace);

        [ContextMenu("Test - Show Evil Smile")]
        private void TestEvil() => SetFace(_evilSmileFace);
#endif

        #endregion
    }
}
