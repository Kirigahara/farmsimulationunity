using System;
using UnityEngine;
using GameTemplate.Core.Patterns.Reactive;

namespace GameTemplate.Gameplay.Stats
{
    /// <summary>
    /// CharacterStats - chỉ số FINAL của character lúc đang chơi.
    ///
    /// Vai trò: TỔNG HỢP 4 nguồn → expose ReactiveProperty cho UI subscribe.
    ///
    ///   Final stat = Default (base + level scaling)
    ///              + Saved (invested point + permanent bonus)
    ///              + Equipment (modifier từ item đang equip - vĩnh viễn)
    ///              + Buffs (modifier từ skill/potion/aura - tạm thời, có duration)
    ///
    /// Đặc điểm:
    ///   - Pure C# class (test được không cần Unity Editor)
    ///   - Mỗi stat là ReactiveProperty → UI tự update
    ///   - Recompute khi: level up, spend stat point, equip/unequip, buff add/remove
    ///   - Tick() mỗi frame để buff tự expire + apply tick damage (DoT/HoT)
    ///   - Current HP là MUTABLE (tách riêng MaxHp) - giảm khi take damage
    /// </summary>
    public class CharacterStats
    {
        // ===== 4 nguồn data =====
        public DefaultCharacterStats Defaults { get; }
        public SavedCharacterStats Saved { get; }
        public EquipmentModifierSet Equipment { get; }
        public BuffSet Buffs { get; }

        // ===== Reactive stats - UI subscribe =====
        public ReactiveProperty<int> MaxHp { get; } = new ReactiveProperty<int>();
        public ReactiveProperty<int> CurrentHp { get; } = new ReactiveProperty<int>();
        public ReactiveProperty<int> Attack { get; } = new ReactiveProperty<int>();
        public ReactiveProperty<int> Defense { get; } = new ReactiveProperty<int>();
        public ReactiveProperty<float> MoveSpeed { get; } = new ReactiveProperty<float>();
        public ReactiveProperty<int> CritRate { get; } = new ReactiveProperty<int>();

        // Progression reactive cho UI exp bar, level number
        public ReactiveProperty<int> Level { get; } = new ReactiveProperty<int>();
        public ReactiveProperty<int> CurrentExp { get; } = new ReactiveProperty<int>();
        public ReactiveProperty<int> UnspentStatPoints { get; } = new ReactiveProperty<int>();

        // ===== Events cho gameplay logic =====
        public event Action OnLevelUp;
        public event Action OnDied;

        public bool IsDead => CurrentHp.Value <= 0;

        public CharacterStats(
            DefaultCharacterStats defaults,
            SavedCharacterStats saved,
            EquipmentModifierSet equipment)
        {
            Defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
            Saved = saved ?? throw new ArgumentNullException(nameof(saved));
            Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));

            // Buff với 2 callback:
            //   - tick damage (DoT/HoT)
            //   - trigger fired (on-attack, on-damaged, on-kill, on-hp-threshold)
            Buffs = new BuffSet(
                onTickDamage: ApplyTickDamage,
                onTriggerFired: HandleTriggerFired);

            // Recompute mỗi khi equipment hoặc buff đổi
            Equipment.OnModifiersChanged += Recompute;
            Buffs.OnBuffsChanged += Recompute;

