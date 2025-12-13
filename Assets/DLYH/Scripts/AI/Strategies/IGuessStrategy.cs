// IGuessStrategy.cs
// Interface for AI guess strategies
// Created: December 13, 2025
// Developer: TecVooDoo LLC

using System.Collections.Generic;

namespace DLYH.AI.Strategies
{
    /// <summary>
    /// Represents the type of guess an AI can make.
    /// </summary>
    public enum GuessType
    {
        Letter,
        Coordinate,
        Word
    }

    /// <summary>
    /// Result of a strategy evaluation containing the recommended guess.
    /// </summary>
    public struct GuessRecommendation
    {
        /// <summary>
        /// The type of guess being recommended.
        /// </summary>
        public GuessType Type;

        /// <summary>
        /// For Letter guesses: the letter to guess.
        /// For Word guesses: not used (see WordGuess).
        /// </summary>
        public char Letter;

        /// <summary>
        /// For Coordinate guesses: the row to guess (0-indexed).
        /// </summary>
        public int Row;

        /// <summary>
        /// For Coordinate guesses: the column to guess (0-indexed).
        /// </summary>
        public int Col;

        /// <summary>
        /// For Word guesses: the complete word to guess.
        /// </summary>
        public string WordGuess;

        /// <summary>
        /// For Word guesses: which word pattern index this guess is for (0-indexed).
        /// </summary>
        public int WordIndex;

        /// <summary>
        /// Confidence score for this recommendation (0-1).
        /// Higher = more confident this is a good guess.
        /// </summary>
        public float Confidence;

        /// <summary>
        /// Whether this recommendation is valid/usable.
        /// </summary>
        public bool IsValid;

        /// <summary>
        /// Creates a letter guess recommendation.
        /// </summary>
        public static GuessRecommendation CreateLetterGuess(char letter, float confidence)
        {
            return new GuessRecommendation
            {
                Type = GuessType.Letter,
                Letter = char.ToUpper(letter),
                Row = -1,
                Col = -1,
                WordGuess = null,
                WordIndex = -1,
                Confidence = confidence,
                IsValid = true
            };
        }

        /// <summary>
        /// Creates a coordinate guess recommendation.
        /// </summary>
        public static GuessRecommendation CreateCoordinateGuess(int row, int col, float confidence)
        {
            return new GuessRecommendation
            {
                Type = GuessType.Coordinate,
                Letter = '\0',
                Row = row,
                Col = col,
                WordGuess = null,
                WordIndex = -1,
                Confidence = confidence,
                IsValid = true
            };
        }

        /// <summary>
        /// Creates a word guess recommendation.
        /// </summary>
        public static GuessRecommendation CreateWordGuess(string word, int wordIndex, float confidence)
        {
            return new GuessRecommendation
            {
                Type = GuessType.Word,
                Letter = '\0',
                Row = -1,
                Col = -1,
                WordGuess = word != null ? word.ToUpper() : null,
                WordIndex = wordIndex,
                Confidence = confidence,
                IsValid = !string.IsNullOrEmpty(word)
            };
        }

        /// <summary>
        /// Creates an invalid/empty recommendation (strategy has no suggestion).
        /// </summary>
        public static GuessRecommendation CreateInvalid()
        {
            return new GuessRecommendation
            {
                Type = GuessType.Letter,
                Letter = '\0',
                Row = -1,
                Col = -1,
                WordGuess = null,
                WordIndex = -1,
                Confidence = 0f,
                IsValid = false
            };
        }

        /// <summary>
        /// Returns a string representation for debugging.
        /// </summary>
        public override string ToString()
        {
            if (!IsValid)
            {
                return "Invalid recommendation";
            }

            switch (Type)
            {
                case GuessType.Letter:
                    return string.Format("Letter '{0}' (confidence: {1:P0})", Letter, Confidence);
                case GuessType.Coordinate:
                    return string.Format("Coordinate ({0},{1}) (confidence: {2:P0})", Row, Col, Confidence);
                case GuessType.Word:
                    return string.Format("Word '{0}' for pattern {1} (confidence: {2:P0})", WordGuess, WordIndex, Confidence);
                default:
                    return "Unknown guess type";
            }
        }
    }

