using HylianGrimoire.Codecs;
using HylianGrimoire.Models;

namespace HylianGrimoire.Headers;

public static partial class CHeaderImporter
{
    private static void AppendMacro(List<MessageToken> tokens, string macro, string argumentText)
    {
        if (macro.Equals("NEWLINE", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new LineBreakToken());
        }
        else if (macro.Equals("BOX_BREAK", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new CommandToken(MessageCommand.Break));
        }
        else if (macro.Equals("BOX_BREAK_DELAYED", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new BreakDelayToken((byte)ParseArgumentByte(argumentText)));
        }
        else if (macro.Equals("COLOR", StringComparison.OrdinalIgnoreCase))
        {
            byte color = Colors.TryGetValue(argumentText.Trim(), out string? mapped)
                && MessageTokenMaps.ColorBytes.TryGetValue(mapped, out byte mappedColor)
                    ? mappedColor
                    : (byte)ParseArgumentByte(argumentText);
            tokens.Add(new ColorToken((MessageColor)color));
        }
        else if (macro.Equals("SHIFT", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new ShiftToken((byte)ParseArgumentByte(argumentText)));
        }
        else if (macro.Equals("TEXTID", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new TextIdToken((ushort)ParseArgumentWord(argumentText)));
        }
        else if (macro.Equals("FADE", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new FadeToken((byte)ParseArgumentByte(argumentText)));
        }
        else if (macro.Equals("FADE2", StringComparison.OrdinalIgnoreCase)
            || macro.Equals("END_FADE", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new EndFadeToken((ushort)ParseArgumentWord(argumentText)));
        }
        else if (macro.Equals("SFX", StringComparison.OrdinalIgnoreCase))
        {
            string argument = argumentText.Trim();
            int value = MessageSfxMaps.HeaderValues.TryGetValue(argument, out int headerMapped)
                ? headerMapped
                : MessageSfxMaps.Values.TryGetValue(argument, out int mapped)
                ? mapped
                : ParseArgumentWord(argumentText);
            tokens.Add(new SfxToken((ushort)value));
        }
        else if (macro.Equals("ITEM_ICON", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new IconToken((byte)ParseItemArgument(argumentText)));
        }
        else if (macro.Equals("TEXT_SPEED", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Add(new TextSpeedToken((byte)ParseArgumentByte(argumentText)));
        }
        else if (macro.Equals("BACKGROUND", StringComparison.OrdinalIgnoreCase))
        {
            var args = CHeaderCallParser.SplitTopLevel(argumentText);
            int value = args.Count switch
            {
                3 => ParseArgumentByte(args[0]) << 16
                    | ParseArgumentByte(args[1]) << 8
                    | ParseArgumentByte(args[2]),
                5 => ParseBackground(args),
                _ => throw new InvalidDataException("BACKGROUND must have three legacy arguments or five modern arguments."),
            };
            tokens.Add(new BackgroundToken(value));
        }
        else if (macro.Equals("HIGHSCORE", StringComparison.OrdinalIgnoreCase))
        {
            int value = Highscores.TryGetValue(argumentText.Trim(), out int mapped)
                ? mapped
                : ParseArgumentByte(argumentText);
            tokens.Add(new HighscoreToken((byte)value));
        }
        else if (NoArgTags.TryGetValue(macro, out string? tag))
        {
            tokens.Add(new CommandToken((MessageCommand)MessageTokenMaps.CommandBytes[tag]));
        }
        else
        {
            throw new InvalidDataException($"Unknown C header message macro: {macro}.");
        }
    }
}
