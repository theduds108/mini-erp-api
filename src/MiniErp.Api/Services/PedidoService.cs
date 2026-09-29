using MiniErp.Api.Contracts;
using MiniErp.Api.Data;
using MiniErp.Api.Domain;

namespace MiniErp.Api.Services;

/// <summary>
/// Regras de negócio de pedidos de venda: validação de cliente, estoque e crédito,
/// baixa de estoque na inclusão e estorno no cancelamento.
/// </summary>
public sealed class PedidoService(ErpStore store, TimeProvider relogio)
{
    public Resultado<Pedido> Incluir(NovoPedidoRequest requisicao)
    {
        if (requisicao.Itens is null || requisicao.Itens.Count == 0)
            return Resultado<Pedido>.Falha(TipoErro.Validacao, "O pedido precisa ter ao menos um item.");

        if (requisicao.Itens.Any(i => i.Quantidade <= 0))
            return Resultado<Pedido>.Falha(TipoErro.Validacao, "Todas as quantidades devem ser maiores que zero.");

        lock (store.Lock)
        {
            var cliente = store.BuscarCliente(requisicao.CodigoCliente);
            if (cliente is null)
                return Resultado<Pedido>.Falha(TipoErro.NaoEncontrado, $"Cliente {requisicao.CodigoCliente} não encontrado.");

            if (cliente.Bloqueado)
                return Resultado<Pedido>.Falha(TipoErro.Conflito, $"Cliente {cliente.Codigo} está bloqueado para faturamento.");

            // Agrupa itens repetidos do mesmo produto para validar o estoque pelo total solicitado.
            var solicitados = requisicao.Itens
                .GroupBy(i => i.CodigoProduto.ToUpperInvariant())
                .Select(g => new { Codigo = g.Key, Quantidade = g.Sum(i => i.Quantidade) })
                .ToList();

            var itens = new List<ItemPedido>();
            foreach (var solicitado in solicitados)
            {
                var produto = store.BuscarProduto(solicitado.Codigo);
                if (produto is null || !produto.Ativo)
                    return Resultado<Pedido>.Falha(TipoErro.NaoEncontrado, $"Produto {solicitado.Codigo} não encontrado ou inativo.");

                if (produto.Estoque < solicitado.Quantidade)
                    return Resultado<Pedido>.Falha(
                        TipoErro.Conflito,
                        $"Estoque insuficiente para {produto.Codigo}: disponível {produto.Estoque}, solicitado {solicitado.Quantidade}.");

                itens.Add(new ItemPedido
                {
                    Item = itens.Count + 1,
                    CodigoProduto = produto.Codigo,
                    Descricao = produto.Descricao,
                    Quantidade = solicitado.Quantidade,
                    PrecoUnitario = produto.PrecoVenda,
                });
            }

            var total = itens.Sum(i => i.Total);
            var emAberto = store.Pedidos
                .Where(p => p.CodigoCliente == cliente.Codigo && p.Status == StatusPedido.Aberto)
                .Sum(p => p.Total);

            if (emAberto + total > cliente.LimiteCredito)
                return Resultado<Pedido>.Falha(
                    TipoErro.Conflito,
                    $"Limite de crédito excedido: limite {cliente.LimiteCredito:N2}, em aberto {emAberto:N2}, pedido {total:N2}.");

            // Todas as validações passaram: baixa o estoque e grava o pedido.
            foreach (var item in itens)
                store.BuscarProduto(item.CodigoProduto)!.Estoque -= item.Quantidade;

            var pedido = new Pedido
            {
                Numero = store.ProximoNumeroPedido(),
                CodigoCliente = cliente.Codigo,
                EmitidoEm = relogio.GetLocalNow().DateTime,
                Itens = itens,
            };
            store.AdicionarPedido(pedido);

            return Resultado<Pedido>.Ok(pedido);
        }
    }

    public Resultado<Pedido> Faturar(string numero)
    {
        lock (store.Lock)
        {
            var pedido = store.BuscarPedido(numero);
            if (pedido is null)
                return Resultado<Pedido>.Falha(TipoErro.NaoEncontrado, $"Pedido {numero} não encontrado.");

            if (pedido.Status != StatusPedido.Aberto)
                return Resultado<Pedido>.Falha(TipoErro.Conflito, $"Apenas pedidos em aberto podem ser faturados (status atual: {pedido.Status}).");

            pedido.Status = StatusPedido.Faturado;
            return Resultado<Pedido>.Ok(pedido);
        }
    }

    public Resultado<Pedido> Cancelar(string numero)
    {
        lock (store.Lock)
        {
            var pedido = store.BuscarPedido(numero);
            if (pedido is null)
                return Resultado<Pedido>.Falha(TipoErro.NaoEncontrado, $"Pedido {numero} não encontrado.");

            if (pedido.Status != StatusPedido.Aberto)
                return Resultado<Pedido>.Falha(TipoErro.Conflito, $"Apenas pedidos em aberto podem ser cancelados (status atual: {pedido.Status}).");

            // Estorna o estoque reservado pelo pedido.
            foreach (var item in pedido.Itens)
            {
                var produto = store.BuscarProduto(item.CodigoProduto);
                if (produto is not null) produto.Estoque += item.Quantidade;
            }

            pedido.Status = StatusPedido.Cancelado;
            return Resultado<Pedido>.Ok(pedido);
        }
    }
}
