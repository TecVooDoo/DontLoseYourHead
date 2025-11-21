using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

namespace TecVooDoo.DontLoseYourHead.Core
{
    public class GameEventListener : MonoBehaviour
    {
        [Title("Event Configuration")]
        [Required]
        [SerializeField] private GameEventSO _event;

        [Title("Response")]
        [SerializeField] private UnityEvent _response;

        private void OnEnable()
        {
            if (_event != null)
            {
                _event.RegisterListener(this);
            }
        }

        private void OnDisable()
        {
            if (_event != null)
            {
                _event.UnregisterListener(this);
            }
        }

        public void OnEventRaised()
        {
            _response?.Invoke();
        }
    }
}