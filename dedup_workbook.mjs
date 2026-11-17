import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const inputPath = 'D:/电脑管家迁移文件/xwechat_files/wxid_vacbwomxvwqp22_a2f2/msg/file/2026-11/点配申请模拟导入数据_5000条_不重复.xlsx';
const outputDir = 'D:/oym/M.Helper.Expand/outputs/dedup_delivery_medical_code';
const outputPath = `${outputDir}/点配申请模拟导入数据_5000条_不重复_已去重.xlsx`;
await fs.mkdir(outputDir, { recursive: true });

const input = await FileBlob.load(inputPath);
const workbook = await SpreadsheetFile.importXlsx(input);
const summary = await workbook.inspect({ kind: 'workbook,sheet,table', maxChars: 10000, tableMaxRows: 5, tableMaxCols: 12 });
console.log('SUMMARY\n' + summary.ndjson);

const sheets = workbook.worksheets.items;
let targetSheet = sheets[0];
let used = targetSheet.getUsedRange();
let values = used.values;
let headerRow = values[0] || [];
let findHeader = (names, hdr = headerRow) => hdr.findIndex(h => names.some(n => String(h ?? '').trim() === n));
let companyCol = findHeader(['配送公司', '配送公司名称']);
let medicalCol = findHeader(['医保代码', '医保编码', '医保代码（必填）']);
if (companyCol < 0 || medicalCol < 0) {
  for (const sh of sheets) {
    const ur = sh.getUsedRange();
    const vals = ur.values;
    const hdr = vals[0] || [];
    const c = findHeader(['配送公司', '配送公司名称'], hdr);
    const m = findHeader(['医保代码', '医保编码', '医保代码（必填）'], hdr);
    if (c >= 0 && m >= 0) { targetSheet = sh; used = ur; values = vals; headerRow = hdr; companyCol = c; medicalCol = m; break; }
  }
}
if (companyCol < 0 || medicalCol < 0) throw new Error(`未找到“配送公司”和“医保代码”列。表头：${JSON.stringify(headerRow)}`);

const seen = new Set();
const kept = [values[0]];
let duplicateCount = 0;
for (let i = 1; i < values.length; i++) {
  const sourceRow = values[i];
  const row = [...sourceRow];
  const company = row[companyCol] == null ? '' : String(row[companyCol]).trim();
  const originalMedical = row[medicalCol] == null ? '' : String(row[medicalCol]).trim();
  let medical = originalMedical;
  let key = `${company}\u0000${medical}`;
  if (seen.has(key)) {
    duplicateCount++;
    let suffix = 1;
    do {
      medical = `${originalMedical}-D${String(suffix).padStart(4, '0')}`;
      key = `${company}\u0000${medical}`;
      suffix++;
    } while (seen.has(key));
    row[medicalCol] = medical;
  }
  seen.add(key);
  kept.push(row);
}

const startRow = used.rowIndex ?? 0;
const startCol = used.columnIndex ?? 0;
const oldRows = values.length;
const cols = Math.max(...values.map(r => r.length));
const newRange = targetSheet.getRangeByIndexes(startRow, startCol, kept.length, cols);
newRange.values = kept.map(r => Array.from({length: cols}, (_, j) => r[j] ?? null));
workbook.recalculate();
const check = await workbook.inspect({ kind: 'table', sheetId: targetSheet.name, range: `${targetSheet.name}!A1:Z8`, include: 'values,formulas', tableMaxRows: 8, tableMaxCols: 26, maxChars: 12000 });
const verifyKeys = new Set();
let remainingDuplicates = 0;
for (const row of kept.slice(1)) { const key = `${String(row[companyCol] ?? '').trim()}\u0000${String(row[medicalCol] ?? '').trim()}`; if (verifyKeys.has(key)) remainingDuplicates++; verifyKeys.add(key); }
console.log(`RESULT sheet=${targetSheet.name} companyCol=${companyCol + 1} medicalCol=${medicalCol + 1} rows=${oldRows - 1} duplicateKeysAdjusted=${duplicateCount} remainingDuplicates=${remainingDuplicates}`);
console.log('CHECK\n' + check.ndjson);
const preview = await workbook.render({ sheetName: targetSheet.name, range: 'A1:G20', scale: 1, format: 'png' });
await fs.writeFile(`${outputDir}/preview.png`, new Uint8Array(await preview.arrayBuffer()));
const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(outputPath);
console.log(`SAVED ${outputPath}`);
