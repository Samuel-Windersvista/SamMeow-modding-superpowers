using EFT;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace ReachExtender.Patches {
    class InteractionRaycastPatch : ModulePatch {
        protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod("FindInteractable");

        [PatchPrefix]
        private static bool PatchPrefix(GameWorld __instance, ref GameObject __result, Ray ray, out RaycastHit hit) {
            __result = null;
            GameObject gameObject = null;
            if (ReachExtenderPlugin.SphereCastRadius.Value == 0f) {
                gameObject = EFTPhysicsClass.Raycast(ray, out hit, Mathf.Max(EFTHardSettings.Instance.LOOT_RAYCAST_DISTANCE, EFTHardSettings.Instance.PLAYER_RAYCAST_DISTANCE + EFTHardSettings.Instance.BEHIND_CAST), GameWorld.InteractiveLootMaskWPlayer) ? hit.collider.gameObject : null;
            } else {
                gameObject = EFTPhysicsClass.SphereCast(ray, ReachExtenderPlugin.SphereCastRadius.Value, out hit, Mathf.Max(EFTHardSettings.Instance.LOOT_RAYCAST_DISTANCE, EFTHardSettings.Instance.PLAYER_RAYCAST_DISTANCE + EFTHardSettings.Instance.BEHIND_CAST), GameWorld.InteractiveLootMaskWPlayer) ? hit.collider.gameObject : null;
            }

            if (ReachExtenderPlugin.ShowSphereCastDebug.Value && ReachExtenderPlugin.SphereCastRadius.Value != 0f) {
                RaycastHit balls;
                GameObject ball = EFTPhysicsClass.Raycast(ray, out balls, Mathf.Infinity, GameWorld.LootMaskObstruction) ? balls.collider.gameObject : null;
                // Get/create sphere and move/scale it to where we want
                if (ReachExtenderPlugin.Sphere == null) {
                    ReachExtenderPlugin.Sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    GameObject.Destroy(ReachExtenderPlugin.Sphere.GetComponent<SphereCollider>());
                    Color col = Color.red;
                    col.a = 0.5f;
                    Material newMat = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
                    newMat.SetColor("_Color", col);
                    ReachExtenderPlugin.Sphere.GetComponent<MeshRenderer>().sharedMaterial = newMat;
                }

                if (ball) {
                    ReachExtenderPlugin.Sphere.transform.position = ray.GetPoint(balls.distance);
                    ReachExtenderPlugin.Sphere.transform.localScale = Vector3.one * (ReachExtenderPlugin.SphereCastRadius.Value * 2f);
                }
            }

            if (gameObject) {
                // Get raycast to obstruction
                RaycastHit obstructionHit;
                if (!Physics.Linecast(ray.origin, hit.point, out obstructionHit, GameWorld.LootMaskObstruction)) {
                    // If theres no obstruction, return the gameobject of the loot item we found in the first raycast
                    __result = gameObject;
                } else {
                    // Distance between loot item and point that obstruction hit
                    float distSquared = (obstructionHit.point - gameObject.transform.position).sqrMagnitude;
                    if (distSquared <= ReachExtenderPlugin.DistanceFromWallSquared) {
                        __result = gameObject;
                    }
                }
            }

            return false;
        }
    }
}
