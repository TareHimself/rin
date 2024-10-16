#pragma once
class WindowModule;

struct WindowCreateOptions
{
private:
    bool _resizable = false;

    bool _borderless = false;

    bool _focused = false;

    bool _tooltip = false;

    bool _popup = false;

protected:
    friend WindowModule;

    [[nodiscard]] int Apply() const;

public:
    WindowCreateOptions& Resizable(bool newState = true);
    WindowCreateOptions& Borderless(bool newState = true);
    WindowCreateOptions& Focused(bool newState = true);
    WindowCreateOptions& Tooltip(bool newState = true);
    WindowCreateOptions& Popup(bool newState = true);
};
