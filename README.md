# Hylian Grimoire

![Hylian Grimoire running on Windows 11](media/screenshot1.png)

Hylian Grimoire is a Windows desktop editor for Ocarina of Time message data. I originally built it while working on my own translation of the game, and the project has been rewritten from the ground up several times. The current version focuses on exact binary round-tripping, readable editor syntax, ROM workflows, and a modern Windows 11 interface.

The editor works with extracted `.bin` / `.tbl` message files, C message headers, and supported `.z64` ROMs.

## Features

- Load and save message `.bin` / `.tbl` files.
- Load and save supported `.z64` ROMs, including compressed and decompressed ROM workflows.
- Import and export C message headers in Modern, Legacy, and OTRMod-oriented formats.
- Import headers into ROM message banks when the imported data fits the selected ROM section.
- Search message IDs by ID or message text.
- Add, rename, delete, and reorder message IDs.
- Edit textbox type and position metadata.
- Preview message boxes with game-style text rendering, item icons, colors, choices, highscore tokens, ocarina staff display, credits text, and multi-box messages.
- Open the preview in a separate window with zoom, column layout, alignment guides, and always-on-top support.
- Manage glyph profiles for editor display characters, glyph images, and preview glyph widths without accidentally changing the intended byte values.
- Remap glyph byte usage across the active script.
- Edit ROM title-screen text for supported retail ROMs.
- Apply supported ROM tweaks from the Tools menu.
- Follow Windows light/dark mode through WinUI 3 and Windows App SDK.

## Message Editing

![Main editor window](media/mainwindow.png)

The main editor is built around fast navigation, readable message syntax, and exact save/export behavior. Messages can be searched by ID or text, edited directly, reordered, imported from headers, exported back to headers, or written into supported ROM message banks.

## Glyph Manager

![Glyph Manager](media/glyphmanager.png)

The Glyph Manager is designed for translation projects that need custom characters. A glyph profile can change which character a byte displays as, replace the preview image, and adjust the glyph width while keeping the underlying byte value stable. This makes it possible to work with language-specific characters without losing control over the actual encoded data.

## Title Text

![Title text editor](media/titletext.png)

Supported retail ROMs can edit title-screen text with a live preview. The tool keeps the title text constraints visible so changes can be made deliberately instead of by trial and error.

## Tweaks

![ROM tweaks](media/tweaks.png)

The Tweaks window exposes supported ROM patches as simple on/off switches. Each tweak is version-aware, so unsupported ROMs keep unavailable options disabled instead of applying unsafe changes.

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

## Glyph Profiles

The Glyph Manager lets you customize how individual glyph bytes appear while editing and previewing messages.

Glyph profiles can change:

- the editor display character
- the preview glyph image
- the preview glyph width

The byte value itself does not change just because the display character changes. This is useful for translation work where a project may want to display an original glyph byte as a language-specific character. For example, a profile can make byte `0x92` appear as `å` in the editor while still saving byte `0x92` to the message data.

Glyph profiles are stored outside the bundled files and can be reset per character.

When saving or exporting, Hylian Grimoire rejects text characters that cannot be encoded by the active glyph profile. For example, typing `å` without a profile that maps `å` to a valid glyph byte will stop the save and report the affected message ID. This prevents unknown Unicode or Latin-1 characters from silently being written as invalid game bytes.

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
src/HylianGrimoire/Rom/          ROM detection, message-bank handling, glyph sessions, and ROM patch helpers
src/HylianGrimoire/Services/     File operations, searching, message list operations, and document services
src/HylianGrimoire/TitleText/    Title-screen text editing support
src/HylianGrimoire/Tweaks/       Supported ROM tweaks
tests/HylianGrimoire.Tests/      xUnit tests for codec, import/export, preview-adjacent behavior, and file parity
```

## Notes

Hylian Grimoire is a standalone editor project. It is meant for extracted message files, supported ROM workflows, and project workflows where exact save/export behavior matters. Keep backups of your source `.bin`, `.tbl`, header, and ROM files when working on real translation or modding projects.
