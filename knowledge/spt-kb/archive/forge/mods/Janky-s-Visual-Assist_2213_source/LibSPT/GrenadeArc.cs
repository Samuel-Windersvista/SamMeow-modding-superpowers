using System;
using System.Reflection;
using EFT;
using EFT.UI;
using HarmonyLib;
using UnityEngine;

namespace VisualAssist;

public class GrenadeArc : MonoBehaviour
{
    public Player localPlayer;
    public GrenadeThrow GrenadeThrow;

    private LineRenderer _line;
    private GameObject _sphere;
    private Renderer _sphereRenderer;
    private Vector3[] _positions;
    private Vector3 _playerVelocity;

    private string _itemName;
    private float _mass;

    private float _gravity;
    private const float LinearDrag = 0.1f;
    
    private static readonly FieldInfo GrenadePrefabField = AccessTools.Field(typeof(Player.GrenadeHandsController), "grenadePrefab_0");

    public void Awake()
    {
        // Add a LineRenderer component
        _line = gameObject.AddComponent<LineRenderer>();

        // Set the material
        _line.material = new Material(Shader.Find("Sprites/Default"));

        // Disable lighting
        // _line.generateLightingData = false;
        _line.numCapVertices = 1;
        _line.numCornerVertices = 3;

        // Set the color
        _line.startColor = Plugin.GrenadeArcStartColor.Value;
        _line.endColor = Plugin.GrenadeArcEndColor.Value;

        // Set the width
        _line.startWidth = Plugin.GrenadeArcStartGirth.Value;
        _line.endWidth = Plugin.GrenadeArcEndGirth.Value;

        _sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _sphere.transform.localScale = Vector3.one * Plugin.GrenadeArcKnobSize.Value;
        _sphere.GetComponent<Collider>().enabled = false;
        _sphereRenderer = _sphere.GetComponent<Renderer>();
        _sphereRenderer.material = new Material(Shader.Find("Sprites/Default"))
        {
            color = Plugin.GrenadeArcKnobColor.Value
        };

        Plugin.GrenadeArcStartColor.SettingChanged += UpdateSettings;
        Plugin.GrenadeArcEndColor.SettingChanged += UpdateSettings;
        Plugin.GrenadeArcKnobColor.SettingChanged += UpdateSettings;
        Plugin.GrenadeArcKnobSize.SettingChanged += UpdateSettings;
        Plugin.GrenadeArcStartGirth.SettingChanged += UpdateSettings;
        Plugin.GrenadeArcEndGirth.SettingChanged += UpdateSettings;
        
        _positions = new Vector3[200];
        _playerVelocity = Vector3.zero;

        _gravity = -Physics.gravity.y;

        _itemName = null;
        _mass = 0.55f;
    }

    public void OnDestroy()
    {
        Plugin.GrenadeArcStartColor.SettingChanged -= UpdateSettings;
        Plugin.GrenadeArcEndColor.SettingChanged -= UpdateSettings;
        Plugin.GrenadeArcKnobColor.SettingChanged -= UpdateSettings;
        Plugin.GrenadeArcKnobSize.SettingChanged -= UpdateSettings;
        Plugin.GrenadeArcStartGirth.SettingChanged -= UpdateSettings;
        Plugin.GrenadeArcEndGirth.SettingChanged -= UpdateSettings;
        
        Plugin.Log.LogInfo("Unsubscribed GrenadeArc from config notifications");
    }

    public void LateUpdate()
    {
        if (localPlayer == null || !localPlayer.HealthController.IsAlive || !Plugin.GrenadeArcEnabled.Value)
            return;

        _playerVelocity = Vector3.Lerp(localPlayer.Velocity, _playerVelocity, 0.9f);

        var grenadeHandsController = localPlayer.HandsController as Player.GrenadeHandsController;

        if (grenadeHandsController == null
            || (grenadeHandsController.CurrentOperation is not Player.GrenadeHandsController.Class1275
                && grenadeHandsController.CurrentOperation is not Player.GrenadeHandsController.Class1276))
        {
            _line.enabled = false;
            _sphereRenderer.enabled = false;
            return;
        }

        if (!_line.enabled)
            _line.enabled = true;

        // Class1156 is high throw 1157 low throw
        var isLowThrow = grenadeHandsController.CurrentOperation is Player.GrenadeHandsController.Class1276;
        GrenadeThrow = CalculateGrenadeThrow(isLowThrow);

        if (grenadeHandsController.Item != null && _itemName != grenadeHandsController.Item.Name)
        {
            var prefab = GrenadePrefabField.GetValue(grenadeHandsController) as GrenadePrefab;
            
            if (prefab != null && prefab.GrenadeItself != null && prefab.GrenadeItself.gameObject != null)
            {
                var rigidbody = prefab.GrenadeItself.gameObject.GetComponent<Rigidbody>();

                if (rigidbody != null)
                {
                    _mass = rigidbody.mass;          
                    _itemName = grenadeHandsController.Item.Name;
                }
            }
        }
        
        // NB: The actual grenade weight is either 0.5 or 0.6, depending on the item.
        // The mass itself seems to materialize in the rigidbody that's attached to the grenade in a roundabout way.
        // Velocity is (impulse / rigidbody.mass) * Time.fixedDeltaTime, assuming that the impulse was scaled up to 1 second unit by dividing by fixedDeltaTime
        // Since we don't do the division by fixedDeltaTime in CalculateGrenadeThrow, we don't need to multiply here.
        // NB: in AddForce with Impulse mode, unity will assume that the impulse is *per fixed frame time* and will then scale it up to a whole second
        // if we observe the accumulated forces on the rigidbody.
        // Drag is implemented as Mathf.Clamp01(1f - rigidbody.drag * Time.fixedDeltaTime) ran every fixed delta frame
        // The drag itself seems to be set to 0.1f
        var throwVelocity = GrenadeThrow.ThrowForce / _mass;
        var intervalDistance = Plugin.GrenadeArcResolution.Value;
        var maxDistance = Plugin.GrenadeArcDistance.Value;
        var collided = GetBallisticArcWithLinearDrag(
            _positions, GrenadeThrow.ThrowPosition, throwVelocity, intervalDistance, maxDistance, _gravity, LinearDrag, out var positionCount
        );

        if (collided)
        {
            _sphereRenderer.enabled = true;
            _sphere.transform.position = _positions[positionCount - 1];
        }
        else
        {
            _sphereRenderer.enabled = false;
        }

        _line.positionCount = positionCount;
        _line.SetPositions(_positions);
    }

