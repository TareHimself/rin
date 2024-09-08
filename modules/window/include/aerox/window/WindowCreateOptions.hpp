#pragma once
namespace aerox::window
{
    class WindowModule;

    struct WindowCreateOptions
    {
    private:
        bool _resizable = true;

        bool _visible = true;

        bool _decorated = true;

        bool _focused = true;

        bool _floating = false;

        bool _maximized = false;

        bool _cursorCentered = false;
    protected:
        friend WindowModule;

        void Apply() const;

    public:
        WindowCreateOptions& Resizable(bool newState);
        WindowCreateOptions& Visible(bool newState);
        WindowCreateOptions& Decorated(bool newState);
        WindowCreateOptions& Focused(bool newState);
        WindowCreateOptions& Floating(bool newState);
        WindowCreateOptions& Maximized(bool newState);
        WindowCreateOptions& CursorCentered(bool newState);
    };
}
