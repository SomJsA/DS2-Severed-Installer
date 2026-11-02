# Third-party notices

The code-only source publication contains no game archives, localization/font streams,
artwork, icons, DLL/ASI/EXE dependencies or compiled installer.

- MarkerPatch: https://github.com/Wemino/MarkerPatch, source commit
  `d40f9ce31175c8fa5d59cca9dc3b417a396076f8`. GPL-2.0, license in
  `licenses/MarkerPatch-LICENSE.txt`. Obtain/build binaries from upstream.
- DS2SeveredPC: https://github.com/Tubzbuster/DS2SeveredPC, release tag
  `v1.1.5`. License in `licenses/DS2SeveredPC-LICENSE.txt`.
- 7-Zip: https://www.7-zip.org/, version used 26.05. LGPL/unRAR and other
  terms in `licenses/7-Zip-License.txt`; corresponding source is available
  from https://www.7-zip.org/download.html.
- Swei Shear Sans CJK SC Medium: https://github.com/max32002/swei-shear-sans,
  SIL OFL 1.1 in `licenses/Swei-OFL.txt`. No font binary or generated stream
  is published here.
- User-supplied Titan Station artwork, legacy icon/credits and Severed poster are externally
  supplied build resources. MarkerPatch uses its official upstream logo.
  Those images are not covered by this repository's code license.

The release installer embeds the separately supplied runtime dependencies,
optional Chinese assets and UI assets. The English DLC package is separate.
Electronic Arts game data and third-party artwork retain their original rights.
See DEPENDENCIES.json for the exact external input identities.

## SomJs Severed Power Node fix (2026.11.2)

Modified DS2SeveredPC core plus a Severed-scoped shop policy and SafetyHook/Zydis. Complete corresponding plugin source and license-bearing dependency sources are in `runtime-fixes` in this repository. Original upstream notices are retained there. This local build bundles Candidate 2 with detailed hook diagnostics and a validated MarkerPatch callback fallback. Corresponding source and licenses are included in runtime-fixes. Earlier Steam feedback applies to Candidate 1 with the previous Marker build; Candidate 1 shop-hook installation failed with official MarkerPatch 3.0.7 in EA. Candidate 2 gameplay validation is pending.

MarkerPatch 3.0.7 uses the official release DLL and achievement assets from Wemino/MarkerPatch, source tag 3.0.7 (d40f9ce31175c8fa5d59cca9dc3b417a396076f8). The asset manifest contains metadata only; actual third-party binaries/images stay outside the code repository. Existing upstream and dependency licenses remain applicable.
