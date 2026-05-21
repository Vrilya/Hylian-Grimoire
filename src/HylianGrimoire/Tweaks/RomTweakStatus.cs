namespace HylianGrimoire.Tweaks;

public sealed record RomTweakStatus(RomTweakState State, string Detail)
{
    public bool CanToggle => State is RomTweakState.Off or RomTweakState.On or RomTweakState.Mixed;
}
