using DoorBreach;
using System.Linq;
using System.Text;
using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.Interactive;
using DoorOverhaul.Destruction.Core;
using UnityEngine;

namespace DoorOverhaul.Destruction
{
    internal sealed class DestructionController : MonoBehaviour
    {
        private const float LookDistance = 8f;
        private const float MapHeight = 300f;
        private const float ImpactSeconds = 12f;

        private int _lookMask;
        private GUIStyle _label;
        private Door _lookedAt;

        private void Awake()
        {
            _lookMask = LayerMask.GetMask("HighPolyCollider", "Interactive", "DoorLowPolyCollider", "LowPolyCollider");
        }

        private void LateUpdate()
        {
            DoorDamageIntake.Flush();
            DestructionVisuals.Tick();
            DestructionFragments.Tick();
        }

        private void OnGUI()
        {
            bool info = Settings.ShowDoorInfo.Value;
            bool map = Settings.ShowDamageMap.Value;
            bool impacts = Settings.ShowImpacts.Value;
            if ((!info && !map && !impacts) || !Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            Camera camera = CameraManager.Instance.Camera;
            if (camera == null)
            {
                camera = Camera.main;
            }

            if (camera == null)
            {
                return;
            }

            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13, wordWrap = false };
            }

            if (Event.current.type == EventType.Layout)
            {
                _lookedAt = FindLookedAtDoor(camera);
            }

            DoorDestructionRegistry.TryGet(_lookedAt, out DoorEntry entry);

            if (info)
            {
                DrawInfo(entry);
            }

            if (map && entry?.State != null)
            {
                DrawMap(entry);
            }

            if (impacts)
            {
                DrawImpacts(camera);
            }
        }

        private Door FindLookedAtDoor(Camera camera)
        {
            var ray = new Ray(camera.transform.position, camera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, LookDistance, _lookMask, QueryTriggerInteraction.Collide))
            {
                Door door = hit.collider.GetComponentInParent<Door>();
                if (door != null)
                {
                    return door;
                }
            }

