using System;
using System.Collections.Generic;
using HylianGrimoire.Models;

namespace HylianGrimoire.Codecs;

/// <summary>
/// Handles binary encoding/decoding for individual OoT messages.
/// </summary>
public static partial class MessageCodec
{
    // --------------------------------------------------------
    // Character / command lookup tables
    // --------------------------------------------------------

    private static readonly IReadOnlyDictionary<byte, string> ButtonBytes = MessageTokenMaps.ButtonTags;

    private static readonly IReadOnlyDictionary<byte, string> NoArgCmds = MessageTokenMaps.CommandTags;

}