            Recompute();
            CurrentHp.Value = MaxHp.Value;
        }

        // ============================================================
        // TICK - gọi từ MonoBehaviour wrapper (CharacterController) mỗi frame
        // ============================================================
        public void Tick()
        {
            // Pass self để Tick() có thể check OnHpThreshold trigger
            Buffs.Tick(this);
        }

        // ============================================================
        // TRIGGER NOTIFY - gameplay gọi khi event xảy ra
        // ============================================================

        /// <summary>Gọi khi character này đánh enemy.</summary>
        public void NotifyAttack(CharacterStats target, int damageDealt)
        {
            Buffs.NotifyAttack(new TriggerContext
            {
                Self = this,
                Target = target,
                DamageAmount = damageDealt,
            });
        }

        /// <summary>Gọi khi character này nhận damage từ enemy.</summary>
        public void NotifyDamaged(CharacterStats attacker, int damageTaken)
        {
            Buffs.NotifyDamaged(new TriggerContext
            {
                Self = this,
                Attacker = attacker,
                DamageAmount = damageTaken,
            });
        }

        /// <summary>Gọi khi character này giết enemy.</summary>
        public void NotifyKill(CharacterStats victim)
        {
            Buffs.NotifyKill(new TriggerContext
            {
                Self = this,
                Target = victim,
            });
        }

        // ============================================================
        // TRIGGER HANDLER - xử lý effect khi trigger fire
        // ============================================================
        private void HandleTriggerFired(TriggerContext ctx, BuffInstance buff)
        {
            float value = buff.Definition.EffectValue * buff.Stacks;

            switch (buff.Definition.Effect)
            {
                case TriggerEffect.HealSelf:
                    Heal(Mathf.RoundToInt(value));
                    break;

                case TriggerEffect.DamageAttacker:
                    // Thorns: damage ngược lại attacker (chỉ work khi OnDamaged có ctx.Attacker)
                    ctx.Attacker?.TakeDamage(Mathf.RoundToInt(value));
                    break;

                case TriggerEffect.DamagePercent:
                    // % HP của target/attacker
                    var target = ctx.Target ?? ctx.Attacker;
                    if (target != null)
                    {
                        int dmg = Mathf.RoundToInt(target.MaxHp.Value * value / 100f);
                        target.TakeDamage(dmg);
                    }
                    break;

                case TriggerEffect.AddBuffSelf:
                    // Apply buff khác lên mình (vd: HP <30% → trigger AddBuffSelf Berserk)
                    if (buff.Definition.BuffToApply != null)
                        ApplyBuff(buff.Definition.BuffToApply);
                    break;
            }
        }

        // ============================================================
        // RECOMPUTE - tính lại final stats từ 4 nguồn
        // ============================================================
        public void Recompute()
        {
            // ===== HP =====
            int baseHp = Defaults.BaseHp + Defaults.HpPerLevel * (Saved.Level - 1);
            int hpFromInvest = Saved.InvestedHp * 10;
            int hpBase = baseHp + hpFromInvest + Saved.PermanentHpBonus;

            // Equipment + Buff cộng dồn (flat cộng, percent cộng - không nhân)
            var hpEquip = Equipment.Calculate(StatType.Hp);
            var hpBuff = Buffs.Calculate(StatType.Hp);
            var hpTotal = MergeTotals(hpEquip, hpBuff);
            int newMaxHp = hpTotal.Apply(hpBase);

            // Khi MaxHp đổi, giữ tỷ lệ % HP hiện tại
            float hpRatio = MaxHp.Value > 0 ? (float)CurrentHp.Value / MaxHp.Value : 1f;
            MaxHp.Value = newMaxHp;
            CurrentHp.Value = Mathf.Clamp(Mathf.RoundToInt(newMaxHp * hpRatio), 0, newMaxHp);

            // ===== Attack =====
            int baseAtk = Defaults.BaseAttack + Defaults.AttackPerLevel * (Saved.Level - 1);
            int atkBase = baseAtk + Saved.InvestedAttack * 2 + Saved.PermanentAttackBonus;
            var atkTotal = MergeTotals(Equipment.Calculate(StatType.Attack), Buffs.Calculate(StatType.Attack));
            Attack.Value = atkTotal.Apply(atkBase);

            // ===== Defense =====
            int baseDef = Defaults.BaseDefense + Defaults.DefensePerLevel * (Saved.Level - 1);
            int defBase = baseDef + Saved.InvestedDefense;
            var defTotal = MergeTotals(Equipment.Calculate(StatType.Defense), Buffs.Calculate(StatType.Defense));
            Defense.Value = defTotal.Apply(defBase);

            // ===== Move Speed (buff có thể slow/haste) =====
            float speedBase = Defaults.BaseMoveSpeed;
            var speedTotal = MergeTotals(Equipment.Calculate(StatType.MoveSpeed), Buffs.Calculate(StatType.MoveSpeed));
            MoveSpeed.Value = Mathf.Max(0, speedTotal.ApplyFloat(speedBase));

            // ===== Crit Rate (cap by MaxCritRate trong Default) =====
            int critBase = Defaults.BaseCritRate;
            var critTotal = MergeTotals(Equipment.Calculate(StatType.CritRate), Buffs.Calculate(StatType.CritRate));
            CritRate.Value = Mathf.Min(critTotal.Apply(critBase), Defaults.MaxCritRate);

            // ===== Progression sync =====
            Level.Value = Saved.Level;
            CurrentExp.Value = Saved.CurrentExp;
            UnspentStatPoints.Value = Saved.UnspentStatPoints;
        }

        /// <summary>Cộng 2 ModifierTotal (Equipment + Buff) thành 1 total.</summary>
        private static ModifierTotal MergeTotals(ModifierTotal a, ModifierTotal b)
            => new ModifierTotal { Flat = a.Flat + b.Flat, Percent = a.Percent + b.Percent };

        // ============================================================
        // GAMEPLAY ACTIONS
        // ============================================================

        public void TakeDamage(int rawDamage)
        {
            int finalDamage = Mathf.Max(1, rawDamage - Defense.Value);
            CurrentHp.Value = Mathf.Max(0, CurrentHp.Value - finalDamage);
            if (CurrentHp.Value == 0) OnDied?.Invoke();
        }

        public void Heal(int amount)
            => CurrentHp.Value = Mathf.Min(MaxHp.Value, CurrentHp.Value + amount);

        public void FullHeal() => CurrentHp.Value = MaxHp.Value;

        /// <summary>Callback BuffSet gọi mỗi tick (DoT/HoT). Âm = heal, dương = damage.</summary>
        private void ApplyTickDamage(int amount)
        {
            if (amount > 0) TakeDamage(amount);
            else if (amount < 0) Heal(-amount);
        }

        public bool AddExp(int amount)
        {
            bool leveledUp = Saved.AddExp(amount, Defaults);
            if (leveledUp)
            {
                Recompute();
                FullHeal();
                OnLevelUp?.Invoke();
            }
            else
            {
                CurrentExp.Value = Saved.CurrentExp;
            }
            return leveledUp;
        }

        public bool SpendStatPoint(StatType type)
        {
            if (!Saved.SpendStatPoint(type)) return false;
            Recompute();
            return true;
        }

        // ============================================================
        // EQUIPMENT
        // ============================================================

        public void Equip(string itemId, EquipmentModifier[] mods)
        {
            Saved.EquipItem(itemId);
            foreach (var mod in mods)
                Equipment.Add(mod);
        }

        public void Unequip(string itemId)
        {
            Saved.UnequipItem(itemId);
            Equipment.Remove(itemId);
        }

        // ============================================================
        // BUFF - convenience methods
        // ============================================================

        public void ApplyBuff(BuffDefinition def) => Buffs.Apply(def);
        public bool RemoveBuff(string buffId) => Buffs.Remove(buffId);
        public void ClearAllBuffs() => Buffs.Clear();

        // ============================================================
        // CLEANUP
        // ============================================================

        public void Dispose()
        {
            Equipment.OnModifiersChanged -= Recompute;
            Buffs.OnBuffsChanged -= Recompute;
            Buffs.Clear();

            MaxHp.ClearSubscribers();
            CurrentHp.ClearSubscribers();
            Attack.ClearSubscribers();
            Defense.ClearSubscribers();
            MoveSpeed.ClearSubscribers();
            CritRate.ClearSubscribers();
            Level.ClearSubscribers();
            CurrentExp.ClearSubscribers();
            UnspentStatPoints.ClearSubscribers();
        }
    }
}
