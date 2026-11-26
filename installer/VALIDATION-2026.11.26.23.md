# 2026.11.26.23 validation scope

This release changes update presentation and installation-record wording. A MarkerPatch-only update is titled “MarkerPatch update” and links to upstream; actual installer updates retain a distinct title, version and installer release link. Query failures never claim that the installer is current. A supported older installation record retains its version without an age label.

`UpdatePresentationChecks.cs` reproduces the misleading record label with the previous 2026.11.26.22 binary, then passes 190 checks with this release. It covers six update states in Chinese and English, installation-record labels, upstream/installer link routing and reachable buttons at 1280×680, 800×560 and 640×440 logical work areas. Two transparent-window renders verify the MarkerPatch-only notice.

The existing 46 browser-link checks, 10 headless language/settings checks and 7 updater compatibility checks also pass: 253 targeted checks in total. The prior 691 layout/dialog/installation checks belong to 2026.11.26.22 and were not rerun wholesale for this wording change.

SHA-256 comparisons match all 22 prior embedded resources except the intentionally changed English UI strings. Bundled MarkerPatch remains 3.0.7; this release does not integrate upstream 3.3.1 or claim its compatibility with the bundled DLC/runtime fixes. No real game installation, save loading or system display setting change was performed.
