using UnityEngine;
using System;

namespace Mass.HyperBurst
{
    /// <summary>
    /// Maps a consecutive shot count to a fire-rate multiplier.
    /// </summary>
    [Serializable]
    public struct FireRateControlPoint
    {
        [SerializeField] private int _shotCount;
        [SerializeField] private float _fireRateMultiplier;

        /// <summary>
        /// Creates a fire-rate control point.
        /// </summary>
        public FireRateControlPoint(int shotCount, float fireRateMultiplier)
        {
            _shotCount = shotCount;
            _fireRateMultiplier = fireRateMultiplier;
        }

        /// <summary>
        /// Consecutive shot count where this multiplier is reached.
        /// </summary>
        public int ShotCount
        {
            get { return Mathf.Max(0, _shotCount); }
        }

        /// <summary>
        /// Fire-rate multiplier at this shot count.
        /// </summary>
        public float FireRateMultiplier
        {
            get { return _fireRateMultiplier; }
        }
    }

    /// <summary>
    /// Example controller that ramps fire rate while the weapon keeps firing.
    /// Attach this component to a WeaponContainer prefab instead of HyperBurstController.
    /// </summary>
    public class ProgressiveFireRateController : VariableFireRateControllerBase
    {
        [SerializeField]
        private FireRateControlPoint[] _fireRateControlPoints =
        {
            new FireRateControlPoint(0, 1.045f),
            new FireRateControlPoint(20, 0.965f),
            new FireRateControlPoint(40, 0.885f),
        };

        private float[] _resolvedFireRateMultipliers;

        private void OnEnable()
        {
            BuildFireRateMultiplierTable();
        }

        /// <summary>
        /// Returns whether the progressive fire-rate multiplier should be applied.
        /// </summary>
        public override bool ShouldApplyFireRateModifier()
        {
            return CanModifyFireRate();
        }

        /// <summary>
        /// Evaluates the configured shot-count-to-fire-rate curve. Values between control points are linearly interpolated,
        /// and values after the final control point hold the final multiplier.
        /// </summary>
        public override float GetFireRateMultiplier()
        {
            if (!ShouldApplyFireRateModifier())
            {
                return 1f;
            }

            if (_resolvedFireRateMultipliers == null || _resolvedFireRateMultipliers.Length == 0)
            {
                return 1f;
            }

            int shotCount = Mathf.Max(0, ShotCount);

            return shotCount < _resolvedFireRateMultipliers.Length
                ? _resolvedFireRateMultipliers[shotCount]
                : _resolvedFireRateMultipliers[_resolvedFireRateMultipliers.Length - 1];
        }

        /// <summary>
        /// Returns whether shot sound pitch should follow the progressive fire-rate multiplier.
        /// </summary>
        public override bool ModifyShotSoundPitch
        {
            get { return CanModifyFireRate(); }
        }

        /// <summary>
        /// Returns whether weapon fire sound behavior should use the progressive multiplier.
        /// </summary>
        public override bool ShouldApplySoundModifier()
        {
            return CanModifyFireRate(); 
        }

        private void BuildFireRateMultiplierTable()
        {
            if (_fireRateControlPoints == null || _fireRateControlPoints.Length == 0)
            {
                _resolvedFireRateMultipliers = new float[0];
                return;
            }

            FireRateControlPoint[] controlPoints = new FireRateControlPoint[_fireRateControlPoints.Length];
            _fireRateControlPoints.CopyTo(controlPoints, 0);
            System.Array.Sort(controlPoints, CompareControlPoints);

            int lastShotCount = controlPoints[controlPoints.Length - 1].ShotCount;
            _resolvedFireRateMultipliers = new float[lastShotCount + 1];

            FireRateControlPoint firstPoint = controlPoints[0];
            FillFireRateMultiplierRange(0, firstPoint.ShotCount, firstPoint.FireRateMultiplier);

            for (int i = 1; i < controlPoints.Length; i++)
            {
                FireRateControlPoint previousPoint = controlPoints[i - 1];
                FireRateControlPoint nextPoint = controlPoints[i];

                FillInterpolatedFireRateMultiplierRange(previousPoint, nextPoint);
            }
        }

        private static int CompareControlPoints(FireRateControlPoint left, FireRateControlPoint right)
        {
            return left.ShotCount.CompareTo(right.ShotCount);
        }

        private void FillFireRateMultiplierRange(int startShotCount, int endShotCount, float fireRateMultiplier)
        {
            for (int shotCount = startShotCount; shotCount <= endShotCount; shotCount++)
            {
                _resolvedFireRateMultipliers[shotCount] = fireRateMultiplier;
            }
        }

        private void FillInterpolatedFireRateMultiplierRange(FireRateControlPoint previousPoint, FireRateControlPoint nextPoint)
        {
            int previousShotCount = previousPoint.ShotCount;
            int nextShotCount = nextPoint.ShotCount;

            if (nextShotCount == previousShotCount)
            {
                _resolvedFireRateMultipliers[nextShotCount] = nextPoint.FireRateMultiplier;
                return;
            }

            for (int shotCount = previousShotCount; shotCount <= nextShotCount; shotCount++)
            {
                float progress = (float)(shotCount - previousShotCount) / (nextShotCount - previousShotCount);
                _resolvedFireRateMultipliers[shotCount] = Mathf.Lerp(previousPoint.FireRateMultiplier, nextPoint.FireRateMultiplier, progress);
            }
        }
    }
}
