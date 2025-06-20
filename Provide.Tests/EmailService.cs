namespace Provide.Tests;

public class EmailService : IEmailService
{
    private IFileService _fileService;
    public EmailService(IProvider provider)
    {
        Console.WriteLine("EmailService Created");
        _fileService = provider.Get<IFileService>();
    }
}