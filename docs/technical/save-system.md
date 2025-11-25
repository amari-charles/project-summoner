# Save System Architecture

## Current Implementation

The save system (`json_profile_repository.gd`) already implements several industry best practices:

### ✅ Implemented Features

1. **Atomic Writes**
   - Writes to temporary file first (`profile.tmp`)
   - Renames temp → main (atomic operation on most filesystems)
   - Prevents corruption from interrupted writes

2. **Dual Backup System**
   - `profile.json` - Main save
   - `profile.bak1` - Previous save
   - `profile.bak2` - Save before previous
   - Rotates backups on each save

3. **Corruption Recovery**
   - Tries main save first
   - Falls back to backup1 if corrupted
   - Falls back to backup2 if backup1 corrupted
   - Creates fresh profile if all saves corrupted

4. **Debounced Autosave**
   - 0.5 second delay after data change
   - Immediate save for critical operations (settings, match results)
   - Reduces I/O while maintaining data safety

5. **Write-Ahead Log (WAL)**
   - Logs all operations with timestamps and UUIDs
   - Enables future cloud sync with conflict resolution
   - Auto-trims to last 100 entries

6. **Version Migration**
   - Tracks save version number
   - Migration path for schema changes
   - Preserves player data across updates

### File Structure

```
user://profiles/{profile_id}/
├── profile.json     # Main save file
├── profile.bak1     # Previous save (backup 1)
├── profile.bak2     # Older save (backup 2)
├── profile.tmp      # Temporary file during atomic write
└── wal.json         # Write-ahead log (future)
```

## Recommendations for Future Improvements

### Priority 1: Critical Safety

1. **Checksum/CRC Verification** (Not Yet Implemented)
   - Add SHA-256 hash to save file
   - Verify integrity on load
   - Detect silent corruption

```gdscript
# Add to save data
_data["checksum"] = _calculate_checksum(_data)

# Verify on load
func _verify_checksum(data: Dictionary) -> bool:
    var stored_checksum: String = data.get("checksum", "")
    data.erase("checksum")
    var calculated: String = _calculate_checksum(data)
    return stored_checksum == calculated
```

2. **Crash Protection** ✅ IMPLEMENTED
   - Saves on `NOTIFICATION_WM_CLOSE_REQUEST` (desktop close)
   - Saves on `NOTIFICATION_APPLICATION_PAUSED` (mobile background)
   - Saves on `NOTIFICATION_APPLICATION_FOCUS_OUT` if pending changes

### Priority 2: User Experience

3. **Multiple Save Profiles**
   - Already stubbed in `_get_or_create_default_profile()`
   - Add profile selection UI
   - Each profile is independent

4. **Manual Save Slots** (Optional)
   - Allow players to create named save points
   - "Quick Save" and "Quick Load" functionality
   - Useful for roguelike "checkpoint" saves

### Priority 3: Future Features

5. **Cloud Sync (Using WAL)**
   - WAL enables conflict resolution
   - Upload WAL entries to cloud service
   - Merge WAL from multiple devices
   - Resolve conflicts by timestamp or user choice

6. **Save Compression** (If Needed)
   - Only needed if save files grow large
   - Use Godot's `compress_string()`
   - Typical saves are < 50KB, compression unnecessary

7. **Encryption** (If Needed)
   - Only needed for anti-cheat protection
   - Use `Crypto` class for AES encryption
   - Store key securely (not in code)
   - Adds complexity, only add if cheating is a concern

### Priority 4: Developer Tools

8. **Save File Validator**
   - Standalone tool to validate save file schema
   - Check for orphaned references (decks pointing to deleted cards)
   - Useful for debugging player-reported issues

9. **Save File Export/Import**
   - Allow players to backup saves externally
   - Export as downloadable file
   - Import to restore from backup

## Implementation Notes

### When to Save (Current)
- Resource changes: Debounced (0.5s)
- Card collection changes: Debounced
- Deck changes: Debounced
- Settings changes: Immediate
- Match results: Immediate
- Hero unlocks: Debounced

### When to Save (Recommended Additions)
- Scene transitions (entering battle, leaving shop)
- Tutorial milestones
- Any "point of no return" gameplay moment

## References

- [GDC: Saving & Loading Games](https://www.gdcvault.com/browse/gdc-19/play/1025889) - Industry patterns
- [Godot FileAccess Best Practices](https://docs.godotengine.org/en/stable/tutorials/io/saving_games.html)
- [Game Save System Design](https://gameprogrammingpatterns.com/command.html) - Command pattern for undo/redo