    private GrenadeThrow CalculateGrenadeThrow(bool low)
    {
        // Taken from BaseGrenadeHandsController.vmethod_1
        var lowHighThrow = low ? 0.66f : 1f + (float)localPlayer.Skills.StrengthBuffThrowDistanceInc;
        var forcePower = EFTHardSettings.Instance.GrenadeForce;

        // Taken from the assignment of transform_0 in Player.BaseGrenadeHandsController
        var rootTransform = localPlayer.PlayerBones.WeaponRoot.Original;

        if (!(bool)localPlayer.Skills.ThrowingEliteBuff)
        {
            var handStamina = localPlayer.Physical.HandsStamina.NormalValue;
            lowHighThrow *= Mathf.Lerp(0.4f, 1f, handStamina + 0.5f);
            // Remove the hand stamina based randomization from the prediction arc as it just makes things jittery
            // direction = (-rootTransform.up * 5f + Mathf.Clamp01(0.5f - handStamina) * Random.onUnitSphere).normalized;
        }

        var direction = -rootTransform.up;

        var force = direction * (forcePower * lowHighThrow) + _playerVelocity;

        // var throwPosition = grenadeHandsController.FindThrowPosition();
        // For less noise: use a custom position that doesn't jiggle with the hand movements
        var throwPosition = localPlayer.PlayerBones.WeaponRoot.Original.position + 0.5f * direction;
        return new GrenadeThrow { ThrowPosition = throwPosition, ThrowForce = force };
    }

    private void UpdateSettings(object sender, EventArgs e)
    {
        // This is very lazy but I can't be bothered a sophisticated notification system just so people can tweak their knob in real time...
        _line.startWidth = Plugin.GrenadeArcStartGirth.Value;
        _line.endWidth = Plugin.GrenadeArcEndGirth.Value;
        
        _line.startColor = Plugin.GrenadeArcStartColor.Value;
        _line.endColor = Plugin.GrenadeArcEndColor.Value;

        _sphere.transform.localScale = Vector3.one * Plugin.GrenadeArcKnobSize.Value;
        _sphereRenderer.material.color = Plugin.GrenadeArcKnobColor.Value;
    }

    private static bool GetBallisticArcWithLinearDrag(
        Vector3[] positions,
        Vector3 startPosition,
        Vector3 initialVelocity,
        float intervalDistance,
        float maxDistance,
        float gravity,
        float linearDragCoefficient,
        out int positionCount
    )
    {
        var i = 0;

        positions[i] = startPosition;
        i++;

        // For linear drag, the analytical solution is:
        // x(t) = x0 + (v0x/k) * (1 - e^(-k*t))
        // y(t) = y0 + (1/k) * ((v0y + g/k) * (1 - e^(-k*t)) - g*t)
        // z(t) = z0 + (v0z/k) * (1 - e^(-k*t))
        var k = linearDragCoefficient;
        if (k < 0.0001f) k = 0.0001f; // Avoid division by zero

        var v0X = initialVelocity.x;
        var v0Y = initialVelocity.y;
        var v0Z = initialVelocity.z;

        // Calculate horizontal speed for distance tracking
        var horizontalSpeed = Mathf.Sqrt(v0X * v0X + v0Z * v0Z);
        if (horizontalSpeed < 0.001f)
        {
            positionCount = i;
            return false;
        }

        // Maximum reachable horizontal distance due to drag
        var maxReachableDistance = horizontalSpeed / k;

        var currentDistance = intervalDistance;

        while (currentDistance <= maxDistance && currentDistance < maxReachableDistance * 0.999f && i < positions.Length)
        {
            // Solve for time given horizontal distance
            // d = sqrt((v0x/k)^2 + (v0z/k)^2) * (1 - e^(-k*t))
            // t = -ln(1 - d*k/sqrt(v0x^2 + v0z^2)) / k
            var ratio = currentDistance * k / horizontalSpeed;
            var t = -Mathf.Log(1f - ratio) / k;

            // Calculate position using analytical solution
            var dragTerm = Mathf.Exp(-k * t);

            var x = startPosition.x + (v0X / k) * (1f - dragTerm);
            var y = startPosition.y + (1f / k) * ((v0Y + gravity / k) * (1f - dragTerm) - gravity * t);
            var z = startPosition.z + (v0Z / k) * (1f - dragTerm);
            var candidatePos = new Vector3(x, y, z);

            var prevPos = positions[i - 1];
            var arcLine = candidatePos - prevPos;

            if (Physics.SphereCast(
                    prevPos, 0.05f, arcLine.normalized, out var hit, arcLine.magnitude, LayerMasksDataAbstractClass.HitMask.value
                ))
            {
                positions[i] = hit.point;
                positionCount = i + 1;
                return true;
            }

            positions[i] = candidatePos;
            currentDistance += intervalDistance;
            i++;
        }

        positionCount = i;
        return false;
    }
}

public struct GrenadeThrow
{
    public Vector3 ThrowPosition;
    public Vector3 ThrowForce;
}