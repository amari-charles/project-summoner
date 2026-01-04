class_name RNGDomain

## Domains for seeded RNG - each domain has its own independent RNG stream.
## This allows deterministic replay while keeping different systems isolated.

enum Domain {
	## Deck shuffling (initial shuffle and recycle)
	DECK_SHUFFLE,

	## Enemy AI decisions (card selection, timing)
	AI_DECISIONS,

	## Combat critical hits
	COMBAT_CRITS,

	## AI spawn position selection
	SPAWN_POSITIONS,
}

## Domains that affect gameplay and MUST be synchronized across network
const SYNCHRONIZED_DOMAINS: Array[Domain] = [
	Domain.DECK_SHUFFLE,
	Domain.AI_DECISIONS,
	Domain.COMBAT_CRITS,
	Domain.SPAWN_POSITIONS,
]

## Check if a domain requires network synchronization
static func is_synchronized(domain: Domain) -> bool:
	return domain in SYNCHRONIZED_DOMAINS
