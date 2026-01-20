// JsonParsingUtility.cs
// Unified JSON field extraction utilities
// Created: January 19, 2026
// Developer: TecVooDoo LLC
//
// Consolidates duplicate JSON parsing code from:
// - UIFlowController.cs
// - GameSessionService.cs
// - GameStateSynchronizer.cs
// - PlayerService.cs

using System;
using System.Collections.Generic;

namespace DLYH.Core.Utilities
{
    /// <summary>
    /// Static utility class for extracting fields from JSON strings.
    /// Used for manual JSON parsing where Unity's JsonUtility doesn't handle nested objects well.
    /// </summary>
    public static class JsonParsingUtility
    {
        /// <summary>
        /// Extracts a string field from JSON.
        /// Handles whitespace, null values, and quoted strings.
        /// </summary>
        /// <param name="json">JSON string to parse</param>
        /// <param name="fieldName">Field name to extract</param>
        /// <returns>Field value or null if not found</returns>
        public static string ExtractStringField(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json)) return null;

            string pattern = $"\"{fieldName}\":";
            int start = json.IndexOf(pattern);
            if (start < 0) return null;

            start += pattern.Length;

            // Skip whitespace
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;

            // Check for null
            if (start + 4 <= json.Length && json.Substring(start, 4) == "null") return null;

            // Expect opening quote
            if (start >= json.Length || json[start] != '"') return null;
            start++; // skip the opening quote

            int end = json.IndexOf("\"", start);
            if (end < 0) return null;

