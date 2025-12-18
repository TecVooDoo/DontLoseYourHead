using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// Controls the visual display and animation of a guillotine assembly.
    /// Manages hash mark generation, blade position based on miss count,
    /// and game over animations.
    /// </summary>
    public class GuillotineDisplay : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Frame References")]
        [SerializeField] private RectTransform _leftPost;
        [SerializeField] private RectTransform _rightPost;
        [SerializeField] private RectTransform _topBeam;

        [Header("Blade References")]
        [SerializeField] private RectTransform _bladeGroup;
        [SerializeField] private Image _bladeImage;

        [Header("Head and Basket")]
        [SerializeField] private Image _headImage;
        [SerializeField] private RectTransform _headTransform;
        [SerializeField] private RectTransform _basket;
        [SerializeField] private RectTransform _lunette;

        [Header("Hash Marks")]
        [SerializeField] private RectTransform _hashMarksContainer;
        [SerializeField] private GameObject _hashMarkPrefab;

        [Header("Animation Settings")]
        [SerializeField] private float _bladeRaiseDuration = 0.3f;
        [SerializeField] private float _bladeDropDuration = 0.15f;
        [SerializeField] private float _headFallDuration = 0.4f;
        [SerializeField] private float _headBounceStrength = 0.3f;
        [SerializeField] private Ease _bladeRaiseEase = Ease.OutQuad;
        [SerializeField] private Ease _bladeDropEase = Ease.InQuad;

        [Header("Visual Settings")]
        [SerializeField] private Color _hashMarkColor = new Color(0.4f, 0.25f, 0.1f, 1f);
        [SerializeField] private float _hashMarkWidth = 4f;
        [SerializeField] private float _hashMarkLength = 15f;

        [Header("Face Controller")]
        [SerializeField, Tooltip("Controller for head facial expressions")]
        private HeadFaceController _faceController;
        [SerializeField, Tooltip("True if this head should look left (towards opponent on right)")]
        private bool _faceLooksLeft = true;
        #endregion

        #region Private Fields
        private int _missLimit;
        private int _currentMisses;
        private float _bladeStartY;
        private float _bladeEndY;
        private float _bladeTravel;
        private List<RectTransform> _hashMarks = new List<RectTransform>();
        private bool _isInitialized;
        private Color _playerColor;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes the guillotine display for a new game.
        /// Generates hash marks based on miss limit and sets head color.
        /// </summary>
        /// <param name="missLimit">Maximum misses before game over</param>
        /// <param name="playerColor">Color for the head (player's color)</param>
        public void Initialize(int missLimit, Color playerColor)
        {
            _missLimit = Mathf.Max(1, missLimit);
            _playerColor = playerColor;
            _currentMisses = 0;

            // Set head color
            if (_headImage != null)
            {
                _headImage.color = playerColor;
            }

            // Initialize face controller
            if (_faceController != null)
            {
                _faceController.Initialize(_faceLooksLeft);
            }

            // Calculate blade travel range
            CalculateBladeRange();

            // Generate hash marks
            GenerateHashMarks();

            // Reset blade to starting position (bottom)
            ResetBladePosition();

            _isInitialized = true;
        }

        /// <summary>
        /// Updates the display to reflect current miss count.
        /// Animates blade upward as misses increase.
        /// </summary>
        /// <param name="misses">Current number of misses</param>
        public void UpdateMissCount(int misses)
        {
            if (!_isInitialized) return;

            int previousMisses = _currentMisses;
            _currentMisses = Mathf.Clamp(misses, 0, _missLimit);

            if (_currentMisses != previousMisses)
            {
                AnimateBladeToPosition(_currentMisses);
            }
        }

        /// <summary>
        /// Updates the head face based on both players' danger levels.
        /// Call this after miss counts change.
        /// </summary>
        /// <param name="opponentMisses">Opponent's current misses</param>
        /// <param name="opponentMissLimit">Opponent's miss limit</param>
        public void UpdateFace(int opponentMisses, int opponentMissLimit)
        {
            if (_faceController != null)
            {
                _faceController.UpdateFace(_currentMisses, _missLimit, opponentMisses, opponentMissLimit);
            }
        }

        /// <summary>
        /// Sets the execution face (horror for executed, evil smile for winner).
        /// </summary>
        /// <param name="isBeingExecuted">True if this head is being executed</param>
        public void SetExecutionFace(bool isBeingExecuted)
        {
            if (_faceController != null)
            {
                _faceController.SetExecutionFace(isBeingExecuted);
            }
        }

        /// <summary>
        /// Plays the game over animation - blade drops and head falls into basket.
        /// Used when player loses by reaching miss limit.
        /// </summary>
        public void AnimateGameOver()
        {
            if (!_isInitialized) return;

            // Set horror face for execution
            SetExecutionFace(true);

            // Kill any existing tweens
            DOTween.Kill(_bladeGroup);
            DOTween.Kill(_headTransform);

            // Play execution sound (fast chop - sudden death from miss limit)
            DLYH.Audio.GuillotineAudioManager.ExecutionFast();

            // Sequence: blade drops, then head falls
            Sequence gameOverSequence = DOTween.Sequence();

            // Blade drops quickly to bottom
            if (_bladeGroup != null)
            {
                gameOverSequence.Append(
                    _bladeGroup.DOAnchorPosY(_bladeStartY, _bladeDropDuration)
                        .SetEase(_bladeDropEase)
                );
            }

            // Head shakes then falls into basket
            if (_headTransform != null && _basket != null)
            {
                // Shake the head briefly
                gameOverSequence.Append(
                    _headTransform.DOShakeAnchorPos(_headFallDuration * 0.3f, 5f, 20)
                );

                // Play head removed sound when head starts falling
                gameOverSequence.AppendCallback(() => DLYH.Audio.GuillotineAudioManager.HeadRemoved());

                // Head falls toward basket
                Vector2 basketPos = _basket.anchoredPosition;
                gameOverSequence.Append(
                    _headTransform.DOAnchorPos(basketPos, _headFallDuration)
                        .SetEase(Ease.InBack)
                );

                // Bounce in basket
                gameOverSequence.Append(
                    _headTransform.DOAnchorPosY(basketPos.y + 10f, 0.1f)
                        .SetEase(Ease.OutQuad)
                );
                gameOverSequence.Append(
                    _headTransform.DOAnchorPosY(basketPos.y, 0.1f)
                        .SetEase(Ease.InQuad)
                );
            }
        }

        /// <summary>
        /// Plays the defeat animation when opponent wins by finding all words.
        /// Blade rises to top, then drops for execution.
        /// </summary>
        public void AnimateDefeatByWordsFound()
        {
            if (!_isInitialized) return;

            // Set horror face for execution
            SetExecutionFace(true);

            // Kill any existing tweens
            DOTween.Kill(_bladeGroup);
            DOTween.Kill(_headTransform);

            // Play blade raise sound for the dramatic raise
            DLYH.Audio.GuillotineAudioManager.BladeRaise();

            Sequence defeatSequence = DOTween.Sequence();

            // First, raise blade to top (dramatic pause before execution)
            if (_bladeGroup != null)
            {
                defeatSequence.Append(
                    _bladeGroup.DOAnchorPosY(_bladeEndY, _bladeRaiseDuration * 2f)
                        .SetEase(Ease.OutQuad)
                );

                // Pause at top for dramatic effect
                defeatSequence.AppendInterval(0.5f);

                // Play slow/dramatic chop sound when blade drops
                defeatSequence.AppendCallback(() => DLYH.Audio.GuillotineAudioManager.ExecutionSlow());

                // Then drop!
                defeatSequence.Append(
                    _bladeGroup.DOAnchorPosY(_bladeStartY, _bladeDropDuration)
                        .SetEase(_bladeDropEase)
                );
            }

            // Head shakes then falls into basket
            if (_headTransform != null && _basket != null)
            {
                // Shake the head briefly
                defeatSequence.Append(
                    _headTransform.DOShakeAnchorPos(_headFallDuration * 0.3f, 5f, 20)
                );

                // Play head removed sound when head starts falling
                defeatSequence.AppendCallback(() => DLYH.Audio.GuillotineAudioManager.HeadRemoved());

                // Head falls toward basket
                Vector2 basketPos = _basket.anchoredPosition;
                defeatSequence.Append(
                    _headTransform.DOAnchorPos(basketPos, _headFallDuration)
                        .SetEase(Ease.InBack)
                );

                // Bounce in basket
                defeatSequence.Append(
                    _headTransform.DOAnchorPosY(basketPos.y + 10f, 0.1f)
                        .SetEase(Ease.OutQuad)
                );
                defeatSequence.Append(
                    _headTransform.DOAnchorPosY(basketPos.y, 0.1f)
                        .SetEase(Ease.InQuad)
                );
            }
        }

        /// <summary>
        /// Resets the guillotine to initial state for a new game.
        /// </summary>
        public void Reset()
        {
            _currentMisses = 0;
            ResetBladePosition();
            ResetHeadPosition();
        }
        #endregion

        #region Private Methods
        private void CalculateBladeRange()
        {
            if (_hashMarksContainer == null || _bladeGroup == null) return;

            // Convert HashMarksContainer corners to world space, then to BladeGroup's parent space
            // This handles different anchor configurations between the two objects

            // Get the parent transform (they share the same parent)
            RectTransform parentRect = _bladeGroup.parent as RectTransform;
            if (parentRect == null) return;

            // Get world corners of the HashMarksContainer
            Vector3[] containerCorners = new Vector3[4];
            _hashMarksContainer.GetWorldCorners(containerCorners);
            // corners[0] = bottom-left, corners[1] = top-left, corners[2] = top-right, corners[3] = bottom-right

            // Convert to parent's local space
            Vector3 bottomLocal = parentRect.InverseTransformPoint(containerCorners[0]);
            Vector3 topLocal = parentRect.InverseTransformPoint(containerCorners[1]);

            // BladeGroup has center anchor (0.5, 0.5), so its anchoredPosition.y is relative to parent center
            // We need the Y values where BladeGroup.anchoredPosition.y should be
            float parentHeight = parentRect.rect.height;
            float parentCenterY = 0f; // Center anchor means 0 is at center

            // Convert local positions to anchored position space (relative to center)
            _bladeStartY = bottomLocal.y;
            _bladeEndY = topLocal.y;

            _bladeTravel = _bladeEndY - _bladeStartY;

            Debug.Log($"[GuillotineDisplay] Container bottom: {bottomLocal.y}, top: {topLocal.y}");
            Debug.Log($"[GuillotineDisplay] Blade range: {_bladeStartY} to {_bladeEndY} (travel: {_bladeTravel})");
        }

        private void GenerateHashMarks()
        {
            // Clear existing hash marks
            foreach (var mark in _hashMarks)
            {
                if (mark != null)
                {
                    Destroy(mark.gameObject);
                }
            }
            _hashMarks.Clear();

            if (_hashMarksContainer == null || _missLimit <= 0) return;

            // Calculate spacing between hash marks
            // Hash marks evenly distributed, first mark above bottom (after 1 miss)
            // Bottom of container is 0 misses, no mark there
            float containerHeight = _hashMarksContainer.rect.height;
            float spacing = containerHeight / _missLimit;

            // Create hash marks for each miss level (1 to missLimit)
            // Mark i corresponds to blade position after i misses
            for (int i = 1; i <= _missLimit; i++)
            {
                GameObject hashMark = CreateHashMark();
                if (hashMark != null)
                {
                    RectTransform rt = hashMark.GetComponent<RectTransform>();
                    rt.SetParent(_hashMarksContainer, false);

                    // Position: i * spacing places mark at blade position for i misses
                    float yPos = spacing * i;
                    rt.anchoredPosition = new Vector2(0, yPos);

                    _hashMarks.Add(rt);
                }
            }

            Debug.Log($"[GuillotineDisplay] Generated {_missLimit} hash marks with spacing {spacing}");
        }

        private GameObject CreateHashMark()
        {
            // If prefab exists, use it
            if (_hashMarkPrefab != null)
            {
                return Instantiate(_hashMarkPrefab);
            }

            // Otherwise create a simple hash mark
            GameObject hashMark = new GameObject("HashMark");

            RectTransform rt = hashMark.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(_hashMarkLength, _hashMarkWidth);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = hashMark.AddComponent<Image>();
            img.color = _hashMarkColor;

            return hashMark;
        }

        private void AnimateBladeToPosition(int misses)
        {
            if (_bladeGroup == null) return;

            // Calculate target Y based on miss count
            float progress = (float)misses / _missLimit;
            float targetY = Mathf.Lerp(_bladeStartY, _bladeEndY, progress);

            // Kill any existing tween
            DOTween.Kill(_bladeGroup);

            // Play blade raise sound (rope stretch + blade movement)
            DLYH.Audio.GuillotineAudioManager.BladeRaise();

            // Animate to new position
            _bladeGroup.DOAnchorPosY(targetY, _bladeRaiseDuration)
                .SetEase(_bladeRaiseEase);
        }

        private void ResetBladePosition()
        {
            if (_bladeGroup != null)
            {
                DOTween.Kill(_bladeGroup);
                _bladeGroup.anchoredPosition = new Vector2(
                    _bladeGroup.anchoredPosition.x,
                    _bladeStartY
                );
            }
        }

        private void ResetHeadPosition()
        {
            if (_headTransform != null)
            {
                DOTween.Kill(_headTransform);
                // Head should be at its original position (set in prefab/scene)
                // We store and restore this if needed
            }
        }
        #endregion

        #region Editor Helpers
#if UNITY_EDITOR
        [ContextMenu("Test Initialize (20 misses)")]
        private void TestInitialize()
        {
            Initialize(20, Color.blue);
        }

        [ContextMenu("Test Add Miss")]
        private void TestAddMiss()
        {
            UpdateMissCount(_currentMisses + 1);
        }

        [ContextMenu("Test Game Over")]
        private void TestGameOver()
        {
            AnimateGameOver();
        }

        [ContextMenu("Test Reset")]
        private void TestReset()
        {
            Reset();
        }
#endif
        #endregion
    }
}
