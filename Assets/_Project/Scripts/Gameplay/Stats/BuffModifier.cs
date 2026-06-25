using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTemplate.Gameplay.Stats
{
    // ============================================================
    // ENUMS
    // ============================================================

    /// <summary>Trigger event để kích hoạt buff effect.</summary>
    public enum BuffTriggerType
    {
        OnAttack,        // Khi character này attack enemy → buff effect chạy
        OnDamaged,       // Khi nhận damage từ enemy → buff effect chạy (vd: thorns)
        OnKill,          // Khi giết enemy → buff effect chạy (vd: regen on-kill)
        OnHpThreshold,   // Khi HP xuống dưới % nhất định
    }

    /// <summary>Loại effect khi trigger fire.</summary>
    public enum TriggerEffect
    {
        HealSelf,         // Hồi HP cho mình (HealAmount)
        DamageAttacker,   // Damage ngược lại attacker (DamageAmount) - cần context
        DamagePercent,    // Damage % HP attacker
        AddBuffSelf,      // Apply 1 buff khác lên mình (BuffToApply)
    }

    // ============================================================
    // BUFF DEFINITION - ScriptableObject (asset designer config)
    // ============================================================

    /// <summary>
    /// BuffDefinition - template của 1 buff.
    ///
    /// 3 logic độc lập, bật/tắt qua cờ:
    ///   ☑ HasStatModifiers  → Cộng/trừ stats (Attack, Defense, MoveSpeed...)
    ///   ☑ HasTickEffect     → DoT/HoT mỗi tickInterval giây
    ///   ☑ HasTriggerEffect  → On-attack, On-damaged, On-kill, On-HP-threshold
    ///
    /// Designer tự do combo - vd:
    ///   - Berserk: chỉ Stats (+50% ATK 10s)
    ///   - Poison: chỉ Tick (-2 HP/s, stack 5)
    ///   - Thorns: chỉ Trigger (OnDamaged → DamageAttacker 5)
    ///   - Vampiric Aura: Stats (+10% ATK) + Trigger (OnAttack → HealSelf 5%)
    ///   - Berserk Rage: HpThreshold (HP <30%) → AddBuffSelf (Berserk)
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Buff Definition", fileName = "Buff_")]
    public class BuffDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _buffId = "berserk";
        [SerializeField] private string _displayName = "Berserk";
        [SerializeField] private Sprite _icon;
        [SerializeField, TextArea] private string _description = "Attack +50% trong 10s.";

        [Header("Duration")]
        [Tooltip("Duration seconds. -1 = infinite (cần remove tay, vd: toggle aura).")]
        [SerializeField] private float _durationSeconds = 10f;

        [Header("Stacking")]
        [Tooltip("Cho phép stack? Nếu false, re-apply chỉ refresh duration.")]
        [SerializeField] private bool _stackable = false;
        [SerializeField] private int _maxStacks = 5;
        [SerializeField] private bool _refreshDurationOnMaxStack = true;

        // ═══════════════════════════════════════════════════════════
        // CỜ KÍCH HOẠT 3 LOGIC - tự do combo
        // ═══════════════════════════════════════════════════════════

        [Header("☑ Logic 1: Stat Modifiers (cộng/trừ stats)")]
        [SerializeField] private bool _hasStatModifiers = true;
        [Tooltip("Mỗi stack nhân các value này lên.")]
        [SerializeField] private BuffStatModifier[] _statModifiers;

        [Header("☑ Logic 2: Tick Effect (DoT / HoT)")]
        [SerializeField] private bool _hasTickEffect = false;
        [SerializeField] private float _tickInterval = 1f;
        [Tooltip("Tick mỗi interval. Dương = damage, âm = heal. Nhân stack.")]
        [SerializeField] private int _tickDamage = 0;

        [Header("☑ Logic 3: Trigger Effect (event-driven)")]
        [SerializeField] private bool _hasTriggerEffect = false;
        [SerializeField] private BuffTriggerType _triggerType = BuffTriggerType.OnAttack;
        [SerializeField] private TriggerEffect _effect = TriggerEffect.HealSelf;
        [Tooltip("Số HP heal, damage, hoặc % tùy effect.")]
        [SerializeField] private float _effectValue = 5f;
        [Tooltip("Chỉ dùng khi triggerType = OnHpThreshold. Vd: 30 = trigger khi HP < 30%.")]
        [Range(0, 100)]
        [SerializeField] private int _hpThresholdPercent = 30;
        [Tooltip("Cooldown giữa các lần trigger (seconds). 0 = trigger mỗi lần.")]
        [SerializeField] private float _triggerCooldown = 0f;
        [Tooltip("Chỉ dùng khi effect = AddBuffSelf.")]
        [SerializeField] private BuffDefinition _buffToApply;

        [Header("Visuals")]
        [SerializeField] private bool _showOnHud = true;

        // ===== Properties =====
        public string BuffId => _buffId;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public string Description => _description;

        public float DurationSeconds => _durationSeconds;
        public bool IsInfinite => _durationSeconds < 0;

        public bool Stackable => _stackable;
        public int MaxStacks => _stackable ? _maxStacks : 1;
        public bool RefreshDurationOnMaxStack => _refreshDurationOnMaxStack;

        // Logic 1: Stats
        public bool HasStatModifiers => _hasStatModifiers;
        public BuffStatModifier[] StatModifiers => _statModifiers;

        // Logic 2: Tick
        public bool HasTickEffect => _hasTickEffect;
        public float TickInterval => _tickInterval;
        public int TickDamage => _tickDamage;

        // Logic 3: Trigger
        public bool HasTriggerEffect => _hasTriggerEffect;
        public BuffTriggerType TriggerType => _triggerType;
        public TriggerEffect Effect => _effect;
        public float EffectValue => _effectValue;
        public int HpThresholdPercent => _hpThresholdPercent;
        public float TriggerCooldown => _triggerCooldown;
        public BuffDefinition BuffToApply => _buffToApply;

        public bool ShowOnHud => _showOnHud;
    }

    /// <summary>Modifier của buff lên 1 stat.</summary>
    [Serializable]
    public class BuffStatModifier
    {
        public StatType Type;
        public ModifierKind Kind;
        public float Value;
    }

    // ============================================================
    // BUFF INSTANCE - 1 buff đang active
    // ============================================================

    /// <summary>
    /// State runtime của 1 buff đang active.
    /// Multiple instance có thể chia sẻ cùng BuffDefinition.
    /// </summary>
    public class BuffInstance
    {
        public BuffDefinition Definition { get; }
        public int Stacks { get; private set; }
        public float StartTime { get; private set; }
        public float LastTickTime { get; set; }
        public float LastTriggerTime { get; set; }

        public BuffInstance(BuffDefinition def)
        {
            Definition = def;
            Stacks = 1;
            StartTime = Time.time;
            LastTickTime = Time.time;
            LastTriggerTime = -999f; // cho phép trigger ngay lần đầu
        }

        public float RemainingTime
        {
            get
            {
                if (Definition.IsInfinite) return float.PositiveInfinity;
                return Mathf.Max(0, StartTime + Definition.DurationSeconds - Time.time);
            }
        }

        public bool IsExpired
            => !Definition.IsInfinite && Time.time >= StartTime + Definition.DurationSeconds;

        public bool IsTriggerOnCooldown
            => Time.time - LastTriggerTime < Definition.TriggerCooldown;

        public void Refresh()
        {
            if (Definition.Stackable && Stacks < Definition.MaxStacks)
            {
                Stacks++;
                StartTime = Time.time;
            }
            else if (!Definition.Stackable || Definition.RefreshDurationOnMaxStack)
            {
                StartTime = Time.time;
            }
        }
    }

    // ============================================================
    // TRIGGER CONTEXT - data pass cho trigger handler
    // ============================================================

    /// <summary>Context khi 1 trigger event fire - dùng cho DamageAttacker etc.</summary>
    public struct TriggerContext
    {
        public CharacterStats Self;       // character đang giữ buff
        public CharacterStats Attacker;   // chỉ có khi OnDamaged (ai damage mình)
        public CharacterStats Target;     // chỉ có khi OnAttack/OnKill (mình đánh ai)
        public int DamageAmount;          // damage liên quan
    }

    // ============================================================
    // BUFF SET - quản lý các buff đang active
    // ============================================================

    /// <summary>
    /// Quản lý buff active. Có 3 entry point:
    ///   - Tick() mỗi frame: expire + tick damage
    ///   - NotifyAttack/NotifyDamaged/NotifyKill: gameplay gọi khi event xảy ra → fire trigger
    ///   - Calculate(): tính ModifierTotal cho 1 stat (logic 1)
    /// </summary>
    public class BuffSet
    {
        private readonly Dictionary<string, BuffInstance> _buffs = new Dictionary<string, BuffInstance>();
        private readonly Action<int> _onTickDamage;
        private readonly Action<TriggerContext, BuffInstance> _onTriggerFired;

        public event Action OnBuffsChanged;
        public event Action<BuffInstance> OnBuffApplied;
        public event Action<BuffInstance> OnBuffRemoved;

        public IReadOnlyDictionary<string, BuffInstance> Active => _buffs;

        public BuffSet(
            Action<int> onTickDamage = null,
            Action<TriggerContext, BuffInstance> onTriggerFired = null)
        {
            _onTickDamage = onTickDamage;
            _onTriggerFired = onTriggerFired;
        }

        // ============================================================
        // APPLY / REMOVE
        // ============================================================

        public void Apply(BuffDefinition def)
        {
            if (_buffs.TryGetValue(def.BuffId, out var existing))
            {
                existing.Refresh();
                OnBuffsChanged?.Invoke();
            }
            else
            {
                var instance = new BuffInstance(def);
                _buffs[def.BuffId] = instance;
                OnBuffApplied?.Invoke(instance);
                OnBuffsChanged?.Invoke();
            }
        }

        public bool Remove(string buffId)
        {
            if (_buffs.TryGetValue(buffId, out var instance))
            {
                _buffs.Remove(buffId);
                OnBuffRemoved?.Invoke(instance);
                OnBuffsChanged?.Invoke();
                return true;
            }
            return false;
        }

        public void Clear()
        {
            if (_buffs.Count == 0) return;
            foreach (var inst in _buffs.Values)
                OnBuffRemoved?.Invoke(inst);
            _buffs.Clear();
            OnBuffsChanged?.Invoke();
        }

        // ============================================================
        // TICK - logic 2 (DoT/HoT) + expire check
        // ============================================================

        public void Tick(CharacterStats selfContext = null)
        {
            List<BuffInstance> expiredList = null;
            float now = Time.time;

            foreach (var kv in _buffs)
            {
                var inst = kv.Value;

                // ===== Logic 2: Tick effect =====
                if (inst.Definition.HasTickEffect &&
                    now - inst.LastTickTime >= inst.Definition.TickInterval)
                {
                    int dmg = inst.Definition.TickDamage * inst.Stacks;
                    _onTickDamage?.Invoke(dmg);
                    inst.LastTickTime = now;
                }

                // ===== Logic 3: OnHpThreshold trigger =====
                // Check mỗi frame nếu HP đang dưới threshold
                if (inst.Definition.HasTriggerEffect
                    && inst.Definition.TriggerType == BuffTriggerType.OnHpThreshold
                    && selfContext != null
                    && !inst.IsTriggerOnCooldown)
                {
                    float hpPercent = selfContext.MaxHp.Value > 0
                        ? (float)selfContext.CurrentHp.Value / selfContext.MaxHp.Value * 100f
                        : 100f;

                    if (hpPercent <= inst.Definition.HpThresholdPercent)
                    {
                        FireTrigger(inst, new TriggerContext { Self = selfContext });
                    }
                }

                // ===== Expire check =====
                if (inst.IsExpired)
                {
                    expiredList ??= new List<BuffInstance>();
                    expiredList.Add(inst);
                }
            }

            if (expiredList != null)
            {
                foreach (var inst in expiredList)
                {
                    _buffs.Remove(inst.Definition.BuffId);
                    OnBuffRemoved?.Invoke(inst);
                }
                OnBuffsChanged?.Invoke();
            }
        }

        // ============================================================
        // TRIGGER NOTIFY - logic 3 (event-driven)
        // Gameplay gọi khi event xảy ra
        // ============================================================

        public void NotifyAttack(TriggerContext ctx)
            => FireTriggersOfType(BuffTriggerType.OnAttack, ctx);

        public void NotifyDamaged(TriggerContext ctx)
            => FireTriggersOfType(BuffTriggerType.OnDamaged, ctx);

        public void NotifyKill(TriggerContext ctx)
            => FireTriggersOfType(BuffTriggerType.OnKill, ctx);

        private void FireTriggersOfType(BuffTriggerType type, TriggerContext ctx)
        {
            foreach (var inst in _buffs.Values)
            {
                if (!inst.Definition.HasTriggerEffect) continue;
                if (inst.Definition.TriggerType != type) continue;
                if (inst.IsTriggerOnCooldown) continue;
                FireTrigger(inst, ctx);
            }
        }

        private void FireTrigger(BuffInstance inst, TriggerContext ctx)
        {
            inst.LastTriggerTime = Time.time;
            _onTriggerFired?.Invoke(ctx, inst);
        }

        // ============================================================
        // CALCULATE - logic 1 (sum stat modifiers)
        // ============================================================

        public ModifierTotal Calculate(StatType type)
        {
            var result = new ModifierTotal();
            foreach (var inst in _buffs.Values)
            {
                if (!inst.Definition.HasStatModifiers) continue;
                foreach (var mod in inst.Definition.StatModifiers)
                {
                    if (mod.Type != type) continue;
                    float scaled = mod.Value * inst.Stacks;
                    if (mod.Kind == ModifierKind.Flat) result.Flat += scaled;
                    else result.Percent += scaled;
                }
            }
            return result;
        }
    }
}
