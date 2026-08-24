# UI Handoff Capture Index

Working capture set for reviewing the current UI against the original commission. This is not the final designer brief.

## Sharing Workflow

The official external handoff lives in the private
[UI Design Commission Google Drive folder](https://drive.google.com/drive/folders/1JY987EC77hmu1elZ8JyOCuVzt5C2HDI5).

- Maintain the questionnaire and wireframe handoff as paired local Markdown and
  pageless Google Docs, keeping both versions synchronized after edits.
- Keep the full-resolution screenshot archive in the Drive folder and replace it
  when the capture set changes.
- Do not maintain separate PDF copies.
- Share the folder directly with the collaborator's email address, verify their
  access, and send the folder link.

## Commissioned Screens

### Title / Main

- [Current automatic title/loading presentation](screenshots/title-loading.png)

The game only needs illustrated opening artwork displayed while it loads. I had
originally envisioned that artwork as part of the commission. If the quote only
accounted for UI/title treatment and not the artwork, this screen can be removed
from the required deliverables.

### Summoner Screens

- [Starting summoner selection](screenshots/summoner-selection.png)

At the beginning of the game, the player chooses from the available summoners
or selects a random option. Each choice displays the summoner's name, elemental
affinity, and character art. Dialogue may accompany this sequence but should
not obscure the choices.

- [Summoner reveal](screenshots/summoner-reveal.png)

After the initial choice, this confirmation state presents the selected
summoner's character art, name, and elemental affinity before the player
continues.

- [Summoner switching carousel](screenshots/summoner-switch-carousel-sprites.png)

### Hero Info

- [Summoner profile and equipment](screenshots/summoner-profile.png)

Displays the active summoner's character art, level and XP, stats, traits,
upgrade points, and equipped items. Traits and equipment can be selected to
open their related management views.

### Campaign Map → Trait Tree

- [Trait Tree](screenshots/trait-development.png)
- [Trait node detail](screenshots/trait-development-node-detail.png)
- [Trait confirmation within the node popover](screenshots/trait-development-confirmation.png)

This replaces the originally commissioned Campaign Map. It is not the same
screen thematically, but the underlying UI need is similar: a navigable map of
connected nodes with progression states. The player opens a trait from the Hero
Info screen, selects nodes to view their effect, status, and cost, and can unlock
available nodes using upgrade points after confirmation.

The sample path shown here is not a required composition. Trait trees may branch
and should not be designed as a single straight line.

### Shop

- [Campus shop](screenshots/shop.png)
- [Shop item detail](screenshots/shop-item-detail.png)

Displays the player's gold and the cards or packs currently available to
purchase. Selecting an item opens its details, including artwork, description,
price, and purchase action.

### Collection

- [Collection and deck management](screenshots/collection-decks.png)
- [Card detail](screenshots/collection-card-detail.png)
- [New-deck dialog](screenshots/collection-new-deck-dialog.png)

The player browses, filters, and inspects owned cards. Deck creation and
management currently share this screen.

### Event Screen → Journal

- [Journal with an open quest selected](screenshots/journal-open-quest.png)

The originally commissioned Event Screen is now represented by the Quest
Journal, which organizes open, active, and completed quests and shows the
selected quest's giver, location, progress, description, and rewards.

### Dialogue

- [NPC dialogue](screenshots/dialogue-line.png)
- [Dialogue response choices](screenshots/dialogue-choices.png)

Dialogue shows the current speaker and conversation text, followed by response
choices when the player needs to make a decision.

### Rewards / Post-Battle

- [Battle results, progression, and rewards](screenshots/post-battle-results-rewards.png)

Appears after a match and shows the result, summoner and participating-card XP,
and earned rewards before the player continues.

### Settings

- [Standalone settings](screenshots/settings.png)
- [Settings during battle](screenshots/battle-pause-settings.png)

The same settings interface is available outside battle and from the pause
menu, with sections for audio, display, controls, gameplay, and accessibility.
Manual Load/Save is no longer needed because progress is saved automatically.

### Battle HUD

- [Battle HUD during preparation](screenshots/battle-hud.png)
- [Victory/Defeat overlay](screenshots/battle-victory-overlay.png)
- [Battle pause menu](screenshots/battle-pause-menu.png)

The Victory/Defeat overlay is an end-of-battle HUD state shown before the
post-battle results screen.

The HUD shows both summoners' health and mana, the current battle phase and
timer, the player's card hand, and speed and pause controls.
