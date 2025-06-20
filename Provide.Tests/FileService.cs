namespace Provide.Tests;

public class FileService : IFileService
{
    public FileService(IProvider provider)
    {
        Console.WriteLine("FileService Created");
    }
}