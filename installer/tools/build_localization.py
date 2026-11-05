#!/usr/bin/env python3
"""Build the Chinese Severed overlay archive without modifying base DAT files."""

from __future__ import annotations

import csv
import hashlib
import json
import os
import re
import struct
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent
PACKAGE_ROOT = (
    ROOT
    if (ROOT / "workbook_translations.json").exists()
    else ROOT.parent
)
WORKBOOK_PATH = Path(
    os.environ.get(
        "DS2_WORKBOOK_JSON",
        str(PACKAGE_ROOT / "workbook_translations.json")
        if (PACKAGE_ROOT / "workbook_translations.json").exists()
        else str(PACKAGE_ROOT / "translation" / "workbook_translations.json"),
    )
)
SUMMARY_PATH = Path(
    os.environ.get(
        "DS2_SUMMARY_JSON",
        str(PACKAGE_ROOT / "translation" / "severed_218.json")
        if (PACKAGE_ROOT / "translation" / "severed_218.json").exists()
        else str(ROOT / "severed_218.json"),
    )
)
OUT = Path(
    os.environ.get("DS2_BUILD_OUT", str(PACKAGE_ROOT / "generated"))
)
if not os.environ.get("DS2_BASE_DAT2") or not os.environ.get("DS2_BASE_DAT0"):
    raise SystemExit("Set DS2_BASE_DAT2 to the English 81a145da... DLC and DS2_BASE_DAT0 to a pristine fd7d1bdb... DAT0.")
SOURCE_DAT2 = Path(os.environ["DS2_BASE_DAT2"])
SOURCE_DAT0 = Path(os.environ["DS2_BASE_DAT0"])
PARENT = SOURCE_DAT2.parent
SOURCE_DAT2_SHA256 = "81a145da647660d34a6fcb15ea3a8709c74855cf1beef23fc668a3ae42eb6ea0"
DLC_STREAM_NAME = "pdlc1_ch01_mng\\pdlc1_ch01_mng.str"
BASE_FONT_HASH = 0x4564F3D7
BASE_TEXT_HASH = 0xD22D5B5A
LH2_ORIGINAL_BYTES = 94302
ALIGN = 0x800
RESEARCH = Path(os.environ.get("DS2_RESEARCH_DIR", str(ROOT / "research")))
if not (RESEARCH / "ds2" / "bigarchive.py").is_file():
    raise SystemExit("Set DS2_RESEARCH_DIR to DS2SeveredPC release-source/tools/research (frozen release tag v1.1.5).")
sys.path.insert(0, str(RESEARCH))
from ds2 import bigarchive, streamfile, streamwrite, toc  # noqa: E402


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def file_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        while True:
            chunk = handle.read(1 << 20)
            if not chunk:
                break
            digest.update(chunk)
    return digest.hexdigest()


def normalize(text: str) -> str:
    return re.sub(r"\s+", " ", text).strip().casefold()


def space_chinese(text: str) -> str:
    """Apply the spacing convention used by the recovered DS2 Chinese patch."""
    output = []
    for part in re.split(r"(<[^>]+>|\\\\n)", text):
        if part.startswith("<") or part == "\\n":
            output.append(part)
            continue
        part = re.sub(r"(?<=[\u4e00-\u9fff])(?=[\u4e00-\u9fff])", " ", part)
        part = re.sub(r"(?<=[\u4e00-\u9fff])(?=[A-Za-z0-9])", " ", part)
        part = re.sub(r"(?<=[A-Za-z0-9])(?=[\u4e00-\u9fff])", " ", part)
        part = re.sub(r"(?<=[，。！？：；])(?=[^\s])", " ", part)
        output.append(part)
    return re.sub(r" {2,}", " ", "".join(output)).strip()


def load_records() -> tuple[list[dict], dict[str, str]]:
    summary = json.loads(SUMMARY_PATH.read_text(encoding="utf-8"))
    workbook = json.loads(WORKBOOK_PATH.read_text(encoding="utf-8"))
    records = summary["records"]
    if len(records) != 218:
        raise ValueError(f"expected 218 records, found {len(records)}")

    workbook_rows = workbook["rows"]
    if len(workbook_rows) != len(records):
        raise ValueError(
            f"workbook has {len(workbook_rows)} rows, expected {len(records)}"
        )
    if [row["string_id"] for row in workbook_rows] != [
        record["string_id"] for record in records
    ]:
        raise ValueError("workbook string_id order differs from Severed DLC table")

    translations = {}
    for record, row in zip(records, workbook_rows):
        if row["english"] != record["text"]:
            raise ValueError(f"English source mismatch: {record['string_id']}")
        chinese = row["chinese"]
        if not chinese:
            raise ValueError(f"empty translation: {record['string_id']}")
        for tag in ("<vocalization>", "<vocalizations>"):
            if record["text"].count(tag) != chinese.count(tag):
                raise ValueError(f"tag mismatch: {record['string_id']} {tag}")
        translations[record["string_id"]] = chinese
    return records, translations


