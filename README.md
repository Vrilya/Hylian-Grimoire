# Hylian Grimoire

![Hylian Grimoire running on Windows 11](media/screenshot1.png)

Hylian Grimoire is a Windows desktop editor for Ocarina of Time message data. I originally built it while working on my own translation of the game, and the project has been rewritten from the ground up several times. The current version focuses on exact binary round-tripping, readable editor syntax, decomp-friendly C header workflows, ROM workflows, and a modern Windows 11 interface.

The editor works with extracted `.bin` / `.tbl` message files, C message headers, and `.z64` ROMs. Header import and export are treated as first-class workflows, with support for modern multi-language decomp-style headers, legacy headers, and OTRMod-oriented output.

## Features

- Load and save message `.bin` / `.tbl` files.
- Load and save `.z64` ROMs, including compressed and decompressed ROM workflows.
- Import and export C message headers in Modern, Legacy, and OTRMod-oriented formats.
- Import headers into ROM message banks when the imported data fits the selected ROM section.
- Search message IDs by ID or message text.
- Add, rename, delete, and reorder message IDs.
- Edit textbox type and position metadata.
- Preview message boxes with game-style text rendering, item icons, colors, choices, highscore tokens, ocarina staff display, credits text, and multi-box messages.
- Open the preview in a separate window with zoom, column layout, alignment guides, and always-on-top support.
- Manage glyph profiles for editor display characters, glyph images, and preview glyph widths without accidentally changing the intended byte values.
- Read glyph images and widths directly from ROMs, edit them in the Glyph Manager, and write the changes back to the ROM.
- Remap glyph byte usage across the active script.
- Edit ROM title-screen text.
- Edit pause-menu prompt positions.
- Apply ROM tweaks from the Tools menu.
- Follow Windows light/dark mode through WinUI 3 and Windows App SDK.

## Message Editing

![Main editor window](media/mainwindow.png)

The main editor is built around fast navigation, readable message syntax, and exact save/export behavior. Messages can be searched by ID or text, edited directly, reordered, imported from headers, exported back to headers, or written into ROM message banks.

## Header Workflows

Hylian Grimoire treats C message headers as real project files, not disposable export output. It supports modern multi-language decomp-style headers, legacy headers, and OTRMod-oriented output, while keeping control codes, item icons, language slots, and readable formatting intact wherever possible.

Headers can be loaded directly, exported from data files or ROMs, and imported into ROM message banks. For multi-language ROMs, the editor can work with a selected language or export all ROM languages in a modern header layout.

## Message Preview

![Message preview window](media/preview.png)

The preview window renders message boxes with game-style glyphs, colors, item icons, choices, ocarina staff display, credits text, multi-box messages, zoom controls, columns, and alignment guides.

## Glyph Manager

![Glyph Manager](media/glyphmanager.png)

The Glyph Manager is designed for translation projects that need custom characters. Glyph profiles can change which character a byte displays as, replace the preview image, and adjust the preview width while keeping the underlying byte value stable. For example, a profile can make byte `0x92` appear as `å` in the editor while still saving byte `0x92` to the message data.

In ROM mode, Hylian Grimoire can read glyph images and widths directly from the loaded ROM, compare them against the expected baseline, and write image or width changes back into the ROM. This makes it possible to inspect and edit custom ROM fonts without treating them as separate loose assets.

When saving or exporting, text characters must be encodable by the active glyph profile. If a message contains an unsupported character, the save is stopped and the affected message ID is reported instead of silently writing an invalid byte.

## Title Text

![Title text editor](media/titletext.png)

The Title Text tool edits title-screen text with a live preview. It keeps the title text constraints visible so changes can be made deliberately instead of by trial and error.

## Prompt Editor

![Prompt Editor](media/prompteditor.png)

The Prompt Editor adjusts pause-menu prompt placement for the button and text elements used by the file, melody, gear, and item panels. The live preview shows the prompt graphics directly from the loaded ROM, with optional guides and frames for careful alignment.

## Tweaks

![ROM tweaks](media/tweaks.png)

The Tweaks window exposes ROM patches as simple on/off switches. Each tweak is version-aware, so unavailable options stay disabled instead of applying unsafe changes.

## Editor Syntax

Messages are shown as editable text with bracketed tags for control codes. For example:

```text
[unskippable][item:30][quicktexton]Du bytte [color:red]Kojiro[color:default]!
[break]
Denna konstiga svamp försvinner
om du inte levererar den i tid!
```

Common tags include:

- `[break]`
- `[breakdelay:xx]`
- `[color:red]`
- `[color:default]`
- `[item:30]`
- `[sfx:Laugh2]`
- `[Triangle]`
- `[twochoice]`
- `[threechoice]`

The editor syntax is intended to be readable while preserving the original encoded data as closely as possible.

## Build

Requirements:

- Windows 10 1809 or newer, Windows 11 recommended
- .NET 8 SDK

Build from the repository root:

```powershell
dotnet build .\HylianGrimoire.slnx -c Release
```

Run from source:

```powershell
dotnet run --project .\src\HylianGrimoire\HylianGrimoire.csproj -c Release
```

Run tests:

```powershell
dotnet test .\HylianGrimoire.slnx -c Release
```

## Project Structure

```text
src/HylianGrimoire/              WinUI 3 application
src/HylianGrimoire/Codecs/       Message byte encoding, decoding, token maps, and editor syntax
src/HylianGrimoire/Glyphs/       Glyph metadata, glyph profiles, and the Glyph Manager window
src/HylianGrimoire/Headers/      C header import/export
src/HylianGrimoire/Interop/      Windows window theming, icons, sizing, and native interop helpers
src/HylianGrimoire/Models/       Message entries, message tokens, and UI list models
src/HylianGrimoire/Preview/      Game-style message preview renderer and preview window
src/HylianGrimoire/PromptEditor/ Pause-menu prompt placement editing support
src/HylianGrimoire/Rom/          ROM detection, message-bank handling, glyph sessions, and ROM patch helpers
src/HylianGrimoire/Services/     File operations, searching, message list operations, and document services
src/HylianGrimoire/TitleText/    Title-screen text editing support
src/HylianGrimoire/Tweaks/       ROM tweaks
tests/HylianGrimoire.Tests/      xUnit tests for codec, import/export, preview-adjacent behavior, and file parity
```

## Notes

Hylian Grimoire is a standalone editor project. It is meant for extracted message files, ROM workflows, and project workflows where exact save/export behavior matters. Keep backups of your source `.bin`, `.tbl`, header, and ROM files when working on real translation or modding projects.