    /// <summary>
    /// Data passed to strategies for evaluation.
    /// Contains all information the AI knows about the current game state.
    /// </summary>
    public class AIGameState
    {
        /// <summary>
        /// Size of the opponent's grid (e.g., 8 for 8x8).
        /// </summary>
        public int GridSize;

        /// <summary>
        /// Number of words the opponent has.
        /// </summary>
        public int WordCount;

        /// <summary>
        /// Letters that have already been guessed (hit or miss).
        /// </summary>
        public HashSet<char> GuessedLetters;

        /// <summary>
        /// Letters that were hits (exist in opponent's words).
        /// </summary>
        public HashSet<char> HitLetters;

        /// <summary>
        /// Coordinates that have already been guessed.
        /// </summary>
        public HashSet<(int row, int col)> GuessedCoordinates;

        /// <summary>
        /// Coordinates that were hits (contain letters).
        /// </summary>
        public HashSet<(int row, int col)> HitCoordinates;

        /// <summary>
        /// Current word patterns showing revealed letters.
        /// Each string uses '_' for unknown letters, actual letters for known.
        /// Example: "_A_E" for a 4-letter word with A and E revealed.
        /// </summary>
        public List<string> WordPatterns;

        /// <summary>
        /// Which word patterns have been completely solved.
        /// </summary>
        public List<bool> WordsSolved;

        /// <summary>
        /// The word bank to check valid words against.
        /// </summary>
        public HashSet<string> WordBank;

        /// <summary>
        /// Current AI skill level (0-1).
        /// </summary>
        public float SkillLevel;

        /// <summary>
        /// Current grid fill ratio (letters / total cells).
        /// </summary>
        public float FillRatio;

        /// <summary>
        /// Creates a new empty game state.
        /// </summary>
        public AIGameState()
        {
            GuessedLetters = new HashSet<char>();
            HitLetters = new HashSet<char>();
            GuessedCoordinates = new HashSet<(int row, int col)>();
            HitCoordinates = new HashSet<(int row, int col)>();
            WordPatterns = new List<string>();
            WordsSolved = new List<bool>();
            WordBank = new HashSet<string>();
        }

        /// <summary>
        /// Gets the count of unsolved words.
        /// </summary>
        public int UnsolvedWordCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < WordsSolved.Count; i++)
                {
                    if (!WordsSolved[i])
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Gets unguessed letters (A-Z minus guessed).
        /// </summary>
        public HashSet<char> GetUnguessedLetters()
        {
            HashSet<char> unguessed = new HashSet<char>();

            for (char c = 'A'; c <= 'Z'; c++)
            {
                if (!GuessedLetters.Contains(c))
                {
                    unguessed.Add(c);
                }
            }

            return unguessed;
        }

        /// <summary>
        /// Gets unguessed coordinates.
        /// </summary>
        public List<(int row, int col)> GetUnguessedCoordinates()
        {
            List<(int row, int col)> unguessed = new List<(int, int)>();

            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    if (!GuessedCoordinates.Contains((row, col)))
                    {
                        unguessed.Add((row, col));
                    }
                }
            }

            return unguessed;
        }
    }

    /// <summary>
    /// Interface for AI guess strategies.
    /// Each strategy evaluates the game state and recommends a guess.
    /// </summary>
    public interface IGuessStrategy
    {
        /// <summary>
        /// The type of guess this strategy produces.
        /// </summary>
        GuessType StrategyType { get; }

        /// <summary>
        /// Evaluates the current game state and returns a guess recommendation.
        /// </summary>
        /// <param name="state">Current game state information</param>
        /// <returns>A guess recommendation (check IsValid before using)</returns>
        GuessRecommendation Evaluate(AIGameState state);
    }
}
