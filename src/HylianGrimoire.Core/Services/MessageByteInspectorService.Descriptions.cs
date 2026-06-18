using HylianGrimoire.Codecs;
using HylianGrimoire.Codecs.MajorasMask;

namespace HylianGrimoire.Services;

public static partial class MessageByteInspectorService
{
    private static string GetCommandTag(byte command)
        => MessageTokenMaps.CommandTags.TryGetValue(command, out string? tag)
            ? $"[{tag}]"
            : $"[0x{command:x2}]";

    private static string GetCommandDescription(byte command)
        => command switch
        {
            0x04 => "Starts a new textbox page.",
            0x08 => "Draws following text instantly.",
            0x09 => "Returns to normal text drawing.",
            0x0a => "Ends with a persistent textbox state; used by shop-style text.",
            0x0b => "Ends with an event-controlled textbox state.",
            0x0d => "Waits until the player presses a button.",
            0x0f => "Prints the player's file name.",
            0x10 => "Draws the ocarina staff.",
            0x16 => "Prints the Running Man marathon time.",
            0x17 => "Prints the last race timer value.",
            0x18 => "Prints horseback archery points.",
            0x19 => "Prints the current Gold Skulltula count.",
            0x1a => "Disallows skipping the textbox.",
            0x1b => "Shows a two-choice prompt.",
            0x1c => "Shows a three-choice prompt.",
            0x1d => "Prints caught fish information.",
            0x1f => "Prints the current in-game time.",
            _ => "Ocarina of Time control code.",
        };

    private static string GetMajorasMaskCommandDescription(byte command, string tag)
        => command switch
        {
            0x0a => "Text speed marker; this MM text format emits it without an explicit encoded argument.",
            0x0b => "Prints the Swamp Cruise Archery required-hit value.",
            0x0c => "Prints the current Stray Fairy count.",
            0x0d => "Prints the current Gold Skulltula token count.",
            0x0e => "Prints the current point value up to 99.",
            0x0f => "Prints the current point value up to 9999.",
            0x10 => "Starts a new textbox page.",
            0x12 => "Starts a new textbox page using the BOX_BREAK2 layout adjustment.",
            0x13 => "Resets horizontal text position without adding newline spacing.",
            0x14 => "Shifts the following text horizontally.",
            0x15 => "Ends the current message with the continue icon.",
            0x16 => "Prints the player's file name.",
            0x17 => "Draws following text instantly.",
            0x18 => "Returns to normal text drawing.",
            0x19 => "Ends with an event-controlled textbox state.",
            0x1a => "Ends with a persistent textbox state.",
            0x1b => "Starts a delayed textbox break after the frame count.",
            0x1c => "Ends with a normal fade after the frame count.",
            0x1d => "Ends with a skippable fade after the frame count.",
            0x1e => "Plays a sound effect.",
            0x1f => "Pauses text drawing for the specified frame count.",
            0xc1 => "Draws the Majora's Mask message background command.",
            0xc2 => "Sets the textbox to a two-choice prompt.",
            0xc3 => "Sets the textbox to a three-choice prompt.",
            0xc4 => "Prints the Postman timer.",
            0xc5 => "Prints minigame timer 1.",
            0xc6 => "Prints timer 2.",
            0xc7 => "Prints the Moon crash timer.",
            0xc8 => "Prints minigame timer 2.",
            0xc9 => "Prints the environmental hazard timer.",
            0xca => "Prints the current in-game time.",
            0xcb => "Prints the chest flag value.",
            0xcc => "Ends with bank rupee input behavior.",
            0xcd => "Prints the selected rupee amount.",
            0xce => "Prints the total rupee amount.",
            0xcf => "Prints remaining time until the Moon crashes.",
            0xd0 => "Ends with Doggy Racetrack bet input behavior.",
            0xd1 => "Ends with Bomber Code input behavior.",
            0xd2 => "Ends with pause-menu textbox behavior.",
            0xd3 => "Prints the active time speed.",
            0xd4 => "Prints the Song of Soaring destination.",
            0xd5 => "Ends with Lottery Code input behavior.",
            0xd6 => "Prints the full Spider House mask code.",
            0xd7 => "Prints remaining Woodfall stray fairies.",
            0xd8 => "Prints remaining Snowhead stray fairies.",
            0xd9 => "Prints remaining Great Bay stray fairies.",
            0xda => "Prints remaining Stone Tower stray fairies.",
            0xdb => "Prints the Swamp Cruise Archery score.",
            0xdc => "Prints the winning Lottery Code.",
            0xdd => "Prints the player's Lottery Code guess.",
            0xde => "Prints the held item price.",
            0xdf => "Prints the Bomber Code.",
            0xe0 => "Ends with the alternate event-controlled textbox state.",
            0xe1 => "Prints Spider House mask code part 1.",
            0xe2 => "Prints Spider House mask code part 2.",
            0xe3 => "Prints Spider House mask code part 3.",
            0xe4 => "Prints Spider House mask code part 4.",
            0xe5 => "Prints Spider House mask code part 5.",
            0xe6 => "Prints Spider House mask code part 6.",
            0xe7 => "Prints remaining hours until the Moon crashes.",
            0xe8 => "Prints remaining time until the next day.",
            0xf0 => "Prints the bank rupees high score.",
            0xf1 => "Prints high-score points value 1.",
            0xf2 => "Prints the fishing points high score.",
            0xf3 => "Prints the Boat Archery high score as a time.",
            0xf4 => "Prints the Horseback Balloon high score as a time.",
            0xf5 => "Prints the Lottery guess high score as a time.",
            0xf6 => "Prints the Town Shooting Gallery high score.",
            0xf7 => "Prints unknown high-score value 1.",
            0xf8 => "Prints unknown high-score value 3 lower digits.",
            0xf9 => "Prints the Horseback Balloon high score.",
            0xfa => "Prints the Deku Playground Day 1 high score.",
            0xfb => "Prints the Deku Playground Day 2 high score.",
            0xfc => "Prints the Deku Playground Day 3 high score.",
            0xfd => "Prints the Day 1 Deku Playground player name.",
            0xfe => "Prints the Day 2 Deku Playground player name.",
            0xff => "Prints the Day 3 Deku Playground player name.",
            _ => $"Majora's Mask control code [{tag}].",
        };