            return json.Substring(start, end - start);
        }

        /// <summary>
        /// Extracts an integer field from JSON.
        /// </summary>
        /// <param name="json">JSON string to parse</param>
        /// <param name="fieldName">Field name to extract</param>
        /// <param name="defaultValue">Value to return if field not found</param>
        /// <returns>Field value or defaultValue if not found</returns>
        public static int ExtractIntField(string json, string fieldName, int defaultValue = 0)
        {
            if (string.IsNullOrEmpty(json)) return defaultValue;

            string pattern = $"\"{fieldName}\":";
            int start = json.IndexOf(pattern);
            if (start < 0) return defaultValue;

            start += pattern.Length;

            // Skip whitespace
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;

            int end = start;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-'))
            {
                end++;
            }

            if (end == start) return defaultValue;

            if (int.TryParse(json.Substring(start, end - start), out int result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Extracts a boolean field from JSON.
        /// </summary>
        /// <param name="json">JSON string to parse</param>
        /// <param name="fieldName">Field name to extract</param>
        /// <returns>Field value or false if not found</returns>
        public static bool ExtractBoolField(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json)) return false;

            string pattern = $"\"{fieldName}\":";
            int start = json.IndexOf(pattern);
            if (start < 0) return false;

            start += pattern.Length;

            // Skip whitespace
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;

            if (start + 4 > json.Length) return false;
            return json.Substring(start, 4) == "true";
        }

        /// <summary>
        /// Extracts a nested object field from JSON as a raw JSON string.
        /// </summary>
        /// <param name="json">JSON string to parse</param>
        /// <param name="fieldName">Field name to extract</param>
        /// <returns>Raw JSON object string, "null" for null values, or null if not found</returns>
        public static string ExtractObjectField(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json)) return null;

            string pattern = $"\"{fieldName}\":";
            int start = json.IndexOf(pattern);
            if (start < 0) return null;
            start += pattern.Length;

            // Skip whitespace
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;

            if (start >= json.Length) return null;

            // Check for null value
            if (json[start] == 'n') return "null";

            // Expect opening brace for object
            if (json[start] != '{') return null;

            int depth = 0;
            int end = start;
            while (end < json.Length)
            {
                if (json[end] == '{') depth++;
                else if (json[end] == '}') depth--;
                if (depth == 0) break;
                end++;
            }
            return json.Substring(start, end - start + 1);
        }

        /// <summary>
        /// Extracts a string array field from JSON.
        /// </summary>
        /// <param name="json">JSON string to parse</param>
        /// <param name="fieldName">Field name to extract</param>
        /// <returns>Array of strings or empty array if not found</returns>
        public static string[] ExtractStringArray(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<string>();

            string pattern = $"\"{fieldName}\":[";
            int start = json.IndexOf(pattern);
            if (start < 0) return Array.Empty<string>();
            start += pattern.Length;

            int end = json.IndexOf("]", start);
            if (end < 0) return Array.Empty<string>();

            string content = json.Substring(start, end - start);
            if (string.IsNullOrWhiteSpace(content)) return Array.Empty<string>();

            List<string> result = new List<string>();
            int pos = 0;
            while (pos < content.Length)
            {
                int quoteStart = content.IndexOf("\"", pos);
                if (quoteStart < 0) break;
                int quoteEnd = content.IndexOf("\"", quoteStart + 1);
                if (quoteEnd < 0) break;
                result.Add(content.Substring(quoteStart + 1, quoteEnd - quoteStart - 1));
                pos = quoteEnd + 1;
            }
            return result.ToArray();
        }

        /// <summary>
        /// Extracts an integer array field from JSON.
        /// </summary>
        /// <param name="json">JSON string to parse</param>
        /// <param name="fieldName">Field name to extract</param>
        /// <returns>Array of integers or empty array if not found</returns>
        public static int[] ExtractIntArray(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<int>();

            string pattern = $"\"{fieldName}\":[";
            int start = json.IndexOf(pattern);
            if (start < 0) return Array.Empty<int>();
            start += pattern.Length;

            int end = json.IndexOf("]", start);
            if (end < 0) return Array.Empty<int>();

            string content = json.Substring(start, end - start);
            if (string.IsNullOrWhiteSpace(content)) return Array.Empty<int>();

            List<int> result = new List<int>();
            string[] parts = content.Split(',');
            foreach (string part in parts)
            {
                if (int.TryParse(part.Trim(), out int val))
                {
                    result.Add(val);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Extracts a coordinate pair array field from JSON.
        /// Expected format: [[row,col],[row,col],...]
        /// </summary>
        /// <param name="json">JSON string to parse</param>
        /// <param name="fieldName">Field name to extract</param>
        /// <returns>Array of coordinate pairs (row, col) or empty array if not found</returns>
        public static (int row, int col)[] ExtractCoordinateArray(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<(int, int)>();

            string pattern = $"\"{fieldName}\":[";
            int start = json.IndexOf(pattern);
            if (start < 0) return Array.Empty<(int, int)>();
            start += pattern.Length;

            // Find matching ]
            int depth = 1;
            int end = start;
            while (end < json.Length && depth > 0)
            {
                if (json[end] == '[') depth++;
                else if (json[end] == ']') depth--;
                end++;
            }
            end--;

            string content = json.Substring(start, end - start);
            if (string.IsNullOrWhiteSpace(content)) return Array.Empty<(int, int)>();

            List<(int row, int col)> result = new List<(int, int)>();

            // Parse [[row,col],[row,col],...]
            int pos = 0;
            while (pos < content.Length)
            {
                int innerStart = content.IndexOf("[", pos);
                if (innerStart < 0) break;
                int innerEnd = content.IndexOf("]", innerStart);
                if (innerEnd < 0) break;

                string pair = content.Substring(innerStart + 1, innerEnd - innerStart - 1);
                string[] parts = pair.Split(',');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0].Trim(), out int row) &&
                    int.TryParse(parts[1].Trim(), out int col))
                {
                    result.Add((row, col));
                }
                pos = innerEnd + 1;
            }

            return result.ToArray();
        }

        /// <summary>
        /// Extracts a revealed cells array field from JSON.
        /// Expected format: [{"row":0,"col":1,"letter":"A","isHit":true},...]
        /// </summary>
        /// <param name="json">JSON string to parse</param>
        /// <param name="fieldName">Field name to extract</param>
        /// <returns>Array of revealed cell data or empty array if not found</returns>
        public static (int row, int col, string letter, bool isHit)[] ExtractRevealedCellsArray(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<(int, int, string, bool)>();

            string pattern = $"\"{fieldName}\":[";
            int start = json.IndexOf(pattern);
            if (start < 0) return Array.Empty<(int, int, string, bool)>();
            start += pattern.Length;

            // Find matching ]
            int depth = 1;
            int end = start;
            while (end < json.Length && depth > 0)
            {
                if (json[end] == '[') depth++;
                else if (json[end] == ']') depth--;
                end++;
            }
            end--;

            string content = json.Substring(start, end - start);
            if (string.IsNullOrWhiteSpace(content)) return Array.Empty<(int, int, string, bool)>();

            List<(int row, int col, string letter, bool isHit)> result = new List<(int, int, string, bool)>();

            // Parse array of objects [{...},{...},...]
            int pos = 0;
            while (pos < content.Length)
            {
                int objStart = content.IndexOf("{", pos);
                if (objStart < 0) break;

                // Find matching }
                int objDepth = 1;
                int objEnd = objStart + 1;
                while (objEnd < content.Length && objDepth > 0)
                {
                    if (content[objEnd] == '{') objDepth++;
                    else if (content[objEnd] == '}') objDepth--;
                    objEnd++;
                }

                string objJson = content.Substring(objStart, objEnd - objStart);

                int row = ExtractIntField(objJson, "row");
                int col = ExtractIntField(objJson, "col");
                string letter = ExtractStringField(objJson, "letter") ?? "";
                bool isHit = ExtractBoolField(objJson, "isHit");

                result.Add((row, col, letter, isHit));
                pos = objEnd;
            }

            return result.ToArray();
        }
    }
}
