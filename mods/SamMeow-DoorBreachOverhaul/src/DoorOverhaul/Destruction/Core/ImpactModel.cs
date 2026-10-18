using System;

namespace DoorOverhaul.Destruction.Core
{
    public sealed class MaterialProfile
    {
        public string Name = "Wood";

        public float ThroughPenetration = 25f;

        public float CellHealth = 120f;

        public float BondDamageFactor = 0.6f;

        public float BondStrength = 20000f;

        public float HingeHealth = 600f;
        public float LatchHealth = 300f;

        public float Density = 550f;

        public bool AllowChunks = true;

        public float DamageScale = 1f;

        public float HoleScale = 2.5f;

        public float ThroughDamageScale = 1.5f;
        public float ThroughMinCellShare = 1.05f;

        public float CrackRadius = 0.12f;
        public float CrackShare = 0.35f;

        public float BreachScale = 0.55f;

        public float BreachAnchorScale = 2.6f;

        public float HoleSizeScale = 1f;

        public static MaterialProfile WoodThin()
        {
            return new MaterialProfile { Name = "WoodThin" };
        }

        public static MaterialProfile WoodThick()
        {
            return new MaterialProfile
            {
                Name = "WoodThick",
                ThroughPenetration = 30f,
                CellHealth = 260f,
                BondStrength = 40000f,
                HingeHealth = 1000f,
                LatchHealth = 500f,
                Density = 700f,
                HoleScale = 2f,
                ThroughMinCellShare = 0.55f,
                CrackShare = 0.25f,
                BreachScale = 0.4f
            };
        }

        public static MaterialProfile MetalThin()
        {
            return new MaterialProfile
            {
                Name = "MetalThin",
                ThroughPenetration = 40f,
                CellHealth = 450f,
                BondStrength = 150000f,
                HingeHealth = 1500f,
                LatchHealth = 700f,
                Density = 2500f,
                AllowChunks = false,
                HoleScale = 1.2f,
                ThroughDamageScale = 1f,
                ThroughMinCellShare = 0.35f,
                CrackShare = 0f,
                BreachScale = 0.15f,
                BreachAnchorScale = 1f
            };
        }
    }

    public enum ImpactSource
    {
        Bullet,
        GrenadeFragment,
        Blast
    }

    public sealed class ImpactInput
    {
        public ImpactSource Source = ImpactSource.Bullet;
        public float PenetrationPower;
        public float Damage;
        public float DiameterMm = 5.56f;
        public float MassGram = 4f;
        public int ProjectileCount = 1;

        public float Distance;

        public bool ExitSeen;
    }

    public sealed class ImpactKernel
    {
        public float CoreRadius;

        public float Radius;

        public float Amount;
        public bool Through;

        public bool Breaching;

        public float CrackRadius;
        public float CrackAmount;

        public float StruckCellAmount;

        public float StruckRadius;

        public float AnchorScale = 1f;

        public float Impulse;
    }

    public static class ImpactModel
    {
        private const float SurfaceShare = 0.2f;
        private const float AssumedImpactSpeed = 400f;

        private const float SlugMassGram = 15f;

        private const float BreachFullRange = 5f;
        private const float BreachFarRange = 25f;
        private const float BreachFarShare = 0.3f;

        public static ImpactKernel Evaluate(ImpactInput hit, MaterialProfile material, float cellSize)
        {
            float diameter = Math.Max(hit.DiameterMm, 2f) / 1000f;
            float damage = Math.Max(0f, hit.Damage) * material.DamageScale;
            bool through = hit.PenetrationPower >= material.ThroughPenetration;
            bool breaching = !through
                && hit.Source == ImpactSource.Bullet
                && (hit.ProjectileCount > 1 || hit.MassGram >= SlugMassGram);

            var kernel = new ImpactKernel
            {
                Through = through,
                Breaching = breaching,
                Impulse = Math.Max(0f, hit.MassGram) / 1000f * AssumedImpactSpeed
            };

            if (through)
            {
                kernel.Amount = damage * material.ThroughDamageScale;
                kernel.StruckCellAmount = Math.Max(0f, material.CellHealth * material.ThroughMinCellShare * material.DamageScale - kernel.Amount);
                kernel.CoreRadius = diameter * material.HoleScale;
                kernel.Radius = Math.Max(cellSize * 0.6f, diameter * 4f);
                kernel.CrackRadius = material.CrackRadius;
                kernel.CrackAmount = kernel.Amount * material.CrackShare;
            }
            else if (breaching)
            {
                kernel.Amount = damage * material.BreachScale * BreachRangeShare(hit.Distance);
                kernel.CoreRadius = diameter * 2f;
                kernel.Radius = Math.Max(cellSize * 0.75f, diameter * 6f);
                kernel.CrackRadius = cellSize * 2.5f;
                kernel.AnchorScale = material.BreachAnchorScale;
                kernel.CrackAmount = kernel.Amount * material.CrackShare;
            }
            else
            {
                kernel.Amount = damage * SurfaceShare;
                kernel.CoreRadius = diameter * 1.5f;
                kernel.Radius = Math.Max(cellSize * 0.75f, diameter * 6f);
            }

            if (hit.Source == ImpactSource.GrenadeFragment)
            {
                kernel.Radius = Math.Max(cellSize * 0.6f, diameter * 4f);
            }

            float scale = Math.Max(0.5f, material.HoleSizeScale);
            kernel.CoreRadius *= scale;
            kernel.Radius *= scale;
            kernel.CrackRadius *= MathF.Sqrt(scale);
            if (kernel.StruckCellAmount > 0f && scale > 1f)
            {
                kernel.StruckRadius = (scale - 1f) * cellSize * 0.5f;
            }

            kernel.Radius = Math.Max(kernel.Radius, kernel.CoreRadius);
            return kernel;
        }

        public static float BreachRangeShare(float distance)
        {
            if (distance <= BreachFullRange)
            {
                return 1f;
            }

            float t = Math.Clamp((distance - BreachFullRange) / (BreachFarRange - BreachFullRange), 0f, 1f);
            return 1f + (BreachFarShare - 1f) * t;
        }

        public static ImpactKernel Blast(float strength, float distance, float range, float cellSize)
        {
            float falloff = range <= 0f ? 0f : Math.Clamp(1f - distance / range, 0f, 1f);
            return new ImpactKernel
            {
                CoreRadius = cellSize * 1.5f + 0.2f * falloff * falloff,
                Radius = cellSize * 3f + 0.35f * falloff,
                Amount = strength * falloff * falloff * falloff,
                Through = falloff > 0.5f,
                Impulse = 25f * falloff
            };
        }

        public static float Falloff(float distance, float coreRadius, float radius)
        {
            if (distance <= coreRadius)
            {
                return 1f;
            }

            if (distance >= radius || radius <= coreRadius)
            {
                return 0f;
            }

            float t = (distance - coreRadius) / (radius - coreRadius);
            return (1f - t) * (1f - t);
        }
    }
}
