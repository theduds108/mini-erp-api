using MiniErp.Api.Domain;

namespace MiniErp.Api.Data;

/// <summary>
/// Armazenamento em memória que simula as tabelas do ERP.
/// Todas as operações de escrita passam pelo mesmo lock para manter o estoque consistente
/// quando várias requisições chegam ao mesmo tempo.
/// </summary>
public sealed class ErpStore
{
    private readonly Dictionary<string, Produto> _produtos = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Cliente> _clientes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Pedido> _pedidos = new(StringComparer.OrdinalIgnoreCase);
    private int _ultimoPedido;

    public object Lock { get; } = new();

    public ErpStore()
    {
        Semear();
    }

    public IReadOnlyCollection<Produto> Produtos => _produtos.Values;
    public IReadOnlyCollection<Cliente> Clientes => _clientes.Values;
    public IReadOnlyCollection<Pedido> Pedidos => _pedidos.Values;

    public Produto? BuscarProduto(string codigo) => _produtos.GetValueOrDefault(codigo);
    public Cliente? BuscarCliente(string codigo) => _clientes.GetValueOrDefault(codigo);
    public Pedido? BuscarPedido(string numero) => _pedidos.GetValueOrDefault(numero);

    public bool AdicionarProduto(Produto produto) => _produtos.TryAdd(produto.Codigo, produto);

    public string ProximoNumeroPedido() => (++_ultimoPedido).ToString("D6");

    public void AdicionarPedido(Pedido pedido) => _pedidos[pedido.Numero] = pedido;

    private void Semear()
    {
        Produto[] produtos =
        [
            new() { Codigo = "PA0001", Descricao = "Cabo flexível 2,5mm 100m", Unidade = "RL", PrecoVenda = 289.90m, Estoque = 40 },
            new() { Codigo = "PA0002", Descricao = "Disjuntor monopolar 20A", Unidade = "UN", PrecoVenda = 18.50m, Estoque = 320 },
            new() { Codigo = "PA0003", Descricao = "Tomada 2P+T 20A", Unidade = "UN", PrecoVenda = 12.90m, Estoque = 500 },
            new() { Codigo = "PA0004", Descricao = "Luminária LED 18W", Unidade = "UN", PrecoVenda = 64.00m, Estoque = 75 },
            new() { Codigo = "PA0005", Descricao = "Eletroduto corrugado 3/4 50m", Unidade = "RL", PrecoVenda = 79.90m, Estoque = 0 },
        ];

        Cliente[] clientes =
        [
            new() { Codigo = "C00001", RazaoSocial = "Construtora Horizonte Ltda", Documento = "11.222.333/0001-81", Cidade = "Bauru", Uf = "SP", LimiteCredito = 50_000m },
            new() { Codigo = "C00002", RazaoSocial = "Elétrica Central ME", Documento = "44.555.666/0001-02", Cidade = "Marília", Uf = "SP", LimiteCredito = 8_000m },
            new() { Codigo = "C00003", RazaoSocial = "Comercial Pioneira Ltda", Documento = "77.888.999/0001-33", Cidade = "Jaú", Uf = "SP", LimiteCredito = 15_000m, Bloqueado = true },
        ];

        foreach (var p in produtos) _produtos[p.Codigo] = p;
        foreach (var c in clientes) _clientes[c.Codigo] = c;
    }
}
