# Opening Sequence Design

## Core Problem

Players don't care about story, lore, or mechanics before they're invested in the game. Long tutorials and exposition before gameplay cause players to disengage or skip.

## Design Principles

1. **Hook players fast** - Show the game's strengths immediately
2. **Gameplay first, story second** - Let players experience the fun before explaining the world
3. **Choices have permanent consequences** - Establish this expectation early
4. **"Fate is forged"** - The game's thematic core: your choices shape your destiny

---

## Current Opening Flow

1. Title Screen -> Loading
2. Awakening Ceremony (Summoner Selection)
   - Merlin introduces the ceremony
   - Player selects their elemental affinity (summoner)
   - Random option rewards bold players with "Fortune Favors the Bold" trait
3. Player enters the Academy campus

---

## Long-Term Vision: Playable Prologue

**Future opening flow:**

1. Title Screen -> Loading
2. **Playable Battle as Merlin** (NEW)
   - Player controls Merlin with a full, powerful deck
   - Epic battle that showcases the game at its best
   - No tutorial interruptions - just play
   - Demonstrates: card playing, unit summoning, spells, combat, winning
   - Sets the bar for what the player can aspire to
3. Transition: "Years later..." or similar
4. Awakening Ceremony (player as initiate, picking their summoner)
5. Academy quest play begins with the starter deck

**Why this works:**
- Players experience the "endgame fantasy" immediately
- They understand what the game IS before we ask them to commit to a path
- The contrast (powerful Merlin -> weak initiate) creates motivation to grow
- No wasted time explaining - they learn by doing

---

## The "Fortune Favors the Bold" Pattern

**Intent:** Reward players who embrace uncertainty and take risks.

**Current implementation:**
- Random summoner selection grants the "Fortune Favors the Bold" trait
- +10% damage to all attacks when you embrace the unknown
- Merlin's dialogue hints: "fate does favor the bold"

**Future expansion:**
- After summoner selection, communicate what players "missed" by not choosing random
- This creates the feeling of "what if?" and encourages replay
- Could be subtle (players discover others have the trait) or explicit (Merlin mentions it)
- Reinforces that choices matter and are permanent

---

## Merlin's Awakening Ceremony Dialogue

The dialogue is intentionally short and punchy (3 lines):

1. "The time has come, Initiate. Step into the circle."
2. "Every summoner carries an elemental spark. Yours is about to ignite."
3. "Choose your path wisely... though fate does favor the bold."

The final line directly references the trait name. Players who choose random will see "Fortune Favors the Bold" in their traits and connect the dots.

---

## Technical Notes

- Merlin battle will require: pre-built "Merlin deck", tutorial battle scene, victory transition
- Summoner reveal screen may need updates for post-Merlin-battle context
- Loading screen between title and gameplay should hide scene setup

---

*See also: [Summoner Progression](summoners/progression-system.md) | [Trait System](modifier-system.md)*
