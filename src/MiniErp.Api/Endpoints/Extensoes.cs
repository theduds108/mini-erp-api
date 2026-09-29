using MiniErp.Api.Services;

namespace MiniErp.Api.Endpoints;

internal static class Extensoes
{
    /// <summary>Converte um Resultado de negócio na resposta HTTP adequada (RFC 7807 em caso de erro).</summary>
    public static IResult ParaHttp<T>(this Resultado<T> resultado, Func<T, IResult> sucesso)
    {
        if (resultado.Sucesso) return sucesso(resultado.Valor!);

        var (status, titulo) = resultado.Erro switch
        {
            TipoErro.NaoEncontrado => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            TipoErro.Validacao => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            TipoErro.Conflito => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada"),
            _ => (StatusCodes.Status500InternalServerError, "Erro inesperado"),
        };

        return Results.Problem(statusCode: status, title: titulo, detail: resultado.Mensagem);
    }
}
