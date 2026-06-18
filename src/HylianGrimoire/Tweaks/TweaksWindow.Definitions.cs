using HylianGrimoire.Games;
using HylianGrimoire.Rom;
using Microsoft.UI.Xaml.Controls;

namespace HylianGrimoire.Tweaks;

public sealed partial class TweaksWindow
{
    private IReadOnlyList<TweakUi> CreateTweakDefinitions() =>
    [
        new(
            GameKind.OcarinaOfTime,
            BootLogoCard,
            BootLogoSwitch,
            BootLogoStatusText,
            romData => GcBootLogoTweak.GetStatus(romData.DecompressedRom, romData.Profile),
            (romData, enabled) => GcBootLogoTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled Nintendo boot logo tweak.",
            "Disabled Nintendo boot logo tweak."),
        new(
            GameKind.OcarinaOfTime,
            ColorCard,
            ColorSwitch,
            ColorStatusText,
            romData => GcColorTweak.GetStatus(romData.DecompressedRom, romData.Profile),
            (romData, enabled) => GcColorTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled N64 interface colors tweak.",
            "Disabled N64 interface colors tweak."),
        new(
            GameKind.OcarinaOfTime,
            ViPalCard,
            ViPalSwitch,
            ViPalStatusText,
            romData => ViPalTweak.GetStatus(romData.DecompressedRom, romData.Profile),
            (romData, enabled) => ViPalTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled N64 VI PAL timing tweak.",
            "Disabled N64 VI PAL timing tweak."),
        new(
            GameKind.OcarinaOfTime,
            ViSelectorCard,
            ViSelectorSwitch,
            ViSelectorStatusText,
            romData => ViSelectorTweak.GetStatus(romData.DecompressedRom, romData.Profile),
            (romData, enabled) => ViSelectorTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled FPAL/MPAL selector tweak.",
            "Disabled FPAL/MPAL selector tweak."),
        new(
            GameKind.OcarinaOfTime,
            NoControllerCard,
            NoControllerSwitch,
            NoControllerStatusText,
            romData => GcNoControllerTweak.GetStatus(romData.DecompressedRom, romData.Profile),
            (romData, enabled) => GcNoControllerTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled no-controller message tweak.",
            "Disabled no-controller message tweak."),
        new(
            GameKind.OcarinaOfTime,
            CreditsCard,
            CreditsSwitch,
            CreditsStatusText,
            romData => GcCreditsTweak.GetStatus(romData.DecompressedRom, romData.Profile),
            (romData, enabled) => GcCreditsTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled GC credits N64 crash fix.",
            "Disabled GC credits N64 crash fix."),
        new(
            GameKind.OcarinaOfTime,
            AntiPiracyCard,
            AntiPiracySwitch,
            AntiPiracyStatusText,
            romData => AntiPiracyTweak.GetStatus(romData.DecompressedRom, romData.Profile),
            (romData, enabled) => AntiPiracyTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Disabled anti-piracy checks.",
            "Restored anti-piracy checks."),
        new(
            GameKind.MajorasMask,
            MmFpalCard,
            MmFpalSwitch,
            MmFpalStatusText,
            romData => MmFpalTweak.GetStatus(romData.DecompressedRom, romData.Profile),
            (romData, enabled) => MmFpalTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled N64 VI PAL timing tweak.",
            "Disabled N64 VI PAL timing tweak.",
            ShowMixedAsOn: false),
        new(
            GameKind.MajorasMask,
            MmViSelectorCard,
            MmViSelectorSwitch,
            MmViSelectorStatusText,
            romData => MmViSelectorTweak.GetStatus(romData.DecompressedRom, romData.Profile),
            (romData, enabled) => MmViSelectorTweak.SetEnabled(romData.DecompressedRom, romData.Profile, enabled),
            "Enabled MM FPAL/MPAL selector tweak.",
            "Disabled MM FPAL/MPAL selector tweak.",
            ShowMixedAsOn: false),
    ];

    private sealed record TweakUi(
        GameKind Game,
        Border Card,
        ToggleSwitch Switch,
        TextBlock StatusText,
        Func<RomMessageData, RomTweakStatus> GetStatus,
        Action<RomMessageData, bool> SetEnabled,
        string EnabledMessage,
        string DisabledMessage,
        bool ShowMixedAsOn = true);
}
