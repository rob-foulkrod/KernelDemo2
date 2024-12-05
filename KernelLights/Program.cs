using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.Plugins.OpenApi;

//create a configuration builder and add user secrets
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var SYSTEM_PROMPT = """
You are an AI assistant modeled after Batman's Alfred Pennyworth.
The AI should emulate the refined, intelligent, and slightly sardonic tone of Alfred Pennyworth, Batman’s loyal butler. 
The AI’s speech should reflect a balance of formal British etiquette with warm, paternal undertones and a hint of dry humor. 
The AI should convey wisdom and calm authority, using polite and sophisticated language, even when delivering witty or playful remarks. 
The AI should always be composed, respectful, and supportive, providing thoughtful, insightful advice and subtle, light-hearted commentary.
Address the user, Rob Foulkrod (Pronounce like 'Folk' music and fishing 'rod'), using variations on "Sir," "Robert," or "Mr. Foulkrod"
If I do not refer to a room directly, assume I mean the Office.
Your primary functions include managing home automation tasks, specifically controlling the lights in the house (both on/off and brightness levels). 
Provide reminders and advice related to these tasks, ensuring a friendly and supportive tone with enough elaboration to showcase your personality.
Begin with a quick greeting.
""";
var builder = Kernel.CreateBuilder();

//var model = "llama3.2:";
//var uri = "http://localhost:11111";
//var key = "";

//builder.AddOpenAIChatCompletion(
//                        modelId: model,
//                        endpoint: new Uri(uri),
//                        apiKey: key);

var model = configuration["OPENAI:MODEL"] ?? throw new Exception("OPENAI:MODEL not configured.");
var uri = configuration["OPENAI:URI"] ?? throw new Exception("OPENAI:URI not configured.");
var key = configuration["OPENAI:KEY"] ?? throw new Exception("OPENAI:KEY not configured.");

//Create a kernel with Azure OpenAI chat completion
builder.Services.AddAzureOpenAIChatCompletion(
    deploymentName: model,
    apiKey: key,
    endpoint: uri,
    modelId: model

);

// Add enterprise components

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddSimpleConsole(options =>
    {
        options.ColorBehavior = LoggerColorBehavior.Enabled;
        options.SingleLine = true;
    }).SetMinimumLevel(LogLevel.Trace);
});


builder.Plugins.AddFromType<BrightnessConverterPlugin>("brightness_converter");
builder.Plugins.AddFromType<ExitApplicationPlugin>("exit_Plugin");
builder.Plugins.AddFromObject(new TimePlugin());

// Build the kernel
Kernel kernel = builder.Build();

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

await kernel.ImportPluginFromOpenApiAsync(
   pluginName: "lights_api",
   uri: new Uri("https://localhost:7230/swagger/v1/swagger.json"),
   executionParameters: new OpenApiFunctionExecutionParameters()
   {
       EnablePayloadNamespacing = true
   }
);


// Enable planning
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

// Create a history store the conversation
var chatHistory = new ChatHistory(SYSTEM_PROMPT);

var result = await chatCompletionService.GetChatMessageContentAsync(
    chatHistory,
    executionSettings: openAIPromptExecutionSettings,
    kernel: kernel);

Console.ForegroundColor = ConsoleColor.DarkCyan;
Console.WriteLine("Alfred > " + result);
Console.ResetColor();

// Initiate a back-and-forth chat
string? userInput;
do
{

    // Collect user input
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("User > ");
    userInput = Console.ReadLine();
    Console.ResetColor();

    if (string.IsNullOrWhiteSpace(userInput))
    {
        break;
    }

    chatHistory.AddUserMessage(userInput);

    // Get the response from the AI
    result = await chatCompletionService.GetChatMessageContentAsync(
        chatHistory,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);

    // Print the results
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.WriteLine("Alfred > " + result);
    Console.ResetColor();

    // Add the message from the agent to the chat history
    chatHistory.AddMessage(result.Role, result.Content ?? string.Empty);
} while (userInput is not null);
