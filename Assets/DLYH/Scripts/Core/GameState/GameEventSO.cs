using UnityEngine;
using System.Collections.Generic;

namespace TecVooDoo.DontLoseYourHead.Core
{
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "DLYH/Events/Game Event")]
    public class GameEventSO : ScriptableObject
    {
        private readonly List<GameEventListener> _listeners = new List<GameEventListener>();

        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised();
            }
        }

        public void RegisterListener(GameEventListener listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void UnregisterListener(GameEventListener listener)
        {
            if (_listeners.Contains(listener))
            {
                _listeners.Remove(listener);
            }
        }
    }
}