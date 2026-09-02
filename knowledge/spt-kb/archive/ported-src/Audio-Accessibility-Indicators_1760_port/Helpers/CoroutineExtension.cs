using System.Collections;
using UnityEngine;

namespace acidphantasm_accessibilityindicators.Helpers
{
    public static class CoroutineExtension
    {
        /// <summary>
        /// Tries to stop a coroutine based on a Coroutine Handle.
        /// will only stop the Coroutine if the handle is not null
        /// </summary>
        /// <returns>the Monobehaviour script running the coroutine, allowing chained commands</returns>
        /// <param name="handle">Handle.</param>
        public static MonoBehaviour TryStopCoroutine(this MonoBehaviour script, ref Coroutine handle)
        {
            if (!script) return null;
            if (handle != null) script.StopCoroutine(handle);
            handle = null;
            return script;
        }

        /// <summary>
        /// Starts the coroutine and sets the routine to a Coroutine handle.
        /// </summary>
        /// <returns>the Monobehaviour script running the coroutine, allowing chained commands</returns>
        /// <param name="routine">Routine.</param>
        /// <param name="handle">Handle.</param>
        public static MonoBehaviour StartCoroutine(this MonoBehaviour script, IEnumerator routine, ref Coroutine handle)
        {
            if (!script)
            {
                return null;
            }

            if (!script.enabled || !script.gameObject.activeInHierarchy)
            {
                return script;
            }

            handle = script.StartCoroutine(routine);

            return script;
        }


        /// <summary>
        /// Stops any possible coroutine running on the specified handle and runs a new routine in its place
        /// </summary>
        /// <returns>the Monobehaviour script running the coroutine, allowing chained commands</returns>
        /// <param name="script">Script.</param>
        /// <param name="routine">Routine.</param>
        /// <param name="handle">Handle.</param>
        public static MonoBehaviour RestartCoroutine(this MonoBehaviour script, IEnumerator routine, ref Coroutine handle)
        {
            return script.TryStopCoroutine(ref handle).StartCoroutine(routine, ref handle);
        }
    }
}
