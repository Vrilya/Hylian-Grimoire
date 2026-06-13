namespace HylianGrimoire.Services;

using HylianGrimoire.Games;

public sealed record MessageControlCodeGroup(string Name, IReadOnlyList<MessageControlCodeEntry> Entries);

public sealed record MessageControlCodeEntry(
    string Label,
    string InsertText,
    string Description,
    int SelectionStartOffset = -1,
    int SelectionLength = 0);

public static class MessageControlCodeCatalog
{
    private static readonly IReadOnlyList<MessageControlCodeGroup> OcarinaOfTimeGroups =
    [
        new("Colors",
        [
            Entry("Default", "[color:default]", "Use the default message color."),
            Entry("Red", "[color:red]", "Use red text."),
            Entry("Green", "[color:green]", "Use green text."),
            Entry("Blue", "[color:blue]", "Use blue text."),
            Entry("Light Blue", "[color:lightblue]", "Use light blue text."),
            Entry("Purple", "[color:purple]", "Use purple text."),
            Entry("Yellow", "[color:yellow]", "Use yellow text."),
            Entry("Black", "[color:black]", "Use black text."),
        ]),

        new("Buttons",
        [
            Entry("A Button", "[A-button]", "Draw the A button glyph."),
            Entry("B Button", "[B-button]", "Draw the B button glyph."),
            Entry("C Button", "[C-button]", "Draw the C button glyph."),
            Entry("C-Up", "[C-up]", "Draw the C-Up button glyph."),
            Entry("C-Down", "[C-down]", "Draw the C-Down button glyph."),
            Entry("C-Left", "[C-left]", "Draw the C-Left button glyph."),
            Entry("C-Right", "[C-right]", "Draw the C-Right button glyph."),
            Entry("L Button", "[L-button]", "Draw the L button glyph."),
            Entry("R Button", "[R-button]", "Draw the R button glyph."),
            Entry("Z Button", "[Z-button]", "Draw the Z button glyph."),
            Entry("Triangle", "[Triangle]", "Draw the message continuation triangle."),
            Entry("Control Stick", "[Stick]", "Draw the control stick glyph."),
        ]),

        new("Flow",
        [
            Entry("Line Break", "\n", "Insert a manual line break."),
            Entry("New Textbox", "[break]", "Start a new textbox page."),
            Entry("Wait For Button", "[waitbutton]", "Wait until the player presses a button."),
            Entry("Two Choices", "[twochoice]", "Show a two-choice prompt."),
            Entry("Three Choices", "[threechoice]", "Show a three-choice prompt."),
            Entry("Event", "[event]", "Keep the textbox open until game logic closes it."),
            Entry("Persistent", "[shop]", "Prevent normal textbox closing; used by shop-style text."),
            Entry("No Skip", "[unskippable]", "Disallow skipping the textbox."),
        ]),

        new("Timing",
        [
            Entry("Quick Text On", "[quicktexton]", "Draw following text instantly."),
            Entry("Quick Text Off", "[quicktextoff]", "Return to normal text drawing."),
            ValueEntry("Text Speed", "[textspeed:00]", "Set the per-character text speed."),
            ValueEntry("Textbox Delay", "[breakdelay:00]", "Wait before switching to the next textbox."),
            ValueEntry("Fade", "[fade:00]", "Wait before ending the textbox."),
            ValueEntry("Long Fade", "[endfade:0000]", "Wait before ending the textbox with a 16-bit duration."),
            ValueEntry("Shift", "[shift:00]", "Shift text horizontally by the given pixel amount."),
        ]),

        new("Dynamic Values",
        [
            Entry("Player Name", "[name]", "Print the player's file name."),
            Entry("Time", "[time]", "Print the current in-game time."),
            Entry("Gold Skulltulas", "[skulltulas]", "Print the current Gold Skulltula count."),
            Entry("Points", "[points]", "Print horseback archery points."),
            Entry("Fish Info", "[fishinfo]", "Print caught fish information."),
            Entry("Running Man Time", "[marathontime]", "Print the Running Man marathon time."),
            Entry("Race Time", "[racetime]", "Print the last race timer value."),
            Entry("Ocarina Staff", "[ocarina]", "Draw the ocarina staff."),
        ]),

        new("High Scores",
        [
            Entry("Horseback Archery", "[archery]", "Print the horseback archery high score."),
            Entry("Poe Points", "[poe]", "Print the Poe Salesman score."),
            Entry("Fishing", "[fish]", "Print the fishing high score."),
            Entry("Horse Race", "[horserace]", "Print the horse race high score."),
            Entry("Marathon", "[marathon]", "Print the marathon high score."),
            ValueEntry("Raw Minigame ID", "[minigame:00]", "Print a high score by raw minigame ID."),
        ]),

        new("References",
        [
            ValueEntry("Jump To Text ID", "[textid:0000]", "Jump to another message ID."),
            ValueEntry("Sound Effect", "[sfx:CowMoo]", "Play a sound effect."),
            ValueEntry("Item Icon", "[item:00]", "Draw an item icon by item ID."),
            ValueEntry("Background", "[background:000000]", "Draw a message background effect by RGB value."),
        ]),
    ];

