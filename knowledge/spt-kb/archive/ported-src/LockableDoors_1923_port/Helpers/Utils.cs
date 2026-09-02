using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using SPT.Common.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LockableDoors.Helpers
{
    internal class Utils
    {
        private static string _assemblyPath = Assembly.GetExecutingAssembly().Location;
        public static string AssemblyFolderPath = Path.GetDirectoryName(_assemblyPath);

        public static Vector3 PlayerFront
        {
            get
            {
                Player player = Singleton<GameWorld>.Instance.AllPlayersEverExisted.FirstOrDefault(possiblePlayer => possiblePlayer.IsYourPlayer);
                return player.Transform.Original.position + player.Transform.Original.forward + (player.Transform.Original.up / 2);
            }
        }

        public static void ExecuteAfterSeconds(int seconds, Action<object> callback, object arg = null)
        {
            StaticManager.BeginCoroutine(ExecuteAfterSecondsRoutine(seconds, callback, arg));
        }

        public static IEnumerator ExecuteAfterSecondsRoutine(int seconds, Action<object> callback, object arg)
        {
            yield return new WaitForSeconds(seconds);
            callback(arg);
        }

        public static void ExecuteNextFrame(Action callback)
        {
            StaticManager.BeginCoroutine(ExecuteNextFrameRoutine(callback));
        }

        public static IEnumerator ExecuteNextFrameRoutine(Action callback)
        {
            yield return null;
            callback();
        }

        public static Quaternion ScaleQuaternion(Quaternion rotation, float scale)
        {
            rotation.ToAngleAxis(out float angle, out Vector3 axis);
            angle *= scale;
            return Quaternion.AngleAxis(angle, axis);
        }

        public static List<GameObject> GetAllDescendants(GameObject parent)
        {
            List<GameObject> descendants = [];

            foreach (Transform child in parent.transform)
            {
                descendants.Add(child.gameObject);
                descendants.AddRange(GetAllDescendants(child.gameObject));
            }

            return descendants;
        }

        public static T ServerRoute<T>(string url, object data = default)
        {
            string json = JsonConvert.SerializeObject(data);
            var req = RequestHandler.PostJson(url, json);
            return JsonConvert.DeserializeObject<T>(req);
        }
        public static string ServerRoute(string url, object data = default)
        {
            string json = JsonConvert.SerializeObject(data);
            return RequestHandler.PostJson(url, json);
        }
    }
}
