using UnityEngine;

namespace acidphantasm_accessibilityindicators.Scripts
{

    internal class KeepVerticalRotation : MonoBehaviour
    {
        private static GameObject mainPivotPoint;
        private static RectTransform mainPivotRect;

        public void Start()
        {
            mainPivotPoint = gameObject.transform.parent.gameObject;
            mainPivotRect = mainPivotPoint.GetComponent<RectTransform>();
        }

        public void Update()
        {
            var reverseRotation = -Mathf.Abs(mainPivotPoint.transform.rotation.z);
            this.transform.rotation = Quaternion.Euler(0, 0, reverseRotation);
        }
    }
}
