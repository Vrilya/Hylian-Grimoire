# Hylian Grimoire

Hylian Grimoire is a Windows desktop editor for Ocarina of Time message data. I originally built it while working on my own translation of the game, and the project has been rewritten from the ground up several times. The current version focuses on exact binary round-tripping, readable editor syntax, and a modern Windows 11 interface.

The editor works with extracted `.bin` and `.tbl` message files. It does not load ROM files directly.

## Features

- Load and save message `.bin` / `.tbl` files.
- Import and export C message headers.
- Search message IDs by ID or message text.
- Add, rename, delete, and reorder message IDs.
- Edit textbox type and position metadata.
- Preview message boxes with game-style text rendering, item icons, colors, choices, highscore tokens, ocarina staff display, credits text, and multi-box messages.
- Open the preview in a separate window with zoom, column layout, alignment guides, and always-on-top support.
- Override editor glyph display characters, glyph images, and preview glyph widths without changing the underlying byte values.
- Follow Windows light/dark mode through WinUI 3 and Windows App SDK.

## Editor Syntax

Messages are shown as editable text with bracketed tags for control codes. For example:

```text
[unskippable][item:30][quicktexton]Du bytte [color:red]Kojiro [color:default][quicktextoff]mot en [color:red]konstig
svamp[color:default]![quicktextoff]
[break]
[unskippable][item:30][quicktexton]Denna konstiga svamp försvinner
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

## Character Overrides

The Character Overrides window lets you customize how individual glyph bytes appear while editing and previewing messages.

Overrides can change:

- the editor display character
- the preview glyph image
- the preview glyph width

The byte value itself does not change. This is useful for translation work where a project may want to display certain original glyph bytes as language-specific characters while still saving the expected original byte values.

Overrides are stored outside the bundled files and can be reset per character.

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
src/HylianGrimoire/Glyphs/       Glyph metadata, overrides, and the Character Overrides window
src/HylianGrimoire/Headers/      C header import/export
src/HylianGrimoire/Interop/      Windows window theming, icons, sizing, and native interop helpers
src/HylianGrimoire/Models/       Message entries, message tokens, and UI list models
src/HylianGrimoire/Preview/      Game-style message preview renderer and preview window
src/HylianGrimoire/Services/     File operations, searching, message list operations, and catalogs
tests/HylianGrimoire.Tests/      xUnit tests for codec, import/export, preview-adjacent behavior, and file parity
```

## Notes

Hylian Grimoire is a standalone editor project. It is meant for extracted message files and project workflows where exact save/export behavior matters. Keep backups of your source `.bin`, `.tbl`, and header files when working on real translation or modding projects.
