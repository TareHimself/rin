using Gpt4All;

namespace ChatApp;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var modelFactory = new Gpt4AllModelFactory();
        Console.WriteLine("Hello, World!");
        using var model = modelFactory.LoadModel(@"C:\Users\Taree\Downloads\mistral-7b-instruct-v0.1.Q4_0 (2).gguf");

        var prompt = "USER: How are you ?\nGPT:";
        var result = await model.GetStreamingPredictionAsync(
            prompt,
            PredictRequestOptions.Defaults);

        await foreach (var token in result.GetPredictionStreamingAsync()) Console.Write(token);
    }
}