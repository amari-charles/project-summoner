# Online Matchmaking

**Status:** Competitive implemented; Casual deferred  
**Last Updated:** 2026-08-13

## Current Player Flow

The Academy Online screen presents Casual and Competitive as the two intended matchmaking modes. Competitive is selected and functional. Casual remains visibly disabled until it has distinct matchmaking rules and backend queue routing; it must not silently use the Competitive queue.

Starting Competitive matchmaking replaces the rank display with queue status and elapsed time. The Start action becomes Cancel until the player leaves the queue or a match is found.

## Competitive Rank Display

The ranking system continues to store and calculate a global Elo rating. The Online screen presents that rating as:

- the player's current tier; and
- League Points earned above that tier's Elo floor.

`LP = current Elo - current tier floor`

LP resets to zero when a player crosses into a new rating-based tier. Sage is the final rating-based tier, so its LP continues accumulating above the Sage floor. Fateforged remains a leaderboard-derived top-20 display tier; its LP is still calculated from the underlying Sage floor.

Tier thresholds and LP calculation belong to `EloCalculator`. The UI reads the result through `RankingService` and does not duplicate rating thresholds.

## Deferred Work

- Define Casual matchmaking rules and queue identity.
- Add backend routing that keeps Casual matches separate from Competitive rating changes.
- Enable the Casual toggle only when that flow exists end to end.
