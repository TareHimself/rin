# P2PChat

Peer to peer text chat over TCP. One instance hosts on a port, others connect to its address. Start two copies to try it (use `127.0.0.1` on one machine, or the host's LAN address).

```
dotnet run --project Examples/P2PChat/P2PChat.csproj
```

On the start screen enter a name, then pick Host a room (port field) or Join a room (address field, `host` or `host:port`, default port 7777). Enter or the Send button sends a message. Leave returns to the start screen. `--preview connect|join|chat` shows a screen with sample content and no networking, for checking the UI.

- `Net/` holds the networking. `ChatHost` accepts any number of peers and relays every chat message to all of them, including the sender, so everyone sees the same order. `ChatClient` connects and sends a hello with its name. Frames are a 4 byte big-endian length followed by UTF-8 JSON (`FrameCodec`). Each session runs on background threads and queues `ChatEvent`s that the app drains on the main thread every frame.
- `Views/` holds the UI: `ConnectView` (start screen with host and join tabs), `ChatRoomView` (status bar, messages in a `ScrollListView` that follows the tail, input row) and `MessageRowView` (name and time above a bubble, own messages on the right). Building blocks: `CardView` (rounded rect with border and a shadow made of stacked translucent rects), `ActionButton` (hover and pressed colors, centered label), `InputCard` and `LineInputView` (a `TextInputBoxView` with a placeholder that sends on Enter), `StatusDot`, `SpacerView` and the palette in `Ui`.
- There is no NAT traversal, encryption or authentication, so it works on a LAN or loopback only. Names are not checked for duplicates.

References: Examples.Common.