def parse_lh2(data: bytes) -> tuple[tuple[int, ...], list[str], list[list[bytes]]]:
    if data[:4] != b"LCH2":
        raise ValueError("expected LCH2")
    heads = struct.unpack_from("<8I", data, 0)
    footer, count, language_count = heads[3], heads[4], heads[5]
    if count != 218:
        raise ValueError(f"expected 218 LH2 entries, found {count}")
    languages = [
        data[footer + i * 8 : footer + i * 8 + 2].decode("ascii")
        for i in range(language_count)
    ]
    table = 0x20 + count * 4
    rows = []
    for language_index in range(language_count):
        values = []
        for index in range(count):
            offset = struct.unpack_from(
                "<I", data, table + language_index * count * 4 + index * 4
            )[0]
            end = data.find(b"\0", offset)
            if end < 0:
                raise ValueError("unterminated LH2 string")
            values.append(data[offset:end])
        rows.append(values)
    return heads, languages, rows


def build_lh2(
    original: bytes, records: list[dict], translations: dict[str, str]
) -> bytes:
    heads, languages, rows = parse_lh2(original[:LH2_ORIGINAL_BYTES])
    count, language_count = heads[4], heads[5]
    chinese = [
        space_chinese(translations[record["string_id"]]).encode("utf-8")
        for record in records
    ]

    out = bytearray(32)
    for record in records:
        out += struct.pack("<I", int(record["string_id"], 16))
    offset_table = len(out)
    out += bytes(4 * count * language_count)

    for language_index in range(language_count):
        for index in range(count):
            value = chinese[index] if language_index == 0 else rows[language_index][index]
            struct.pack_into("<I", out, offset_table + (language_index * count + index) * 4, len(out))
            out += value + b"\0"

    footer = len(out)
    for language in languages:
        out += language.encode("ascii").ljust(8, b"\0")
    file_size = len(out)
    struct.pack_into("<8I", out, 0, heads[0], file_size, heads[2], footer,
                     heads[4], heads[5], heads[6], heads[7])
    return bytes(out)


def patch_sdat_payload(stream: bytes, asset_offset: int, payload: bytes) -> bytes:
    """Patch an uncompressed asset across its SDAT/TADS chunks in place."""
    out = bytearray(stream)
    offset = 12
    active = False
    chunks = []
    while offset + 8 <= len(out):
        tag = bytes(out[offset : offset + 4])
        size = struct.unpack_from("<I", out, offset + 4)[0]
        if size == 0:
            break
        if tag == b"COHS":
            sub = bytes(out[offset + 8 : offset + 12])
            if offset == asset_offset:
                if sub != b"RDHS":
                    raise ValueError("asset offset does not point at SHDR")
                active = True
            elif sub == b"RDHS":
                active = False
            elif active and sub == b"TADS":
                chunks.append((offset + 12, size - 12))
        offset += size
    capacity = sum(length for _, length in chunks)
    if active or capacity < len(payload):
        raise ValueError(f"insufficient SDAT capacity: {capacity} < {len(payload)}")
    source_offset = 0
    for chunk_offset, chunk_size in chunks:
        take = min(chunk_size, len(payload) - source_offset)
        if take <= 0:
            break
        out[chunk_offset : chunk_offset + take] = payload[
            source_offset : source_offset + take
        ]
        source_offset += take
    if source_offset != len(payload):
        raise ValueError("SDAT payload patch did not consume the full payload")
    return bytes(out)


def read_archive_entry(path: Path, name_hash: int) -> bytes:
    with path.open("rb") as handle:
        index, _ = bigarchive.read_index(handle)
        offset, size = index[name_hash]
        handle.seek(offset)
        return handle.read(size)


