using Examples.TextInput.Views;
using Rin.Core.Graphics;

namespace Examples.TextInput;

public sealed class TextInputExample : Example
{
    public override string Name => "text-input";

    public override string Title => "Text Input";

    public override Extent2D WindowSize => new(900, 640);

    public override void Start(ExampleContext context)
    {
        context.Add(new TextInputDemoView());
    }
}
