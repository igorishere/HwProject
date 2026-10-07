# AGENTS.md

## Project

Hot Wheels Price Finder is a .NET application that identifies Hot Wheels from photos and searches market sources to estimate their value.

The current backend flow is:

```text
Image
  -> Car Identification
  -> Price Search
  -> Price Analysis
```

## Architecture

The backend follows Clean Architecture principles.

- **Application** contains use cases, contracts, commands and handlers.
- **Infrastructure** contains implementations and external integrations.
- **API** exposes the HTTP endpoints.
- Infrastructure dependencies must not leak into Application.

Keep dependencies pointing inward.

### AI abstraction

AI integrations must use abstractions from the application layer.

Current abstraction:

- `IChatClient` from `Microsoft.Extensions.AI`

Current provider:

- OpenAI

Do not couple application code directly to OpenAI-specific implementations.

## Current components

### Car identification

The identification component receives a Hot Wheels image and extracts information from the packaging.

Current implementation:

- Application contract: `ICarIdentifier`
- Infrastructure implementation: `IdentifierService`

The AI should prioritize visible packaging information and must not invent information. Unknown values should be represented as `null` when the contract allows it.

### Price search

Price search is a separate component from identification.

Current implementation:

- Application contract: `IPriceFinder`
- Infrastructure implementation: `PriceFinderService`

The price search receives the identification data and may also receive the original image when useful for identifying variants.

At the current stage, keep price search generic. Do not introduce an `IPriceSource` abstraction until there are concrete source implementations that justify it.

## Price data

The application will eventually use multiple types of sources:

- Primary market: Mercado Livre, Shopee, Amazon Brasil, Magalu, Ri Happy
- Secondary market: eBay, Mercari, Whatnot, hobbyDB, Yahoo! Japan Auctions, OLX Brasil, Enjoei, Todocoleccion
- Price guides: HW Price Guide and Hall's Guide

Sources and their data must remain distinguishable.

Different price types must not be treated as equivalent:

- asking price
- sold price
- reference/guide price

Preserve the source, URL, currency and collection time whenever available.

## Price analysis

Price results must eventually be normalized before comparison.

Consider:

- currency
- country/market
- product condition
- packaging condition
- exact variant
- asking vs. sold vs. reference price
- outliers

Do not compare different variants as if they were the same product.

## API

Current identification endpoint:

```http
POST /api/cars/identify
Content-Type: multipart/form-data
```

The frontend must never receive or contain the OpenAI API key.

Secrets must be provided through secure configuration such as environment variables or a secret manager. Never commit secrets to Git.

## Coding guidelines

- Organize features as vertical slices across project layers, grouped by action.
- Give each action its own controller; do not combine multiple actions in one controller.
- Keep each action's API controller and request, application command and handler, and related tests together in that action's slice where applicable.
- Keep Application independent of Infrastructure.
- Prefer small, focused interfaces.
- Prefer dependency injection over concrete dependencies.
- Keep external provider details inside Infrastructure.
- Prefer deterministic APIs/scrapers over autonomous agents when a deterministic integration is available.
- Use AI when interpretation, ambiguity resolution or flexible web navigation is required.
- Preserve source information so results can be audited.
- Make incremental changes; avoid premature abstractions.

## Testing

When changing identification behavior, preserve regression coverage using real Hot Wheels images where appropriate.

Run the test suite with:

```bash
dotnet test
```

When changing contracts or the end-to-end identification/pricing flow, update the relevant integration tests.

## Important project rules

1. Do not invent identification or price information.
2. Do not depend exclusively on a single price source.
3. Keep identification and price search as separate responsibilities.
4. Do not move external-service concerns into Application.
5. Prefer official APIs when available.
6. Use scraping only when necessary and according to the applicable source rules.
7. Preserve evidence used to produce price estimates.
8. Evolve the architecture only when concrete requirements justify additional abstractions.
