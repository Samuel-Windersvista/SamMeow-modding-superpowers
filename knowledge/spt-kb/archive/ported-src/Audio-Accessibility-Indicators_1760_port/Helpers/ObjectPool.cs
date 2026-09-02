using System.Collections.Generic;
using UnityEngine;
using acidphantasm_accessibilityindicators.IndicatorUI;
using acidphantasm_accessibilityindicators.Scripts;

namespace acidphantasm_accessibilityindicators.Helpers
{

    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool SharedInstance;
        public static List<GameObject> stepIndicators;
        public static List<GameObject> shotIndicators;
        public static List<GameObject> voiceIndicators;
        public static List<GameObject> verticalityIndicators;

        void Awake()
        {
            SharedInstance = this;
        }
        public static void PoolStepIndicators(GameObject objectToPool, GameObject parentObject, int amountToPool)
        {
            stepIndicators = new List<GameObject>();
            GameObject tmp;
            for (int i = 0; i < amountToPool; i++)
            {
                tmp = Instantiate(objectToPool, parentObject.transform);
                tmp.AddComponent<ObjectIDInfo>();
                tmp.SetActive(false);
                stepIndicators.Add(tmp);
            }
        }
        public static void PoolVoiceIndicators(GameObject objectToPool, GameObject parentObject, int amountToPool)
        {
            voiceIndicators = new List<GameObject>();
            GameObject tmp;
            for (int i = 0; i < amountToPool; i++)
            {
                tmp = Instantiate(objectToPool, parentObject.transform);
                GameObject armPivot = tmp.transform.GetChild(0).gameObject;
                tmp.AddComponent<ObjectIDInfo>();
                armPivot.AddComponent<KeepVerticalRotation>();
                tmp.SetActive(false);
                voiceIndicators.Add(tmp);
            }
        }
        public static void PoolShotIndicators(GameObject objectToPool, GameObject parentObject, int amountToPool)
        {
            shotIndicators = new List<GameObject>();
            GameObject tmp;
            for (int i = 0; i < amountToPool; i++)
            {
                tmp = Instantiate(objectToPool, parentObject.transform);
                tmp.AddComponent<ObjectIDInfo>();
                tmp.SetActive(false);
                shotIndicators.Add(tmp);
            }
        }
        public static void PoolVerticalityIndicators(GameObject objectToPool, GameObject parentObject, int amountToPool)
        {
            verticalityIndicators = new List<GameObject>();
            GameObject tmp;
            for (int i = 0; i < amountToPool; i++)
            {
                tmp = Instantiate(objectToPool, parentObject.transform);
                GameObject belowArmPivot = tmp.transform.GetChild(0).gameObject;
                GameObject aboveArmPivot = tmp.transform.GetChild(1).gameObject;
                tmp.AddComponent<ObjectIDInfo>();
                belowArmPivot.AddComponent<KeepVerticalRotation>();
                aboveArmPivot.AddComponent<KeepVerticalRotation>();
                tmp.SetActive(false);
                verticalityIndicators.Add(tmp);
            }
        }

        public static GameObject GetPooledStepObject(string ownerID = "none")
        {
            var amountToPool = Panel.poolObjectsSteps;
            for (int i = 0; i < amountToPool; i++)
            {
                ObjectIDInfo info = stepIndicators[i].GetComponent<ObjectIDInfo>();
                if (info._OwnerID == ownerID)
                {
                    return stepIndicators[i];
                }
                if (!stepIndicators[i].activeInHierarchy)
                {
                    info._OwnerID = ownerID;
                    return stepIndicators[i];
                }
            }
            return null;
        }

        public static GameObject GetPooledVoiceObject(string ownerID = "none")
        {
            var amountToPool = Panel.poolObjectsVoice;
            for (int i = 0; i < amountToPool; i++)
            {
                ObjectIDInfo info = voiceIndicators[i].GetComponent<ObjectIDInfo>();
                if (info._OwnerID == ownerID)
                {
                    return voiceIndicators[i];
                }
                if (!voiceIndicators[i].activeInHierarchy)
                {
                    info._OwnerID = ownerID;
                    return voiceIndicators[i];
                }
            }
            return null;
        }

        public static GameObject GetPooledShotObject(string ownerID = "none")
        {
            var amountToPool = Panel.poolObjectsShots;
            for (int i = 0; i < amountToPool; i++)
            {
                ObjectIDInfo info = shotIndicators[i].GetComponent<ObjectIDInfo>();
                if (info._OwnerID == ownerID)
                {
                    return shotIndicators[i];
                }
                if (!shotIndicators[i].activeInHierarchy)
                {
                    info._OwnerID = ownerID;
                    return shotIndicators[i];
                }
            }
            return null;
        }

        public static GameObject GetPooledVerticalityObject(string ownerID = "none")
        {
            var amountToPool = Panel.poolObjectsVerticality;
            for (int i = 0; i < amountToPool; i++)
            {
                ObjectIDInfo info = verticalityIndicators[i].GetComponent<ObjectIDInfo>();
                if (info._OwnerID == ownerID)
                {
                    return verticalityIndicators[i];
                }
                if (!verticalityIndicators[i].activeInHierarchy)
                {
                    info._OwnerID = ownerID;
                    return verticalityIndicators[i];
                }
            }
            return null;
        }
    }

}
