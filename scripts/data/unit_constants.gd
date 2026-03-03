extends RefCounted
class_name UnitConstants

## Constants mirroring C# Unit3D enums for GDScript interop
## These values MUST match the C# enum values in scripts/csharp/Units/Enums.cs

## Team enum - matches ProjectSummoner.Units.Team
enum Team {
	PLAYER = 0,
	ENEMY = 1
}

## UnitType enum - matches ProjectSummoner.Units.UnitType
enum UnitType {
	MELEE = 0,
	RANGED = 1
}

## MovementLayer enum - matches ProjectSummoner.Units.MovementLayer
enum MovementLayer {
	GROUND = 0,
	AIR = 1
}

## TargetLayer enum - matches ProjectSummoner.Units.TargetLayer
enum TargetLayer {
	GROUND_ONLY = 0,
	AIR_ONLY = 1,
	BOTH = 2
}

## ActivationState enum - matches ProjectSummoner.Units.ActivationState
enum ActivationState {
	INACTIVE = 0,
	ACTIVE = 1
}

## FlyingAttackStyle enum - matches ProjectSummoner.Units.FlyingAttackStyle
enum FlyingAttackStyle {
	HOVER = 0,
	LAND_ON_ENGAGE = 1,
	SWOOP = 2,
	ADAPTIVE = 3
}

## FlyingDeathStyle enum - matches ProjectSummoner.Units.FlyingDeathStyle
enum FlyingDeathStyle {
	FALL = 0,
	FADE = 1,
	EXPLODE = 2
}

## CardType enum - matches ProjectSummoner.Cards.CardType
enum CardType {
	SUMMON = 0,
	SPELL = 1
}

