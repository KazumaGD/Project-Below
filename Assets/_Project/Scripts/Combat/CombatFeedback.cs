using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBelow.Combat
{
    [DisallowMultipleComponent, RequireComponent(typeof(CombatController), typeof(AudioSource))]
    public sealed class CombatFeedback : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private RawImage _damageVignette;
        [SerializeField] private RawImage _guardVignette;
        [SerializeField] private RectTransform _enemyHealthFill;
        [SerializeField] private RectTransform _playerHealthFill;

        [Header("Enemy motion")]
        [SerializeField, Min(0f)] private float _enemyMotionScale = 1f;
        [SerializeField, Min(0f)] private float _attackDuration = 0.28f;
        [SerializeField, Min(0f)] private float _heavyAttackDuration = 0.46f;
        [SerializeField, Min(0f)] private float _attackReturnDuration = 0.24f;
        [SerializeField, Min(0f)] private float _preparationDuration = 0.52f;
        [SerializeField, Min(0f)] private float _recoveryDuration = 0.48f;
        [SerializeField, Min(0f)] private float _enemyHitDuration = 0.32f;
        [SerializeField, Min(0f)] private float _deathDuration = 0.82f;
        [SerializeField] private Color _enemyHitColor = new(1f, 0.22f, 0.10f, 0.87f);

        [Header("Player and HUD")]
        [SerializeField] private Color _damageColor = new(0.52f, 0.025f, 0.035f, 0.64f);
        [SerializeField, Min(0f)] private float _damageDuration = 0.36f;
        [SerializeField, Min(0f)] private float _shakeDistance = 0.045f;
        [SerializeField, Min(0f)] private float _shakeFrequency = 17f;
        [SerializeField] private Color _guardColor = new(0.13f, 0.48f, 0.59f, 0.50f);
        [SerializeField, Min(0f)] private float _guardDuration = 0.28f;
        [SerializeField, Min(0f)] private float _healthDuration = 0.38f;

        [Header("Replaceable clips")]
        [SerializeField] private AudioClip _enemyHitClip;
        [SerializeField] private AudioClip _normalAttackClip;
        [SerializeField] private AudioClip _heavyAttackClip;
        [SerializeField] private AudioClip _preparationClip;
        [SerializeField] private AudioClip _recoveryClip;
        [SerializeField] private AudioClip _guardReadyClip;
        [SerializeField] private AudioClip _guardImpactClip;
        [SerializeField] private AudioClip _deathClip;
        [SerializeField] private AudioClip _fleeClip;

        private CombatController _combat;
        private AudioSource _audio;
        private Coroutine _playback;
        private double _soundEndsAt;
        private Transform _view;
        private Vector3 _viewPosition;
        private Transform _visualRoot;
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private Vector3 _towardPlayer;

        private readonly List<TintTarget> _tintTargets = new();

        internal bool IsPlaying => _playback != null
            || (_audio != null && _audio.isActiveAndEnabled && Time.unscaledTimeAsDouble < _soundEndsAt);

        private void Awake()
        {
            _combat = GetComponent<CombatController>();
            _audio = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (_damageVignette == null || _guardVignette == null || _enemyHealthFill == null || _playerHealthFill == null)
            {
                Debug.LogError("Assign both combat vignettes and health fills.", this);
                enabled = false;
                return;
            }

            _combat.Changed += OnCombatChanged;
            if (_combat.Enemy != null)
                BindEncounter();
        }

        private void OnDisable()
        {
            _combat.Changed -= OnCombatChanged;
            ClearEncounter();
        }

        private void OnCombatChanged(CombatEvent combatEvent, int damage, bool blocked)
        {
            if (combatEvent == CombatEvent.Opened)
            {
                BindEncounter();
                return;
            }
            if (combatEvent == CombatEvent.Closed)
            {
                ClearEncounter();
                return;
            }

            bool enemyAttack = combatEvent is CombatEvent.EnemyAttack or CombatEvent.EnemyHeavyAttack;
            bool playerHurt = enemyAttack && damage > 0;
            bool healthChanged = damage > 0 && (enemyAttack || combatEvent == CombatEvent.PlayerAttack);
            bool heavy = _combat.EnemyIntent == EnemyAction.HeavyAttack;
            float poseDuration = combatEvent switch
            {
                CombatEvent.EnemyAttackWindup => heavy ? _heavyAttackDuration : _attackDuration,
                CombatEvent.EnemyAttack or CombatEvent.EnemyHeavyAttack => _attackReturnDuration,
                CombatEvent.EnemyPrepared => _preparationDuration,
                CombatEvent.EnemyRecovered => _recoveryDuration,
                CombatEvent.PlayerAttack => _enemyHitDuration,
                CombatEvent.Victory => _deathDuration,
                _ => 0f
            };

            PlaySound(combatEvent switch
            {
                CombatEvent.PlayerAttack => _enemyHitClip,
                CombatEvent.EnemyAttack when playerHurt => _normalAttackClip,
                CombatEvent.EnemyHeavyAttack when playerHurt => _heavyAttackClip,
                CombatEvent.EnemyPrepared => _preparationClip,
                CombatEvent.EnemyRecovered => _recoveryClip,
                CombatEvent.Defended => _guardReadyClip,
                CombatEvent.Victory => _deathClip,
                CombatEvent.FleeFailed or CombatEvent.Escaped => _fleeClip,
                _ => null
            });
            if (blocked)
                PlaySound(_guardImpactClip);

            if (poseDuration > 0f || playerHurt || blocked || healthChanged || combatEvent == CombatEvent.Victory)
                _playback = StartCoroutine(PlayVisuals(combatEvent, heavy, poseDuration, playerHurt, blocked, healthChanged));
        }

        private void BindEncounter()
        {
            ClearEncounter();
            _view = GetComponent<GameModeController>().PlayerView;
            if (_view != null)
                _viewPosition = _view.localPosition;
            SetFill(_playerHealthFill, HealthRatio(_combat.Player));
            SetFill(_enemyHealthFill, HealthRatio(_combat.Enemy.State));

            _visualRoot = _combat.Enemy.VisualRoot;
            if (_visualRoot == null)
                return;

            _restPosition = _visualRoot.localPosition;
            _restRotation = _visualRoot.localRotation;
            Vector3 toward = _view != null ? Vector3.ProjectOnPlane(_view.position - _visualRoot.position, Vector3.up) : Vector3.zero;
            _towardPlayer = toward.sqrMagnitude > 0.0001f
                ? _visualRoot.parent.InverseTransformDirection(toward.normalized)
                : Vector3.forward;

            foreach (Renderer renderer in _visualRoot.GetComponentsInChildren<Renderer>())
            {
                Material material = renderer.sharedMaterial;
                if (material != null && material.HasProperty(BaseColor))
                    _tintTargets.Add(new TintTarget(renderer, material));
            }
        }

        private IEnumerator PlayVisuals(CombatEvent cue, bool heavy, float poseDuration, bool hurt, bool blocked, bool healthChanged)
        {
            float damageDuration = hurt ? _damageDuration : 0f;
            float guardDuration = blocked ? _guardDuration : 0f;
            float healthDuration = healthChanged ? _healthDuration : 0f;
            float total = Mathf.Max(Mathf.Max(poseDuration, damageDuration), Mathf.Max(guardDuration, healthDuration));
            float playerStart = _playerHealthFill.anchorMax.x;
            float enemyStart = _enemyHealthFill.anchorMax.x;
            float playerTarget = HealthRatio(_combat.Player);
            float enemyTarget = HealthRatio(_combat.Enemy.State);
            float elapsed = 0f;

            while (true)
            {
                ApplyEnemyPose(cue, heavy, Progress(elapsed, poseDuration));
                ApplyPlayerImpact(elapsed, damageDuration, guardDuration);
                if (healthChanged)
                {
                    float eased = Mathf.SmoothStep(0f, 1f, Progress(elapsed, healthDuration));
                    SetFill(_playerHealthFill, Mathf.Lerp(playerStart, playerTarget, eased));
                    SetFill(_enemyHealthFill, Mathf.Lerp(enemyStart, enemyTarget, eased));
                }
                // Complete at least one frame after assignment so a zero-duration cue releases the lock as well.
                yield return null;
                if (elapsed >= total)
                    break;
                elapsed = Mathf.Min(elapsed + Time.deltaTime, total);
            }

            _playback = null;
        }

        private void ApplyEnemyPose(CombatEvent cue, bool heavy, float progress)
        {
            if (_visualRoot == null)
                return;

            float pulse = progress >= 1f ? 0f : Mathf.Sin(progress * Mathf.PI);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            float lunge = (heavy ? 0.57f : 0.34f) * _enemyMotionScale;
            float lean = (heavy ? 26f : 17f) * _enemyMotionScale;
            float strength = _enemyMotionScale;

            switch (cue)
            {
                case CombatEvent.EnemyAttackWindup:
                    float strike = progress < 0.55f
                        ? Mathf.SmoothStep(0f, -0.28f, progress / 0.55f)
                        : Mathf.SmoothStep(-0.28f, 1f, (progress - 0.55f) / 0.45f);
                    SetPose(_towardPlayer * (lunge * strike), new Vector3(lean * strike, 0f, 0f));
                    break;
                case CombatEvent.EnemyAttack:
                case CombatEvent.EnemyHeavyAttack:
                    SetPose(_towardPlayer * (lunge * (1f - eased)), new Vector3(lean * (1f - eased), 0f, 0f));
                    break;
                case CombatEvent.EnemyPrepared:
                    SetPose(Vector3.up * (0.11f * strength * pulse), new Vector3(-21f * strength * pulse, 0f, 0f));
                    break;
                case CombatEvent.EnemyRecovered:
                    SetPose((_towardPlayer * -0.15f + Vector3.down * 0.11f) * (strength * pulse),
                        new Vector3(24f * strength * pulse, 0f, 0f));
                    break;
                case CombatEvent.PlayerAttack:
                    SetPose(_towardPlayer * (-0.24f * strength * pulse), new Vector3(-13f * strength * pulse, 0f, 12f * strength * pulse));
                    ApplyHitTint(1f - Mathf.SmoothStep(0f, 1f, progress));
                    break;
                case CombatEvent.Victory:
                    float fall = progress * progress * strength;
                    SetPose(Vector3.down * (0.14f * fall), new Vector3(0f, 0f, Mathf.Min(88f, 83f * fall)));
                    break;
            }
        }

        private void ApplyPlayerImpact(float elapsed, float damageDuration, float guardDuration)
        {
            float damageFade = 1f - Mathf.SmoothStep(0f, 1f, Progress(elapsed, damageDuration));
            float guardFade = 1f - Mathf.SmoothStep(0f, 1f, Progress(elapsed, guardDuration));
            SetTint(_damageVignette, _damageColor, damageFade);
            SetTint(_guardVignette, _guardColor, guardFade);
            if (_view == null)
                return;

            float cycle = elapsed * _shakeFrequency * (2f * Mathf.PI);
            var shake = new Vector3(Mathf.Cos(cycle), Mathf.Sin(cycle * 1.29f), 0f);
            _view.localPosition = _viewPosition + shake * (_shakeDistance * damageFade);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null || !_audio.isActiveAndEnabled || Mathf.Approximately(_audio.pitch, 0f))
                return;

            _audio.PlayOneShot(clip);
            double end = Time.unscaledTimeAsDouble + clip.length / Mathf.Abs(_audio.pitch);
            if (end > _soundEndsAt)
                _soundEndsAt = end;
        }

        private void ClearEncounter()
        {
            if (_playback != null)
                StopCoroutine(_playback);
            _playback = null;
            _soundEndsAt = 0d;
            if (_audio != null)
                _audio.Stop();
            if (_view != null)
                _view.localPosition = _viewPosition;
            if (_visualRoot != null)
                SetPose(Vector3.zero, Vector3.zero);
            ApplyHitTint(0f);
            _tintTargets.Clear();
            _view = null;
            _visualRoot = null;
            if (_damageVignette != null)
                SetTint(_damageVignette, _damageColor, 0f);
            if (_guardVignette != null)
                SetTint(_guardVignette, _guardColor, 0f);
        }

        private void ApplyHitTint(float strength)
        {
            foreach (TintTarget target in _tintTargets)
            {
                if (target.Renderer == null)
                    continue;
                if (strength > 0f && _enemyHitColor.a > 0f)
                {
                    target.Flash.SetColor(BaseColor, Color.Lerp(target.Color, _enemyHitColor, _enemyHitColor.a * strength));
                    target.Renderer.SetPropertyBlock(target.Flash);
                }
                else
                    target.Renderer.SetPropertyBlock(target.Original);
            }
        }

        private void SetPose(Vector3 offset, Vector3 euler)
        {
            _visualRoot.SetLocalPositionAndRotation(_restPosition + offset, _restRotation * Quaternion.Euler(euler));
        }

        private static float HealthRatio(CombatantState state) => (float)state.Health / state.MaxHealth;
        private static float Progress(float elapsed, float duration) => duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

        private static void SetFill(RectTransform fill, float fraction) => fill.anchorMax = new Vector2(fraction, 1f);

        private static void SetTint(RawImage image, Color color, float strength)
        {
            color.a *= strength;
            image.color = color;
        }

        private sealed class TintTarget
        {
            internal readonly Renderer Renderer;
            internal readonly Color Color;
            internal readonly MaterialPropertyBlock Original = new();
            internal readonly MaterialPropertyBlock Flash = new();

            internal TintTarget(Renderer renderer, Material material)
            {
                Renderer = renderer;
                renderer.GetPropertyBlock(Original);
                renderer.GetPropertyBlock(Flash);
                Color = Original.HasColor(BaseColor) ? Original.GetColor(BaseColor) : material.GetColor(BaseColor);
            }
        }
    }
}
