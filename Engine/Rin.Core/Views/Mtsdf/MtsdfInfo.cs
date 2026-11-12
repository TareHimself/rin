using Rin.Core.Graphics;
using Rin.Core.Shared;

namespace Rin.Core.Views.Mtsdf;

public struct MtsdfInfo
{
    public int Id;
    public MtsdfState State;
    public ResourceHandle Atlas;
    public RectUint Rect;
}
