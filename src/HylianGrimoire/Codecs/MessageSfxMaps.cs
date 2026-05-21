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

    public static readonly IReadOnlyDictionary<int, string> HeaderNames = new Dictionary<int, string>
    {
        { 0x28DF, "NA_SE_EV_COW_CRY" },
        { 0x28E3, "NA_SE_EV_FROG_CRY_0" },
        { 0x28E4, "NA_SE_EV_FROG_CRY_1" },
        { 0x3880, "NA_SE_EN_NUTS_DAMAGE" },
        { 0x3882, "NA_SE_EN_NUTS_FAINT" },
        { 0x38EC, "NA_SE_EN_PO_LAUGH" },
        { 0x4807, "NA_SE_SY_TRE_BOX_APPEAR" },
        { 0x6844, "NA_SE_VO_NA_HELLO_3" },
        { 0x6852, "NA_SE_VO_TA_CRY_0" },
        { 0x6855, "NA_SE_VO_IN_LOST" },
        { 0x685F, "NA_SE_VO_NA_HELLO_2" },
        { 0x6863, "NA_SE_VO_RT_LAUGH_0" },
        { 0x6867, "NA_SE_VO_ST_DAMAGE" },
        { 0x6869, "NA_SE_VO_Z0_HURRY" },
        { 0x686B, "NA_SE_VO_Z0_QUESTION" },
        { 0x686C, "NA_SE_VO_Z0_SIGH_0" },
        { 0x686D, "NA_SE_VO_Z0_SMILE_0" },
    };

    public static readonly IReadOnlyDictionary<string, int> HeaderValues = DictionaryMaps.Reverse(HeaderNames, StringComparer.OrdinalIgnoreCase);
}
