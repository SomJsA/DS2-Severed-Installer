import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const packageRoot = path.resolve(scriptDir, "../..");
const inputPath =
  process.argv[2] ??
  path.join(packageRoot, "translation", "severed_translation_work.xlsx");
const input = await FileBlob.load(inputPath);
const workbook = await SpreadsheetFile.importXlsx(input);
const summary = await workbook.inspect({
  kind: "workbook,sheet,table",
  maxChars: 16000,
  tableMaxRows: 8,
  tableMaxCols: 12,
  tableMaxCellChars: 160,
});
console.log(summary.ndjson);
