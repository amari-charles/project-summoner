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

- File: [Fateforged-UI-Review-Windows.zip](https://drive.google.com/file/d/1YiWdaszFIFJ1zUyjabfyZUDEVKXfntLK/view?usp=drivesdk)
- Uploaded: August 30, 2026
- Size: 87,706,576 bytes (87.7 MB)
- SHA-256: `15abe1c1a53c1d5a5417798b4a5871652267e07d2e67bcf7650627ddd1133c61`
- Export preset: `UI Designer Review`
- Windows validation: [successful GitHub Actions run](https://github.com/amari-charles/project-summoner/actions/runs/33323966761)

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

Replace the contents of the existing
`Fateforged-UI-Review-Windows.zip` Drive file in place. Preserving its Drive file
ID keeps the link in this guide and any messages to the designer valid.

After replacement:

1. Verify the file is still inside the UI Design Commission folder.
2. Record its new upload date, byte size, and SHA-256 above.
3. Replace the Windows validation-run link above with the successful run for
   that revision.
4. Download the shared file once and confirm the archive extracts correctly.
5. Tell the designer what changed and whether they should restart the
   walkthrough from a fresh profile.

Do not upload source-art directories, repository tests, project documentation,
or development tools with the playable handoff. The review export preset
excludes those materials.
