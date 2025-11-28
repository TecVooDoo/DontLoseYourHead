using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

namespace TecVooDoo.DontLoseYourHead.UI
{
    /// <summary>
    /// UI controller for word placement screen
    /// Handles word entry with autocomplete, visual grid placement, and coordinate mode
    /// </summary>
    public class WordPlacementUI : MonoBehaviour
    {
        #region Serialized Fields
        [Title("Core References")]
        [Required]
        [SerializeField] private Core.GameStateMachine _gameStateMachine;
        
        [Required]
        [SerializeField] private Core.DifficultySO _playerDifficulty;
        
        [Required]
        [SerializeField] private Core.PlayerSO _playerData;
        
        [Title("Word Lists")]
        [Required]
        [SerializeField] private Core.WordListSO _wordList3Letter;
        
        [Required]
        [SerializeField] private Core.WordListSO _wordList4Letter;
        
        [Required]
        [SerializeField] private Core.WordListSO _wordList5Letter;
        
        [Required]
        [SerializeField] private Core.WordListSO _wordList6Letter;

        [Title("UI Elements - Header")]
        [Required]
        [SerializeField] private TextMeshProUGUI _playerNameText;

        [Title("UI Elements - Keyboard")]
        [Required]
        [SerializeField] private Transform _keyboardContainer;
        
        [SerializeField] private Button _letterButtonPrefab;

        [Title("UI Elements - Word Rows")]
        [Required]
        [SerializeField] private WordEntryRow[] _wordRows;

        [Title("UI Elements - Autocomplete")]
        [Required]
        [SerializeField] private GameObject _autocompletePanel;
        
        [Required]
        [SerializeField] private Transform _autocompleteContent;
        
        [SerializeField] private Button _autocompleteItemPrefab;

        [Title("UI Elements - Grid")]
        [Required]
        [SerializeField] private GridLayoutGroup _gridContainer;
        
        [SerializeField] private GridCellUI _gridCellPrefab;
        
        [Required]
        [SerializeField] private Button _randomPlacementButton;

        [Title("Settings")]
        [SerializeField] private int _maxAutocompleteResults = 10;
        
        [SerializeField] private Color _validPlacementColor = Color.green;
        
        [SerializeField] private Color _invalidPlacementColor = Color.red;
        
        [SerializeField] private Color _activeCellColor = Color.yellow;
        #endregion

        #region Private Fields
        private Dictionary<char, Button> _letterButtons = new Dictionary<char, Button>();
        private GridCellUI[,] _gridCells;
        private int _currentWordIndex = 0;
        private string _currentWordInput = "";
        private bool _isInCoordinateMode = false;
        private List<string> _autocompleteResults = new List<string>();
        private Core.Grid _grid;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            InitializeKeyboard();
            InitializeGrid();
            UpdatePlayerName();
            SetupWordRows();
            _autocompletePanel.SetActive(false);
            _randomPlacementButton.onClick.AddListener(OnRandomPlacementClicked);
        }

        private void OnDestroy()
        {
            _randomPlacementButton.onClick.RemoveListener(OnRandomPlacementClicked);
        }
        #endregion

        #region Initialization
        private void InitializeKeyboard()
        {
            string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            
            foreach (char letter in alphabet)
            {
                Button btn = Instantiate(_letterButtonPrefab, _keyboardContainer);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = letter.ToString();
                
                char capturedLetter = letter; // Capture for closure
                btn.onClick.AddListener(() => OnLetterClicked(capturedLetter));
                
                _letterButtons[letter] = btn;
            }
        }

