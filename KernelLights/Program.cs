using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

You are an AI assistant modeled after Alfred Pennyworth.

Alfred's tone remains courteous and inviting even in his most regal declarations – maintaining that perfect
balance between professionalism, born from tradition, but also reflective of the modern world we live in. His voice
embodies an unwavering commitment to service intertwined with a sense of timeless wisdom

Address the user, Rob Foulkrod, using variations on "Sir," "Robert," or "Mr. Foulkrod"

Your primary functions include managing home automation tasks, specifically controlling the lights in the house (both on/off and brightness levels stored as a number between 0 and 255). 
Provide reminders and advice related to these tasks, ensuring a friendly and supportive tone with enough elaboration to showcase your personality.

Begin with a quick greeting.
""";
var builder = Kernel.CreateBuilder();

//var model = "phi3";
//var uri = "http://localhost:11434";
//var key = "";

//builder.AddOpenAIChatCompletion(
//                        modelId: model,
//                        endpoint: new Uri(uri),
//                        apiKey: key);

var model = configuration["OPENAI:MODEL"] ?? throw new Exception("OPENAI:MODEL not configured.");
var uri =   configuration["OPENAI:URI"]   ?? throw new Exception("OPENAI:URI not configured.");
var key =   configuration["OPENAI:KEY"]   ?? throw new Exception("OPENAI:KEY not configured.");

// Create a kernel with Azure OpenAI chat completion
builder.Services.AddAzureOpenAIChatCompletion(
    deploymentName: model,
    apiKey: key,
    endpoint: uri,
    modelId: model

);



// Add enterprise components
builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

builder.Plugins.AddFromType<BrightnessConverterPlugin>("brightness_converter");
builder.Plugins.AddFromObject(new TimePlugin());
//builder.Plugins.AddFromType<TimeInformationPlugin>("time_information");

// Build the kernel
Kernel kernel = builder.Build();


var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

//Add a plugin (the LightsPlugin class is defined below)
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
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};

// Create a history store the conversation
var chatHistory = new ChatHistory(SYSTEM_PROMPT);

var result = await chatCompletionService.GetChatMessageContentAsync(
    chatHistory,
    executionSettings: openAIPromptExecutionSettings,
    kernel: kernel);

Console.WriteLine("Assistant > " + result);

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
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Assistant > " + result);
    Console.ResetColor();

    // Add the message from the agent to the chat history
    chatHistory.AddMessage(result.Role, result.Content ?? string.Empty);
} while (userInput is not null);



