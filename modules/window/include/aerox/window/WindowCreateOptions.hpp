#pragma once
class WindowModule;

struct WindowCreateOptions
{
private:
    bool _resizable = true;
        
    bool _borderless = false;

    bool _focused = true;
protected:
    friend WindowModule;

    int Apply() const;

public:
    WindowCreateOptions& Resizable(bool newState);
    WindowCreateOptions& Borderless(bool newState);
    WindowCreateOptions& Focused(bool newState);
};
