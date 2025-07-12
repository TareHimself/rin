// See https://aka.ms/new-console-template for more information

using Provide;
using Provide.Tests;

var provider = new DefaultProvider();
    
    provider
    .AddSingle<IEmailService>(p => new EmailService(p))
    .AddSingle<IFileService>(p => new FileService(p));

provider.Get<IEmailService>();
Console.WriteLine("Hello, World!");
while (true)
{
    Thread.Sleep(100);
}