            return Singleton<GameWorld>.Instance.MainPlayer?.InteractableObject as Door;
        }

        private void DrawInfo(DoorEntry entry)
        {
            var text = new StringBuilder();
            text.AppendLine("<b>Destructible doors (debug)</b>");
            text.AppendLine($"{DoorDestructionRegistry.All.Count} doors, {DoorDestructionRegistry.All.Count(e => e.Eligible)} destructible" +
                (DoorDestructionRegistry.FikaPresent ? ", <color=orange>Fika: off</color>" : string.Empty));
            text.AppendLine($"hits {DoorDamageIntake.Processed}, duplicates {DoorDamageIntake.Duplicates}, ignored {DoorDamageIntake.Ignored}, " +
                $"simulated doors {DoorDestructionRegistry.All.Count(e => e.State != null)}, pieces {DestructionFragments.Count}/{DestructionFragments.Cap} ({DestructionFragments.Moving} moving)");

            if (_lookedAt == null)
            {
                text.AppendLine("<i>not looking at a door</i>");
            }
            else if (entry == null)
            {
                text.AppendLine($"<color=orange>{_lookedAt.Id}: not registered</color>");
            }
            else
            {
                Door door = entry.Door;
                text.AppendLine($"<b>{door.Id}</b> ({door.GetType().Name}) {door.DoorState}");
                text.AppendLine(entry.Eligible
                    ? $"<color=lime>destructible</color>: {entry.Material} ({entry.Profile.Name}, punch-through at pen {entry.Profile.ThroughPenetration:0})"
                    : $"<color=orange>vanilla</color>: {entry.Reason}");
                if (entry.Frame != null)
                {
                    text.AppendLine($"leaf {entry.Frame.Width:0.00} x {entry.Frame.Height:0.00} x {entry.Frame.Thickness:0.000} m from {entry.Frame.Source}");
                }

                text.AppendLine($"hits {entry.Hits}, ignored {entry.IgnoredHits}{(entry.LastIgnoreReason.Length > 0 ? $" ({entry.LastIgnoreReason})" : string.Empty)}");

                FractureState state = entry.State;
                if (state != null)
                {
                    text.AppendLine($"cells {state.IntactCount}/{state.Layout.Cells.Length} intact, leaf {state.Leaf}, latch {(state.LatchEngaged ? "engaged" : "free")}");
                    if (DestructionVisuals.TryGet(entry, out LeafVisual visual))
                    {
                        text.AppendLine($"visual: {visual.Status}, {visual.Vertices} verts, rebuild {visual.LastBuildMs:0.0} ms{(visual.HasHole ? ", portal open" : string.Empty)}");
                        text.AppendLine($"pieces fallen {visual.PiecesSpawned}, splinter bursts {visual.DebrisBursts}");
                    }
                    foreach (Anchor anchor in state.Anchors)
                    {
                        text.AppendLine($"  {anchor.Spec.Kind} at ({anchor.Spec.Position.X:0.00},{anchor.Spec.Position.Y:0.00}): " +
                            (anchor.Failed ? "<color=red>FAILED</color>" : $"{anchor.Health / anchor.MaxHealth:P0}"));
                    }
                }
            }

            GUI.Box(new Rect(10f, 120f, 470f, 22f + 16f * CountLines(text)), GUIContent.none);
            GUI.Label(new Rect(18f, 124f, 460f, 400f), text.ToString(), _label);
        }

        private void DrawMap(DoorEntry entry)
        {
            FractureState state = entry.State;
            float scale = MapHeight / state.Layout.Height;
            float width = state.Layout.Width * scale;
            var origin = new Vector2(Screen.width - width - 30f, 140f);

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(origin.x - 6f, origin.y - 22f, width + 12f, MapHeight + 30f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(origin.x, origin.y - 20f, width + 200f, 20f), $"<b>damage map</b> (hinges left)", _label);

            float cell = Mathf.Max(2f, state.Layout.CellWidth * scale * 0.92f);
            foreach (Cell c in state.Layout.Cells)
            {
                switch (state.StatusOf(c.Id))
                {
                    case CellStatus.Removed:
                        continue;
                    case CellStatus.Detached:
                        GUI.color = new Color(1f, 0f, 1f, 0.5f);
                        break;
                    default:
                        float health = state.CellHealthFraction(c.Id);
                        GUI.color = Color.Lerp(new Color(1f, 0.15f, 0.1f), new Color(0.75f, 0.65f, 0.45f), health);
                        break;
                }

                Vector2 p = ToMap(origin, scale, c.Centroid);
                GUI.DrawTexture(new Rect(p.x - cell * 0.5f, p.y - cell * 0.5f, cell, cell), Texture2D.whiteTexture);
            }

            foreach (Anchor anchor in state.Anchors)
            {
                GUI.color = anchor.Failed ? Color.red : (anchor.Spec.Kind == AnchorKind.Hinge ? Color.cyan : Color.yellow);
                Vector2 p = ToMap(origin, scale, anchor.Spec.Position);
                GUI.DrawTexture(new Rect(p.x - 4f, p.y - 4f, 8f, 8f), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
            foreach (ImpactRecord hit in DoorDamageIntake.Recent.Where(h => h.Entry == entry && h.Kernel != null))
            {
                Vector2 p = ToMap(origin, scale, hit.Panel);
                GUI.DrawTexture(new Rect(p.x - 1.5f, p.y - 1.5f, 3f, 3f), Texture2D.whiteTexture);
            }
        }

        private void DrawImpacts(Camera camera)
        {
            float now = Time.time;
            int labels = 0;
            for (int i = DoorDamageIntake.Recent.Count - 1; i >= 0; i--)
            {
                ImpactRecord hit = DoorDamageIntake.Recent[i];
                if (now - hit.Time > ImpactSeconds)
                {
                    continue;
                }

                Vector3 screen = camera.WorldToViewportPoint(hit.Point);
                if (screen.z <= 0f)
                {
                    continue;
                }

                var p = new Vector2(screen.x * Screen.width, (1f - screen.y) * Screen.height);
                GUI.color = hit.Kernel == null ? Color.grey : hit.Kernel.Through ? Color.red : hit.Kernel.Breaching ? new Color(1f, 0.55f, 0.1f) : Color.yellow;
                GUI.DrawTexture(new Rect(p.x - 3f, p.y - 3f, 6f, 6f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                if (screen.z < 6f && labels++ < 4)
                {
                    GUI.Label(new Rect(p.x + 6f, p.y - 8f, 320f, 20f),
                        $"<size=11>pen {hit.Penetration:0} dmg {hit.Damage:0} {hit.DiameterMm:0.#}mm {hit.Outcome}{(hit.ExitSeen ? " (exit)" : string.Empty)}</size>", _label);
                }
            }
        }

        private static Vector2 ToMap(Vector2 origin, float scale, System.Numerics.Vector2 panel)
        {
            return new Vector2(origin.x + panel.X * scale, origin.y + MapHeight - panel.Y * scale);
        }

        private static int CountLines(StringBuilder text)
        {
            int lines = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    lines++;
                }
            }

            return lines;
        }
    }
}
