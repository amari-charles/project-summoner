# UI Designer Handoff

This is the canonical process for preparing and sharing a playable UI review
build with the UI designer. The review-build implementation details live in
[UI Designer Review Build](ui-designer-review-build.md).

## Handoff destination

All external UI commission material belongs in the private
[UI Design Commission Google Drive folder](https://drive.google.com/drive/folders/1JY987EC77hmu1elZ8JyOCuVzt5C2HDI5).

Share the folder directly with the collaborator's email address, verify their
access, and send the folder link. Do not create a second handoff folder for a
new build.

## Current playable build

- File: [\[DEBUG\] Fateforged UI Review - Windows - 2026-09-06.zip](https://drive.google.com/file/d/1Gwyn4csL0EOmHNyrARAdX0w0ydh4fU9C/view?usp=drivesdk)
- Uploaded: September 6, 2026
- Size: 87,644,967 bytes (87.6 MB)
- SHA-256: `27465c2508c23cec58660d020faf1fe0399ec143f3384dade8f8fffbaad9d03b`
- Export preset: `UI Designer Review`
- Windows validation: [successful GitHub Actions run](https://github.com/amari-charles/project-summoner/actions/runs/34072365699)

The recipient must download and extract the entire ZIP, then run
`Fateforged-UI-Review.exe`. The adjacent
`data_Fateforged_windows_x86_64` directory must remain next to the executable.
The preset enables the guided UI walkthrough automatically. Press F12,
backtick (`), or tilde (~) to open the debug panel; the Arena tab launches a
battle directly.

## Producing a handoff build

1. Commit and push the intended handoff revision.
2. Run the normal fast validation suite:

   ```bash
   ./tools/run_tests.sh --fast
   ```

3. Build and export with the committed review preset:

   ```bash
   mkdir -p builds/ui-review
   dotnet build Fateforged.csproj -c ExportRelease
   /Applications/Godot_mono.app/Contents/MacOS/Godot \
     --headless \
     --path . \
     --export-release "UI Designer Review" \
     builds/ui-review/Fateforged-UI-Review.exe
   ```

4. Package the executable and its matching .NET data directory together. The
   ZIP root must contain both of these entries:

   ```text
   Fateforged-UI-Review.exe
   data_Fateforged_windows_x86_64/
   ```

   From `builds/ui-review`, create the archive with:

   ```bash
   zip -r -9 ../Fateforged-UI-Review-Windows.zip \
     Fateforged-UI-Review.exe \
     data_Fateforged_windows_x86_64
   ```

5. Test the ZIP before uploading:

   ```bash
   unzip -t builds/Fateforged-UI-Review-Windows.zip
   shasum -a 256 builds/Fateforged-UI-Review-Windows.zip
   ```

6. Run `.github/workflows/windows-ui-review-smoke.yml` in GitHub Actions. The
   handoff is ready only after Windows successfully exports and launches the
   build.

## Updating the Drive handoff

Upload each replacement as a clearly named new ZIP in the existing commission
folder. Keep the prior verified build as a fallback, and update the current-build
link above to the new file.

After upload:

1. Verify the file is inside the UI Design Commission folder.
2. Record its upload date, byte size, and SHA-256 above.
3. Replace the Windows validation-run link above with the successful run for
   that revision.
4. Verify the designer inherits access from the commission folder.
5. Download the shared file once and confirm the archive extracts correctly.
6. Tell the designer what changed and whether they should restart the
   walkthrough from a fresh profile.

Do not upload source-art directories, repository tests, project documentation,
or development tools with the playable handoff. The review export preset
excludes those materials.
