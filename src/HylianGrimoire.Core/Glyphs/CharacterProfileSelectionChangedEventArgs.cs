namespace HylianGrimoire.Glyphs;

public sealed class CharacterProfileSelectionChangedEventArgs(
    string previousProfileName,
    string selectedProfileName,
    CharacterProfile? previousProfile)
    : EventArgs
{
    public string PreviousProfileName { get; } = previousProfileName;

    public string SelectedProfileName { get; } = selectedProfileName;

    public CharacterProfile? PreviousProfile { get; } = previousProfile;
}
