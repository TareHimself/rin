using Rin.Shade.Transpiler;

namespace Rin.Shade.Tests.Emitter;

public class PropertySetterTests
{
    private const string Source = """
        using System.Numerics;
        using Rin.Shade;

        namespace SetterCheck;

        public struct Point
        {
            private Vector4 _data;

            public Vector3 Location
            {
                get => new(_data.X, _data.Y, _data.Z);
                set
                {
                    _data.X = value.X;
                    _data.Y = value.Y;
                    _data.Z = value.Z;
                }
            }

            public float Scale { get; set; }
        }

        public struct SetterPush
        {
            public Point Point;
            public BufferRef<float> Output;
        }

        [Shader("Fixtures/setter.slang")]
        public class SetterShader : Shader
        {
            [Push] protected SetterPush Push;

            [Compute(1, 1, 1)]
            public void Compute()
            {
                var point = Push.Point;
                point.Location = new Vector3(1f, 2f, 3f);
                point.Scale = 2f;
                Push.Output[0] = point.Scale + point.Location.X;
            }
        }
        """;

    private static string Emit()
    {
        var result = ShadeEmitter.Emit(CompilationBuilder.Build(Source));
        Assert.That(result.Diagnostics, Is.Empty);
        return result.Shaders["SetterShader"].Replace("\r\n", "\n");
    }

    [Test]
    public void SetterBodyLowersToAMutatingMethodAndItsCall()
    {
        var slang = Emit();

        Assert.That(slang, Does.Contain("[mutating]\n    void setLocation(float3 value)"));
        Assert.That(slang, Does.Contain("this._data.x = value.x;"));
        Assert.That(slang, Does.Contain("point.setLocation(float3(1, 2, 3));"));
    }

    [Test]
    public void AutoPropertyAssignmentWritesTheBackingField()
    {
        var slang = Emit();

        Assert.That(slang, Does.Contain("point.scale = 2;"));
        Assert.That(slang, Does.Not.Contain("setScale"));
    }
}
