using Godot;
using System.Collections.Generic;
using ProjectSummoner.Capabilities;
using ProjectSummoner.Cards;
using ProjectSummoner.Constants;
using ProjectSummoner.Data.Summoners;
using ProjectSummoner.Services.Interfaces;
using ProjectSummoner.Units;

namespace ProjectSummoner.Combat;

/// <summary>
/// Centralized damage calculation and combat event system.
/// Usage: DamageSystem.Instance.ApplyDamage(attacker, target, baseDamage)
/// </summary>
public partial class DamageSystem : Node, IDamageSystem
{
	// =========================================================================
	// SINGLETON
	// =========================================================================

	public static DamageSystem? Instance { get; private set; }

	// =========================================================================
	// SIGNALS
	// =========================================================================

	[Signal] public delegate void DamageDealtEventHandler(CombatEvent evt);
	[Signal] public delegate void DamageTakenEventHandler(CombatEvent evt);
	[Signal] public delegate void UnitKilledEventHandler(CombatEvent evt);
	[Signal] public delegate void UnitDiedEventHandler(CombatEvent evt);
	[Signal] public delegate void UnitHealedEventHandler(CombatEvent evt);
	[Signal] public delegate void AttackStartedEventHandler(CombatEvent evt);
	[Signal] public delegate void AttackCompletedEventHandler(CombatEvent evt);
	[Signal] public delegate void SpellCastEventHandler(CombatEvent evt);

	// =========================================================================
	// CONSTANTS
	// =========================================================================

	private static readonly Dictionary<string, float> DamageTypeMultipliers = new()
	{
		{ DamageTypes.Physical, 1.0f },
		{ DamageTypes.Magical, 1.0f },
		{ DamageTypes.True, 1.0f },  // Ignores armor
		{ DamageTypes.Fire, 1.0f },
		{ DamageTypes.Ice, 1.0f },
		{ DamageTypes.Poison, 1.0f }
	};

	private const float CritChance = 0.15f;      // 15% base crit chance
	private const float CritMultiplier = 2.0f;   // 2x damage on crit

	// =========================================================================
	// LIFECYCLE
	// =========================================================================

	public override void _Ready()
	{
		Instance = this;
		GD.Print("DamageSystem: Initialized");
	}

	// =========================================================================
	// PUBLIC API
	// =========================================================================

	public float ApplyDamage(Node3D attacker, Node3D target, float baseDamage)
	{
		return ApplyDamage(attacker, target, baseDamage, DamageTypes.Physical, null);
	}

	public float ApplyDamage(Node3D attacker, Node3D target, float baseDamage, string damageType)
	{
		return ApplyDamage(attacker, target, baseDamage, damageType, null);
	}