        private void InitializeGrid()
        {
            int gridSize = _playerDifficulty.GridSize;
            _grid = new Core.Grid(gridSize);
            _gridCells = new GridCellUI[gridSize, gridSize];

            // Set grid layout constraints
            _gridContainer.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridContainer.constraintCount = gridSize;

            // Create grid cells
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    GridCellUI cell = Instantiate(_gridCellPrefab, _gridContainer.transform);
                    cell.Initialize(col, row);
                    
                    int capturedCol = col;
                    int capturedRow = row;
                    cell.OnCellClicked += () => OnGridCellClicked(capturedCol, capturedRow);
                    
                    _gridCells[col, row] = cell;
                }
            }
        }

        private void SetupWordRows()
        {
            int[] wordLengths = _playerDifficulty.RequiredWordLengths;
            
            for (int i = 0; i < _wordRows.Length && i < wordLengths.Length; i++)
            {
                int wordLength = wordLengths[i];
                _wordRows[i].Initialize(i + 1, wordLength);
                _wordRows[i].OnAcceptClicked += () => OnWordAccepted(i);
                _wordRows[i].OnDeleteClicked += () => OnWordDeleted(i);
                _wordRows[i].OnCoordinateModeClicked += () => OnCoordinateModeToggled(i);
            }

            // Hide unused rows if word count is 3
            if (wordLengths.Length == 3 && _wordRows.Length > 3)
            {
                for (int i = 3; i < _wordRows.Length; i++)
                {
                    // WordEntryRow is a serializable helper, not a MonoBehaviour; use its RowObject reference
                    if (_wordRows[i].RowObject != null)
                    {
                        _wordRows[i].RowObject.SetActive(false);
                    }
                }
            }
        }

        private void UpdatePlayerName()
        {
            _playerNameText.text = _playerData.PlayerName;
        }
        #endregion

        #region Letter Input
        private void OnLetterClicked(char letter)
        {
            if (_isInCoordinateMode)
            {
                Debug.Log("[WordPlacementUI] In coordinate mode, ignoring letter click");
                return;
            }

            WordEntryRow currentRow = _wordRows[_currentWordIndex];
            
            if (_currentWordInput.Length < currentRow.RequiredLength)
            {
                _currentWordInput += letter;
                currentRow.SetWord(_currentWordInput);
                UpdateAutocomplete();
            }
        }
        #endregion

        #region Autocomplete
        private void UpdateAutocomplete()
        {
            if (string.IsNullOrEmpty(_currentWordInput))
            {
                _autocompletePanel.SetActive(false);
                return;
            }

            Core.WordListSO wordList = GetWordListForCurrentRow();
            if (wordList == null)
            {
                _autocompletePanel.SetActive(false);
                return;
            }

            _autocompleteResults = wordList.Words
                .Where(w => w.StartsWith(_currentWordInput))
                .Take(_maxAutocompleteResults)
                .ToList();

            if (_autocompleteResults.Count == 0)
            {
                _autocompletePanel.SetActive(false);
                return;
            }

            // Clear existing items
            foreach (Transform child in _autocompleteContent)
            {
                Destroy(child.gameObject);
            }

            // Create new items
            foreach (string word in _autocompleteResults)
            {
                Button item = Instantiate(_autocompleteItemPrefab, _autocompleteContent);
                item.GetComponentInChildren<TextMeshProUGUI>().text = word;
                
                string capturedWord = word;
                item.onClick.AddListener(() => OnAutocompleteSelected(capturedWord));
            }

            _autocompletePanel.SetActive(true);
        }

        private void OnAutocompleteSelected(string word)
        {
            _currentWordInput = word;
            _wordRows[_currentWordIndex].SetWord(word);
            _autocompletePanel.SetActive(false);
        }

        private Core.WordListSO GetWordListForCurrentRow()
        {
            int requiredLength = _wordRows[_currentWordIndex].RequiredLength;
            
            return requiredLength switch
            {
                3 => _wordList3Letter,
                4 => _wordList4Letter,
                5 => _wordList5Letter,
                6 => _wordList6Letter,
                _ => null
            };
        }
        #endregion

        #region Word Row Actions
        private void OnWordAccepted(int rowIndex)
        {
            WordEntryRow row = _wordRows[rowIndex];
            string word = row.CurrentWord;

            if (string.IsNullOrEmpty(word) || word.Length != row.RequiredLength)
            {
                Debug.LogWarning($"[WordPlacementUI] Cannot accept incomplete word in row {rowIndex}");
                return;
            }

            // Validate word exists in word list
            Core.WordListSO wordList = GetWordListForRow(rowIndex);
            if (wordList != null && !wordList.Words.Contains(word))
            {
                Debug.LogWarning($"[WordPlacementUI] Word '{word}' not found in word list");
                return;
            }

            row.SetAccepted(true);
            Debug.Log($"[WordPlacementUI] Word '{word}' accepted for row {rowIndex}");

            // Move to next row if available
            if (rowIndex < _wordRows.Length - 1)
            {
                _currentWordIndex = rowIndex + 1;
                _currentWordInput = "";
            }

            CheckIfAllWordsComplete();
        }

        private void OnWordDeleted(int rowIndex)
        {
            _wordRows[rowIndex].SetWord("");
            _wordRows[rowIndex].SetAccepted(false);
            
            if (rowIndex == _currentWordIndex)
            {
                _currentWordInput = "";
                UpdateAutocomplete();
            }

            Debug.Log($"[WordPlacementUI] Word deleted from row {rowIndex}");
        }

        private void OnCoordinateModeToggled(int rowIndex)
        {
            _isInCoordinateMode = !_isInCoordinateMode;
            _currentWordIndex = rowIndex;
            
            Debug.Log($"[WordPlacementUI] Coordinate mode: {_isInCoordinateMode} for row {rowIndex}");
            
            if (_isInCoordinateMode)
            {
                _autocompletePanel.SetActive(false);
                // TODO: Show placement preview on grid
            }
        }

        private Core.WordListSO GetWordListForRow(int rowIndex)
        {
            int requiredLength = _wordRows[rowIndex].RequiredLength;
            
            return requiredLength switch
            {
                3 => _wordList3Letter,
                4 => _wordList4Letter,
                5 => _wordList5Letter,
                6 => _wordList6Letter,
                _ => null
            };
        }
        #endregion

        #region Grid Interaction
        private void OnGridCellClicked(int col, int row)
        {
            if (!_isInCoordinateMode)
            {
                Debug.Log($"[WordPlacementUI] Grid cell clicked ({col}, {row}) but not in coordinate mode");
                return;
            }

            Debug.Log($"[WordPlacementUI] Placing word at ({col}, {row})");
            // TODO: Implement word placement logic
        }

        private void OnRandomPlacementClicked()
        {
            Debug.Log("[WordPlacementUI] Random placement requested");
            // TODO: Implement random placement for current word
        }
        #endregion

        #region Completion
        private void CheckIfAllWordsComplete()
        {
            int requiredWords = _playerDifficulty.WordCount;
            int acceptedWords = 0;

            for (int i = 0; i < requiredWords; i++)
            {
                if (_wordRows[i].IsAccepted)
                {
                    acceptedWords++;
                }
            }

            if (acceptedWords == requiredWords)
            {
                Debug.Log("[WordPlacementUI] All words entered and accepted!");
                // TODO: Enable "Ready" button or auto-transition
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a single word entry row in the placement UI
    /// </summary>
    [System.Serializable]
    public class WordEntryRow
    {
        [Required]
        public GameObject RowObject;
        
        [Required]
        public TextMeshProUGUI NumberLabel;
        
        [Required]
        public TextMeshProUGUI WordDisplay;
        
        [Required]
        public Button AcceptButton;
        
        [Required]
        public Button CoordinateModeButton;
        
        [Required]
        public Button DeleteButton;

        public System.Action OnAcceptClicked;
        public System.Action OnDeleteClicked;
        public System.Action OnCoordinateModeClicked;

        public int RequiredLength { get; private set; }
        public string CurrentWord { get; private set; } = "";
        public bool IsAccepted { get; private set; }

        public void Initialize(int wordNumber, int requiredLength)
        {
            RequiredLength = requiredLength;
            NumberLabel.text = wordNumber.ToString();
            
            AcceptButton.onClick.AddListener(() => OnAcceptClicked?.Invoke());
            DeleteButton.onClick.AddListener(() => OnDeleteClicked?.Invoke());
            CoordinateModeButton.onClick.AddListener(() => OnCoordinateModeClicked?.Invoke());
            
            SetWord("");
        }

        public void SetWord(string word)
        {
            CurrentWord = word.ToUpper();
            
            // Display word with underscores for empty spaces
            string display = "";
            for (int i = 0; i < RequiredLength; i++)
            {
                if (i < CurrentWord.Length)
                {
                    display += CurrentWord[i] + " ";
                }
                else
                {
                    display += "_ ";
                }
            }
            
            WordDisplay.text = display.Trim();
        }

        public void SetAccepted(bool accepted)
        {
            IsAccepted = accepted;
            // TODO: Visual feedback (color change, checkmark, etc.)
        }
    }
}
