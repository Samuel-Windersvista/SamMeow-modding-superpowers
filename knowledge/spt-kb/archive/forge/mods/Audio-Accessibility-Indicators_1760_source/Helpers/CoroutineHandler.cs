using System.Collections;
using UnityEngine;
using Color = UnityEngine.Color;
using Image = UnityEngine.UI.Image;

namespace acidphantasm_accessibilityindicators.Helpers
{
    public class CoroutineHandler : MonoBehaviour
    {
        private Coroutine coroutineHandle;
        public void StartRestartFade(GameObject obj, Image img, float fadeTime)
        {
            this.RestartCoroutine(RunFade(obj, img, fadeTime), ref coroutineHandle);
        }

        private YieldInstruction fadeInstruction = new YieldInstruction();
        public IEnumerator RunFade(GameObject obj, Image img, float fadeTime)
        {
            if (obj != null)
            {
                var elapsedTime = 0.0f;
                Color currentColor = img.color;

                while (elapsedTime < fadeTime)
                {
                    yield return fadeInstruction;
                    elapsedTime += Time.deltaTime;
                    currentColor.a = 1.0f - Mathf.Clamp01(elapsedTime / fadeTime);
                    img.color = currentColor;
                }

                obj.transform.rotation = Quaternion.identity;
                obj.SetActive(false);
            }
        }
    }
}