	/// <summary>
	/// Apply damage from attacker to target.
	/// Returns the actual damage dealt (after modifiers).
	/// </summary>
	public float ApplyDamage(
		Node3D attacker,
		Node3D target,
		float baseDamage,
		string damageType,
		Godot.Collections.Dictionary? flags)
	{
		flags ??= new Godot.Collections.Dictionary();

		// Check if target can take damage
		if (!CanTakeDamage(target))
		{
			GD.PushWarning("DamageSystem: Target cannot take damage");
			return 0f;
		}

		// Calculate final damage
		float finalDamage = baseDamage;
		bool isCrit = false;

		// Check for critical hit
		if (flags.ContainsKey("force_crit") && flags["force_crit"].AsBool())
		{
			isCrit = true;
		}
		else if (!flags.ContainsKey("cannot_crit") || !flags["cannot_crit"].AsBool())
		{
			isCrit = GetSeededCritRoll() < CritChance;
		}

		// Apply crit multiplier
		if (isCrit)
		{
			finalDamage *= CritMultiplier;
		}

		// Apply damage type multiplier
		if (DamageTypeMultipliers.TryGetValue(damageType, out float typeMultiplier))
		{
			finalDamage *= typeMultiplier;
		}

		// Apply elemental matchup multiplier (attacker element vs target element)
		Element attackerElement = GetEntityElement(attacker);
		Element targetElement = GetEntityElement(target);
		float elementalMultiplier = ElementMatchups.GetMultiplier(attackerElement, targetElement);
		if (elementalMultiplier != 1.0f)
		{
			finalDamage *= elementalMultiplier;
			flags["elemental_multiplier"] = elementalMultiplier;
			flags["attacker_element"] = (int)attackerElement;
			flags["target_element"] = (int)targetElement;
		}

		// Apply custom multiplier from flags
		if (flags.ContainsKey("damage_multiplier"))
		{
			finalDamage *= flags["damage_multiplier"].AsSingle();
		}

		// Apply player summoner damage bonuses if attacker is on player team
		if (IsPlayerTeam(attacker))
		{
			finalDamage = ApplySummonerDamageBonuses(finalDamage, damageType);
		}

		// Apply player summoner damage reduction if target is on player team
		if (IsPlayerTeam(target))
		{
			finalDamage = ApplySummonerDamageReduction(finalDamage);
		}

		// Round to avoid floating point issues
		finalDamage = Mathf.Round(finalDamage * 10f) / 10f;

		// Store target's HP before damage
		float targetHpBefore = GetCurrentHp(target);

		// Apply damage to target
		ApplyDamageToTarget(target, finalDamage);

		// Create metadata for event
		var metadata = new Godot.Collections.Dictionary
		{
			{ "is_crit", isCrit },
			{ "base_damage", baseDamage },
			{ "final_damage", finalDamage },
			{ "hp_before", targetHpBefore }
		};

		// Merge custom flags
		foreach (var key in flags.Keys)
		{
			metadata[key] = flags[key];
		}

		// Emit DAMAGE_DEALT event (from attacker's perspective)
		var dealtEvent = new CombatEvent(
			CombatEvent.EventType.DamageDealt,
			attacker,
			target,
			finalDamage,
			damageType,
			metadata
		);
		EmitSignal(SignalName.DamageDealt, dealtEvent);

		// Notify attacker of damage dealt (for OnHit triggers)
		if (attacker is Unit3D attackerUnit)
		{
			attackerUnit.OnDealDamage(finalDamage, target);
		}

		// Emit DAMAGE_TAKEN event (from target's perspective)
		var takenEvent = new CombatEvent(
			CombatEvent.EventType.DamageTaken,
			attacker,
			target,
			finalDamage,
			damageType,
			metadata
		);
		EmitSignal(SignalName.DamageTaken, takenEvent);

		// Check if target died
		if (IsDead(target))
		{
			// Emit UNIT_KILLED event (from attacker's perspective)
			var killedEvent = new CombatEvent(
				CombatEvent.EventType.UnitKilled,
				attacker,
				target,
				finalDamage,
				damageType,
				metadata
			);
			EmitSignal(SignalName.UnitKilled, killedEvent);

			// Notify attacker of kill (for OnKill triggers like Soul Harvest)
			if (attacker is Unit3D killerUnit)
			{
				killerUnit.OnKill(target);
			}

			// Emit UNIT_DIED event (from target's perspective)
			var diedEvent = new CombatEvent(
				CombatEvent.EventType.UnitDied,
				attacker,
				target,
				finalDamage,
				damageType,
				metadata
			);
			EmitSignal(SignalName.UnitDied, diedEvent);
		}

		return finalDamage;
	}

	/// <summary>
	/// Apply healing to target (GDScript-compatible overload).
	/// </summary>
	public float ApplyHealing(Node3D healer, Node3D target, float healAmount)
	{
		return ApplyHealing(healer, target, healAmount, null);
	}

	/// <summary>
	/// Apply healing to target.
	/// Returns the actual amount healed.
	/// </summary>
	public float ApplyHealing(
		Node3D healer,
		Node3D target,
		float healAmount,
		Godot.Collections.Dictionary? flags)
	{
		flags ??= new Godot.Collections.Dictionary();

		// Check if target can be healed
		if (!CanHeal(target))
		{
			GD.PushWarning("DamageSystem: Target cannot be healed");
			return 0f;
		}

		// Calculate final heal amount
		float finalHeal = healAmount;

		// Apply custom multiplier from flags
		if (flags.ContainsKey("heal_multiplier"))
		{
			finalHeal *= flags["heal_multiplier"].AsSingle();
		}

		// Store HP before healing
		float hpBefore = GetCurrentHp(target);

		// Apply healing
		float actualHeal = ApplyHealingToTarget(target, finalHeal);
		float newHp = GetCurrentHp(target);

		// Create metadata
		var metadata = new Godot.Collections.Dictionary
		{
			{ "requested_heal", healAmount },
			{ "final_heal", finalHeal },
			{ "actual_heal", actualHeal },
			{ "hp_before", hpBefore },
			{ "hp_after", newHp },
			{ "overheal", finalHeal - actualHeal }
		};

		// Merge custom flags
		foreach (var key in flags.Keys)
		{
			metadata[key] = flags[key];
		}

		// Emit UNIT_HEALED event
		var healedEvent = new CombatEvent(
			CombatEvent.EventType.UnitHealed,
			healer,
			target,
			actualHeal,
			"healing",
			metadata
		);
		EmitSignal(SignalName.UnitHealed, healedEvent);

		return actualHeal;
	}

