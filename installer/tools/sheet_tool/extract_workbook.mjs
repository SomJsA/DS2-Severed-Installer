import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const packageRoot = path.resolve(scriptDir, "../..");
const inputPath =
  process.argv[2] ??
  path.join(packageRoot, "translation", "severed_translation_work.xlsx");
const outputPath =
  process.argv[3] ??
  path.join(packageRoot, "translation", "workbook_translations.json");

const input = await FileBlob.load(inputPath);
const workbook = await SpreadsheetFile.importXlsx(input);
const sheet = workbook.worksheets.getItem("severed_translation_work");
const values = sheet.getRange("A1:E219").values;
const headers = values[0].map((value) => String(value ?? "").trim());
const expected = ["index", "string_id", "translation_status", "english", "chinese"];
if (JSON.stringify(headers) !== JSON.stringify(expected)) {
  throw new Error(`Unexpected headers: ${JSON.stringify(headers)}`);
}

const rows = values.slice(1).map((row) => ({
  index: Number(row[0]),
  string_id: String(row[1] ?? "").trim(),
  translation_status: String(row[2] ?? "").trim(),
  english: String(row[3] ?? ""),
  chinese: String(row[4] ?? ""),
}));

if (rows.length !== 218) {
  throw new Error(`Expected 218 rows, found ${rows.length}`);
}
if (rows.some((row) => !row.string_id || !row.chinese.trim())) {
  throw new Error("Every row must have string_id and chinese text");
}
if (new Set(rows.map((row) => row.string_id)).size !== rows.length) {
  throw new Error("Duplicate string_id values");
}

await fs.writeFile(
  outputPath,
  JSON.stringify(
    {
      source: inputPath,
      sheet: "severed_translation_work",
      range: "A2:E219",
      rows,
    },
    null,
    2
  ) + "\n",
  "utf8"
);
console.log(
  JSON.stringify({
    output: outputPath,
    rows: rows.length,
    emptyTranslations: rows.filter((row) => !row.chinese.trim()).length,
  })
);
