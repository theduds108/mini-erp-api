using System.Text.Json.Serialization;
using MiniErp.Api.Data;
using MiniErp.Api.Endpoints;
using MiniErp.Api.Security;
using MiniErp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ErpStore>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PedidoService>();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(opcoes =>
    opcoes.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Rotas públicas
app.MapGet("/", () => Results.Ok(new
{
    servico = "Mini ERP API",
    versao = "1.0.0",
    autenticacao = $"Envie o cabeçalho {ApiKeyFilter.Cabecalho} nas rotas /api/*",
    recursos = new[] { "/api/produtos", "/api/clientes", "/api/pedidos" },
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Rotas protegidas por chave de API
app.MapGroup("/api")
    .AddEndpointFilter<ApiKeyFilter>()
    .MapProdutos()
    .MapClientes()
    .MapPedidos();

app.Run();

// Permite referenciar a aplicação em testes de integração.
public partial class Program;
