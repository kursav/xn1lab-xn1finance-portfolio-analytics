# XN1Lab XN1Finance Portfolio Analytics Delivery Progress

This document tracks implementation progress for `XN1Finance Portfolio Analytics`.

Status values:

- `Done`: implemented and build-verified
- `In Progress`: actively being implemented
- `Todo`: planned but not started
- `Blocked`: requires a controlled external step or decision

## 1. Current Baseline

Implemented baseline:

- Blazor WebAssembly product shell
- CoreServices `PortfolioAnalytics` bounded context
- portfolio, holding, instrument, macro series, and macro release CRUD API
- account and system settings API
- shared settings contracts
- permission constants, permission seed definitions, and controller permission attributes
- schema seed service for Portfolio Analytics tables
- reference data seed service for instrument/sector mapping and macro calendar starter data
- macro data provider abstraction with no-op, seed, and FRED providers
- market data provider abstraction with no-op and Alpha Vantage providers
- market data persistence tables for latest prices, daily bars, FX rates, and portfolio valuation snapshots
- market refresh job path that writes Alpha Vantage quotes and daily bars into Portfolio Analytics storage when explicitly enabled
- settings page wired through product-local service and `IPlatformApiClient`
- portfolio holding editor UI with instrument lookup and sector mapping fill
- exposure API with asset class, sector, currency, and single-name buckets valued from latest market price when available
- investor profile target comparison response
- data quality warnings in exposure and scenario responses
- macro calendar API and product-local service method
- scenario engine first slice with heuristic macro sensitivity impact estimates

Last verified builds:

```text
dotnet build XN1Lab.CoreServices.sln -c Debug
Result: succeeded, 0 warnings, 0 errors

dotnet build XN1Lab.XN1Finance.PortfolioAnalytics.sln -c Debug
Result: succeeded, file-lock/access warnings from running local server, 0 errors
```

## 2. Progress Matrix

| Id | Priority | Status | Work Item | Notes |
| --- | --- | --- | --- | --- |
| PA-001 | P0 | Done | Controlled DB and seed smoke test | Ran against the configured dev DB on 2026-05-17. `PortfolioAnalyticsDb` exists; all required tables exist; permission definitions are 12/12; active account entitlements are 12/12; app catalog contains `xn1finance.portfolio-analytics.web`; reference data seed produced 54 instruments, 20 macro series, and 28 macro release events. |
| PA-002 | P0 | Done | Align all Portfolio Analytics API payloads with shared contracts | Controller maps settings, portfolio, holding, instrument, macro series, and release payloads through shared contracts. |
| PA-003 | P0 | Done | Portfolio CRUD UI | Added list, search, create/edit, delete, loading/empty/error states through product-local service. |
| PA-004 | P0 | Done | Holding editor UI | Added portfolio holdings page with list, search, create/edit, delete, and manual mapping fields. |
| PA-005 | P0 | Todo | Permission-aware UI states | Account settings, system settings, macro, scenario, and market-data areas must reflect specific permissions. |
| PA-006 | P1 | Todo | Platform-compliant API error and validation feedback | Replace page-local API banners with platform alert/error-state/localized message patterns. |
| PA-007 | P1 | Done | Instrument lookup and symbol resolution UX | Holding editor can lookup instruments, handle multiple matches, fill sector/type/currency/name, and allow manual fallback. |
| PA-008 | P1 | Done | Market data persistence model | Added latest price, market price bar, FX rate, and valuation snapshot tables plus schema seed. Added Alpha Vantage refresh job path for latest quote and daily bar persistence. |
| PA-009 | P1 | In Progress | Exposure calculation API | Exposure now values holdings from latest stored market price when available, falls back to average cost, and warns for missing/stale/mismatched price data. FX conversion and country/region exposure still need the next slice. |
| PA-010 | P1 | Done | Investor profile target comparison | Exposure API compares actual asset-class allocation against account target percentages and rebalance threshold. |
| PA-011 | P1 | In Progress | Data quality warning model | Added response warnings for empty portfolio, missing/stale market price, currency mismatch, missing cost basis, missing sector, unknown instrument type, zero estimate, missing consensus, and heuristic scenario sensitivity. Persistent warning history is still pending. |
| PA-012 | P2 | In Progress | Macro calendar UI connected to API | Added macro calendar API and product-local service method. UI page is pending. |
| PA-013 | P2 | Done | Scenario engine first slice | Added scenario run API using macro release surprise/override shock and heuristic factor sensitivities over portfolio exposure. |
| PA-014 | P2 | Todo | Reports/export first slice | Printable report view first, CSV/PDF later. |
| PA-015 | P2 | Todo | Tests | Add focused tests for account scope, permissions, CRUD, settings validation, and refresh job behavior. |
| PA-016 | P1 | Done | FRED macro provider adapter | Added `fred` macro provider for historical macro observations. Reads API key from `FRED_API_KEY` or secret config; no key is stored in repo. |
| PA-017 | P1 | Done | Alpha Vantage market provider adapter | Added `alpha-vantage` market provider for symbol search, latest quote, and daily bars. Reads API key from `ALPHA_VANTAGE_API_KEY` or secret config; no key is stored in repo. |
| PA-018 | P0 | Done | Portfolio Analytics identity client alignment | Product host now uses CoreServices client resolution only. Static short `ClientId` fallback was removed from the Keycloak provider path, and CoreServices seed no longer creates the short application key as a legacy Keycloak client id. Required Keycloak client id is `895ed50b-c808-4f5f-9bb4-2fc6175592bc.xn1finance.portfolio-analytics.web`. |

## 3. Recommended Execution Order

1. Implement `PA-005` permission-aware UI states.
2. Implement macro calendar and scenario UI pages.
3. Add FX conversion and country/region exposure support.
4. Implement `PA-006` platform-compliant feedback.
5. Add focused tests for API scope, permissions, refresh job, exposure math, warnings, and scenario calculations.

## 4. DB Smoke Test Checklist

Last run:

- Date: 2026-05-17 13:33 Europe/Vienna
- Target: configured CoreServices dev PostgreSQL databases
- Bootstrap: CoreServices host seeds `PortfolioAnalyticsDb`; local Codex schema bootstrap was used to apply the new market tables for this smoke pass
- Result: required schema, market-data tables, permission definitions, account entitlements, application catalog, and reference data checks passed
- Reference data counts: 54 active instruments, 20 active macro series, 28 active macro release events

Before running CoreServices against a configured database, confirm the intended environment.

Verify:

- `PortfolioAnalyticsDb` points to the intended database
- `xn1finance_portfolio_analytics_account_settings` exists
- `xn1finance_portfolio_analytics_system_settings` exists
- `xn1finance_portfolios` exists
- `xn1finance_portfolio_holdings` exists
- `xn1finance_security_instruments` exists
- `xn1finance_latest_market_prices` exists
- `xn1finance_market_price_bars` exists
- `xn1finance_fx_rates` exists
- `xn1finance_portfolio_valuation_snapshots` exists
- `xn1finance_macro_series` exists
- `xn1finance_macro_release_events` exists
- permission definitions exist for `xn1finance.portfolio-analytics.*`
- application catalog contains `xn1finance.portfolio-analytics.web`
- active account application entitlement grants include all required read/write/model/scenario/market/macro permissions
- authenticated endpoint smoke with a real user token should be covered by `PA-015` tests

## 5. Current Architectural Decision

Keep Portfolio Analytics inside CoreServices for MVP.

Do not create a separate finance backend service yet. A separate service becomes justified only when data ingestion, market data licensing, or analytics compute requires a separately deployable lifecycle.
