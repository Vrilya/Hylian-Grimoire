import fs from "node:fs";
import path from "node:path";

const repoRoot = path.resolve(import.meta.dirname, "..");
const workspaceRoot = path.resolve(repoRoot, "..");

const defaultOptions = {
  shipwrightRoot: path.join(workspaceRoot, "Shipwright"),
  twoShipRoot: path.join(workspaceRoot, "2ship2harkinian"),
  ootRomRoot: path.join(repoRoot, ".local", "rom-fixtures", "oot"),
  mmRomRoot: path.join(repoRoot, ".local", "rom-fixtures", "mm"),
};

const formatNames = new Set(["ci4", "ci8", "i4", "i8", "ia4", "ia8", "ia16", "rgba16", "rgba32"]);

const options = parseArgs(process.argv.slice(2));
const profiles = createProfiles(options);

if (options.list) {
  for (const profile of profiles) {
    console.log(`${profile.id}: ${profile.name}`);
  }

  process.exit(0);
}

const selectedProfiles = selectProfiles(profiles, options.profileIds);
for (const profile of selectedProfiles) {
  generateProfile(profile);
}

function parseArgs(args) {
  const parsed = {
    ...defaultOptions,
    profileIds: [],
    list: false,
  };

  for (let i = 0; i < args.length; i++) {
    const arg = args[i];
    switch (arg) {
      case "--list":
        parsed.list = true;
        break;
      case "--shipwright-root":
        parsed.shipwrightRoot = path.resolve(readOptionValue(args, ++i, arg));
        break;
      case "--two-ship-root":
        parsed.twoShipRoot = path.resolve(readOptionValue(args, ++i, arg));
        break;
      case "--oot-rom-root":
        parsed.ootRomRoot = path.resolve(readOptionValue(args, ++i, arg));
        break;
      case "--mm-rom-root":
        parsed.mmRomRoot = path.resolve(readOptionValue(args, ++i, arg));
        break;
      default:
        if (arg.startsWith("--")) {
          throw new Error(`Unknown option: ${arg}`);
        }

        parsed.profileIds.push(arg);
        break;
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

function createProfiles(parsedOptions) {
  const ootXmlRoot = path.join(parsedOptions.shipwrightRoot, "soh", "assets", "xml");
  const ootFileListRoot = path.join(parsedOptions.shipwrightRoot, "soh", "assets", "extractor", "filelists");
  const ootOutputRoot = path.join(repoRoot, "src", "HylianGrimoire", "Assets", "Games", "Oot", "TextureCatalog");
  const mmXmlRoot = path.join(parsedOptions.twoShipRoot, "mm", "assets", "xml");
  const mmFileListRoot = path.join(parsedOptions.twoShipRoot, "mm", "assets", "extractor", "filelists");
  const mmOutputRoot = path.join(repoRoot, "src", "HylianGrimoire", "Assets", "Games", "MM", "TextureCatalog");

  return [
    {
      id: "oot_retail_ntsc_10",
      name: "Retail NTSC 1.0",
      file: "retail_ntsc_10.txt",
      xmlRoot: path.join(ootXmlRoot, "N64_NTSC_10"),
      fileListPath: path.join(ootFileListRoot, "ntsc_oot.txt"),
      romPath: path.join(parsedOptions.ootRomRoot, "oot_retail_ntsc_1.0_decompressed.z64"),
      dmaTableOffset: 0x7430,
      outputRoot: ootOutputRoot,
    },
    {
      id: "oot_retail_ntsc_11",
      name: "Retail NTSC 1.1",
      file: "retail_ntsc_11.txt",
      xmlRoot: path.join(ootXmlRoot, "N64_NTSC_11"),
      fileListPath: path.join(ootFileListRoot, "ntsc_oot.txt"),
      romPath: path.join(parsedOptions.ootRomRoot, "oot_retail_ntsc_1.1_decompressed.z64"),
      dmaTableOffset: 0x7430,
      outputRoot: ootOutputRoot,
    },
    {
      id: "oot_retail_ntsc_12",
      name: "Retail NTSC 1.2",
      file: "retail_ntsc_12.txt",
      xmlRoot: path.join(ootXmlRoot, "N64_NTSC_12"),
      fileListPath: path.join(ootFileListRoot, "ntsc_12_oot.txt"),
      romPath: path.join(parsedOptions.ootRomRoot, "oot_retail_ntsc_1.2_decompressed.z64"),
      dmaTableOffset: 0x7960,
      outputRoot: ootOutputRoot,
    },
    {
      id: "oot_retail_ntsc_mq",
      name: "Retail NTSC Master Quest",
      file: "retail_ntsc_mq.txt",
      xmlRoot: path.join(ootXmlRoot, "GC_MQ_NTSC_U"),
      fileListPath: path.join(ootFileListRoot, "gamecube.txt"),
      romPath: path.join(parsedOptions.ootRomRoot, "oot_retail_ntsc_mq_decompressed.z64"),
      dmaTableOffset: 0x7170,
      outputRoot: ootOutputRoot,
    },
    {
      id: "oot_retail_ntsc_gc",
      name: "Retail NTSC GameCube",
      file: "retail_ntsc_gc.txt",
      xmlRoot: path.join(ootXmlRoot, "GC_NMQ_NTSC_U"),
      fileListPath: path.join(ootFileListRoot, "gamecube.txt"),
      romPath: path.join(parsedOptions.ootRomRoot, "oot_retail_ntsc_gc_decompressed.z64"),
      dmaTableOffset: 0x7170,
      outputRoot: ootOutputRoot,
    },
    {
      id: "oot_retail_pal_10",
      name: "Retail PAL 1.0",
      file: "retail_pal_10.txt",
      xmlRoot: path.join(ootXmlRoot, "N64_PAL_10"),
      fileListPath: path.join(ootFileListRoot, "pal_oot.txt"),
      romPath: path.join(parsedOptions.ootRomRoot, "oot_retail_pal_1.0_decompressed.z64"),
      dmaTableOffset: 0x7950,
      outputRoot: ootOutputRoot,
    },
    {
      id: "oot_retail_pal_11",
      name: "Retail PAL 1.1",
      file: "retail_pal_11.txt",
      xmlRoot: path.join(ootXmlRoot, "N64_PAL_11"),
      fileListPath: path.join(ootFileListRoot, "pal_oot.txt"),
      romPath: path.join(parsedOptions.ootRomRoot, "oot_retail_pal_1.1_decompressed.z64"),
      dmaTableOffset: 0x7950,
      outputRoot: ootOutputRoot,
    },
    {
      id: "oot_retail_pal_mq",
      name: "Retail PAL Master Quest",
      file: "retail_pal_mq.txt",
      xmlRoot: path.join(ootXmlRoot, "GC_MQ_PAL_F"),
      fileListPath: path.join(ootFileListRoot, "gamecube_pal.txt"),
      romPath: path.join(parsedOptions.ootRomRoot, "oot_retail_pal_mq_decompressed.z64"),
      dmaTableOffset: 0x7170,
      outputRoot: ootOutputRoot,
    },
    {
      id: "oot_retail_pal_gc",
      name: "Retail PAL GameCube",
      file: "retail_pal_gc.txt",
      xmlRoot: path.join(ootXmlRoot, "GC_NMQ_PAL_F"),
      fileListPath: path.join(ootFileListRoot, "gamecube_pal.txt"),
      romPath: path.join(parsedOptions.ootRomRoot, "oot_retail_pal_gc_decompressed.z64"),
      dmaTableOffset: 0x7170,
      outputRoot: ootOutputRoot,
    },
    {
      id: "mm_us_n64",
      name: "Majora's Mask NTSC-U",
      file: "mm_us_n64.txt",
      xmlRoot: path.join(mmXmlRoot, "N64_US"),
      fileListPath: path.join(mmFileListRoot, "mm.txt"),
      romPath: path.join(parsedOptions.mmRomRoot, "mm_us_n64_decompressed.z64"),
      dmaTableOffset: 0x1a500,
      outputRoot: mmOutputRoot,
    },
  ];
}

function selectProfiles(availableProfiles, filters) {
  if (filters.length === 0) {
    return availableProfiles;
  }

  const selected = [];
  const unknown = [];

  for (const filter of filters) {
    const profile = availableProfiles.find((candidate) =>
      candidate.id === filter || candidate.name.toLowerCase() === filter.toLowerCase());

    if (profile) {
      selected.push(profile);
    } else {
      unknown.push(filter);
    }
  }

  if (unknown.length !== 0) {
    throw new Error(`Unknown texture catalog profile: ${unknown.join(", ")}`);
  }

  return selected;
}

function generateProfile(profile) {
  assertDirectory(profile.xmlRoot, `${profile.name} XML root`);
  assertFile(profile.fileListPath, `${profile.name} file list`);
  assertFile(profile.romPath, `${profile.name} decompressed ROM`);
  fs.mkdirSync(profile.outputRoot, { recursive: true });

  const fileList = readFileList(profile.fileListPath);
  const rom = fs.readFileSync(profile.romPath);
  const dmaEntries = readDmaEntries(rom, profile.dmaTableOffset, fileList.length);
  const files = new Map(fileList.map((name, index) => {
    const entry = dmaEntries[index];
    const archive = isCmpDmaArchive(rom, entry.start, entry.end)
      ? decodeCmpDmaArchive(rom, entry.start, entry.end)
      : null;

    return [name, { ...entry, archive }];
  }));
  const records = [];

  for (const xmlPath of walkXml(profile.xmlRoot)) {
    const relative = path.relative(profile.xmlRoot, xmlPath).replaceAll("\\", "/");
    const groupPrefix = path.posix.dirname(relative);
    const xml = stripComments(fs.readFileSync(xmlPath, "utf8"));
    const fileBlocks = [...xml.matchAll(/<File\b(?<attrs>[^>]*)>(?<body>[\s\S]*?)<\/File>/gi)];
    const parsedFileBlocks = fileBlocks.map((block) => ({
      attrs: readAttrs(block.groups.attrs),
      body: block.groups.body,
    }));
    const tlutCountsByFileAndOffset = buildTlutCounts(parsedFileBlocks);

    for (const block of parsedFileBlocks) {
      const fileAttrs = block.attrs;
      const fileName = fileAttrs.Name;
      if (!fileName || !files.has(fileName)) {
        continue;
      }

      const file = files.get(fileName);
      const group = getTextureGroup(groupPrefix, fileName);
      const textureNodes = [...block.body.matchAll(/<Texture\b(?<attrs>[^/>]*?)\/>/gi)];

      for (const node of textureNodes) {
        const attrs = readAttrs(node.groups.attrs);
        const format = attrs.Format?.toLowerCase();
        if (!format || !formatNames.has(format)) {
          continue;
        }

        if (!attrs.Name || !attrs.OutName || !attrs.Offset || !attrs.Width || !attrs.Height) {
          continue;
        }

        const width = Number(attrs.Width);
        const height = Number(attrs.Height);
        const textureLocalOffset = parseHex(attrs.Offset);
        const textureAddress = file.archive
          ? textureLocalOffset
          : file.start + textureLocalOffset;
        const textureSource = file.archive ?? rom;
        validateRange(
          textureSource.length,
          textureAddress,
          getTextureByteLength(width, height, format),
          profile,
          attrs.Name,
          file.archive ? "decoded CmpDma archive" : "ROM");

        const fields = [
          group,
          attrs.Name,
          attrs.OutName,
          textureAddress.toString(16).toUpperCase(),
          toCatalogFormat(format),
          attrs.Width,
          attrs.Height,
        ];

        if (format === "ci4" || format === "ci8") {
          const tlut = resolveTlut(attrs, fileName, file, files, tlutCountsByFileAndOffset, format, rom);
          if (!tlut) {
            continue;
          }

          validateRange(
            tlut.source.length,
            tlut.address,
            tlut.colorCount * 2,
            profile,
            `${attrs.Name} TLUT`,
            tlut.sourceKind);
          const minColorCount = getMinimumPaletteColorCount(textureSource, textureAddress, width, height, format);
          fields.push(tlut.address.toString(16).toUpperCase(), String(Math.max(tlut.colorCount, minColorCount)));
        }

        if (file.archive) {
          fields.push("CmpDmaArchive", file.start.toString(16).toUpperCase(), (file.end - file.start).toString(16).toUpperCase());
        }

        records.push(fields.join("|"));
      }
    }
  }

  records.sort((a, b) => a.localeCompare(b, "en", { sensitivity: "base" }));
  fs.writeFileSync(path.join(profile.outputRoot, profile.file), `${records.join("\n")}\n`, "utf8");
  console.log(`${profile.name}: ${records.length} textures`);
}

function assertDirectory(directoryPath, label) {
  if (!fs.existsSync(directoryPath) || !fs.statSync(directoryPath).isDirectory()) {
    throw new Error(`${label} not found: ${directoryPath}`);
  }
}

function assertFile(filePath, label) {
  if (!fs.existsSync(filePath) || !fs.statSync(filePath).isFile()) {
    throw new Error(`${label} not found: ${filePath}`);
  }
}

function readFileList(filePath) {
  return fs.readFileSync(filePath, "utf8")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);
}

function readDmaEntries(rom, offset, count) {
  const entries = [];
  for (let i = 0; i < count; i++) {
    entries.push({
      start: readU32(rom, offset + i * 16),
      end: readU32(rom, offset + i * 16 + 4),
    });
  }

  return entries;
}

function getMinimumPaletteColorCount(rom, address, width, height, format) {
  const length = format === "ci4"
    ? Math.ceil(width * height / 2)
    : width * height;
  let maxIndex = 0;

  for (let i = 0; i < length; i++) {
    const value = rom[address + i];
    if (format === "ci4") {
      maxIndex = Math.max(maxIndex, value >> 4, value & 0x0f);
    } else {
      maxIndex = Math.max(maxIndex, value);
    }
  }

  return maxIndex + 1;
}

function getTextureByteLength(width, height, format) {
  const pixels = width * height;
  switch (format) {
    case "ci4":
    case "i4":
    case "ia4":
      return Math.ceil(pixels / 2);
    case "ci8":
    case "i8":
    case "ia8":
      return pixels;
    case "ia16":
    case "rgba16":
      return pixels * 2;
    case "rgba32":
      return pixels * 4;
    default:
      throw new Error(`Unsupported texture format: ${format}`);
  }
}

function validateRange(sourceLength, address, length, profile, name, sourceKind) {
  if (address < 0 || length < 0 || address + length > sourceLength) {
    throw new Error(`${profile.name} ${name} points outside the ${sourceKind}: 0x${address.toString(16).toUpperCase()} (${length} bytes)`);
  }
}

function isCmpDmaArchive(rom, start, end) {
  if (start < 0 || end <= start || end > rom.length || end - start < 8) {
    return false;
  }

  const dataStart = readU32(rom, start);
  const length = end - start;
  if (dataStart < 8 || (dataStart & 3) !== 0 || dataStart > length) {
    return false;
  }

  const fileCount = dataStart / 4 - 1;
  if (fileCount <= 0) {
    return false;
  }

  let previous = 0;
  for (let i = 0; i < fileCount; i++) {
    const next = readU32(rom, start + (i + 1) * 4);
    if (next < previous || dataStart + next > length) {
      return false;
    }

    const compressedLength = next - previous;
    if (compressedLength > 0 && !isYaz0(rom, start + dataStart + previous)) {
      return false;
    }

    previous = next;
  }

  return true;
}

function decodeCmpDmaArchive(rom, start, end) {
  const dataStart = readU32(rom, start);
  const fileCount = dataStart / 4 - 1;
  const files = [];
  let previous = 0;

  for (let i = 0; i < fileCount; i++) {
    const next = readU32(rom, start + (i + 1) * 4);
    const compressedLength = next - previous;
    if (compressedLength > 0) {
      files.push(decodeYaz0(rom.subarray(start + dataStart + previous, start + dataStart + next)));
    }

    previous = next;
  }

  return Buffer.concat(files);
}

function isYaz0(buffer, offset) {
  return offset >= 0
    && offset <= buffer.length - 16
    && buffer[offset] === 0x59
    && buffer[offset + 1] === 0x61
    && buffer[offset + 2] === 0x7a
    && buffer[offset + 3] === 0x30;
}

function decodeYaz0(source) {
  if (!isYaz0(source, 0)) {
    throw new Error("Invalid Yaz0 data.");
  }

  const output = Buffer.alloc(readU32(source, 4));
  let sourceIndex = 16;
  let destIndex = 0;
  let validBitCount = 0;
  let codeByte = 0;

  while (destIndex < output.length) {
    if (validBitCount === 0) {
      if (sourceIndex >= source.length) {
        throw new Error("Unexpected end of Yaz0 stream.");
      }

      codeByte = source[sourceIndex++];
      validBitCount = 8;
    }

    if ((codeByte & 0x80) !== 0) {
      output[destIndex++] = source[sourceIndex++];
    } else {
      const byte1 = source[sourceIndex++];
      const byte2 = source[sourceIndex++];
      const distance = ((byte1 & 0x0f) << 8) | byte2;
      let copySource = destIndex - (distance + 1);
      if (copySource < 0) {
        throw new Error("Invalid Yaz0 back-reference.");
      }

      let count = byte1 >> 4;
      if (count === 0) {
        count = source[sourceIndex++] + 0x12;
      } else {
        count += 2;
      }

      for (let i = 0; i < count && destIndex < output.length; i++) {
        output[destIndex++] = output[copySource++];
      }
    }

    validBitCount--;
    codeByte <<= 1;
  }

  return output;
}

function readU32(buffer, offset) {
  return (
    (buffer[offset] << 24)
    | (buffer[offset + 1] << 16)
    | (buffer[offset + 2] << 8)
    | buffer[offset + 3]
  ) >>> 0;
}

function* walkXml(root) {
  for (const entry of fs.readdirSync(root, { withFileTypes: true })) {
    const fullPath = path.join(root, entry.name);
    if (entry.isDirectory()) {
      yield* walkXml(fullPath);
    } else if (entry.isFile() && entry.name.endsWith(".xml")) {
      yield fullPath;
    }
  }
}

function stripComments(text) {
  return text.replace(/<!--[\s\S]*?-->/g, "");
}

function readAttrs(text) {
  const attrs = {};
  for (const match of text.matchAll(/([A-Za-z0-9_]+)="([^"]*)"/g)) {
    attrs[match[1]] = match[2];
  }

  return attrs;
}

function parseHex(value) {
  return Number.parseInt(value.replace(/^0x/i, ""), 16);
}

function getTextureGroup(groupPrefix, fileName) {
  if (groupPrefix === ".") {
    return fileName;
  }

  const parts = groupPrefix.split("/").filter(Boolean);
  if (parts.at(-1)?.toLowerCase() === fileName.toLowerCase()) {
    return groupPrefix;
  }

  return `${groupPrefix}/${fileName}`;
}

function toCatalogFormat(format) {
  return format === "rgba16"
    ? "RGBA16"
    : format === "rgba32"
      ? "RGBA32"
      : format.toUpperCase();
}

function buildTlutCounts(fileBlocks) {
  const counts = new Map();

  for (const block of fileBlocks) {
    const fileName = block.attrs.Name;
    if (!fileName) {
      continue;
    }

    for (const node of block.body.matchAll(/<Texture\b(?<attrs>[^/>]*?)\/>/gi)) {
      const attrs = readAttrs(node.groups.attrs);
      if (attrs.Format?.toLowerCase() !== "rgba16" || !attrs.Offset || !attrs.Width || !attrs.Height) {
        continue;
      }

      counts.set(`${fileName}|${parseHex(attrs.Offset)}`, Number(attrs.Width) * Number(attrs.Height));
    }
  }

  return counts;
}

function resolveTlut(attrs, fileName, file, files, tlutCountsByFileAndOffset, format, rom) {
  const fallbackColorCount = format === "ci4" ? 16 : 256;

  if (attrs.ExternalTlut) {
    const externalFile = files.get(attrs.ExternalTlut);
    if (!externalFile || !attrs.ExternalTlutOffset) {
      return null;
    }

    const externalOffset = parseHex(attrs.ExternalTlutOffset);
    return {
      address: externalFile.archive ? externalOffset : externalFile.start + externalOffset,
      colorCount: tlutCountsByFileAndOffset.get(`${attrs.ExternalTlut}|${externalOffset}`) ?? fallbackColorCount,
      source: externalFile.archive ?? rom,
      sourceKind: externalFile.archive ? "decoded CmpDma archive" : "ROM",
    };
  }

  if (!attrs.TlutOffset) {
    return null;
  }

  const tlutOffset = parseHex(attrs.TlutOffset);
  return {
    address: file.archive ? tlutOffset : file.start + tlutOffset,
    colorCount: tlutCountsByFileAndOffset.get(`${fileName}|${tlutOffset}`) ?? fallbackColorCount,
    source: file.archive ?? rom,
    sourceKind: file.archive ? "decoded CmpDma archive" : "ROM",
  };
}
