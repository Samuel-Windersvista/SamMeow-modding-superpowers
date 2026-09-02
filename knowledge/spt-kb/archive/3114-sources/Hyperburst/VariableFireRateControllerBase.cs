using EFT.InventoryLogic;
using UnityEngine;
using static EFT.Player;

namespace Mass.HyperBurst
{
    /// <summary>
    /// Base controller for weapon mechanics that need shot tracking and dynamic fire-rate changes.
    /// Attach a derived controller to a WeaponContainer prefab in Unity Editor.
    /// </summary>
    public abstract class VariableFireRateControllerBase : MonoBehaviour
    {
        [SerializeField] private float _shotResetDelaySeconds = 0.05f;

        private int _shotCount = 0;
        private bool _isFiring = false;
        private float _shotTimer = 0f;
        private FirearmController _firearmController = null;
        private WeaponSoundPlayer _weaponSoundPlayer;

        /// <summary>
        /// Current FirearmController.
        /// </summary>
        public FirearmController FirearmController
        {
            get { return _firearmController; }
        }

        /// <summary>
        /// Current WeaponSoundPlayer.
        /// </summary>
        public WeaponSoundPlayer WeaponSoundPlayer
        {
            get { return _weaponSoundPlayer; }
        }

        /// <summary>
        /// Current number of consecutive tracked shots.
        /// </summary>
        public virtual int ShotCount
        {
            get { return _shotCount; }
        }

        /// <summary>
        /// Returns whether this controller should be used by runtime patches.
        /// </summary>
        public virtual bool IsControllerEnabled()
        {
            return isActiveAndEnabled;
        }

        private void Start()
        {
            _weaponSoundPlayer = GetComponent<WeaponSoundPlayer>();
        }

        private void Update()
        {
            UpdateShotTracking();
        }

        /// <summary>
        /// Returns whether this controller can currently modify weapon fire rate.
        /// Override this to implement custom activation rules for variable fire-rate weapons.
        /// </summary>
        public virtual bool CanModifyFireRate()
        {
            return _firearmController != null && _firearmController.Item != null && _firearmController.Item.SelectedFireMode != Weapon.EFireMode.single;
        }

        /// <summary>
        /// Returns whether this controller should track consecutive shots for the current weapon.
        /// </summary>
        public virtual bool CanTrackShots()
        {
            return CanModifyFireRate();
        }

        /// <summary>
        /// Returns the current weapon fire-rate multiplier.
        /// Override this to implement custom variable fire-rate behavior.
        /// </summary>
        public virtual float GetFireRateMultiplier()
        {
            return 1f;
        }

        /// <summary>
        /// Returns whether the fire-rate patch should apply this controller's multiplier.
        /// </summary>
        public virtual bool ShouldApplyFireRateModifier()
        {
            return CanModifyFireRate();
        }

        /// <summary>
        /// Returns whether this controller should modify recoil for the current shot.
        /// </summary>
        public virtual bool ShouldApplyRecoilModifier()
        {
            return false;
        }

        /// <summary>
        /// Returns the recoil multiplier applied when ShouldApplyRecoilModifier returns true.
        /// </summary>
        public virtual float GetRecoilMultiplier()
        {
            return 1f;
        }

        /// <summary>
        /// Returns whether this controller should modify shot spread for the current shot.
        /// </summary>
        public virtual bool ShouldApplySpreadModifier()
        {
            return false;
        }

        /// <summary>
        /// Returns the spread value applied when ShouldApplySpreadModifier returns true.
        /// </summary>
        public virtual int GetSpread()
        {
            return 0;
        }

        /// <summary>
        /// Returns whether this controller should modify weapon fire sound behavior.
        /// </summary>
        public virtual bool ShouldApplySoundModifier()
        {
            return false;
        }

        /// <summary>
        /// Should WeaponSoundPlayer play separate shot sound.
        /// </summary>
        public virtual bool PlaySeparateShotSound
        {
            get { return false; }
        }

        /// <summary>
        /// Returns whether this controller should modify shot sound pitch.
        /// </summary>
        public virtual bool ModifyShotSoundPitch
        {
            get { return false; }
        }

        /// <summary>
        /// Returns whether this controller should override the weapon sound queue behavior.
        /// </summary>
        public virtual bool OverrideSoundEnqueue
        {
            get { return false; }
        }

        /// <summary>
        /// Notify controller that a weapon firing sound has been played.
        /// </summary>
        public virtual bool OnFireSoundPlayed()
        {
            return true;
        }

        /// <summary>
        /// Notify controller that a weapon is about to be fired.
        /// </summary>
        public virtual bool OnWeaponToFire(FirearmController fc)
        {
            if (_firearmController != fc)
            {
                ResetController(fc);
            }
            if (CanTrackShots() == false)
            {
                return false;
            }
            _isFiring = true;
            return true;
        }

        /// <summary>
        /// Notify controller that a weapon has been fired.
        /// </summary>
        public virtual void OnWeaponFired()
        {
            if (CanTrackShots() == false)
            {
                return;
            }
            _shotCount++;
            OnShotCountChanged();
        }

        /// <summary>
        /// Notify controller that FirearmController has changed.
        /// </summary>
        public virtual void ResetController(FirearmController fc)
        {
            _firearmController = fc;
            ResetShotTracking();
        }

        /// <summary>
        /// Called after ShotCount changes.
        /// Override this to update custom shot-based state.
        /// </summary>
        protected virtual void OnShotCountChanged()
        {
        }

        /// <summary>
        /// Called when this controller resets its consecutive shot state.
        /// Override this to reset custom state in derived controllers.
        /// </summary>
        protected virtual void OnMechanicReset()
        {
        }

        /// <summary>
        /// Resets the tracked consecutive shot state.
        /// </summary>
        protected virtual void ResetShotTracking()
        {
            _isFiring = false;
            _shotCount = 0;
            _shotTimer = 0f;
            OnMechanicReset();
        }

        private void UpdateShotTracking()
        {
            if (_firearmController == null)
            {
                return;
            }

            if (_isFiring)
            {
                _shotTimer += Time.deltaTime;
                if (!_firearmController.autoFireOn && _shotTimer >= _shotResetDelaySeconds)
                {
                    ResetShotTracking();
                }
            }
        }
    }
}
