#!/usr/bin/env bash
# Teste de ponta a ponta da API. Requer a API rodando (dotnet run) e curl + jq.
# Uso: ./scripts/smoke-test.sh [url-base] [api-key]

set -euo pipefail

BASE="${1:-http://localhost:5073}"
KEY="${2:-troque-esta-chave-em-producao}"
FALHAS=0

req() { # metodo caminho [corpo] -> imprime "status corpo"
    local metodo=$1 caminho=$2 corpo=${3:-}
    if [[ -n "$corpo" ]]; then
        curl -s -o /tmp/resp.json -w "%{http_code}" -X "$metodo" "$BASE$caminho" \
            -H "X-Api-Key: $KEY" -H "Content-Type: application/json" -d "$corpo"
    else
        curl -s -o /tmp/resp.json -w "%{http_code}" -X "$metodo" "$BASE$caminho" -H "X-Api-Key: $KEY"
    fi
}

verificar() { # descricao esperado obtido
    if [[ "$2" == "$3" ]]; then
        printf "  ok    %s\n" "$1"
    else
        printf "  FALHA %s (esperado %s, obtido %s)\n" "$1" "$2" "$3"
        FALHAS=$((FALHAS + 1))
    fi
}

echo "Autenticação"
verificar "rota protegida sem chave retorna 401" 401 "$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/produtos")"
verificar "rota pública /health responde" 200 "$(curl -s -o /dev/null -w "%{http_code}" "$BASE/health")"

echo "Produtos"
verificar "lista produtos" 200 "$(req GET /api/produtos)"
verificar "filtra somente com estoque (4 de 5)" 4 "$(req GET '/api/produtos?somenteComEstoque=true' >/dev/null; jq length /tmp/resp.json)"
verificar "cadastra produto" 201 "$(req POST /api/produtos '{"codigo":"PA0100","descricao":"Interruptor simples","unidade":"UN","precoVenda":9.9,"estoque":50}')"
verificar "rejeita código duplicado" 409 "$(req POST /api/produtos '{"codigo":"PA0100","descricao":"Duplicado","unidade":"UN","precoVenda":1,"estoque":1}')"
verificar "valida campos obrigatórios" 400 "$(req POST /api/produtos '{"codigo":"","descricao":"","unidade":"","precoVenda":-1,"estoque":0}')"

echo "Pedidos"
verificar "inclui pedido válido" 201 "$(req POST /api/pedidos '{"codigoCliente":"C00001","itens":[{"codigoProduto":"PA0002","quantidade":10},{"codigoProduto":"PA0003","quantidade":20}]}')"
NUMERO=$(jq -r .numero /tmp/resp.json)
verificar "total calculado (10x18,50 + 20x12,90)" 443.00 "$(jq .total /tmp/resp.json)"
verificar "estoque baixado (320 - 10)" 310 "$(req GET /api/produtos/PA0002 >/dev/null; jq .estoque /tmp/resp.json)"
verificar "bloqueia estoque insuficiente" 422 "$(req POST /api/pedidos '{"codigoCliente":"C00001","itens":[{"codigoProduto":"PA0005","quantidade":1}]}')"
verificar "bloqueia cliente bloqueado" 422 "$(req POST /api/pedidos '{"codigoCliente":"C00003","itens":[{"codigoProduto":"PA0002","quantidade":1}]}')"
verificar "bloqueia limite de crédito" 422 "$(req POST /api/pedidos '{"codigoCliente":"C00002","itens":[{"codigoProduto":"PA0001","quantidade":30}]}')"
verificar "cliente inexistente retorna 404" 404 "$(req POST /api/pedidos '{"codigoCliente":"C99999","itens":[{"codigoProduto":"PA0002","quantidade":1}]}')"
verificar "pedido sem itens retorna 400" 400 "$(req POST /api/pedidos '{"codigoCliente":"C00001","itens":[]}')"

echo "Ciclo de vida"
req POST /api/pedidos '{"codigoCliente":"C00002","itens":[{"codigoProduto":"PA0004","quantidade":5}]}' >/dev/null
CANCELAR=$(jq -r .numero /tmp/resp.json)
verificar "cancela pedido em aberto" 200 "$(req POST "/api/pedidos/$CANCELAR/cancelar")"
verificar "estoque estornado no cancelamento (75)" 75 "$(req GET /api/produtos/PA0004 >/dev/null; jq .estoque /tmp/resp.json)"
verificar "fatura pedido em aberto" 200 "$(req POST "/api/pedidos/$NUMERO/faturar")"
verificar "status passa a Faturado" '"Faturado"' "$(jq .status /tmp/resp.json)"
verificar "não cancela pedido faturado" 422 "$(req POST "/api/pedidos/$NUMERO/cancelar")"
verificar "filtra pedidos por status" 1 "$(req GET '/api/pedidos?status=Cancelado' >/dev/null; jq length /tmp/resp.json)"

echo
if [[ $FALHAS -eq 0 ]]; then echo "Todos os testes passaram."; else echo "$FALHAS teste(s) falharam."; exit 1; fi
