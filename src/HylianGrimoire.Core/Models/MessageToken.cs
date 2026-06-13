namespace HylianGrimoire.Models;

public abstract record MessageToken;

public sealed record TextToken(string Text) : MessageToken;

public sealed record LineBreakToken : MessageToken;

public enum MessageCommand : byte
{
    Break = 0x04,
    QuickTextOn = 0x08,
    QuickTextOff = 0x09,
    Shop = 0x0a,
    Event = 0x0b,
    WaitButton = 0x0d,
    Name = 0x0f,
    Ocarina = 0x10,
    MarathonTime = 0x16,
    RaceTime = 0x17,
    Points = 0x18,
    Skulltulas = 0x19,
    Unskippable = 0x1a,
    TwoChoice = 0x1b,
    ThreeChoice = 0x1c,
    FishInfo = 0x1d,
    Time = 0x1f,
}

public sealed record CommandToken(MessageCommand Command) : MessageToken
{
    public byte Code => (byte)Command;
}

public enum MessageColor : byte
{
    Default = 0x40,
    Red = 0x41,
    Green = 0x42,
    Blue = 0x43,
    LightBlue = 0x44,
    Purple = 0x45,
    Yellow = 0x46,
    Black = 0x47,
}

public sealed record ColorToken(MessageColor Color) : MessageToken
{
    public byte Index => (byte)Color;
}

public sealed record ShiftToken(byte Pixels) : MessageToken;

public sealed record TextIdToken(ushort Id) : MessageToken;

public sealed record BreakDelayToken(byte Frames) : MessageToken;

public sealed record FadeToken(byte Frames) : MessageToken;

public sealed record EndFadeToken(ushort Frames) : MessageToken;

public sealed record SfxToken(ushort Id) : MessageToken;

public sealed record IconToken(byte Id) : MessageToken;

public sealed record TextSpeedToken(byte Speed) : MessageToken;

public sealed record BackgroundToken(int Rgb) : MessageToken;

public sealed record HighscoreToken(byte Id) : MessageToken;

public enum MessageButton : byte
{
    A = 0x9f,
    B = 0xa0,
    C = 0xa1,
    L = 0xa2,
    R = 0xa3,
    Z = 0xa4,
    CUp = 0xa5,
    CDown = 0xa6,
    CLeft = 0xa7,
    CRight = 0xa8,
    Triangle = 0xa9,
    Stick = 0xaa,
}

public sealed record ButtonToken(MessageButton Button) : MessageToken
{
    public byte Code => (byte)Button;
}