    private static readonly IReadOnlyList<MessageControlCodeGroup> MajorasMaskGroups =
    [
        new("Colors",
        [
            Entry("Default", "[color:default]", "Use the default message color."),
            Entry("Red", "[color:red]", "Use red text."),
            Entry("Green", "[color:green]", "Use green text."),
            Entry("Blue", "[color:blue]", "Use blue text."),
            Entry("Yellow", "[color:yellow]", "Use yellow text."),
            Entry("Light Blue", "[color:lightblue]", "Use light blue text."),
            Entry("Pink", "[color:pink]", "Use pink text."),
            Entry("Silver", "[color:silver]", "Use silver text."),
            Entry("Orange", "[color:orange]", "Use orange text."),
        ]),

        new("Buttons",
        [
            Entry("A Button", "[A-button]", "Draw the A button glyph."),
            Entry("B Button", "[B-button]", "Draw the B button glyph."),
            Entry("C Button", "[C-button]", "Draw the C button glyph."),
            Entry("C-Up", "[C-up]", "Draw the C-Up button glyph."),
            Entry("C-Down", "[C-down]", "Draw the C-Down button glyph."),
            Entry("C-Left", "[C-left]", "Draw the C-Left button glyph."),
            Entry("C-Right", "[C-right]", "Draw the C-Right button glyph."),
            Entry("L Button", "[L-button]", "Draw the L button glyph."),
            Entry("R Button", "[R-button]", "Draw the R button glyph."),
            Entry("Z Button", "[Z-button]", "Draw the Z button glyph."),
            Entry("Z Target", "[Z-target]", "Draw the Z-target glyph."),
            Entry("Control Pad", "[Control-Pad]", "Draw the control pad glyph."),
        ]),

        new("Flow",
        [
            Entry("Line Break", "\n", "Insert a manual line break."),
            Entry("New Textbox", "[break]", "Start a new textbox page."),
            Entry("New Textbox 2", "[break2]", "Start an alternate textbox break."),
            Entry("Carriage Return", "[carriagereturn]", "Insert a carriage-return filler command."),
            Entry("Continue", "[continue]", "Continue into the next configured message."),
            Entry("Two Choices", "[twochoice]", "Show a two-choice prompt."),
            Entry("Three Choices", "[threechoice]", "Show a three-choice prompt."),
            Entry("Event", "[event]", "Mark this as event-controlled text."),
            Entry("Event 2", "[event2]", "Mark this as the alternate event-controlled text command."),
            Entry("Persistent", "[persistent]", "Prevent normal textbox closing."),
            Entry("Background", "[background]", "Use the message background command."),
            Entry("Pause Menu", "[pausemenu]", "Open the pause-menu related text behavior."),
        ]),

        new("Timing",
        [
            Entry("Quick Text On", "[quicktexton]", "Draw following text instantly."),
            Entry("Quick Text Off", "[quicktextoff]", "Return to normal text drawing."),
            Entry("Text Speed", "[textspeed]", "Insert the MM text-speed command."),
            ValueEntry("Shift", "[shift:00]", "Shift text horizontally by the given pixel amount."),
            ValueEntry("Delay", "[delay:0010]", "Pause text for the given number of frames."),
            ValueEntry("Delayed Textbox", "[breakdelay:0010]", "Pause, then start a new textbox."),
            ValueEntry("Fade", "[fade:0010]", "Wait before ending the textbox."),
            ValueEntry("Skippable Fade", "[fadeskippable:0010]", "Wait before ending the textbox, but allow skipping."),
            ValueEntry("Sound Effect", "[sfx:0000]", "Play a sound effect by ID."),
        ]),

        new("Dynamic Values",
        [
            Entry("Player Name", "[name]", "Print the player's file name."),
            Entry("Current Time", "[time]", "Print the current time."),
            Entry("Time Until Moon Crash", "[timeuntilmooncrash]", "Print remaining time until the Moon crashes."),
            Entry("Hours Until Moon Crash", "[hoursuntilmooncrash]", "Print remaining hours until the Moon crashes."),
            Entry("Time Until New Day", "[timeuntilnewday]", "Print remaining time until the next day."),
            Entry("Time Speed", "[timespeed]", "Print the active time speed."),
            Entry("Gold Skulltulas", "[tokens]", "Print the current Gold Skulltula token count."),
            Entry("Stray Fairies", "[strayfairies]", "Print the current Stray Fairy count."),
            Entry("Chest Flags", "[chestflags]", "Print the chest flag value."),
            Entry("Held Item Price", "[helditemprice]", "Print the held item price."),
        ]),

        new("Prompts",
        [
            Entry("Bank Input", "[inputbank]", "Print the bank deposit or withdrawal prompt."),
            Entry("Selected Rupees", "[rupeesselected]", "Print the selected rupee amount."),
            Entry("Total Rupees", "[rupeestotal]", "Print the total rupee amount."),
            Entry("Dog Race Bet", "[inputdogbet]", "Print the Doggy Racetrack bet prompt."),
            Entry("Bomber Code Input", "[inputbombercode]", "Print the Bomber Code input prompt."),
            Entry("Lottery Code Input", "[inputlotterycode]", "Print the Lottery Code input prompt."),
            Entry("Bomber Code", "[bombercode]", "Print the Bomber Code."),
            Entry("Lottery Code", "[lotterycode]", "Print the winning Lottery Code."),
            Entry("Lottery Guess", "[lotterycodeguess]", "Print the player's Lottery Code guess."),
            Entry("Owl Warp", "[owlwarp]", "Print the Song of Soaring destination prompt."),
        ]),

        new("Fairies And Masks",
        [
            Entry("Woodfall Fairies", "[fairieswoodfall]", "Print remaining Woodfall stray fairies."),
            Entry("Snowhead Fairies", "[fairiessnowhead]", "Print remaining Snowhead stray fairies."),
            Entry("Great Bay Fairies", "[fairiesgreatbay]", "Print remaining Great Bay stray fairies."),
            Entry("Stone Tower Fairies", "[fairiesstonetower]", "Print remaining Stone Tower stray fairies."),
            Entry("Spider House Mask Code", "[spiderhousemaskcode]", "Print the full Spider House mask code."),
            Entry("Spider House Mask 1", "[spiderhousemask1]", "Print Spider House mask code part 1."),
            Entry("Spider House Mask 2", "[spiderhousemask2]", "Print Spider House mask code part 2."),
            Entry("Spider House Mask 3", "[spiderhousemask3]", "Print Spider House mask code part 3."),
            Entry("Spider House Mask 4", "[spiderhousemask4]", "Print Spider House mask code part 4."),
            Entry("Spider House Mask 5", "[spiderhousemask5]", "Print Spider House mask code part 5."),
            Entry("Spider House Mask 6", "[spiderhousemask6]", "Print Spider House mask code part 6."),
        ]),

        new("Scores",
        [
            Entry("Boat Archery Required Hits", "[hsboatarchery]", "Print the required Swamp Cruise Archery hits."),
            Entry("Boat Archery Points", "[pointsboatarchery]", "Print the Swamp Cruise Archery score."),
            Entry("Town Shooting Gallery", "[hstownshooting]", "Print the Town Shooting Gallery high score."),
            Entry("Points Tens", "[pointstens]", "Print a minigame score up to 99."),
            Entry("Points Thousands", "[pointsthousands]", "Print a minigame score up to 9999."),
            Entry("Bank Rupees High Score", "[hsbankrupees]", "Print the bank rupees high score."),
            Entry("Fishing Points", "[hsfishingpoints]", "Print the fishing points high score."),
            Entry("Boat Archery Time", "[hsboatarcherytime]", "Print the Boat Archery high score as time."),
            Entry("Horse Balloon Time", "[hshorseballoontime]", "Print the horse balloon high score as time."),
            Entry("Lottery Time", "[hslotterytime]", "Print the Lottery guess high score as time."),
            Entry("Horse Balloon", "[hshorseballoon]", "Print the horse balloon high score."),
            Entry("Deku Playground Day 1", "[hsdekuplayground1]", "Print the Deku Playground Day 1 high score."),
            Entry("Deku Playground Day 2", "[hsdekuplayground2]", "Print the Deku Playground Day 2 high score."),
            Entry("Deku Playground Day 3", "[hsdekuplayground3]", "Print the Deku Playground Day 3 high score."),
            Entry("Deku Playground Name 1", "[dekuplaygroundname1]", "Print the Day 1 Deku Playground player name."),
            Entry("Deku Playground Name 2", "[dekuplaygroundname2]", "Print the Day 2 Deku Playground player name."),
            Entry("Deku Playground Name 3", "[dekuplaygroundname3]", "Print the Day 3 Deku Playground player name."),
        ]),
    ];

    public static IReadOnlyList<MessageControlCodeGroup> GetGroups(GameKind gameKind)
        => gameKind switch
        {
            GameKind.OcarinaOfTime => OcarinaOfTimeGroups,
            GameKind.MajorasMask => MajorasMaskGroups,
            _ => [],
        };

    private static MessageControlCodeEntry Entry(string label, string insertText, string description)
        => new(label, insertText, description);

    private static MessageControlCodeEntry ValueEntry(string label, string insertText, string description)
    {
        int colon = insertText.IndexOf(':', StringComparison.Ordinal);
        int close = insertText.LastIndexOf(']');
        return colon >= 0 && close > colon
            ? new MessageControlCodeEntry(label, insertText, description, colon + 1, close - colon - 1)
            : new MessageControlCodeEntry(label, insertText, description);
    }
}
