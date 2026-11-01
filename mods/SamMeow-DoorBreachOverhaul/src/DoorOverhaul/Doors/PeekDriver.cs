using System;
using DoorBreach;
using EFT;
using EFT.Interactive;
using DoorOverhaul.Actions;
using UnityEngine;

namespace DoorOverhaul.Doors
{
    internal static class PeekDriver
    {
        private enum Phase
        {
            None,
            WaitingForUnlatch,
            Driving
        }

        private const float UnlatchTimeout = 4f;
        private const float EaseInSeconds = 0.25f;
        private const float GlidePace = 0.6f;
        private const float MinGlideSeconds = 0.3f;

        private const float CrackFullPaceAt = 0.4f;
        private const float MinCrackPace = 0.3f;

        private const float TapSeconds = 0.25f;

        private static Phase _phase;
        private static Door _door;
        private static bool _hold;
        private static Func<bool> _held;
        private static bool _released;
        private static float _releasedAt;
        private static float _target;
        private static float _tapTarget;
        private static float _fraction;
        private static float _beginTime;
        private static float _phaseStart;
        private static Action _onFinished;

        public static bool IsActive => _phase != Phase.None;
        public static Door Door => _door;

        public static void BeginHold(GamePlayerOwner owner, Door door, Func<bool> held, float tapTarget, Action onFinished = null)
        {
            Begin(owner, door, true, held, tapTarget, onFinished);
        }

        public static void BeginAuto(GamePlayerOwner owner, Door door, float target, Action onFinished = null)
        {
            Begin(owner, door, false, null, target, onFinished);
        }

        private static void Begin(GamePlayerOwner owner, Door door, bool hold, Func<bool> held, float target, Action onFinished)
        {
            Cancel();

            if (!DoorMath.CanSwing(door) || !door.Operatable)
            {
                onFinished?.Invoke();
                return;
            }

            float fullyOpen = DoorMath.FullyOpenFraction(door);
            _door = door;
            _hold = hold && held != null;
            _held = held;
            _beginTime = Time.time;
            _released = _hold && !held();
            _releasedAt = _released ? Time.time : -1f;
            _tapTarget = Mathf.Min(Mathf.Clamp01(target), fullyOpen);
            _target = _hold ? fullyOpen : _tapTarget;
            _onFinished = onFinished;
            _phaseStart = Time.time;

            if (door.DoorState == EDoorState.Shut)
            {
                float crack = _hold ? Settings.MinimumPeekPercent.Value / 100f : _target;
                float pace = Mathf.Clamp(crack / CrackFullPaceAt, MinCrackPace, 1f);
                if (!DoorActions.OpenWithOverride(owner, door, crack, pace, Settings.SilentPeek.Value))
                {
                    Finish();
                    return;
                }

                _phase = Phase.WaitingForUnlatch;
            }
            else if (door.DoorState == EDoorState.Open)
            {
                StartDriving();
            }
            else
            {
                Finish();
            }
        }

        public static void Tick()
        {
            if (_phase == Phase.None)
            {
                return;
            }

            if (_door == null)
            {
                Finish();
                return;
            }

            if (_hold && !_released && !_held())
            {
                _released = true;
                _releasedAt = Time.time;
            }

            if (_hold && _released && _releasedAt - _beginTime <= TapSeconds)
            {
                _hold = false;
                _target = _tapTarget;
                if (_phase == Phase.Driving)
                {
                    BeginGlide();
                }
            }

            if (_phase == Phase.WaitingForUnlatch)
            {
                EDoorState state = _door.DoorState;
                if (state == EDoorState.Open)
                {
                    if (_hold && _released)
                    {
                        Finish();
                        return;
                    }

                    StartDriving();
                }
                else if (Time.time - _phaseStart > UnlatchTimeout
                    || state == EDoorState.Locked
                    || (state == EDoorState.Shut && Time.time - _phaseStart > 1.5f))
                {
                    DoorOpenOverrides.Disarm(_door);
                    Finish();
                }

                return;
            }

            if (_door.DoorState != EDoorState.Open)
            {
                Finish();
                return;
            }

            if (_hold && _released)
            {
                Finish();
                return;
            }

            float goal = _target;
            bool arrived;
            if (_hold)
            {
                float speed = 1f / Mathf.Max(0.1f, Settings.SecondsToFullyOpen.Value);
                speed *= Mathf.Lerp(0.3f, 1f, Mathf.Clamp01((Time.time - _phaseStart) / EaseInSeconds));
                _fraction = Mathf.MoveTowards(_fraction, goal, speed * Time.deltaTime);
                arrived = Mathf.Abs(_fraction - goal) <= 0.0005f;
            }
            else
            {
                float t = Mathf.Clamp01((Time.time - _glideStart) / _glideSeconds);
                _fraction = Mathf.Lerp(_glideFrom, goal, t * t * (3f - 2f * t));
                arrived = t >= 1f;
            }

            _door.CurrentAngle = DoorMath.AngleAt(_door, _fraction);
            if (arrived)
            {
                Finish();
            }
        }

        private static float _glideFrom;
        private static float _glideStart;
        private static float _glideSeconds = 1f;

        private static void BeginGlide()
        {
            _glideFrom = _fraction;
            _glideStart = Time.time;
            _glideSeconds = Mathf.Max(MinGlideSeconds, Mathf.Abs(_target - _fraction) * Settings.SecondsToFullyOpen.Value * GlidePace);
        }

        public static void Cancel()
        {
            _phase = Phase.None;
            _door = null;
            _held = null;
            _onFinished = null;
        }

        private static void StartDriving()
        {
            _phase = Phase.Driving;
            _fraction = DoorMath.OpenFraction(_door);
            _phaseStart = Time.time;
            if (!_hold)
            {
                BeginGlide();
            }
        }

        private static void Finish()
        {
            Action callback = _onFinished;
            _phase = Phase.None;
            _door = null;
            _held = null;
            _onFinished = null;
            callback?.Invoke();
        }
    }
}
