namespace MiniErp.Api.Services;

public enum TipoErro
{
    NaoEncontrado,
    Validacao,
    Conflito,
}

/// <summary>
/// Resultado de uma operação de negócio: sucesso com valor ou falha com tipo e mensagem.
/// Evita usar exceções para fluxos esperados (estoque insuficiente, cliente bloqueado etc.).
/// </summary>
public sealed class Resultado<T>
{
    public T? Valor { get; }
    public TipoErro? Erro { get; }
    public string? Mensagem { get; }
    public bool Sucesso => Erro is null;

    private Resultado(T? valor, TipoErro? erro, string? mensagem)
    {
        Valor = valor;
        Erro = erro;
        Mensagem = mensagem;
    }

    public static Resultado<T> Ok(T valor) => new(valor, null, null);
    public static Resultado<T> Falha(TipoErro erro, string mensagem) => new(default, erro, mensagem);
}
