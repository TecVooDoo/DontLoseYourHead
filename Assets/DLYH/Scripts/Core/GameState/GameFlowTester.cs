using UnityEngine;
using Sirenix.OdinInspector;

namespace TecVooDoo.DontLoseYourHead.Core
{
    /// <summary>
    /// Test component for verifying Game Flow State Machine integration
    /// </summary>
    public class GameFlowTester : MonoBehaviour
    {
        [Title("Dependencies")]
        [Required]
        [SerializeField] private GameManager _gameManager;
        
        [Required]
        [SerializeField] private GameStateMachine _stateMachine;

        [Title("Test Controls")]
        [InfoBox("These buttons test that GameManager only processes guesses during GameplayActive phase")]
        
        [Button("Test Letter Guess (Should Fail if Not in Gameplay)", ButtonSizes.Large)]
        [GUIColor(1f, 0.7f, 0.7f)]
        public void TestLetterGuess()
        {
            Debug.Log($"[GameFlowTester] Current Phase: {_stateMachine.CurrentPhase}");
            Debug.Log($"[GameFlowTester] Attempting letter guess 'A'...");
            
            bool result = _gameManager.ProcessLetterGuess(0, _gameManager.OpponentGrid, 'A');
            
            if (_stateMachine.IsInGameplay)
            {
                Debug.Log($"[GameFlowTester] ✓ In gameplay phase - guess was processed: {result}");
            }
            else
            {
                Debug.Log($"[GameFlowTester] ✓ Not in gameplay phase - guess should have been blocked");
            }
        }

        [Button("Test Coordinate Guess (Should Fail if Not in Gameplay)", ButtonSizes.Large)]
        [GUIColor(1f, 0.7f, 0.7f)]
        public void TestCoordinateGuess()
        {
            Debug.Log($"[GameFlowTester] Current Phase: {_stateMachine.CurrentPhase}");
            Debug.Log($"[GameFlowTester] Attempting coordinate guess (0,0)...");
            
            bool result = _gameManager.ProcessCoordinateGuess(0, _gameManager.OpponentGrid, new Vector2Int(0, 0));
            
            if (_stateMachine.IsInGameplay)
            {
                Debug.Log($"[GameFlowTester] ✓ In gameplay phase - guess was processed: {result}");
            }
            else
            {
                Debug.Log($"[GameFlowTester] ✓ Not in gameplay phase - guess should have been blocked");
            }
        }

        [Title("Phase Info")]
        [ShowInInspector]
        [ReadOnly]
        [PropertyOrder(100)]
        private string CurrentPhase => _stateMachine != null ? _stateMachine.CurrentPhase.ToString() : "N/A";

        [ShowInInspector]
        [ReadOnly]
        [PropertyOrder(101)]
        private bool CanProcessGuesses => _stateMachine != null ? _stateMachine.IsInGameplay : false;
    }
}