	/// <summary>
	/// Emit attack started event (GDScript-compatible overload).
	/// </summary>
	public void EmitAttackStarted(Node3D attacker, Node3D target)
	{
		EmitAttackStarted(attacker, target, null);
	}

	/// <summary>
	/// Emit attack started event (for animation/VFX timing).
	/// </summary>
	public void EmitAttackStarted(Node3D attacker, Node3D target, Godot.Collections.Dictionary? metadata)
	{
		var evt = new CombatEvent(
			CombatEvent.EventType.AttackStarted,
			attacker,
			target,
			0f,
			"",
			metadata
		);
		EmitSignal(SignalName.AttackStarted, evt);
	}

	/// <summary>
	/// Emit attack completed event (GDScript-compatible overload).
	/// </summary>
	public void EmitAttackCompleted(Node3D attacker, Node3D target)
	{
		EmitAttackCompleted(attacker, target, null);
	}

	/// <summary>
	/// Emit attack completed event (for animation/VFX timing).
	/// </summary>
	public void EmitAttackCompleted(Node3D attacker, Node3D target, Godot.Collections.Dictionary? metadata)
	{
		var evt = new CombatEvent(
			CombatEvent.EventType.AttackCompleted,
			attacker,
			target,
			0f,
			"",
			metadata
		);
		EmitSignal(SignalName.AttackCompleted, evt);
	}

	/// <summary>
	/// Emit spell cast event (GDScript-compatible overload).
	/// </summary>
	public void EmitSpellCast(Node3D caster, string spellId)
	{
		EmitSpellCast(caster, spellId, null);
	}

	/// <summary>
	/// Emit spell cast event.
	/// </summary>
	public void EmitSpellCast(Node3D caster, string spellId, Godot.Collections.Dictionary? metadata)
	{
		metadata ??= new Godot.Collections.Dictionary();
		metadata["spell_id"] = spellId;

		var evt = new CombatEvent(
			CombatEvent.EventType.SpellCast,
			caster,
			null,
			0f,
			"",
			metadata
		);
		EmitSignal(SignalName.SpellCast, evt);
	}

	/// <summary>
	/// Calculate damage preview (GDScript-compatible overload).
	/// </summary>
	public Godot.Collections.Dictionary PreviewDamage(Node3D target, float baseDamage)
	{
		return PreviewDamage(target, baseDamage, DamageTypes.Physical, false);
	}

	/// <summary>
	/// Calculate damage preview (GDScript-compatible overload).
	/// </summary>
	public Godot.Collections.Dictionary PreviewDamage(Node3D target, float baseDamage, string damageType)
	{
		return PreviewDamage(target, baseDamage, damageType, false);
	}

	/// <summary>
	/// Calculate damage preview (doesn't apply, just calculates).
	/// Useful for damage prediction UI.
	/// </summary>
	public Godot.Collections.Dictionary PreviewDamage(
		Node3D target,
		float baseDamage,
		string damageType,
		bool assumeCrit)
	{
		float finalDamage = baseDamage;

		// Apply crit if assumed
		if (assumeCrit)
		{
			finalDamage *= CritMultiplier;
		}

		// Apply damage type multiplier
		if (DamageTypeMultipliers.TryGetValue(damageType, out float typeMultiplier))
		{
			finalDamage *= typeMultiplier;
		}

		finalDamage = Mathf.Round(finalDamage * 10f) / 10f;

		return new Godot.Collections.Dictionary
		{
			{ "base_damage", baseDamage },
			{ "final_damage", finalDamage },
			{ "is_crit", assumeCrit },
			{ "damage_type", damageType },
			{ "will_kill", GetCurrentHp(target) <= finalDamage }
		};
	}

	// =========================================================================
	// HELPER METHODS (replaces DamageableInterop)
	// =========================================================================

