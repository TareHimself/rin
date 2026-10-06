namespace Examples.P2PChat.Net;

public enum FrameKind
{
    Hello,
    Chat,
    Notice
}

public sealed record Frame(FrameKind Kind, string Sender, string Text);
