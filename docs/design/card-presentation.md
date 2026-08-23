# Card Presentation

## Canonical Proportion

Every full card uses the gameplay card's 3:4 aspect ratio. A screen may choose a
named presentation tier, but it must not author an arbitrary width, height, or
aspect ratio.

| Tier | Design-space size | Intended use |
| --- | --- | --- |
| Compact | 90x120 | Dense secondary previews when Standard cannot fit |
| Standard | 120x160 | Battle hand, Online deck rail, collection/deck browsing, quest acceptance |
| Large | 180x240 | Journal, inspection, results, reward reveal |

These sizes describe the complete card face. Parent containers may center or
scroll cards, but must not stretch them. `Control.scale` must not be used to
invent another display size.

## Current Surface Assignments

- Battle hand: Standard
- Online selected-deck rail: Standard
- Collection and general deck editor: Standard
- Activity Preparation deck editor: Large
- Quest acceptance reward preview: Standard
- Quest Journal reward preview: Large
- Card detail, post-battle rewards, and granted-reward reveal: Large

Card icons, cropped artwork, and deliberately non-card reward rows are not full
card presentations and do not need to use these dimensions.

## Legacy Exception

`first_card_selection.tscn` is a bespoke legacy choice screen made from large
buttons rather than either shared card presentation. It must be removed with the
superseded onboarding path or rebuilt from the shared card surface; changing only
its button ratio would not constitute a real migration.
