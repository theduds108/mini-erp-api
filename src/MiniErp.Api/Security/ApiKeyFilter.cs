using System.Security.Cryptography;
using System.Text;

namespace MiniErp.Api.Security;

/// <summary>
/// Autenticação simples por chave de API enviada no cabeçalho X-Api-Key,
/// padrão comum em integrações entre sistemas.
/// </summary>
public sealed class ApiKeyFilter(IConfiguration configuracao) : IEndpointFilter
{
    public const string Cabecalho = "X-Api-Key";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext contexto, EndpointFilterDelegate proximo)
    {
        var esperada = configuracao["Seguranca:ApiKey"];
        var recebida = contexto.HttpContext.Request.Headers[Cabecalho].ToString();

        if (string.IsNullOrEmpty(esperada) || !ChavesIguais(esperada, recebida))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Não autorizado",
                detail: $"Informe uma chave válida no cabeçalho {Cabecalho}.");
        }

        return await proximo(contexto);
    }

    // Comparação em tempo constante para não vazar informação pelo tempo de resposta.
    private static bool ChavesIguais(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
