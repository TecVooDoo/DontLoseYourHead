using UnityEngine;
using Sirenix.OdinInspector;

namespace TecVooDoo.DontLoseYourHead.Core
{
    [CreateAssetMenu(fileName = "NewDifficulty", menuName = "DLYH/Difficulty")]
    public class DifficultySO : ScriptableObject
    {
        [Title("Difficulty Settings")]
        [SerializeField] private string _difficultyName = "Easy";

        [SerializeField]
        [Range(6, 10)]
        private int _gridSize = 6;

        [SerializeField]
        [Range(6, 15)]
        private int _missLimit = 8;

        public string DifficultyName => _difficultyName;
        public int GridSize => _gridSize;
        public int MissLimit => _missLimit;
    }
}