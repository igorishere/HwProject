# Handoff: busca de preço assíncrona

Atualizado em 2026-10-07. Leia também [AGENTS.MD](../AGENTS.MD), que contém as regras arquiteturais do projeto.

## Objetivo

Permitir que a identificação responda rapidamente e que a pesquisa de preço rode em background, consultável pelo cliente. O usuário prefere respostas curtas, em português, e avaliar possibilidades uma por vez. Não mudar o desenho arquitetural existente: controllers no API, contratos/handlers na Application e integrações/serviços em Infrastructure.

## Decisões do usuário

- Um `CarResult` é o envelope comum de resposta dos endpoints. Propriedades ainda indisponíveis ficam `null`.
- `CarResult` reside em `CarIdentifier.Application.Contracts`.
- `CarIdentification.PriceSearchJobId` é um GUID criado pela aplicação, não pela IA.
- A imagem deve ser preservada desde a identificação. O cliente não precisa enviá-la novamente para iniciar a busca.
- A busca inicia automaticamente em background após a identificação. O cliente usa o GUID retornado para consultar o job por GET; não há POST separado para iniciar a busca.
- Jobs pendentes expiram em 30 minutos; concluídos ou falhos expiram em 24 horas.
- Store e fila em memória são a primeira versão. Persistência distribuída e SSE/SignalR ficam para depois.
- Respostas curtas. Ao explorar possibilidades, apresentar/testar uma por vez.

## Implementação atual

### Endpoints

1. `POST /api/cars/identify` recebe multipart com campo `Image`, identifica o produto, salva os bytes da imagem no job, enfileira a busca e retorna `CarResult` sem esperar pelos preços, com:
   - `CarIdentification`, incluindo `PriceSearchJobId`;
   - `PriceSearchStatus = Queued`;
   - `PriceSearchResult`, datas e motivo de falha nulos.
2. `GET /api/cars/price-jobs/{id}` retorna o `CarResult` com status, resultado, datas e motivo de falha, ou `404` para ID inexistente/expirado.

Estados: `Queued -> Running -> Completed | Failed`. `Pending` também existe para jobs ainda não enfileirados.

### Arquitetura e arquivos

- [IdentifyCarCommandHandler.cs](../src/CarIdentifier.Application/CommandHandlers/IdentifyCar/IdentifyCarCommandHandler.cs): copia a imagem, identifica, cria o GUID, registra e enfileira o job.
- [CarResult.cs](../src/CarIdentifier.Application/Contracts/CarResult.cs): envelope com identificação, resultado de preço, estado, datas e motivo de falha.
- [GetPriceSearchJobController.cs](../src/CarIdentifier.Api/Controllers/GetPriceSearchJob/GetPriceSearchJobController.cs): endpoint GET para consultar o job.
- [PriceSearchJob.cs](../src/CarIdentifier.Application/Abstraction/PriceSearch/PriceSearchJob.cs): dados preservados para o processamento e estado do job.
- `IPriceJobStore` e `IPriceJobQueue` são abstrações em Application.
- [InMemoryPriceJobStore.cs](../src/CarIdentifier.Infra/PriceSearch/InMemoryPriceJobStore.cs): armazenamento thread-safe. Pending expira em 30 min; Completed/Failed expiram em 24 h; queued/running não expiram.
- [ChannelPriceJobQueue.cs](../src/CarIdentifier.Infra/PriceSearch/ChannelPriceJobQueue.cs): fila baseada em `Channel<Guid>`.
- [PriceSearchWorker.cs](../src/CarIdentifier.Infra/PriceSearch/PriceSearchWorker.cs): hosted worker com escopo DI por job que executa `IPriceSearcher`.
- `PriceJobCleanupService` remove jobs expirados periodicamente.
- [IdentifierService.cs](../src/CarIdentifier.Infra/CarIdentification/IdentifierService.cs): desserializa um DTO de IA separado, para o JSON Schema não incluir o ID operacional.
- DI e worker estão registrados em [Program.cs](../src/CarIdentifier.Api/Program.cs).

## Validação já realizada

- `dotnet build src/CarIdentifier.Api/CarIdentifier.Api.csproj --no-restore`: sucesso, 0 erros e 0 avisos.
- `dotnet test tests/CarIdentifier.UnitTests/CarIdentifier.UnitTests.csproj --no-restore`: 9 testes passaram.
- O teste do worker usa um buscador substituto; ainda falta exercício ponta a ponta com credenciais OpenAI locais.

## Como testar manualmente

Swagger local é iniciado em `http://localhost:5000/swagger` pelo perfil Development:

1. Chame Identify com uma imagem multipart; a busca é enfileirada automaticamente.
2. Copie `carIdentification.priceSearchJobId`.
3. Consulte `GET /api/cars/price-jobs/{id}` até `Completed` ou `Failed`.

Os exemplos HTTP estão em [CarIdentifier.Api.http](../src/CarIdentifier.Api/CarIdentifier.Api.http). O arquivo referencia `./car.jpg`, que não está no repositório; substitua pelo caminho de uma imagem local ao usar o cliente HTTP ou use o seletor de arquivo do Swagger.

## Próximas ações recomendadas

1. Fazer uma verificação ponta a ponta via Swagger com credenciais OpenAI configuradas localmente; não colocar segredos no repositório.
2. Se houver falha, identificar uma causa por vez e corrigi-la sem ampliar o escopo.
3. Completar testes para: falha do worker, submissões concorrentes do mesmo ID, status HTTP dos controllers e limpeza periódica de expiração.
4. Revisar se o comportamento de falhas parciais de `PriceFinderService` deve deixar o job `Completed` com resultado parcial ou `Failed`; atualmente o finder captura falhas individuais de fonte, enquanto o worker marca `Completed` se `FindAsync` retornar.
5. Considerar limite de tamanho para imagem e capacidade limitada da fila. A fila atual é unbounded e a store mantém bytes em memória; reinício do processo apaga jobs.

## Restrições do domínio

- Não inventar dados de identificação ou preços.
- Não misturar variantes como se fossem o mesmo produto.
- Preservar fonte, URL, moeda, evidência e horário de coleta quando disponíveis.
- Application não pode depender de Infrastructure.
- Manter identificação e pesquisa de preço como responsabilidades separadas.
