# Build 2026.11.3.9

Build runtime-fixes/build.cmd and extras/build.cmd with x86 MSVC and the Windows SDK. Run installer/tools/build_installer.ps1 with both -SeveredAsi and -ExtrasAsi, plus the pinned private dependencies in DEPENDENCIES.json. Official MarkerPatch 3.0.7 and the original Chinese/assets/delta resources are unchanged. Installer target: .NET Framework 4, x86.

The standalone extras has no import from the DLC core; its only loader requirement is MarkerPatch. Game image preview data remains the existing DS2DAT3.DAT managed by the installer. Tests for native hooks, both load orders, ownership, migration, rollback, UI and browser failures are in the respective tests directories.

Reuse the existing private source resources; do not commit game data or compiled binaries. See tools/build_save_preview_assets.py for reproducing preview deltas from legitimate inputs.
