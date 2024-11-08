using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using rin.Core;
using rin.Core.Extensions;
using rin.Graphics;
using rin.Core.Math;
using rin.Widgets.Enums;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Quads;
using rin.Widgets.Sdf;
using rsl;
using SixLabors.Fonts;

namespace rin.Widgets.Content;
internal struct CachedQuadLayout(int atlas,Matrix3 transform, Vector2<float> size, Vector4<float> uv)
{
    public readonly int Atlas = atlas;
    public Matrix3 Transform = transform;
    public Vector2<float> Size = size;
    public Vector4<float> UV = uv;
}


/// <summary>
///     Draw's text using an <see cref="MtsdfFont" />. Currently, hardcoded to
///     <a href="https://fonts.google.com/specimen/Roboto">Roboto</a>.
/// </summary>
public class TextBox : Widget
{
    
    protected class CharacterBounds
    {
        public readonly char Character;
        public readonly int ContentIndex;
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public float Right => X + Width;
        public float Left => X;
        public float Top => Y;
        public float Bottom => Y + Height;

        public CharacterBounds(char character,int contentIndex, float x, float y, float width, float height)
        {
            Character = character;
            ContentIndex = contentIndex;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public CharacterBounds(char character,int contentIndex, GlyphBounds bounds) : this(character,contentIndex,bounds.Bounds.X,bounds.Bounds.Y,bounds.Bounds.Width,bounds.Bounds.Height)
        {
            
        }

    }
    
    private string _content;
    private Font? _latestFont;
    private SdfFont? _mtsdf;
    private CachedQuadLayout[]? _cachedLayouts;
    private CharacterBounds[]? _cachedBounds;
    private float _fontSize = 100.0f;

    protected float LineHeight => _latestFont?.FontMetrics is { } metrics
        ? (metrics.HorizontalMetrics.AdvanceHeightMax * 64 / metrics.ScaleFactor * FontSize)
        : 0;
    
    

    public TextBox(string inContent = "", float inFontSize = 100f,string fontFamily = "Arial")
    {
        FontSize = inFontSize;
        var gs = SGraphicsModule.Get();
        _content = inContent;
        _cachedLayouts = null;
        _cachedBounds = null;
        SWidgetsModule.Get().GetOrCreateFont(fontFamily).After(msdf =>
        {
            _mtsdf = msdf;
            
            MakeNewFont();
        });
    }


    public Color ForegroundColor { get; set; } = Color.White;

    public Color BackgroundColor { get; set; } = Color.White.Clone(a: 0.0f);
    
    public float FontSize
    {
        get => _fontSize;
        set
        {
            _cachedLayouts = null;
            _fontSize = value;
        }
    }
    

    public string Content
    {
        get => _content;
        set
        {
            var hasChanged = _content != value;
            if (hasChanged)
            {
                TextChanged(value);
            }
        }
    }

    protected virtual void TextChanged(string newText)
    {
        _cachedLayouts = null;
        _cachedBounds = null;
        _content = newText;
        Invalidate(InvalidationType.DesiredSize);
    }

    
    protected bool FontReady => _mtsdf != null && _latestFont != null;

    protected override Vector2<float> LayoutContent(Vector2<float> availableSpace)
    {
        float? wrap = float.IsFinite(availableSpace.X) ? availableSpace.X : null;
        var bounds = GetCharacterBounds(wrap).ToArray();
        var width = bounds.MaxBy(c => c.Right)?.Right ?? 0.0f;
        var height = bounds.MaxBy(c => c.Bottom)?.Bottom ?? 0.0f;
        return new Vector2<float>(width,height);
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _mtsdf?.Dispose();
    }

    private void MakeNewFont()
    {
        _latestFont = _mtsdf?.GetFontFamily().CreateFont(FontSize, FontStyle.Regular);
        _cachedBounds = null;
        _cachedLayouts = null;
        Invalidate(InvalidationType.DesiredSize);
    }

    private object _boundsLock = new object();

    protected IEnumerable<CharacterBounds> GetCharacterBounds(float? wrap = null,bool cache = true)
    {
        lock (_boundsLock)
        {
            if (_latestFont == null)
            {
                return [];
            }

            // if (cache && _cachedBounds != null)
            // {
            //     return _cachedBounds;
            // }
        
            var opts = new TextOptions(_latestFont)
            {
                WrappingLength = wrap.GetValueOrDefault(-1)
            };

            var content = Content + "";
            TextMeasurer.TryMeasureCharacterBounds(content, opts, out var tempBounds);
            if (tempBounds.IsEmpty) return [];
            var boundsIdx = 0;
            var allBounds = tempBounds.ToArray();
            var line = 0;
            
            var result = content.Select((c,idx) =>
            {
                if (c is not ( '\n' or '\r'))
                {
                
                    var result = new CharacterBounds(c,idx, allBounds[boundsIdx]);
                    line = (int)Math.Floor(result.Y / LineHeight);
                    boundsIdx++;
                    return result;
                }

                if (c == '\n')
                {
                    line++;
                }

                return new CharacterBounds(c, idx,0, (line * LineHeight) + LineHeight / 2.0f, 0, 0);
            }).ToArray();
            // if (cache)
            // {
            //     _cachedBounds = result;
            // }
            //
            return result;
        }
    }

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        if (Content.Empty() || _latestFont == null) return new Vector2<float>(0.0f,LineHeight);
        CharacterBounds? last = GetCharacterBounds(cache: false).MaxBy(c => c.Right);
        var lines = Math.Max(1,Content.Split("\n").Length);
        var height = LineHeight * lines;

        return new Vector2<float>(last?.Right ?? 0.0f, height);
    }

    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        if (!FontReady) return;
        if (Content.NotEmpty() && _cachedLayouts == null)
        {
            List<CachedQuadLayout> layouts = [];
            foreach (var bound in GetCharacterBounds(GetContentSize().X))
            {
                var charInfo = _mtsdf?.GetGlyphInfo(bound.Character);
                
                if(charInfo == null) continue;
                
                var atlasId = _mtsdf?.GetAtlasTextureId(charInfo.AtlasIdx);
                
                if (atlasId == null) continue;

                var charOffset = new Vector2<float>(bound.X,
                    bound.Y);
                
                var charSize = new Vector2<float>(bound.Width, bound.Height);
                
                //if (!charRect.IntersectsWith(drawInfo.Clip)) continue;
                //if(!charRect.Offset.Within(drawInfo.Clip) && !(charRect.Offset + charRect.Size).Within(drawInfo.Clip)) continue;

                var finalTransform = Matrix3.Identity.Translate(charOffset)
                    .Translate(new Vector2<float>(0.0f, charSize.Y)).Scale(new Vector2<float>(1.0f, -1.0f));
                var size = new Vector2<float>(bound.Width, bound.Height);
                var layout = new CachedQuadLayout(atlasId.Value, finalTransform, size, charInfo.Coordinates);
                layouts.Add(layout);
                drawCommands.AddSdf(layout.Atlas,info.Transform * layout.Transform,layout.Size,Color.White,layout.UV);
            }
            
            _cachedLayouts = layouts.ToArray();
        }
        else if(_cachedLayouts != null)
        {
            drawCommands.Add(new QuadDrawCommand(_cachedLayouts.Select(c => Quad.NewSdf(c.Atlas,info.Transform * c.Transform,c.Size,Color.White,c.UV))));
        }
    }


   
}