    private static string GetMajorasMaskParameterDescription(string tag, ushort argument)
    {
        if (tag.Equals("sfx", StringComparison.OrdinalIgnoreCase)
            && MmMessageSfxMaps.Names.TryGetValue(argument, out string? sfxName))
        {
            return $"{sfxName} (0x{argument:x4})";
        }

        return $"0x{argument:x4}";
    }

    private static string GetColorTag(byte value)
        => MessageTokenMaps.ColorTags.TryGetValue(value, out string? tag) ? tag : $"{value:x2}";

    private static string GetColorLabel(byte value)
        => MessageTokenMaps.ColorTags.TryGetValue(value, out string? tag) ? $"{GetTagLabel(tag)} (0x{value:x2})" : $"0x{value:x2}";

    private static string GetButtonTag(byte value)
        => MessageTokenMaps.ButtonTags.TryGetValue(value, out string? tag) ? $"[{tag}]" : $"[0x{value:x2}]";

    private static string GetButtonLabel(byte value)
        => MessageTokenMaps.ButtonTags.TryGetValue(value, out string? tag) ? GetTagLabel(tag) : $"Button 0x{value:x2}";

    private static string GetHighscoreTag(byte value)
        => MessageTokenMaps.HighscoreTags.TryGetValue(value, out string? tag) ? $"[{tag}]" : $"[minigame:{value:x2}]";

    private static string GetHighscoreLabel(byte value)
        => MessageTokenMaps.HighscoreTags.TryGetValue(value, out string? tag) ? $"{GetTagLabel(tag)} (0x{value:x2})" : $"Minigame 0x{value:x2}";

    private static string GetSfxTag(ushort value)
        => MessageSfxMaps.Tags.TryGetValue(value, out string? tag) ? tag : $"{value:x4}";

    private static string GetSfxLabel(ushort value)
        => MessageSfxMaps.Tags.TryGetValue(value, out string? tag) ? $"{tag} (0x{value:x4})" : $"0x{value:x4}";

    private static string GetTagLabel(string tag)
    {
        string cleaned = tag.Trim('[', ']');
        if (cleaned.Length == 0)
        {
            return tag;
        }

        return string.Join(
            " ",
            cleaned.Split(['-', ':'], StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static string TrimForDescription(string text)
    {
        string singleLine = text.Replace("\r", "\\r").Replace("\n", "\\n");
        return singleLine.Length <= 48 ? singleLine : $"{singleLine[..45]}...";
    }
}
