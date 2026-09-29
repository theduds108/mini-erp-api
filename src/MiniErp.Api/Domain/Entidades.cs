namespace MiniErp.Api.Domain;

public sealed class Produto
{
    public required string Codigo { get; init; }
    public required string Descricao { get; set; }
    public required string Unidade { get; init; }
    public decimal PrecoVenda { get; set; }
    public decimal Estoque { get; set; }
    public bool Ativo { get; set; } = true;
}

public sealed class Cliente
{
    public required string Codigo { get; init; }
    public required string RazaoSocial { get; init; }
    public required string Documento { get; init; }
    public required string Cidade { get; init; }
    public required string Uf { get; init; }
    public decimal LimiteCredito { get; init; }
    public bool Bloqueado { get; init; }
}

public enum StatusPedido
{
    Aberto,
    Faturado,
    Cancelado,
}

public sealed class ItemPedido
{
    public required int Item { get; init; }
    public required string CodigoProduto { get; init; }
    public required string Descricao { get; init; }
    public decimal Quantidade { get; init; }
    public decimal PrecoUnitario { get; init; }
    public decimal Total => Math.Round(Quantidade * PrecoUnitario, 2);
}

public sealed class Pedido
{
    public required string Numero { get; init; }
    public required string CodigoCliente { get; init; }
    public DateTime EmitidoEm { get; init; }
    public StatusPedido Status { get; set; } = StatusPedido.Aberto;
    public List<ItemPedido> Itens { get; init; } = [];
    public decimal Total => Itens.Sum(i => i.Total);
}
