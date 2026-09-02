using UnityEngine;

namespace Mass.HyperBurst
{
    /// <summary>
    /// Hyper-burst implementation.
    /// Applies high fire rate, reduced recoil, extra spread, and sound handling during the opening shots.
    /// </summary>
    public class HyperBurstController : VariableFireRateControllerBase
    {
        [SerializeField] private float _fireRateMultiplier = 4f;
        [SerializeField] private float _recoilMultiplier = 0.5f;
        [SerializeField] private int _shotThreshold = 2;
        [SerializeField] private int _spread = 50;

        private bool _isHyperBursting = false;
        private int _shotSoundCount = 0;
        private bool _playSeparateShotSound = false;

        /// <summary>
        /// Returns true when the weapon is currently inside the hyper-burst shot window.
        /// </summary>
        public virtual bool IsHyperBursting
        {
            get { return CanModifyFireRate() && _isHyperBursting; }
        }

        /// <summary>
        /// Legacy property for existing integrations.
        /// </summary>
        public virtual float BurstROFMulti
        {
            get { return GetFireRateMultiplier(); }
        }

        /// <summary>
        /// Legacy property for existing integrations.
        /// </summary>
        public virtual float BurstRecoilMulti
        {
            get { return GetRecoilMultiplier(); }
        }

        /// <summary>
        /// Legacy property for existing integrations.
        /// </summary>
        public virtual int BurstSpread
        {
            get { return GetSpread(); }
        }

        /// <summary>
        /// Returns the current weapon fire-rate multiplier.
        /// </summary>
        public override float GetFireRateMultiplier()
        {
            return ShouldApplyFireRateModifier() ? _fireRateMultiplier : 1f;
        }

        /// <summary>
        /// Returns whether the fire-rate patch should apply the hyper-burst multiplier.
        /// </summary>
        public override bool ShouldApplyFireRateModifier()
        {
            return IsHyperBursting;
        }

        /// <summary>
        /// Returns whether recoil should be reduced during hyper-burst.
        /// </summary>
        public override bool ShouldApplyRecoilModifier()
        {
            return IsHyperBursting;
        }

        /// <summary>
        /// Returns the recoil multiplier applied during hyper-burst.
        /// </summary>
        public override float GetRecoilMultiplier()
        {
            return _recoilMultiplier;
        }

        /// <summary>
        /// Returns whether spread should be added during hyper-burst.
        /// </summary>
        public override bool ShouldApplySpreadModifier()
        {
            return IsHyperBursting;
        }

        /// <summary>
        /// Returns the spread value applied during hyper-burst.
        /// </summary>
        public override int GetSpread()
        {
            return _spread;
        }

        /// <summary>
        /// Returns whether weapon fire sound behavior should be adjusted during hyper-burst.
        /// </summary>
        public override bool ShouldApplySoundModifier()
        {
            return CanModifyFireRate() && _shotSoundCount <= _shotThreshold + 2;
        }

        /// <summary>
        /// Should WeaponSoundPlayer play separate shot sound.
        /// </summary>
        public override bool PlaySeparateShotSound
        {
            get { return _playSeparateShotSound; }
        }

        /// <summary>
        /// Returns whether this controller should override the weapon sound queue behavior.
        /// </summary>
        public override bool OverrideSoundEnqueue
        {
            get { return true; }
        }

        /// <summary>
        /// Notify controller that a weapon is about to be fired.
        /// </summary>
        public override bool OnWeaponToFire(EFT.Player.FirearmController fc)
        {
            bool canTrackShots = base.OnWeaponToFire(fc);
            _isHyperBursting = canTrackShots && ShotCount < _shotThreshold;
            return _isHyperBursting;
        }

        /// <summary>
        /// Notify controller that a weapon has been fired.
        /// </summary>
        public override void OnWeaponFired()
        {
            base.OnWeaponFired();
            _isHyperBursting = ShotCount < _shotThreshold;
        }

        /// <summary>
        /// Notify controller that a weapon firing sound has been played.
        /// </summary>
        public override bool OnFireSoundPlayed()
        {
            _shotSoundCount++;
            _playSeparateShotSound = _shotSoundCount < _shotThreshold;
            return _shotSoundCount != _shotThreshold + 1 && _shotSoundCount != 1;
        }

        /// <summary>
        /// Notify controller that controller data should be reset.
        /// </summary>
        protected override void OnMechanicReset()
        {
            _isHyperBursting = false;
            _shotSoundCount = 0;
            _playSeparateShotSound = true;
        }
    }
}