def rebuild_dlc_stream(original_stream: bytes, translated_lh2: bytes) -> bytes:
    be, assets = streamfile.read(original_stream)
    if be:
        raise ValueError("expected a PC DLC stream")
    if len(assets) != 451:
        raise ValueError(f"expected 451 DLC stream assets, found {len(assets)}")
    if assets[423].type != "LH2" or "weller1" not in assets[423].path.lower():
        raise ValueError("unexpected DLC LH2 asset index")

    writer = streamwrite.Writer()
    for index, asset in enumerate(assets):
        payload = translated_lh2 if index == 423 else asset.payload()
        writer.add_asset(asset, payload)
    rebuilt = writer.data()

    _, readback = streamfile.read(rebuilt)
    if len(readback) != len(assets):
        raise ValueError("DLC stream asset count changed")
    for index, (before, after) in enumerate(zip(assets, readback)):
        expected = translated_lh2 if index == 423 else before.payload()
        if after.payload() != expected:
            raise ValueError(f"DLC stream readback mismatch at asset {index}")
    return rebuilt


def update_dlc_toc(toc_data: bytes, stream_size: int) -> bytes:
    parsed = toc.parse(toc_data, "<")
    stream_name = "pdlc1_ch01_mng\\pdlc1_ch01_mng"
    if stream_name not in parsed["stream_names"]:
        raise ValueError("DLC main stream not found in TOC")
    return toc.set_size(toc_data, stream_name, stream_size)


