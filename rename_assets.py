#!/usr/bin/env python3
"""
Asset renaming script for Project Summoner
Renames all assets to snake_case and updates all references
"""

import os
import re
import subprocess
from pathlib import Path

def to_snake_case(name):
    """Convert filename to snake_case"""
    # Remove parentheses and convert to underscores
    name = name.replace('(', '_').replace(')', '')

    # Replace spaces with underscores
    name = name.replace(' ', '_')

    # Replace hyphens with underscores
    name = name.replace('-', '_')

    # Convert to lowercase
    name = name.lower()

    # Remove duplicate underscores
    name = re.sub(r'_+', '_', name)

    # Remove trailing underscores before extension
    name = re.sub(r'_+\.', '.', name)

    return name

def get_asset_files():
    """Get all asset files that need renaming"""
    assets_dir = Path('assets')
    extensions = ['.png', '.jpg', '.jpeg', '.PNG', '.JPG']

    files_to_rename = []

    for ext in extensions:
        for filepath in assets_dir.rglob(f'*{ext}'):
            if filepath.is_file():
                filename = filepath.name
                new_filename = to_snake_case(filename)

                # Only add if name would change
                if filename != new_filename:
                    new_path = filepath.parent / new_filename
                    files_to_rename.append((str(filepath), str(new_path)))

    return files_to_rename

def create_rename_map():
    """Create mapping of old paths to new paths"""
    files = get_asset_files()

    # Sort by path depth (deepest first) to avoid conflicts
    files.sort(key=lambda x: x[0].count('/'), reverse=True)

    return files

def perform_renames(rename_map, dry_run=True):
    """Perform the actual renaming using git mv"""
    print(f"\n{'DRY RUN: ' if dry_run else ''}Renaming {len(rename_map)} files...\n")

    success_count = 0
    failed_renames = []

    for old_path, new_path in rename_map:
        try:
            if not dry_run:
                # Use git mv to preserve history
                subprocess.run(['git', 'mv', old_path, new_path], check=True, capture_output=True)
                success_count += 1
            else:
                print(f"Would rename: {old_path} → {new_path}")
        except subprocess.CalledProcessError as e:
            failed_renames.append((old_path, new_path, str(e)))
            print(f"FAILED: {old_path}")

    if not dry_run:
        print(f"\nSuccessfully renamed {success_count} files")
        if failed_renames:
            print(f"Failed to rename {len(failed_renames)} files:")
            for old, new, error in failed_renames:
                print(f"  {old} → {new}")
                print(f"    Error: {error}")

    return failed_renames

def update_references(rename_map):
    """Update all references in .tres, .tscn, and .gd files"""
    print("\nUpdating references in resource and scene files...")

    # Build path mapping (old → new, without 'res://' prefix)
    path_replacements = {}
    for old, new in rename_map:
        # Store both with and without res:// prefix
        path_replacements[old] = new
        path_replacements[f"res://{old}"] = f"res://{new}"

    # Find all files that might contain references
    file_types = ['*.tres', '*.tscn', '*.gd']
    files_to_update = []

    for pattern in file_types:
        files_to_update.extend(Path('.').rglob(pattern))

    updated_count = 0

    for filepath in files_to_update:
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()

            original_content = content
            modified = False

            # Replace all old paths with new paths
            for old_path, new_path in path_replacements.items():
                if old_path in content:
                    content = content.replace(old_path, new_path)
                    modified = True

            if modified:
                with open(filepath, 'w', encoding='utf-8') as f:
                    f.write(content)
                updated_count += 1
                print(f"  Updated: {filepath}")

        except Exception as e:
            print(f"  ERROR updating {filepath}: {e}")

    print(f"\nUpdated references in {updated_count} files")

def remove_import_files(rename_map):
    """Remove .import files for renamed assets (Godot will regenerate them)"""
    print("\nRemoving .import files...")

    removed_count = 0

    for old_path, new_path in rename_map:
        # Remove old .import file
        old_import = f"{old_path}.import"
        if os.path.exists(old_import):
            os.remove(old_import)
            removed_count += 1

    print(f"Removed {removed_count} .import files (Godot will regenerate)")

def main():
    """Main execution"""
    import sys

    dry_run = '--execute' not in sys.argv

    rename_map = create_rename_map()

    print(f"Found {len(rename_map)} files to rename\n")

    # Print first 20 for verification
    for i, (old, new) in enumerate(rename_map[:20]):
        print(f"{old}")
        print(f"  → {new}")
        if i == 19 and len(rename_map) > 20:
            print(f"\n... and {len(rename_map) - 20} more files")

    if dry_run:
        print("\n" + "="*60)
        print("DRY RUN MODE - No changes made")
        print("Run with --execute to perform the actual renaming")
        print("="*60)
    else:
        print("\n" + "="*60)
        print("EXECUTING RENAME OPERATION")
        print("="*60)

        # Perform renames
        failed = perform_renames(rename_map, dry_run=False)

        if not failed:
            # Update references
            update_references(rename_map)

            # Remove old .import files
            remove_import_files(rename_map)

            print("\n" + "="*60)
            print("RENAME COMPLETE")
            print("Next steps:")
            print("1. Open project in Godot to regenerate .import files")
            print("2. Test that all assets load correctly")
            print("3. Commit changes")
            print("="*60)
        else:
            print("\nSome renames failed. Fix errors before updating references.")

    return rename_map

if __name__ == '__main__':
    main()