	/// <summary>
	/// Get the element of a combat entity (unit or summoner).
	/// Returns Neutral if element cannot be determined.
	/// </summary>
	private static Element GetEntityElement(Node3D entity)
	{
		if (entity == null)
			return Element.Neutral;

		// C# Unit3D - look up via UnitId -> CardCatalog
		if (entity is Unit3D unit && !string.IsNullOrEmpty(unit.UnitId))
		{
			var card = CardCatalog.GetCard(unit.UnitId);
			if (card != null)
				return card.ElementalAffinity;
		}

		// GDScript Summoner - check for summoner_id property -> SummonerCatalog
		var summonerIdVar = entity.Get("summoner_id");
		if (summonerIdVar.VariantType != Variant.Type.Nil)
		{
			string summonerId = summonerIdVar.AsString();
			var summoner = SummonerCatalog.GetSummoner(summonerId);
			if (summoner != null)
				return summoner.ElementalAffinity;
		}

		return Element.Neutral;
	}

	private static bool CanTakeDamage(Node3D target)
	{
		if (target == null)
			return false;

		// C# units implement IDamageable
		if (target is IDamageable)
			return true;

		// GDScript units have TakeDamage or take_damage method
		return target.HasMethod("TakeDamage") || target.HasMethod("take_damage");
	}

	private static void ApplyDamageToTarget(Node3D target, float amount)
	{
		if (target is IDamageable damageable)
		{
			damageable.TakeDamage(amount);
		}
		else if (target.HasMethod("TakeDamage"))
		{
			target.Call("TakeDamage", amount);
		}
		else if (target.HasMethod("take_damage"))
		{
			target.Call("take_damage", amount);
		}
	}

	private static bool CanHeal(Node3D target)
	{
		if (target == null)
			return false;

		// C# units implement IDamageable
		if (target is IDamageable)
			return true;

		// GDScript units need HP properties
		var currentHpVar = target.Get("CurrentHp");
		if (currentHpVar.VariantType != Variant.Type.Nil)
			return true;

		currentHpVar = target.Get("current_hp");
		return currentHpVar.VariantType != Variant.Type.Nil;
	}

	private static float ApplyHealingToTarget(Node3D target, float amount)
	{
		// C# units have a Heal method
		if (target is Unit3D unit)
		{
			return unit.Heal(amount);
		}

		// GDScript entities (Summoner, Base) - manually calculate and set HP
		float currentHp = GetCurrentHp(target);
		float maxHp = GetMaxHp(target);

		if (maxHp <= 0)
			return 0f;

		float newHp = Mathf.Min(currentHp + amount, maxHp);
		float actualHeal = newHp - currentHp;

		SetCurrentHp(target, newHp);
		return actualHeal;
	}

	private static float GetCurrentHp(Node3D target)
	{
		if (target is IDamageable damageable)
			return damageable.CurrentHp;

		var hpVar = target.Get("CurrentHp");
		if (hpVar.VariantType != Variant.Type.Nil)
			return hpVar.AsSingle();

		hpVar = target.Get("current_hp");
		if (hpVar.VariantType != Variant.Type.Nil)
			return hpVar.AsSingle();

		return 0f;
	}

	private static float GetMaxHp(Node3D target)
	{
		if (target is IDamageable damageable)
			return damageable.MaxHp;

		var hpVar = target.Get("MaxHp");
		if (hpVar.VariantType != Variant.Type.Nil)
			return hpVar.AsSingle();

		hpVar = target.Get("max_hp");
		if (hpVar.VariantType != Variant.Type.Nil)
			return hpVar.AsSingle();

		return 0f;
	}

	private static void SetCurrentHp(Node3D target, float value)
	{
		// Unit3D uses Heal() method directly, handled in ApplyHealingToTarget
		// This method is only for GDScript targets

		// Try PascalCase first (C# convention)
		var hpVar = target.Get("CurrentHp");
		if (hpVar.VariantType != Variant.Type.Nil)
		{
			target.Set("CurrentHp", value);
			return;
		}

		// Try snake_case (GDScript convention)
		hpVar = target.Get("current_hp");
		if (hpVar.VariantType != Variant.Type.Nil)
		{
			target.Set("current_hp", value);
		}
	}

	private static bool IsDead(Node3D target)
	{
		if (target is IDamageable damageable)
			return !damageable.IsAlive;

		// Check IsAlive property
		var aliveVar = target.Get("IsAlive");
		if (aliveVar.VariantType != Variant.Type.Nil)
			return !aliveVar.AsBool();

		aliveVar = target.Get("is_alive");
		if (aliveVar.VariantType != Variant.Type.Nil)
			return !aliveVar.AsBool();

		// Fallback to HP check
		return GetCurrentHp(target) <= 0;
	}

