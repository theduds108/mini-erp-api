namespace MiniErp.Api.Contracts;

public sealed record NovoProdutoRequest(string Codigo, string Descricao, string Unidade, decimal PrecoVenda, decimal Estoque);

public sealed record AjusteEstoqueRequest(decimal Quantidade, string Motivo);

public sealed record NovoPedidoRequest(string CodigoCliente, List<NovoItemPedidoRequest> Itens);

public sealed record NovoItemPedidoRequest(string CodigoProduto, decimal Quantidade);
