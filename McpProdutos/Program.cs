using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.Error.WriteLine("Iniciando Servidor MCP de Produtos...");

var builder = Host.CreateApplicationBuilder(args);

// Configura o logging para stderr, para não interferir
// com a comunicação stdio do MCP.
//builder.Logging.AddConsole(options =>
//        options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});


builder.Services
    .AddMcpServer()
    .WithStdioServerTransport() // Comunicação via Entrada/Saída Padrão
    .WithToolsFromAssembly();   // Descobre as ferramentas neste projeto

var app = builder.Build();

await app.RunAsync();

