using HylianGrimoire.Games;

namespace HylianGrimoire.Glyphs;

public sealed partial class CharacterProfileStore
{
    public void SetGameKind(GameKind gameKind)
    {
        if (_activeGameKind == gameKind)
        {
            return;
        }

        string previousProfileName = SelectedProfileName;
        CharacterProfile? previousProfile = CloneProfile(GetProfile(previousProfileName));
        _activeGameKind = gameKind;
        _temporaryProfile = null;
        string automaticProfileName = AutomaticProfileNameSetting;
        SelectedProfileName = automaticProfileName == AutomaticProfileName
            ? DefaultProfileName
            : automaticProfileName;
        Version++;
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
        RaiseSelectionChanged(previousProfileName, previousProfile);
    }

    public void UseDefaultProfile()
    {
        SelectProfile(DefaultProfileName);
    }

    public void ApplyAutomaticProfile(bool hasCustomRomGlyphs)
    {
        SetCustomGlyphsAvailable(hasCustomRomGlyphs);

        if (AutomaticProfileNameSetting == AutomaticProfileName)
        {
            SelectProfile(hasCustomRomGlyphs ? CustomGlyphsProfileName : DefaultProfileName);
        }
        else
        {
            SelectProfile(AutomaticProfileNameSetting);
        }
    }

    public void SetCustomGlyphsAvailable(bool available)
    {
        if (available)
        {
            if (_temporaryProfile is null)
            {
                _temporaryProfile = new CharacterProfile { Name = CustomGlyphsProfileName, GameKind = _activeGameKind };
                Version++;
                ProfilesChanged?.Invoke(this, EventArgs.Empty);
            }

            return;
        }

        if (_temporaryProfile is null)
        {
            return;
        }

        _temporaryProfile = null;
        Version++;
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
        if (SelectedProfileName == CustomGlyphsProfileName)
        {
            string previousProfileName = SelectedProfileName;
            CharacterProfile? previousProfile = CloneProfile(GetProfile(previousProfileName));
            SelectedProfileName = DefaultProfileName;
            Version++;
            RaiseSelectionChanged(previousProfileName, previousProfile);
        }
    }

    public void UseCustomGlyphsProfile()
    {
        if (_temporaryProfile is null)
        {
            _temporaryProfile = new CharacterProfile { Name = CustomGlyphsProfileName, GameKind = _activeGameKind };
            Version++;
            ProfilesChanged?.Invoke(this, EventArgs.Empty);
        }

        SelectProfile(CustomGlyphsProfileName);
    }

    public void SetAutomaticProfile(string name)
    {
        if (!IsAutomaticProfileValid(name))
        {
            name = AutomaticProfileName;
        }

        if (AutomaticProfileNameSetting == name)
        {
            return;
        }

        _automaticProfileByGame[_activeGameKind] = name;
        SaveConfig();
        AutomaticProfileChanged?.Invoke(this, EventArgs.Empty);
    }

    public void HideCustomGlyphsProfile()
    {
        if (_temporaryProfile is null)
        {
            return;
        }

        _temporaryProfile = null;
        Version++;
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
        if (SelectedProfileName == CustomGlyphsProfileName)
        {
            string previousProfileName = SelectedProfileName;
            CharacterProfile? previousProfile = CloneProfile(GetProfile(previousProfileName));
            SelectedProfileName = DefaultProfileName;
            Version++;
            RaiseSelectionChanged(previousProfileName, previousProfile);
        }
    }

    public void SelectProfile(string name)
    {
        if (!ProfileExists(name))
        {
            name = DefaultProfileName;
        }

        if (SelectedProfileName == name)
        {
            return;
        }

        string previousProfileName = SelectedProfileName;
        CharacterProfile? previousProfile = CloneProfile(GetProfile(previousProfileName));
        SelectedProfileName = name;
        Version++;
        RaiseSelectionChanged(previousProfileName, previousProfile);
    }

    public bool CreateProfile(string name)
    {
        name = NormalizeProfileName(name);
        if (name.Length == 0
            || name.Equals(DefaultProfileName, StringComparison.OrdinalIgnoreCase)
            || name.Equals(CustomGlyphsProfileName, StringComparison.OrdinalIgnoreCase)
            || ProfileExists(name))
        {
            return false;
        }

        var profile = new CharacterProfile
        {
            Name = name,
            GameKind = _activeGameKind,
            Characters = CopyCurrentProfileCharacters(),
            Widths = CopyCurrentProfileWidths(),
        };
        profile.Images = CopyCurrentProfileImages(profile);
        profile.ImageData = CopyCurrentProfileImageData(profile.Images);

        _profiles.Add(profile);
        string previousProfileName = SelectedProfileName;
        CharacterProfile? previousProfile = CloneProfile(GetProfile(previousProfileName));
        SelectedProfileName = name;
        SaveConfig();
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
        RaiseSelectionChanged(previousProfileName, previousProfile);
        return true;
    }

    public bool DeleteSelectedProfile()
    {
        if (!CanDeleteSelectedProfile)
        {
            return false;
        }

        string deletedProfileName = SelectedProfileName;
        CharacterProfile? deletedProfile = CloneProfile(GetProfile(deletedProfileName));
        int removed = _profiles.RemoveAll(profile => profile.GameKind == _activeGameKind
            && profile.Name.Equals(deletedProfileName, StringComparison.OrdinalIgnoreCase));
        if (removed == 0)
        {
            return false;
        }

        if (deletedProfile is not null)
        {
            CharacterProfileAssets.DeleteProfileAssets(deletedProfile);
        }
        SelectedProfileName = DefaultProfileName;
        bool automaticProfileChanged = false;
        if (!IsAutomaticProfileValid(AutomaticProfileNameSetting))
        {
            _automaticProfileByGame[_activeGameKind] = AutomaticProfileName;
            automaticProfileChanged = true;
        }

        SaveConfig();
        ProfilesChanged?.Invoke(this, EventArgs.Empty);
        RaiseSelectionChanged(deletedProfileName, deletedProfile);
        if (automaticProfileChanged)
        {
            AutomaticProfileChanged?.Invoke(this, EventArgs.Empty);
        }

        return true;
    }
}
