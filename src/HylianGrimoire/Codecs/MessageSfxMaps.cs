using System.Collections.Generic;

namespace HylianGrimoire.Codecs;

public static class MessageSfxMaps
{
    public static readonly IReadOnlyDictionary<int, string> Tags = new Dictionary<int, string>
    {
        { 0x28DF, "CowMoo" },
        { 0x28E3, "FrogCroak1" },
        { 0x28E4, "FrogCroak2" },
        { 0x3880, "Scrub1" },
        { 0x3882, "Scrub2" },
        { 0x38EC, "PoeLaugh" },
        { 0x4807, "Treasure" },
        { 0x6844, "NaviHello" },
        { 0x6852, "TalonHuh" },
        { 0x6855, "IngoLost" },
        { 0x685F, "NaviHey" },
        { 0x6863, "Laugh1" },
        { 0x6867, "CursedMan" },
        { 0x6869, "Gasp" },
        { 0x686B, "Question" },
        { 0x686C, "Sigh" },
        { 0x686D, "Laugh2" },
    };

    public static readonly IReadOnlyDictionary<string, int> Values = DictionaryMaps.Reverse(Tags, StringComparer.OrdinalIgnoreCase);
}
