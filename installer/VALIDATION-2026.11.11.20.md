# 2026.11.11.20 validation scope

This release keeps the 2026.11.6.19 runtime modules, fonts, punctuation and 21 short-label corrections. It changes only 24 existing subtitle duration words in the localized DLC stream. Every selected window ends at the next existing TOB start or at the conservative audio-stream tail; subtitle starts, audio cue times, text IDs and the 15 entries without spare room are unchanged.

The main example is `0A9BB566` (“可现在，你们俩都成了这个计划最不确定的因素。对不起，盖伯。”), extended from 2000 ms to 3600 ms. The resource-level audit is recorded in `maintenance/subtitle-duration-audit-20261111/installed-subtitle-density-audit.json` and the exact patch map in `maintenance/subtitle-duration-audit-20261111/subtitle-duration-patch.json`.

This is a targeted resource timing change. The 2026.11.11.20 binary passed the isolated install check and a targeted Steam upgrade using the existing saves/settings: the installer report matched the new DAT2 SHA-256 and found no save or profile files changed. It does not claim a complete DLC playthrough or all platform/checkpoint combinations.
