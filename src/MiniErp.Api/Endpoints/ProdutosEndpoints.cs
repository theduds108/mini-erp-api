using MiniErp.Api.Contracts;
using MiniErp.Api.Data;
using MiniErp.Api.Domain;

namespace MiniErp.Api.Endpoints;

public static class ProdutosEndpoints
{
    public static RouteGroupBuilder MapProdutos(this RouteGroupBuilder api)
    {
        var grupo = api.MapGroup("/produtos");

        grupo.MapGet("/", (ErpStore store, string? busca, bool somenteComEstoque = false) =>
        {
            lock (store.Lock)
            {
                IEnumerable<Produto> consulta = store.Produtos.Where(p => p.Ativo);

                if (!string.IsNullOrWhiteSpace(busca))
                    consulta = consulta.Where(p =>
                        p.Descricao.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                        p.Codigo.Contains(busca, StringComparison.OrdinalIgnoreCase));

                if (somenteComEstoque)
                    consulta = consulta.Where(p => p.Estoque > 0);

                return Results.Ok(consulta.OrderBy(p => p.Codigo).ToList());
            }
        });

        grupo.MapGet("/{codigo}", (ErpStore store, string codigo) =>
        {
            lock (store.Lock)
            {
                var produto = store.BuscarProduto(codigo);
                return produto is null ? NaoEncontrado(codigo) : Results.Ok(produto);
            }
        });

        grupo.MapPost("/", (ErpStore store, NovoProdutoRequest req) =>
        {
            var erros = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(req.Codigo)) erros["codigo"] = ["Obrigatório."];
            if (string.IsNullOrWhiteSpace(req.Descricao)) erros["descricao"] = ["Obrigatório."];
            if (string.IsNullOrWhiteSpace(req.Unidade)) erros["unidade"] = ["Obrigatório."];
            if (req.PrecoVenda < 0) erros["precoVenda"] = ["Não pode ser negativo."];
            if (req.Estoque < 0) erros["estoque"] = ["Não pode ser negativo."];
            if (erros.Count > 0) return Results.ValidationProblem(erros);

            var produto = new Produto
            {
                Codigo = req.Codigo.Trim().ToUpperInvariant(),
                Descricao = req.Descricao.Trim(),
                Unidade = req.Unidade.Trim().ToUpperInvariant(),
                PrecoVenda = req.PrecoVenda,
                Estoque = req.Estoque,
            };

            lock (store.Lock)
            {
                if (!store.AdicionarProduto(produto))
                    return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Produto já cadastrado",
                        detail: $"Já existe um produto com o código {produto.Codigo}.");
            }

            return Results.Created($"/api/produtos/{produto.Codigo}", produto);
        });

        grupo.MapPost("/{codigo}/ajuste-estoque", (ErpStore store, string codigo, AjusteEstoqueRequest req) =>
        {
            if (req.Quantidade == 0 || string.IsNullOrWhiteSpace(req.Motivo))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["ajuste"] = ["Informe uma quantidade diferente de zero e o motivo do ajuste."],
                });

            lock (store.Lock)
            {
                var produto = store.BuscarProduto(codigo);
                if (produto is null) return NaoEncontrado(codigo);

                if (produto.Estoque + req.Quantidade < 0)
                    return Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: "Regra de negócio violada",
                        detail: $"O ajuste deixaria o estoque de {produto.Codigo} negativo.");

                produto.Estoque += req.Quantidade;
                return Results.Ok(produto);
            }
        });

        return api;
    }

    private static IResult NaoEncontrado(string codigo) =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Recurso não encontrado",
            detail: $"Produto {codigo} não encontrado.");
}
