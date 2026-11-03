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

## Split runtime additions (2026.11.3.9)

The existing DLC core/menu handling is in `runtime-fixes`; independent Power Node and APX save-display additions are in `extras`. Complete corresponding sources are included. The official MarkerPatch 3.0.7 binary is unchanged. Chinese localization remains a data patch.

SafetyHook and Zydis license copies are in `extras/licenses`. Original upstream license/copyright notices in the amalgamated sources remain intact.
