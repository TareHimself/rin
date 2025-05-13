// See https://aka.ms/new-console-template for more information

using Rin.Engine.Graphics;

using var image = HostImage.Create(File.OpenRead(@"C:\Users\Taree\Downloads\channels4_profile.jpg"));
using var test = HostImage.Create(new Extent2D(1000, 1000), ImageFormat.RGBA8);
test.Mutate(ctx => { ctx.DrawImage(image, Offset2D.Zero); });
//image.Save(File.OpenWrite("./out.png"));
test.Save(File.OpenWrite("./out4.png"));
Console.WriteLine("Hello, World!");