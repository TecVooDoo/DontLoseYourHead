using System;
using System.Collections;
using UnityEngine;

namespace SABI
{
    public static class MonoBehaviourExtensions
    {
        #region DelayedExecution
        /// Extension method for MonoBehaviour that schedules a callback to run after a delay in seconds.
        /// Return this MonoBehaviour for method chaining.
        /// Arguments: float delay: time in seconds to wait before invoking the callback. Action callback: method to invoke after delay.
        public static MonoBehaviour DelayedExecution(
            this MonoBehaviour monoBehaviour,
            float delay,
            Action callback
        )
        {
            monoBehaviour.StartCoroutine(Execute(delay, callback));
            return monoBehaviour;
        }

        private static IEnumerator Execute(float delay, Action callback)
        {
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
        }

        /// Extension method for MonoBehaviour that schedules a callback to run after a delay and returns the started Coroutine.
        /// Return this MonoBehaviour for method chaining.
        /// Arguments: float delay: time in seconds to wait before invoking the callback. Action callback: method to invoke after delay. out Coroutine coroutine: receives the started Coroutine so it can be stopped later.
        public static MonoBehaviour DelayedExecution(
            this MonoBehaviour monoBehaviour,
            float delay,
            Action callback,
            out Coroutine coroutine
        )
        {
            coroutine = monoBehaviour.StartCoroutine(Execute(delay, callback));
            return monoBehaviour;
        }
        #endregion

        #region Delayed Execution Frame
        /// Extension method for MonoBehaviour that schedules a callback to run on the next frame.
        /// Return this MonoBehaviour for method chaining.
        /// Arguments: Action callback: method to invoke on the next frame.
        public static MonoBehaviour DelayedExecutionUntilNextFrame(
            this MonoBehaviour monoBehaviour,
            Action callback
        )
        {
            monoBehaviour.StartCoroutine(ExecuteAfterFrame(callback));
            return monoBehaviour;
        }

        private static IEnumerator ExecuteAfterFrame(Action callback)
        {
            yield return null;
            callback?.Invoke();
        }
        #endregion

        #region Delayed Execution Until Condition True
        /// Extension method for MonoBehaviour that waits until a condition matches the expected result, then invokes a callback.
        /// Return this MonoBehaviour for method chaining.
        /// Arguments: Func<bool> condition: predicate to evaluate each frame. Action callback: method to invoke once condition matches expectedResult. bool expectedResult: desired boolean result (default true).
        public static MonoBehaviour DelayedExecutionUntil(
            this MonoBehaviour monoBehaviour,
            Func<bool> condition,
            Action callback,
            bool expectedResult = true
        )
        {
            if (condition != null)
                monoBehaviour.StartCoroutine(WaitForCondition(condition, callback, expectedResult));
            return monoBehaviour;
        }

        private static IEnumerator WaitForCondition(
            Func<bool> condition,
            Action callback,
            bool expectedResult
        )
        {
            yield return new WaitUntil(() => condition() == expectedResult);
            callback?.Invoke();
        }
        #endregion

        #region Repeated Execution
        /// Extension method for MonoBehaviour that repeats a callback at a given interval while a condition matches the expected result.
        /// Return this MonoBehaviour for method chaining.
        /// Arguments: Func<bool> condition: predicate evaluated each loop. float interval: seconds between callback invocations. Action callback: method invoked each interval. bool expectedResult: desired boolean result (default true).
        public static MonoBehaviour RepeatExecutionWhile(
            this MonoBehaviour monoBehaviour,
            Func<bool> condition,
            float interval,
            Action callback,
            bool expectedResult = true
        )
        {
            if (condition != null)
                monoBehaviour.StartCoroutine(
                    RepeatWhileCoroutine(condition, interval, callback, expectedResult)
                );
            return monoBehaviour;
        }

        private static IEnumerator RepeatWhileCoroutine(
            Func<bool> condition,
            float interval,
            Action callback,
            bool expectedResult
        )
        {
            while (condition() == expectedResult)
            {
                yield return new WaitForSeconds(interval);
                callback?.Invoke();
            }
        }
        #endregion

        /// Extension method for MonoBehaviour that returns an existing component of type T or adds one to the GameObject.
        /// Returns T component found or added on the GameObject.
        public static T GetOrAddComponent<T>(this MonoBehaviour behaviour)
            where T : Component
        {
            T component = behaviour.GetComponent<T>();
            return component != null ? component : behaviour.gameObject.AddComponent<T>();
        }

        /// Extension method for MonoBehaviour that adds a component of type T to the GameObject if it is missing.
        /// Returns void.
        public static void AddComponentIfMissing<T>(this MonoBehaviour behaviour)
            where T : Component
        {
            if (behaviour.GetComponent<T>() == null)
                behaviour.gameObject.AddComponent<T>();
        }
    }
}
