class_name DeckConstants
extends RefCounted

## Deck Constants
##
## Shared constants for deck management across the codebase.

## The name used for the starter deck created during onboarding.
## This must match what's stored in profile data, so use a hardcoded
## string rather than a localization key.
const STARTER_DECK_NAME: String = "Starter Deck"

## Maximum number of cards before auto-add to starter deck stops.
## After this threshold, players must manually manage their deck.
const STARTER_DECK_AUTO_ADD_THRESHOLD: int = 15

## Standard deck size limits
const MIN_DECK_SIZE: int = 1
const MAX_DECK_SIZE: int = 30
