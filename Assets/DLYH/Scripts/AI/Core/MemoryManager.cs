// MemoryManager.cs
// Skill-based memory filtering for AI opponent
// Created: December 13, 2025
// Developer: TecVooDoo LLC

using System.Collections.Generic;
using UnityEngine;
using DLYH.AI.Config;

namespace DLYH.AI.Core
{
    /// <summary>
    /// Manages AI memory with skill-based filtering.
    /// 
    /// Higher skill AI has perfect recall of all information.
    /// Lower skill AI may "forget" older information, making it feel more human.
    /// 
    /// This applies to:
    /// - Known hit coordinates
    /// - Revealed letters
    /// - Pattern information
    /// </summary>
    public class MemoryManager
    {
        // ============================================================
        // CONFIGURATION
        // ============================================================

        private readonly ExecutionerConfigSO _config;

        // ============================================================
        // MEMORY STORAGE
        // ============================================================

        // All known hits (full memory - never actually deleted)
        private readonly List<(int row, int col)> _allKnownHits;

        // All revealed letters (full memory)
        private readonly HashSet<char> _allRevealedLetters;

        // Timestamps for when information was learned (for age-based forgetting)
        private readonly Dictionary<(int row, int col), int> _hitLearnedAtTurn;
        private readonly Dictionary<char, int> _letterLearnedAtTurn;

        // Current turn counter
        private int _currentTurn;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        /// <summary>
        /// Creates a new MemoryManager.
        /// </summary>
        /// <param name="config">Configuration ScriptableObject</param>
        public MemoryManager(ExecutionerConfigSO config)
        {
            _config = config;

            _allKnownHits = new List<(int row, int col)>();
            _allRevealedLetters = new HashSet<char>();
            _hitLearnedAtTurn = new Dictionary<(int row, int col), int>();
            _letterLearnedAtTurn = new Dictionary<char, int>();
            _currentTurn = 0;
        }

        // ============================================================
        // PUBLIC METHODS - RECORDING
        // ============================================================

        /// <summary>
        /// Records a coordinate hit to memory.
        /// </summary>
        /// <param name="row">Row of the hit</param>
        /// <param name="col">Column of the hit</param>
        public void RecordHit(int row, int col)
        {
            (int row, int col) coord = (row, col);

            if (!_hitLearnedAtTurn.ContainsKey(coord))
            {
                _allKnownHits.Add(coord);
                _hitLearnedAtTurn[coord] = _currentTurn;
            }
        }

        /// <summary>
        /// Records a revealed letter to memory.
        /// </summary>
        /// <param name="letter">The letter that was revealed</param>
        public void RecordRevealedLetter(char letter)
        {
            char upperLetter = char.ToUpper(letter);

            if (!_allRevealedLetters.Contains(upperLetter))
            {
                _allRevealedLetters.Add(upperLetter);
                _letterLearnedAtTurn[upperLetter] = _currentTurn;
            }
        }

        /// <summary>
        /// Advances to the next turn. Call at the start of each AI turn.
        /// </summary>
        public void AdvanceTurn()
        {
            _currentTurn++;
        }

        /// <summary>
        /// Resets all memory for a new game.
        /// </summary>
        public void Reset()
        {
            _allKnownHits.Clear();
            _allRevealedLetters.Clear();
            _hitLearnedAtTurn.Clear();
            _letterLearnedAtTurn.Clear();
            _currentTurn = 0;
        }

        // ============================================================
        // PUBLIC METHODS - RETRIEVAL WITH SKILL FILTER
        // ============================================================

        /// <summary>
        /// Gets known hits filtered by skill level.
        /// Lower skill may "forget" older hits.
        /// </summary>
        /// <param name="skillLevel">Current AI skill level (0-1)</param>
        /// <returns>HashSet of remembered hit coordinates</returns>
        public HashSet<(int row, int col)> GetEffectiveKnownHits(float skillLevel)
        {
            // High skill = perfect memory
            if (skillLevel >= 0.8f)
            {
                return new HashSet<(int row, int col)>(_allKnownHits);
            }

            HashSet<(int row, int col)> effectiveHits = new HashSet<(int row, int col)>();
            float forgetChance = _config.GetForgetChanceForSkill(skillLevel);
            int alwaysRememberCount = _config.AlwaysRememberRecent;

            // Process hits from oldest to newest
            for (int i = 0; i < _allKnownHits.Count; i++)
            {
                (int row, int col) hit = _allKnownHits[i];

                // Always remember the most recent N hits
                int hitsFromEnd = _allKnownHits.Count - 1 - i;
                if (hitsFromEnd < alwaysRememberCount)
                {
                    effectiveHits.Add(hit);
                    continue;
                }

                // Chance to forget older hits
                if (Random.value > forgetChance)
                {
                    effectiveHits.Add(hit);
                }
            }

            return effectiveHits;
        }

