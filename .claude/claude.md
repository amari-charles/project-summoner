# Claude Development Notes

## Project Guidelines

### Backwards Compatibility
**NEVER worry about backwards compatibility.** When implementing new features or changes, prioritize the new approach and remove old code paths. Don't keep fallback mechanisms or dual implementations.

Example: When implementing drag-and-drop for cards, remove click-to-play entirely rather than keeping both systems.

### Code Philosophy
- Prefer clean, single-path implementations
- Remove deprecated code immediately
- Don't hedge with "we can keep the old way too"
