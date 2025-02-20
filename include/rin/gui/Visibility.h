#pragma once
namespace rin::gui
{
    enum class Visibility
    {
        // visible and all hit tests
        Visible,

        // will not hit test this widget
        VisibleNoHitTestSelf,

        // will not hit test this widget's children
        VisibleNoHitTestChildren,

        // will be visible but not hit testable
        VisibleNoHitTestAll,

        // will take up space but not be visible or hit testable
        Hidden,

        // Will not take up space
        Collapsed
    };
}
