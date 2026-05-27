import fs from "node:fs";
import path from "node:path";

const repoRoot = path.resolve(import.meta.dirname, "..");
const workspaceRoot = path.resolve(repoRoot, "..");
const shipwrightRoot = process.argv[2]
  ? path.resolve(process.argv[2])
  : path.join(workspaceRoot, "Shipwright");
const romRoot = process.argv[3]
  ? path.resolve(process.argv[3])
  : path.join(workspaceRoot, "retaildecompressed");
const outputRoot = path.join(repoRoot, "src", "HylianGrimoire", "Assets", "TextureCatalog");

const profiles = [
  {
    name: "Retail NTSC 1.0",
    file: "retail_ntsc_10.txt",
    xml: "N64_NTSC_10",
    fileList: "ntsc_oot.txt",
    rom: "ntsc10_orig.z64",
    dmaTableOffset: 0x7430,
  },
  {
    name: "Retail NTSC 1.1",
    file: "retail_ntsc_11.txt",
    xml: "N64_NTSC_11",
    fileList: "ntsc_oot.txt",
    rom: "ntsc11_orig.z64",
    dmaTableOffset: 0x7430,
  },
  {
    name: "Retail NTSC 1.2",
    file: "retail_ntsc_12.txt",
    xml: "N64_NTSC_12",
    fileList: "ntsc_12_oot.txt",
    rom: "ntsc12_orig.z64",
    dmaTableOffset: 0x7960,
  },
  {
    name: "Retail NTSC Master Quest",
    file: "retail_ntsc_mq.txt",
    xml: "GC_MQ_NTSC_U",
    fileList: "gamecube.txt",
    rom: "ntscmq_orig.z64",
    dmaTableOffset: 0x7170,
  },
  {
    name: "Retail NTSC GameCube",
    file: "retail_ntsc_gc.txt",
    xml: "GC_NMQ_NTSC_U",
    fileList: "gamecube.txt",
    rom: "ntscgc_orig.z64",
    dmaTableOffset: 0x7170,
  },
  {
    name: "Retail PAL 1.0",
    file: "retail_pal_10.txt",
    xml: "N64_PAL_10",
    fileList: "pal_oot.txt",
    rom: "pal10_orig.z64",
    dmaTableOffset: 0x7950,
  },
  {
    name: "Retail PAL 1.1",
    file: "retail_pal_11.txt",
    xml: "N64_PAL_11",
    fileList: "pal_oot.txt",
    rom: "pal11_orig.z64",
    dmaTableOffset: 0x7950,
  },
  {
    name: "Retail PAL Master Quest",
    file: "retail_pal_mq.txt",
    xml: "GC_MQ_PAL_F",
    fileList: "gamecube_pal.txt",
    rom: "palmq_orig.z64",
    dmaTableOffset: 0x7170,
  },
  {
    name: "Retail PAL GameCube",
    file: "retail_pal_gc.txt",
    xml: "GC_NMQ_PAL_F",
    fileList: "gamecube_pal.txt",
    rom: "palgc_orig.z64",
    dmaTableOffset: 0x7170,
  },
];

const formatNames = new Set(["ci4", "ci8", "i4", "i8", "ia4", "ia8", "ia16", "rgba16", "rgba32"]);

fs.mkdirSync(outputRoot, { recursive: true });

for (const profile of profiles) {
  const fileList = readFileList(profile.fileList);
  const rom = fs.readFileSync(path.join(romRoot, profile.rom));
  const dmaStarts = readDmaStarts(rom, profile.dmaTableOffset, fileList.length);
  const fileBases = new Map(fileList.map((name, index) => [name, dmaStarts[index]]));
  const xmlRoot = path.join(shipwrightRoot, "soh", "assets", "xml", profile.xml);
  const records = [];

  for (const xmlPath of walkXml(xmlRoot)) {
    const relative = path.relative(xmlRoot, xmlPath).replaceAll("\\", "/");
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
      if (!fileName || !fileBases.has(fileName)) {
        continue;
      }

      const fileBase = fileBases.get(fileName);
      const group = `${groupPrefix}/${fileName}`;
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

        const textureAddress = fileBase + parseHex(attrs.Offset);
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
          const tlut = resolveTlut(attrs, fileName, fileBase, fileBases, tlutCountsByFileAndOffset, format);
          if (!tlut) {
            continue;
          }

          const minColorCount = getMinimumPaletteColorCount(rom, textureAddress, Number(attrs.Width), Number(attrs.Height), format);
          fields.push(tlut.address.toString(16).toUpperCase(), String(Math.max(tlut.colorCount, minColorCount)));
        }

        records.push(fields.join("|"));
      }
    }
  }

  records.sort((a, b) => a.localeCompare(b, "en", { sensitivity: "base" }));
  fs.writeFileSync(path.join(outputRoot, profile.file), `${records.join("\n")}\n`, "utf8");
  console.log(`${profile.name}: ${records.length} textures`);
}

function readFileList(fileName) {
  const filePath = path.join(shipwrightRoot, "soh", "assets", "extractor", "filelists", fileName);
  return fs.readFileSync(filePath, "utf8")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);
}

function readDmaStarts(rom, offset, count) {
  const starts = [];
  for (let i = 0; i < count; i++) {
    starts.push(readU32(rom, offset + i * 16));
  }

  return starts;
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

function resolveTlut(attrs, fileName, fileBase, fileBases, tlutCountsByFileAndOffset, format) {
  const fallbackColorCount = format === "ci4" ? 16 : 256;

  if (attrs.ExternalTlut) {
    const externalBase = fileBases.get(attrs.ExternalTlut);
    if (externalBase === undefined || !attrs.ExternalTlutOffset) {
      return null;
    }

    const externalOffset = parseHex(attrs.ExternalTlutOffset);
    return {
      address: externalBase + externalOffset,
      colorCount: tlutCountsByFileAndOffset.get(`${attrs.ExternalTlut}|${externalOffset}`) ?? fallbackColorCount,
    };
  }

  if (!attrs.TlutOffset) {
    return null;
  }

  const tlutOffset = parseHex(attrs.TlutOffset);
  return {
    address: fileBase + tlutOffset,
    colorCount: tlutCountsByFileAndOffset.get(`${fileName}|${tlutOffset}`) ?? fallbackColorCount,
  };
}
