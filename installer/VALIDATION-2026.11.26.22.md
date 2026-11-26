# 2026.11.26.22 validation scope

The previous 2026.11.11.20 main window uses a fixed 920×822 client area and absolute action-button positions. Restricting it to a 1280×680 logical work area reproduces the clipped installation button. This work area represents a 1920×1080 display at 150% scaling, with room reserved for the taskbar.

The new window adapts artwork before scaling: large artwork when space permits, small-icon cards when height is limited, and text cards in narrower/shorter client areas. It then scales controls, fonts and custom card painting proportionally from fixed original measurements. Common smaller work areas fit without either main-window scrollbar. A minimum scale of 0.72 retains a scroll fallback only for extreme sizes. Installation/uninstallation/progress stay in a docked footer. Resizing can restore the original large-artwork layout without cumulative font shrinking. Settings, details, log and update dialogs use responsive contents and fixed actions/links, avoiding outer dialog scrollbars. The browser-fallback dialog anchors its controls within the work area.

`AdaptiveLayoutChecks.cs` passes 274 checks using real WinForms layout and scroll ranges in transparent preview windows. The test supplies logical work-area rectangles, rather than changing the user's display resolution, Windows scaling or registry. It covers both languages, button visibility, footer separation, no scrollbars for the ordinary small-display cases, extreme-size scroll reachability, language switching after resizing, and update-dialog actions in narrow work areas.

| Display / scaling represented | Logical work area tested |
| --- | --- |
| 1920×1080 / 100% | 1920×1040 |
| 1920×1080 / 125% | 1536×824 |
| 1920×1080 / 150% | 1280×680 |
| 1920×1080 / 200% | 960×500 |
| 1366×768 / 100% | 1366×728 |
| 1280×720 / 100% | 1280×680 |
| 1024×768 / 100% | 1024×728 |
| 800×600 / 100% | 800×560 |
| Additional constrained desktop | 640×440 |

Visual preview checks use the candidate's real controls within a restricted logical work area. These checks do not constitute hardware testing on every resolution, Windows version, taskbar placement or monitor arrangement.

Rendered previews verify the 1280×680 Chinese icon-card layout (scale 0.986) and 800×560 English text-card layout (scale 0.834), with complete installation actions and no main-window scrollbars. Both previews also pass a resize-to-large-and-back check: the 11-point installation-button font restores exactly at full size. A further 10 headless language/settings checks, 7 updater checks and 26 isolated installation-audit checks pass. The audit fixture used the C-drive temporary directory because the project drive was below the existing 512 MiB free-space guard; only the test's fresh-fixture path guard changed in that local runner. Production disk-space protection was retained.

`DialogLayoutChecks.cs` adds 328 checks across 1920×1040, 1280×680, 800×560 and 640×440 logical work areas, in both languages. It opens actual settings/details/log dialogs in preview mode, verifies both settings tabs and the last parameter, cancels and reopens settings to check shared-control lifetime, and checks update/browser-fallback actions and details links. All stay within the dialog without outer scrolling. Parameter descriptions wrap to the current width; only the parameter list and text bodies scroll internally. A further 46 browser-link/fallback checks pass. Total current checks: 691. System-native installation confirmations and error messages were not individually triggered through real game installations.

SHA-256 comparisons match all 22 previous embedded resources except the intentionally updated English UI string resource. The installer was not run against a real game installation during this UI-only release.

This release changes installer UI and version metadata only. The 24 subtitle extensions, all localized game assets and original/runtime modules remain the previous release's exact embedded bytes. No game installation, save loading or gameplay regression is required by this layout-only change, and none is claimed for this release. Earlier game validation remains documented in `VALIDATION-2026.11.11.20.md` and `VALIDATION-2026.11.6.19.md`.
