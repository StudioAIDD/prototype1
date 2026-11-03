using System.ClientModel;
using AgentFrameworkProto1;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAGUIServer();

var baseUrl = Environment.GetEnvironmentVariable("LITELLM_BASE_URL")
    ?? throw new InvalidOperationException("Set LITELLM_BASE_URL");
var modelName = Environment.GetEnvironmentVariable("LITELLM_MODEL") ?? "chatgpt/gpt-6.1-sol";
var apiKey = Environment.GetEnvironmentVariable("LITELLM_API_KEY") ?? "admin";

var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };
ChatClient chatClient = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions)
    .GetChatClient(modelName);

AIAgent agent = chatClient.AsAIAgent(
    instructions: "You are a friendly assistant. Keep your answers brief. "
        + "You have access to Kutztown University's policy register via the "
        + "read_index and read_policy tools.",
    name: "Proto1Agent",
    tools: [
        AIFunctionFactory.Create(PolicyTools.ReadIndex),
        AIFunctionFactory.Create(PolicyTools.ReadPolicy),
    ]);

var sessionConnectionString = Environment.GetEnvironmentVariable("AGENT_MEMORY_DB_CONNECTION_STRING")
    ?? "Host=localhost;Port=5433;Database=agent_memory;Username=agent_memory;Password=agent_memory";
var sessionStore = new PostgresAgentSessionStore(sessionConnectionString);

var hostedAgent = builder.AddAIAgent("Proto1Agent", (_, _) => agent)
    .WithSessionStore(sessionStore, withIsolation: false);

var app = builder.Build();

app.MapAGUIServer(hostedAgent, "/");

app.Run();
