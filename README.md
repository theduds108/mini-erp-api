# Mini ERP API

API REST em **C# / ASP.NET Core 8** que simula a camada de integração de um ERP: cadastro de produtos e clientes e o ciclo completo de pedidos de venda, com as validações que um sistema real precisa aplicar antes de aceitar um pedido.

O objetivo é demonstrar como um sistema externo (e-commerce, força de vendas, app mobile) conversa com o ERP da empresa por meio de uma API segura, previsível e com respostas de erro padronizadas.

## Regras de negócio implementadas

Ao incluir um pedido, a API valida, nesta ordem:

1. Existência do cliente e se ele não está bloqueado para faturamento
2. Existência e situação dos produtos
3. Estoque disponível (itens repetidos do mesmo produto são somados antes da validação)
4. Limite de crédito do cliente, considerando os pedidos que ele já tem em aberto

Se tudo estiver correto, o estoque é baixado e o pedido é gravado. O pedido segue o ciclo **Aberto → Faturado** ou **Aberto → Cancelado**; no cancelamento, o estoque é estornado. Todas as operações de escrita são protegidas por lock, garantindo consistência do estoque sob requisições simultâneas.

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| GET | `/health` | Status do serviço (público) |
| GET | `/api/produtos` | Lista produtos (`?busca=` e `?somenteComEstoque=true`) |
| GET | `/api/produtos/{codigo}` | Consulta um produto |
| POST | `/api/produtos` | Cadastra produto |
| POST | `/api/produtos/{codigo}/ajuste-estoque` | Entrada ou saída manual de estoque, com motivo |
| GET | `/api/clientes` | Lista clientes |
| GET | `/api/clientes/{codigo}` | Consulta um cliente |
| GET | `/api/pedidos` | Lista pedidos (`?status=` e `?cliente=`) |
| GET | `/api/pedidos/{numero}` | Consulta um pedido |
| POST | `/api/pedidos` | Inclui pedido de venda |
| POST | `/api/pedidos/{numero}/faturar` | Fatura pedido em aberto |
| POST | `/api/pedidos/{numero}/cancelar` | Cancela pedido em aberto e estorna estoque |

As rotas em `/api` exigem o cabeçalho `X-Api-Key`. A chave é configurada em `appsettings.json` (ou pela variável de ambiente `Seguranca__ApiKey`).

### Exemplo de erro

Erros seguem o padrão **Problem Details (RFC 7807)**:

```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "Regra de negócio violada",
  "status": 422,
  "detail": "Estoque insuficiente para PA0005: disponível 0, solicitado 1."
}
```

| Status | Quando |
|---|---|
| 400 | Dados inválidos na requisição |
| 401 | Chave de API ausente ou inválida |
| 404 | Cliente, produto ou pedido inexistente |
| 409 | Código de produto já cadastrado |
| 422 | Regra de negócio violada (estoque, crédito, cliente bloqueado, status do pedido) |

## Arquitetura

```
src/MiniErp.Api/
├── Domain/        entidades (Produto, Cliente, Pedido, ItemPedido)
├── Contracts/     modelos de requisição
├── Data/          armazenamento em memória com dados de exemplo
├── Services/      regras de negócio de pedidos e tipo Resultado
├── Endpoints/     rotas agrupadas por recurso (Minimal APIs)
├── Security/      filtro de autenticação por chave de API
└── Program.cs     composição da aplicação
```

Decisões principais:

- **Minimal APIs** com grupos de rotas, sem dependência de pacotes externos.
- **Regras de negócio isoladas** em `PedidoService`, sem conhecimento de HTTP. A conversão para status HTTP fica em um único ponto (`Endpoints/Extensoes.cs`).
- **Tipo `Resultado<T>`** para fluxos de erro esperados, em vez de exceções.
- **Comparação da chave de API em tempo constante**, evitando ataques de temporização.
- **`TimeProvider` injetado**, o que permite controlar a data nos testes.
- Build configurado com **warnings tratados como erros**.

## Como executar

Requer o [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/MiniErp.Api
```

A API sobe em `http://localhost:5073`. O arquivo `src/MiniErp.Api/MiniErp.Api.http` contém uma coleção de requisições prontas para o VS Code (extensão REST Client), Visual Studio ou Rider.

## Testes

O script `scripts/smoke-test.sh` executa 21 verificações de ponta a ponta contra a API em execução, cobrindo autenticação, validações, regras de estoque e crédito e o ciclo de vida do pedido.

```bash
./scripts/smoke-test.sh
```

## Próximos passos

- Persistência com Entity Framework Core e SQL Server
- Documentação OpenAPI/Swagger
- Testes de unidade para `PedidoService` com xUnit
- Paginação nas listagens