        /// <summary>
        /// Gets revealed letters filtered by skill level.
        /// Lower skill may "forget" older letter discoveries.
        /// </summary>
        /// <param name="skillLevel">Current AI skill level (0-1)</param>
        /// <returns>HashSet of remembered letters</returns>
        public HashSet<char> GetEffectiveRevealedLetters(float skillLevel)
        {
            // High skill = perfect memory
            if (skillLevel >= 0.8f)
            {
                return new HashSet<char>(_allRevealedLetters);
            }

            HashSet<char> effectiveLetters = new HashSet<char>();
            float forgetChance = _config.GetForgetChanceForSkill(skillLevel);

            // Sort letters by when they were learned
            List<char> lettersByAge = new List<char>(_allRevealedLetters);
            lettersByAge.Sort((a, b) =>
            {
                int turnA = _letterLearnedAtTurn.ContainsKey(a) ? _letterLearnedAtTurn[a] : 0;
                int turnB = _letterLearnedAtTurn.ContainsKey(b) ? _letterLearnedAtTurn[b] : 0;
                return turnA.CompareTo(turnB);
            });

            int alwaysRememberCount = _config.AlwaysRememberRecent;

            for (int i = 0; i < lettersByAge.Count; i++)
            {
                char letter = lettersByAge[i];

                // Always remember the most recent N letters
                int lettersFromEnd = lettersByAge.Count - 1 - i;
                if (lettersFromEnd < alwaysRememberCount)
                {
                    effectiveLetters.Add(letter);
                    continue;
                }

                // Chance to forget older letters
                if (Random.value > forgetChance)
                {
                    effectiveLetters.Add(letter);
                }
            }

            return effectiveLetters;
        }

        /// <summary>
        /// Gets all known hits without any filtering (for debugging or validation).
        /// </summary>
        /// <returns>HashSet of all hit coordinates</returns>
        public HashSet<(int row, int col)> GetAllKnownHits()
        {
            return new HashSet<(int row, int col)>(_allKnownHits);
        }

        /// <summary>
        /// Gets all revealed letters without any filtering.
        /// </summary>
        /// <returns>HashSet of all revealed letters</returns>
        public HashSet<char> GetAllRevealedLetters()
        {
            return new HashSet<char>(_allRevealedLetters);
        }

        // ============================================================
        // PUBLIC METHODS - QUERIES
        // ============================================================

        /// <summary>
        /// Checks if a coordinate is known to be a hit (with skill-based memory).
        /// </summary>
        /// <param name="row">Row to check</param>
        /// <param name="col">Column to check</param>
        /// <param name="skillLevel">Current AI skill level</param>
        /// <returns>True if the AI "remembers" this hit</returns>
        public bool RemembersHit(int row, int col, float skillLevel)
        {
            HashSet<(int row, int col)> effectiveHits = GetEffectiveKnownHits(skillLevel);
            return effectiveHits.Contains((row, col));
        }

        /// <summary>
        /// Checks if a letter has been revealed (with skill-based memory).
        /// </summary>
        /// <param name="letter">Letter to check</param>
        /// <param name="skillLevel">Current AI skill level</param>
        /// <returns>True if the AI "remembers" this letter</returns>
        public bool RemembersLetter(char letter, float skillLevel)
        {
            HashSet<char> effectiveLetters = GetEffectiveRevealedLetters(skillLevel);
            return effectiveLetters.Contains(char.ToUpper(letter));
        }

        /// <summary>
        /// Gets the total count of known hits.
        /// </summary>
        public int TotalHitCount => _allKnownHits.Count;

        /// <summary>
        /// Gets the total count of revealed letters.
        /// </summary>
        public int TotalRevealedLetterCount => _allRevealedLetters.Count;

        /// <summary>
        /// Gets the current turn number.
        /// </summary>
        public int CurrentTurn => _currentTurn;

        // ============================================================
        // DEBUG
        // ============================================================

        /// <summary>
        /// Gets a debug summary of memory state.
        /// </summary>
        /// <param name="skillLevel">Skill level to use for effective memory calculation</param>
        /// <returns>Multi-line debug string</returns>
        public string GetDebugSummary(float skillLevel)
        {
            HashSet<(int row, int col)> effectiveHits = GetEffectiveKnownHits(skillLevel);
            HashSet<char> effectiveLetters = GetEffectiveRevealedLetters(skillLevel);

            return string.Format(
                "Turn: {0}\nTotal Hits: {1} (remembers {2})\nTotal Letters: {3} (remembers {4})\nForget Chance: {5:P1}",
                _currentTurn,
                _allKnownHits.Count,
                effectiveHits.Count,
                _allRevealedLetters.Count,
                effectiveLetters.Count,
                _config.GetForgetChanceForSkill(skillLevel));
        }
    }
}
