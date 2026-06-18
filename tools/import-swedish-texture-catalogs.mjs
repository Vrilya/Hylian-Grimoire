import fs from "node:fs";
import path from "node:path";

const repoRoot = path.resolve(import.meta.dirname, "..");
const defaultSourceRoot = path.resolve(repoRoot, "..", "oot-workflow", "extrsettings");
const outputRoot = path.join(repoRoot, "src", "HylianGrimoire", "Assets", "Games", "Oot", "TextureCatalog");
const supportedFormats = new Set(["I4", "I8", "IA4", "IA8", "IA16", "RGBA16", "RGBA32"]);

const profiles = [
  ["NTSC SWE v1.0.toml", "swedish_ntsc_10.txt"],
  ["NTSC SWE v1.1.toml", "swedish_ntsc_11.txt"],
  ["NTSC SWE v1.2.toml", "swedish_ntsc_12.txt"],
  ["NTSC SWE MQ.toml", "swedish_ntsc_mq.txt"],
  ["NTSC SWE GC.toml", "swedish_ntsc_gc.txt"],
  ["PAL SWE v1.0.toml", "swedish_pal_10.txt"],
  ["PAL SWE v1.1.toml", "swedish_pal_11.txt"],
  ["PAL SWE MQ.toml", "swedish_pal_mq.txt"],
  ["PAL SWE GC.toml", "swedish_pal_gc.txt"],
  ["iQue SWE.toml", "swedish_ique.txt"],
  ["iQueMQ SWE.toml", "swedish_ique_mq.txt"],
];

const options = parseArgs(process.argv.slice(2));
fs.mkdirSync(outputRoot, { recursive: true });

for (const [sourceFile, outputFile] of profiles) {
  const sourcePath = path.join(options.sourceRoot, sourceFile);
  const outputPath = path.join(outputRoot, outputFile);
  const records = readTomlCatalog(sourcePath);
  fs.writeFileSync(outputPath, `${records.join("\r\n")}\r\n`, "utf8");
  console.log(`${outputFile}: ${records.length} textures`);
}

function parseArgs(args) {
  const parsed = {
    sourceRoot: defaultSourceRoot,
  };

  for (let i = 0; i < args.length; i++) {
    const arg = args[i];
    switch (arg) {
      case "--source-root":
        parsed.sourceRoot = path.resolve(readOptionValue(args, ++i, arg));
        break;
      default:
        throw new Error(`Unknown option: ${arg}`);
    }
  }

  return parsed;
}

function readOptionValue(args, index, optionName) {
  const value = args[index];
  if (!value || value.startsWith("--")) {
    throw new Error(`${optionName} requires a path.`);
  }

  return value;
}

function readTomlCatalog(filePath) {
  if (!fs.existsSync(filePath)) {
    throw new Error(`Missing TOML catalog: ${filePath}`);
  }

  const groups = [];
  let currentGroup = null;
  let currentTexture = null;
  const lines = fs.readFileSync(filePath, "utf8").split(/\r?\n/);

  for (let i = 0; i < lines.length; i++) {
    const lineNumber = i + 1;
    const line = lines[i].trim();
    if (line.length === 0 || line.startsWith("#")) {
      continue;
    }

    if (line === "[[group]]") {
      currentGroup = { path: null, textures: [] };
      currentTexture = null;
      groups.push(currentGroup);
      continue;
    }

    if (line === "[[group.texture]]") {
      if (currentGroup === null) {
        throw new Error(`${filePath}:${lineNumber}: texture declared before group.`);
      }

      currentTexture = {};
      currentGroup.textures.push(currentTexture);
      continue;
    }

    const match = line.match(/^([A-Za-z0-9_]+)\s*=\s*(.+)$/);
    if (!match) {
      throw new Error(`${filePath}:${lineNumber}: unsupported TOML line.`);
    }

    const [, key, value] = match;
    if (key === "path") {
      if (currentGroup === null || currentTexture !== null) {
        throw new Error(`${filePath}:${lineNumber}: path must belong to a group.`);
      }

      currentGroup.path = readString(value, filePath, lineNumber).replaceAll("\\", "/");
      continue;
    }

    if (currentTexture === null) {
      throw new Error(`${filePath}:${lineNumber}: texture field declared before texture.`);
    }

    switch (key) {
      case "name":
        currentTexture.name = readString(value, filePath, lineNumber);
        break;
      case "offset":
        currentTexture.offset = readHexString(value, filePath, lineNumber);
        break;
      case "format":
        currentTexture.format = readString(value, filePath, lineNumber).toUpperCase();
        if (!supportedFormats.has(currentTexture.format)) {
          throw new Error(`${filePath}:${lineNumber}: unsupported texture format ${currentTexture.format}.`);
        }
        break;
      case "size":
        currentTexture.size = readSize(value, filePath, lineNumber);
        break;
      default:
        throw new Error(`${filePath}:${lineNumber}: unsupported texture key ${key}.`);
    }
  }

  const records = [];
  for (const group of groups) {
    if (!group.path) {
      throw new Error(`${filePath}: group is missing path.`);
    }

    for (const texture of group.textures) {
      validateTexture(texture, filePath);
      records.push(`${group.path}|${texture.name}|${texture.name}|${texture.offset}|${texture.format}|${texture.size[0]}|${texture.size[1]}`);
    }
  }

  return records;
}

function readString(value, filePath, lineNumber) {
  const match = value.match(/^"([^"]*)"$/);
  if (!match) {
    throw new Error(`${filePath}:${lineNumber}: expected quoted string.`);
  }

  return match[1];
}

function readHexString(value, filePath, lineNumber) {
  const text = readString(value, filePath, lineNumber).toUpperCase();
  if (!/^[0-9A-F]+$/.test(text)) {
    throw new Error(`${filePath}:${lineNumber}: expected hexadecimal offset.`);
  }

  return text;
}

function readSize(value, filePath, lineNumber) {
  const match = value.match(/^\[(\d+),\s*(\d+)\]$/);
  if (!match) {
    throw new Error(`${filePath}:${lineNumber}: expected size [width, height].`);
  }

  return [Number.parseInt(match[1], 10), Number.parseInt(match[2], 10)];
}

function validateTexture(texture, filePath) {
  for (const key of ["name", "offset", "format", "size"]) {
    if (texture[key] === undefined) {
      throw new Error(`${filePath}: texture is missing ${key}.`);
    }
  }
}
