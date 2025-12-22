using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TecVooDoo.DontLoseYourHead.Editor
{
    /// <summary>
    /// Editor utility to import and filter words from words_alpha.txt into WordListSO assets
    /// </summary>
    public class WordBankImporter : EditorWindow
    {
        private const string SOURCE_FILE_PATH = "Assets/DLYH/Data/WordLists/words_alpha.txt";
        private const string OUTPUT_FOLDER = "Assets/DLYH/ScriptableObjects/Words";

        private static readonly int[] WORD_LENGTHS = { 3, 4, 5, 6 };

        // Banned words - drugs, profanity, slurs, and other inappropriate content
        private static readonly HashSet<string> BANNED_WORDS = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            // Drugs
            "heroin", "cocaine", "meth", "crack", "weed", "opium", "morphine",
            "ecstasy", "molly", "dope", "smack", "coke",
            // Add more banned words here as needed
        };

        private string _statusMessage = "Ready to import words";
        private Vector2 _scrollPosition;

        [MenuItem("DLYH/Tools/Word Bank Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<WordBankImporter>("Word Bank Importer");
            window.minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            GUILayout.Label("Word Bank Importer", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will read words_alpha.txt and create filtered WordListSO assets for 3, 4, 5, and 6-letter words.",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Show source file path
            EditorGUILayout.LabelField("Source File:", SOURCE_FILE_PATH);
            EditorGUILayout.LabelField("Output Folder:", OUTPUT_FOLDER);

            GUILayout.Space(10);

            // Check if source file exists
            bool sourceFileExists = File.Exists(SOURCE_FILE_PATH);
            if (!sourceFileExists)
            {
                EditorGUILayout.HelpBox(
                    "Source file not found!\n\nExpected at: " + SOURCE_FILE_PATH + "\n\nPlease download words_alpha.txt",
                    MessageType.Error
                );
            }

            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(!sourceFileExists);
            if (GUILayout.Button("Import and Filter Words", GUILayout.Height(40)))
            {
                ImportWords();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            // Status message area
            EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));
            EditorGUILayout.TextArea(_statusMessage, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void ImportWords()
        {
            _statusMessage = "Starting import...\n";
            Repaint();

            try
            {
                // Ensure output folder exists
                if (!Directory.Exists(OUTPUT_FOLDER))
                {
                    Directory.CreateDirectory(OUTPUT_FOLDER);
                    AssetDatabase.Refresh();
                    _statusMessage += "Created output folder: " + OUTPUT_FOLDER + "\n";
                }

                // Read all words from source file using ASCII encoding
                _statusMessage += "Reading words from: " + SOURCE_FILE_PATH + "\n";
                string[] allWords = File.ReadAllLines(SOURCE_FILE_PATH, Encoding.ASCII);
                _statusMessage += "Total words loaded: " + allWords.Length.ToString("N0") + "\n\n";

                // Filter and create assets for each word length
                foreach (int length in WORD_LENGTHS)
                {
                    List<string> filteredWords = FilterWordsByLength(allWords, length);
                    _statusMessage += length.ToString() + "-letter words: " + filteredWords.Count.ToString("N0") + "\n";

                    CreateOrUpdateWordListAsset(filteredWords, length);
                }

                _statusMessage += "\nImport completed successfully!\n";
                _statusMessage += "Word list assets created in: " + OUTPUT_FOLDER;

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "Import Complete",
                    "Word lists have been successfully imported!",
                    "OK"
                );
            }
            catch (System.Exception ex)
            {
                _statusMessage += "\nERROR: " + ex.Message + "\n";
                Debug.LogError("[WordBankImporter] Import failed: " + ex.Message);

                EditorUtility.DisplayDialog(
                    "Import Failed",
                    "An error occurred during import:\n" + ex.Message,
                    "OK"
                );
            }

            Repaint();
        }

        private List<string> FilterWordsByLength(string[] allWords, int length)
        {
            var filtered = new List<string>();
            int bannedCount = 0;

            foreach (string word in allWords)
            {
                // Skip empty lines and trim whitespace
                string trimmedWord = word.Trim();
                if (string.IsNullOrWhiteSpace(trimmedWord))
                    continue;

                // Filter by length and only alphabetic characters
                if (trimmedWord.Length == length && IsAlphabetic(trimmedWord))
                {
                    // Skip banned words
                    if (BANNED_WORDS.Contains(trimmedWord))
                    {
                        bannedCount++;
                        continue;
                    }

                    // Convert to uppercase for consistency
                    filtered.Add(trimmedWord.ToUpper());
                }
            }

            if (bannedCount > 0)
            {
                _statusMessage += "  (Filtered " + bannedCount + " banned words)\n";
            }

            // Remove duplicates and sort
            filtered = filtered.Distinct().OrderBy(w => w).ToList();

            return filtered;
        }

        private bool IsAlphabetic(string word)
        {
            foreach (char c in word)
            {
                if (!char.IsLetter(c))
                    return false;
            }
            return true;
        }

        private void CreateOrUpdateWordListAsset(List<string> words, int length)
        {
            string assetPath = OUTPUT_FOLDER + "/" + length.ToString() + "LetterWords.asset";

            // Try to load existing asset
            var wordList = AssetDatabase.LoadAssetAtPath<Core.WordListSO>(assetPath);

            if (wordList == null)
            {
                // Create new asset
                wordList = ScriptableObject.CreateInstance<Core.WordListSO>();
                AssetDatabase.CreateAsset(wordList, assetPath);
                _statusMessage += "  Created new asset: " + assetPath + "\n";
            }
            else
            {
                _statusMessage += "  Updated existing asset: " + assetPath + "\n";
            }

            // Set the words and length
            wordList.SetWords(words, length);

            // Mark as dirty to ensure it saves
            EditorUtility.SetDirty(wordList);
        }
    }
}