	private static bool IsPlayerTeam(Node3D target)
	{
		if (target == null)
			return false;

		if (target is Unit3D unit)
			return unit.Team == 0;

		// Check Team property
		var teamVar = target.Get("Team");
		if (teamVar.VariantType != Variant.Type.Nil)
			return teamVar.AsInt32() == 0;

		teamVar = target.Get("team");
		if (teamVar.VariantType != Variant.Type.Nil)
			return teamVar.AsInt32() == 0;

		return false;
	}

	// =========================================================================
	// SUMMONER TRAIT INTEGRATION
	// =========================================================================

	private float ApplySummonerDamageBonuses(float damage, string damageType)
	{
		var battleContext = GetNodeOrNull("/root/BattleContext");
		if (battleContext == null)
			return damage;

		var statsVar = battleContext.Call("get_player_summoner_stats");
		if (statsVar.VariantType != Variant.Type.Dictionary)
			return damage;

		var summonerStats = statsVar.AsGodotDictionary();
		if (summonerStats.Count == 0)
		{
			// Check if in campaign mode
			var modeVar = battleContext.Get("current_mode");
			if (modeVar.VariantType != Variant.Type.Nil && modeVar.AsInt32() == 0) // CAMPAIGN = 0
			{
				GD.PushWarning("DamageSystem: No summoner stats cached in campaign mode - trait bonuses not applied");
			}
			return damage;
		}

		float modifiedDamage = damage;

		// Apply general damage bonus
		if (summonerStats.ContainsKey("damage_bonus"))
		{
			float damageBonus = summonerStats["damage_bonus"].AsSingle();
			if (damageBonus > 0f)
			{
				modifiedDamage *= 1f + damageBonus / 100f;
			}
		}

		// Apply elemental damage bonus based on damage type
		string elementalBonusKey = damageType + "_damage_bonus";
		if (summonerStats.ContainsKey(elementalBonusKey))
		{
			float elementalBonus = summonerStats[elementalBonusKey].AsSingle();
			if (elementalBonus > 0f)
			{
				modifiedDamage *= 1f + elementalBonus / 100f;
			}
		}

		return modifiedDamage;
	}

	private float ApplySummonerDamageReduction(float damage)
	{
		var battleContext = GetNodeOrNull("/root/BattleContext");
		if (battleContext == null)
			return damage;

		var statsVar = battleContext.Call("get_player_summoner_stats");
		if (statsVar.VariantType != Variant.Type.Dictionary)
			return damage;

		var summonerStats = statsVar.AsGodotDictionary();
		if (summonerStats.Count == 0)
		{
			var modeVar = battleContext.Get("current_mode");
			if (modeVar.VariantType != Variant.Type.Nil && modeVar.AsInt32() == 0)
			{
				GD.PushWarning("DamageSystem: No summoner stats cached in campaign mode - trait reduction not applied");
			}
			return damage;
		}

		// Apply flat damage reduction
		if (summonerStats.ContainsKey("damage_reduction"))
		{
			float damageReduction = summonerStats["damage_reduction"].AsSingle();
			if (damageReduction > 0f)
			{
				damage = Mathf.Max(damage - damageReduction, 0f);
			}
		}

		return damage;
	}

	// =========================================================================
	// SEEDED RNG INTEGRATION
	// =========================================================================

	// RNG Domain constants - must match RNGDomain.Domain enum in rng_domain.gd
	// If the enum order changes, update these values!
	private const int RNG_DOMAIN_COMBAT_CRITS = 2;

	/// <summary>
	/// Get a seeded random value for crit rolls via BattleRNG autoload.
	/// This ensures deterministic crit results across network peers.
	/// </summary>
	private float GetSeededCritRoll()
	{
		var battleRng = GetNodeOrNull("/root/BattleRNG");
		if (battleRng == null)
		{
			// Fallback to unseeded RNG if BattleRNG not available
			GD.PushWarning("DamageSystem: BattleRNG not found, using unseeded RNG for crit roll");
			return GD.Randf();
		}

		var result = battleRng.Call("randf", RNG_DOMAIN_COMBAT_CRITS);
		return result.AsSingle();
	}

}
