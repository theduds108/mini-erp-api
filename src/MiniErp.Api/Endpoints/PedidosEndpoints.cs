using MiniErp.Api.Contracts;
using MiniErp.Api.Data;
using MiniErp.Api.Domain;
using MiniErp.Api.Services;

namespace MiniErp.Api.Endpoints;

public static class PedidosEndpoints
{
    public static RouteGroupBuilder MapPedidos(this RouteGroupBuilder api)
    {
        var grupo = api.MapGroup("/pedidos");

        grupo.MapGet("/", (ErpStore store, StatusPedido? status, string? cliente) =>
        {
            lock (store.Lock)
            {
                IEnumerable<Pedido> consulta = store.Pedidos;
                if (status is not null) consulta = consulta.Where(p => p.Status == status);
                if (!string.IsNullOrWhiteSpace(cliente))
                    consulta = consulta.Where(p => p.CodigoCliente.Equals(cliente, StringComparison.OrdinalIgnoreCase));

                return Results.Ok(consulta.OrderByDescending(p => p.Numero).ToList());
            }
        });

        grupo.MapGet("/{numero}", (ErpStore store, string numero) =>
        {
            lock (store.Lock)
            {
                var pedido = store.BuscarPedido(numero);
                return pedido is null
                    ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Recurso não encontrado",
                        detail: $"Pedido {numero} não encontrado.")
                    : Results.Ok(pedido);
            }
        });

        grupo.MapPost("/", (PedidoService servico, NovoPedidoRequest req) =>
            servico.Incluir(req).ParaHttp(p => Results.Created($"/api/pedidos/{p.Numero}", p)));

        grupo.MapPost("/{numero}/faturar", (PedidoService servico, string numero) =>
            servico.Faturar(numero).ParaHttp(Results.Ok));

        grupo.MapPost("/{numero}/cancelar", (PedidoService servico, string numero) =>
            servico.Cancelar(numero).ParaHttp(Results.Ok));

        return api;
    }
}
