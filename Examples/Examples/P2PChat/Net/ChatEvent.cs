namespace Examples.P2PChat.Net;

public abstract record ChatEvent;

public sealed record ChatStarted(string Description) : ChatEvent;

public sealed record ChatMessage(string Sender, string Text) : ChatEvent;

public sealed record ChatNotice(string Text) : ChatEvent;

public sealed record ChatClosed(string Reason) : ChatEvent;
