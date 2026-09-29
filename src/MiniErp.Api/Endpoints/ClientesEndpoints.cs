using MiniErp.Api.Data;

namespace MiniErp.Api.Endpoints;

public static class ClientesEndpoints
{
    public static RouteGroupBuilder MapClientes(this RouteGroupBuilder api)
    {
        var grupo = api.MapGroup("/clientes");

        grupo.MapGet("/", (ErpStore store) =>
        {
            lock (store.Lock)
                return Results.Ok(store.Clientes.OrderBy(c => c.Codigo).ToList());
        });

        grupo.MapGet("/{codigo}", (ErpStore store, string codigo) =>
        {
            lock (store.Lock)
            {
                var cliente = store.BuscarCliente(codigo);
                return cliente is null
                    ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Recurso não encontrado",
                        detail: $"Cliente {codigo} não encontrado.")
                    : Results.Ok(cliente);
            }
        });

        return api;
    }
}