def rebuild_bigh(source: Path, target: Path, replacements: dict[int, bytes]) -> None:
    with source.open("rb") as handle:
        original_index, trailer = bigarchive.read_index(handle)

    entries = dict(original_index)
    for name_hash, data in replacements.items():
        entries[name_hash] = (None, len(data))
    ordered = sorted(entries.items())
    header_size = 16 + 12 * len(ordered) + len(trailer)
    position = -(-header_size // ALIGN) * ALIGN
    layout = []
    for name_hash, (_, size) in ordered:
        layout.append((name_hash, position, size))
        position = -(-(position + size) // ALIGN) * ALIGN
    if position > 0xFFFFFFFF:
        raise ValueError("rebuilt DAT2 exceeds 4 GiB")

    temporary = target.with_suffix(target.suffix + ".building")
    with source.open("rb") as src, temporary.open("wb") as dst:
        dst.write(b"BIGH")
        dst.write(struct.pack("<I", position))
        dst.write(struct.pack(">II", len(layout), header_size))
        for name_hash, offset, size in layout:
            dst.write(struct.pack(">III", offset, size, name_hash))
        dst.write(trailer)
        for name_hash, offset, size in layout:
            dst.write(bytes(offset - dst.tell()))
            data = replacements.get(name_hash)
            if data is not None:
                if len(data) != size:
                    raise ValueError("replacement size mismatch")
                dst.write(data)
            else:
                source_entry = original_index[name_hash]
                src.seek(source_entry[0])
                remaining = size
                while remaining:
                    chunk = src.read(min(remaining, 1 << 20))
                    if not chunk:
                        raise ValueError("unexpected source DAT2 EOF")
                    dst.write(chunk)
                    remaining -= len(chunk)
        dst.write(bytes(position - dst.tell()))
    os.replace(temporary, target)


def verify_rebuilt_archive(
    source: Path,
    target: Path,
    replacements: dict[int, bytes],
    expected_source_entries: int,
) -> None:
    with source.open("rb") as handle:
        source_index, _ = bigarchive.read_index(handle)
    with target.open("rb") as handle:
        target_index, _ = bigarchive.read_index(handle)
    if len(source_index) != expected_source_entries or len(target_index) != len(source_index):
        raise ValueError("archive entry count mismatch")

    def entry_equals(src, src_offset, dst, dst_offset, size):
        src.seek(src_offset)
        dst.seek(dst_offset)
        remaining = size
        while remaining:
            count = min(remaining, 1 << 20)
            if src.read(count) != dst.read(count):
                return False
            remaining -= count
        return True

    with source.open("rb") as src, target.open("rb") as dst:
        for name_hash, source_entry in source_index.items():
            target_entry = target_index[name_hash]
            if name_hash in replacements:
                dst.seek(target_entry[0])
                if dst.read(target_entry[1]) != replacements[name_hash]:
                    raise ValueError(f"replacement mismatch: {name_hash:08X}")
            elif target_entry[1] != source_entry[1] or not entry_equals(
                src, source_entry[0], dst, target_entry[0], source_entry[1]
            ):
                raise ValueError(f"unchanged archive entry was altered: {name_hash:08X}")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    records, translations = load_records()
    stream_hash = bigarchive.name_hash(DLC_STREAM_NAME)
    original_stream = read_archive_entry(SOURCE_DAT2, stream_hash)
    _, original_assets = streamfile.read(original_stream)
    if len(original_assets) != 451 or original_assets[423].type != "LH2":
        raise ValueError("base DLC stream layout mismatch")
    original_lh2 = original_assets[423].payload()
    translated_lh2 = build_lh2(original_lh2, records, translations)

    patched_stream = rebuild_dlc_stream(original_stream, translated_lh2)

    default_font = ROOT / "font_build" / "patched_font_4564F3D7.workbook.str"
    if not default_font.exists():
        default_font = PACKAGE_ROOT / "assets" / "chinese" / "4564F3D7.str"
    default_text = ROOT / "resource_translation_D22D5B5A.str"
    if not default_text.exists():
        default_text = PACKAGE_ROOT / "assets" / "chinese" / "D22D5B5A.str"
    font_data = Path(os.environ.get("DS2_FONT_STREAM", str(default_font))).read_bytes()
    text_data = Path(os.environ.get("DS2_TEXT_STREAM", str(default_text))).read_bytes()
    if sha256(text_data) != "0a75e1bc191e2127e2cab7f01c2f3ba6dc45e4c3452803aeac97aa403a0546e7":
        raise ValueError("base Chinese translation stream hash mismatch")
    if sha256(font_data) != "88486ee526d5f5cee146adb80efb756cc645c521673e1e0df88bc053e4b8ae4b":
        raise ValueError("base Chinese font stream hash mismatch")

    if file_sha256(SOURCE_DAT2) != SOURCE_DAT2_SHA256:
        raise ValueError("source DAT2 hash mismatch")
    toc_name = "pdlc1_ch01_mng\\pdlc1_ch01_mng.toc"
    toc_hash = bigarchive.name_hash(toc_name)
    patched_toc = update_dlc_toc(read_archive_entry(SOURCE_DAT2, toc_hash), len(patched_stream))
    replacements = {
        stream_hash: patched_stream,
        toc_hash: patched_toc,
    }
    target_dat2 = OUT / "DS2DAT2.zh.DAT"
    rebuild_bigh(SOURCE_DAT2, target_dat2, replacements)
    verify_rebuilt_archive(SOURCE_DAT2, target_dat2, replacements, 422)

    dat0_replacements = {
        BASE_FONT_HASH: font_data,
        BASE_TEXT_HASH: text_data,
    }
    target_dat0 = OUT / "DS2DAT0.zh.DAT"
    rebuild_bigh(SOURCE_DAT0, target_dat0, dat0_replacements)
    verify_rebuilt_archive(SOURCE_DAT0, target_dat0, dat0_replacements, 2188)

    csv_path = OUT / "severed_zh.csv"
    with csv_path.open("w", encoding="utf-8-sig", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(["index", "string_id", "english", "chinese"])
        for record in records:
            writer.writerow([
                record["index"],
                record["string_id"],
                record["text"],
                translations[record["string_id"]],
            ])

    report = {
        "schema": 1,
        "source_dat2": str(SOURCE_DAT2),
        "source_dat2_sha256": SOURCE_DAT2_SHA256,
        "dat2_sha256": file_sha256(target_dat2),
        "dat2_bytes": target_dat2.stat().st_size,
        "dat0_sha256": file_sha256(target_dat0),
        "dat0_bytes": target_dat0.stat().st_size,
        "translated_lh2_bytes": len(translated_lh2),
        "stream_payload_bytes": len(translated_lh2),
        "translated_lh2_sha256": sha256(translated_lh2),
        "patched_stream_bytes": len(patched_stream),
        "patched_stream_sha256": sha256(patched_stream),
        "patched_toc_sha256": sha256(patched_toc),
        "base_font_bytes": len(font_data),
        "base_font_sha256": sha256(font_data),
        "base_text_bytes": len(text_data),
        "base_text_sha256": sha256(text_data),
        "dat2_entry_count": 422,
        "dat0_entry_count": 2188,
        "translation_rows": len(records),
        "reused_base_rows": sum(bool(r["recovered_base_chinese"]) for r in records),
        "machine_translated_rows": sum(not r["recovered_base_chinese"] for r in records),
        "font_name_chinese": "剧情 Swei Shear Sans CJK SC Regular；标题 MStiffHei",
        "font_name_english": "Russell Square",
        "verification": "all unchanged DAT2 entries byte-equal; base Chinese resources remain separate",
    }
    (OUT / "build-report.json").write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
