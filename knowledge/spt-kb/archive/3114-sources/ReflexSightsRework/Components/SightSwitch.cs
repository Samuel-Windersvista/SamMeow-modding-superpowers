using System;
using System.Collections;
using EFT;
using UnityEngine;

namespace SamSWAT.ReflexSightsRework
{
    public class SightSwitch : MonoBehaviour
    {
#pragma warning disable CS0649
        private GameObject _normalMesh;

        private void Awake()
        {
            _normalMesh = gameObject;
            Debug.Log(_normalMesh);
        }

        private void OnEnable()
        {
            var delay = Patch.Overweight > 0 || Patch.TotalErgonomics < 30 ? 0.12f : 0f;
            StaticManager.BeginCoroutine(DelayCoroutine(delay));
        }

        private void OnDisable()
        {
            ExpandMesh();
        }

        private void OnDestroy()
        {
            Debug.Log("Hi");
        }

        private void SwitchMeshes()
        {
            if (_normalMesh == null) return;
            FlattenMesh();
            ExpandMesh();
            // normalMesh.SetActive(!normalMesh.activeSelf);
            // adsMesh.SetActive(!adsMesh.activeSelf);
        }

        private void FlattenMesh()
        {
            Debug.Log($"Flatten {Time.time}");
            _normalMesh.gameObject.transform.localScale = new Vector3(1, 1, 0.1f);
        }

        private void ExpandMesh()
        {
            Debug.Log($"Flatten {Time.time}");
            _normalMesh.gameObject.transform.localScale = new Vector3(1, 1, 1f);
        }

        private IEnumerator DelayCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            SwitchMeshes();
        }
    }
}