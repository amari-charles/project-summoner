# Excursion Combat Format

**Status:** Stationary-summoner combat baseline accepted
**Updated:** 2026-08-16

## Purpose

Define how Academy excursions can expand the playable world without accidentally replacing Fateforged's established battle game with an unrelated second combat system.

## Starting Direction

For the first excursion experience:

- The player can walk, investigate, talk, and interact between battles.
- When a meaningful combat encounter begins, it uses the recognizable Fateforged battle format.
- Excursions can change the reason for fighting, the enemy setup, the player's available cards, the arena, and the win condition.
- Summoners remain stationary during combat. Exploration movement ends when the
  game enters the horizontal Fateforged battle format.

This is the committed production baseline. The movement greybox remains research
evidence, not planned production scope.

## Research Findings

Games with a stable competitive format commonly expand solo play in one of three ways.

### Add a world around recognizable battles

*Street Fighter 6: World Tour* adds exploration, characters, missions, and role-playing progression around recognizable Street Fighter combat. The player experience changes substantially between fights without discarding the series' fighting foundation.

- [Capcom: Street Fighter 6 Showcase Recap](https://news.capcomusa.com/2023/04/20/street-fighter-6-showcase-recap/)

### Keep the controls but change the level and objective

*Splatoon 3* keeps its movement, aiming, and ink mechanics while its solo campaign uses connected areas, collectibles, platforming, puzzles, unusual enemies, and short challenge levels instead of repeating online Turf War.

- [Nintendo: Splatoon 3 gameplay overview](https://splatoon.nintendo.com/en/gameplay/)
- [Nintendo: Play the story mode first](https://splatoon.nintendo.com/en/news/up-your-game-in-splatoon-3-with-these-quick-tips/)

### Put standard battles inside a larger journey

*Hearthstone* solo adventures and *Legends of Runeterra: The Path of Champions* preserve their card-battle foundations while adding runs, routes, shops, deck changes, treasures, special opponents, and encounter-specific rules. *Thronebreaker* goes further by combining exploration, decisions, puzzles, and custom card battles in a story-driven role-playing game.

- [Blizzard: Hearthstone Solo Adventures](https://hearthstone.blizzard.com/en-us/news/22990353)
- [Riot Games: The Path of Champions](https://playruneterra.com/en-us/news/game-updates/the-path-of-champions-in-depth-look)
- [CD Projekt Red: Thronebreaker](https://www.playgwent.com/en/news/23894/thronebreaker-the-witcher-tales-official-launch-trailer)

## Lesson for Fateforged

The reliable pattern is to preserve the game's recognizable combat language while changing its context:

`Explore and act in the world → enter a Fateforged encounter → return to the world with consequences`

## Exploration-to-Battle Transition

An encounter discovered in a walkable excursion transitions into a separate,
large horizontal battlefield themed to that excursion. The battlefield does not
need to fit literally inside the exact patch of exploration geometry where the
encounter began. After victory, defeat handling, or another resolved outcome, the
game returns the player to the relevant exploration location and applies the
encounter's persistent consequences.

This separation lets forests, ruins, corridors, and other bounded spaces retain
believable exploration scale while every combat encounter uses the same readable
Fateforged battle dimensions. A region may reuse a small family of themed battle
arenas rather than requiring a unique battlefield for every encounter point.

Innovation can come from:

- goals other than defeating every enemy;
- unusual allies, enemies, hazards, and battlefield rules;
- constrained or altered decks;
- choices made before, during, or after an encounter;
- consequences that carry across an excursion;
- exploration and interactions that change a later battle.

These options can make solo play meaningfully different without requiring different controls or making cards behave inconsistently.

## Innovation Track

Fateforged may revisit ideas that break this pattern later when they offer enough
value. Possibilities include moving the summoner during combat, spell-only action
encounters, room-bounded summons, or other forms not yet proposed.

Controllable summoner movement cannot become a separate ruin-only combat system.
If a future prototype proves movement valuable enough to reconsider, it must be treated as
a foundational combat redesign that works coherently in standard 1v1 for both
players as well as in excursions. If that broader model is not accepted,
excursion battles retain the stationary-summoner core format. Walking and
interaction outside combat remain a separate navigation layer either way.

An experimental format should be promoted into the game only when a focused prototype shows that it:

- is enjoyable and understandable;
- still feels connected to Fateforged's cards, creatures, and summoner identity;
- works with practical desktop and mobile controls;
- creates reusable experiences that cannot be achieved as well through standard battles with new objectives;
- justifies the additional engineering, balance, and content cost.

The exploration-between-standard-battles model is the production baseline. A
future experiment would need an explicit product-direction change to replace it.

## Excursion Mana

An excursion uses one limited mana supply across the entire excursion. Mana does
not regenerate naturally between or during rooms. Spending mana in one encounter
therefore reduces what the player can use later in the excursion.

Authored recovery opportunities such as items, rewards, or rest points have not
been decided. If they are added, they should remain scarce enough that waiting
cannot erase the excursion's resource pressure.

## Moving-Summoner Placement

When an excursion encounter enables summoner movement, summon cards use a
card-specific placement radius centered on the summoner instead of the standard
"your half of the battlefield" rule. Each summon card can therefore reach a
different distance. Spell targeting remains independent of this summon rule.

The battle placement rule is configurable. Standard battles retain team-half
placement; the compact ruin experiment uses card-range placement. For multi-unit
cards, the selected formation center must be in range, while individual formation
members may extend slightly beyond the circle.

## Compact Ruin Skirmish Prototype

The first greybox experiment tests whether the normal card battle remains readable and enjoyable inside a room-sized ruin encounter. It is an experiment, not an accepted replacement for the starting direction above.

Current test setup:

- one open 84-by-48 greybox floor shown by a pulled-back fixed camera;
- a standard destroy-the-enemy-summoner battle with creatures and spells;
- a small authored player deck and an AI-controlled ruin defense core that occupies the standard opposing objective slot;
- an on-screen toggle between a stationary summoner and WASD summoner movement;
- summon placement radii centered on the moving summoner and supplied by each creature card;
- enemy creature placement constrained to the compact encounter space.
- effectively unlimited mana for both the player and ruin defense core so this isolated test measures combat interactions rather than resource tuning; this is test scaffolding, not the intended excursion economy.

Launch it with **F12 → Experimental Rooms → Compact Ruin**, or run `res://scenes/battle/battlefield/dev/compact_ruin_skirmish.tscn` directly in Godot.

The prototype should answer:

- Does movement add meaningful decisions, or only control burden?
- Does advancing the summoner to extend creature placement create worthwhile risk?
- Can the player move, aim cards, read mana, and follow creatures at the same time?
- Do creatures and spells both feel useful at this room scale?
- Is the compact room believable as a ruin while leaving enough space for the existing combat rules?
- Does stationary combat work well enough that movement is unnecessary?

Known greybox limits:

- The floor edge and invisible movement bounds currently define the test space; enclosing wall geometry is intentionally omitted.
- The camera can zoom between its full-room view and a closer inspection view, but panning remains disabled.
- Spells currently resolve only against battlefield units and cannot directly target the ruin defense core; objective-targeting spells are deferred because they require a general spell-rule decision.
- The movement experiment currently targets desktop keyboard controls; mobile controls are deliberately deferred until the idea proves worthwhile.
- Moving the summoner can currently displace enemy units through the summoner melee-protection bubble; the correct summoner-versus-creature interaction is unresolved and tracked as an open bug.
- Art, narrative dressing, hazards, exploration, and quest flow are outside this isolated combat test.
