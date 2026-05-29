# XN1Lab XN1Finance Portfolio Analytics Architecture And Requirements

## 1. Purpose

This document defines the first architecture and requirement baseline for `XN1Finance Portfolio Analytics`.

The product helps users enter or import investment holdings, map those holdings to instruments, sectors, industries, regions, currencies, and asset classes, then analyze:

- portfolio concentration
- investor profile alignment
- sector and factor exposure
- macro event sensitivity
- estimated scenario impact
- risk and opportunity signals at portfolio level

The product must start as an analytics and decision-support SaaS, not as a regulated buy/sell recommendation engine.

## 2. Naming Decision

### 2.1 Product Family

Use:

```text
XN1Finance
```

Reason:

- The domain is broader than portfolio management.
- Future products can live under the same family without forcing a rename.
- It avoids making `Portfolio` the top-level product identity, because portfolio can mean investment portfolio, customer portfolio, product portfolio, or project portfolio in other contexts.

### 2.2 First Application

Use:

```text
XN1Finance Portfolio Analytics
```

Recommended .NET project:

```text
XN1Lab.XN1Finance.PortfolioAnalytics.Web
```

Application key:

```text
xn1finance.portfolio-analytics.web
```

Default route prefix:

```text
/finance/portfolio
```

UI module display name:

```text
Portfolio Analytics
```

### 2.3 Naming Alternatives Rejected

Rejected:

```text
XN1Portfolio
```

Reason:

- It is not explicit enough.
- It does not state the finance domain.
- It can conflict with other future portfolio concepts in the ecosystem.

Rejected:

```text
XN1Lab.XN1Finance.Portfolio.Analytics.Web
```

Reason:

- It adds an extra segment before the product requires it.
- Current XN1Lab naming guidance prefers `XN1Lab.{ProductName}.{SubComponent}.{HostOrType}`.
- `PortfolioAnalytics` is a clear first bounded capability.

## 3. Ecosystem Fit

XN1Lab architecture separates:

```text
CoreServices = backend source of truth
Platform = reusable SaaS operating layer
Products = feature pages and product workflows
```

`XN1Finance Portfolio Analytics` follows that model.

### 3.1 Product Boundary

The Blazor WebAssembly SaaS application belongs under:

```text
products/xn1finance/portfolio-analytics
```

Recommended first structure:

```text
products/xn1finance/portfolio-analytics
|-- docs
|-- src
|   `-- XN1Lab.XN1Finance.PortfolioAnalytics.Web
|       |-- Components
|       |-- Configuration
|       |-- Features
|       |-- Infrastructure
|       |-- Resources
|       |-- Services
|       |-- Shell
|       |-- wwwroot
|       |-- XN1Lab.XN1Finance.PortfolioAnalytics.Web.csproj
|       `-- XN1Lab.XN1Finance.PortfolioAnalytics.sln
|-- tests
`-- README.md
```

### 3.2 Backend Authority Boundary

Start inside `XN1Lab.CoreServices` as a dedicated `PortfolioAnalytics` context.

Recommended folders:

```text
services/core/XN1Lab.CoreServices/src/XN1Lab.CoreServices.Domain/PortfolioAnalytics
services/core/XN1Lab.CoreServices/src/XN1Lab.CoreServices.Application/PortfolioAnalytics
services/core/XN1Lab.CoreServices/src/XN1Lab.CoreServices.Infrastructure/PortfolioAnalytics
services/core/XN1Lab.CoreServices/src/XN1Lab.CoreServices.Infrastructure/Persistence/PortfolioAnalytics
services/core/XN1Lab.CoreServices/src/XN1Lab.CoreServices.Infrastructure/Persistence/QueryBuilders/PortfolioAnalytics
services/core/XN1Lab.CoreServices/src/XN1Lab.CoreServices.Host/Controllers
```

Do not create a separate `XN1FinanceServices` backend boundary for the first milestone.

Create a separate service only when:

- market data ingestion needs an independent deployment lifecycle
- macro data ingestion becomes large enough to operate separately
- analytics compute workloads become too heavy for CoreServices
- release ownership requires a separate service boundary

### 3.3 Shared Contracts Boundary

Shared wire contracts belong in:

```text
platform/XN1Lab.Platform.CoreServices.Contracts/PortfolioAnalytics
```

These contracts are API payload models only.

They must not contain:

- EF Core attributes
- persistence logic
- product page state
- chart-only view models
- service implementations

### 3.4 Platform Boundary

Do not move finance domain concepts into `platform`.

Platform reuse should come through:

- `XN1Lab.Platform.Core`
- `XN1Lab.Platform.Identity`
- `XN1Lab.Platform.Api`
- `XN1Lab.Platform.CoreServices.Contracts`
- `XN1Lab.Platform.Branding`
- `XN1Lab.Platform.Localization`
- `XN1Lab.Platform.UI`
- `XN1Lab.Platform.SaaSHost`

Finance-specific workflows belong to `XN1Finance` product code and `PortfolioAnalytics` CoreServices context.

## 4. Product Positioning

### 4.1 Product Definition

The first release is:

```text
portfolio risk intelligence and macro sensitivity analytics
```

It is not:

```text
personalized investment advisory
```

### 4.2 Language Rule

The UI may say:

- "Your portfolio is concentrated in technology."
- "This allocation is above the balanced profile target range."
- "This macro scenario has historically been negative for technology-heavy portfolios."
- "Estimated portfolio sensitivity is -0.6% to -1.3%."
- "This position contributes strongly to concentration risk."

The UI must not say:

- "Sell AAPL."
- "Buy XLE."
- "This stock will go up."
- "This is the best trade."
- "You should rebalance into this exact stock."

### 4.3 Advisory Guardrail

The product should present:

- analysis
- ranges
- sensitivity
- historical relationships
- scenario estimates
- educational explanations
- portfolio health status

The product should avoid:

- direct order instructions
- exact buy/sell lists
- guaranteed return language
- promise of future performance
- hidden personalized suitability claims

Legal and compliance review is required before any feature produces direct recommendations, portfolio optimization instructions, or automated model portfolios.

## 5. Target Users

### 5.1 Retail Investor

The user manually enters holdings and wants to understand:

- sector exposure
- risk concentration
- how macro events can affect the portfolio
- whether the portfolio matches their selected risk profile

### 5.2 Active Investor

The user tracks macro events and wants:

- CPI, PCE, NFP, Fed, yield, oil, DXY, and PMI event impact
- scenario impact by holding
- sector factor heatmaps
- short-term sensitivity estimates

### 5.3 Advisor Or Analyst

The user manages or reviews multiple portfolios for analysis purposes.

This persona should be treated carefully because it may introduce regulated workflows depending on jurisdiction.

### 5.4 Platform Operator

The operator manages:

- data providers
- macro series catalog
- market instrument catalog
- analytics model versions
- permission catalog
- support and audit operations

## 6. In Scope

MVP scope:

- authenticated account-scoped SaaS application
- manual holdings entry
- portfolio list and detail pages
- instrument lookup and mapping
- sector, industry, asset class, region, and currency exposure
- investor profile selection
- target allocation range comparison
- concentration risk score
- volatility and drawdown summary when enough price history exists
- macro event catalog
- macro actual, consensus, and surprise storage
- estimated surprise fallback when consensus is unavailable
- sector macro sensitivity table
- portfolio scenario impact estimate
- portfolio risk report
- basic exportable report view

## 7. Out Of Scope For MVP

The MVP must not include:

- direct trading
- broker order routing
- automatic portfolio execution
- tax optimization
- regulated suitability report generation
- guaranteed return projections
- real-time licensed market data unless license is solved
- crypto wallet integration
- options Greeks
- intraday tick data
- portfolio sync from broker APIs
- public social leaderboard of portfolios

## 8. Account And Scope Model

The product follows the account-centered XN1Lab model.

Rules:

- `Account` is the canonical owner boundary.
- `OwnerAccountId` is the account that owns the portfolio.
- `CreatedByAccountId` is the actor account that created a record.
- `UpdatedByAccountId` is the actor account that last updated a record.
- `xn1-account-id` is the active account header.
- The backend remains authoritative for access.
- Frontend navigation filtering is not security.

Do not use:

- `TenantId`
- `OrganizationId`
- `RelationId` as persistence ownership marker

Relations may be used later to allow an advisor account to work on a client account, but persistence still remains account-based.

## 9. Permission Model

Recommended permission prefix:

```text
xn1finance.portfolio-analytics
```

Initial permissions:

```text
xn1finance.portfolio-analytics.read
xn1finance.portfolio-analytics.write
xn1finance.portfolio-analytics.delete
xn1finance.portfolio-analytics.import
xn1finance.portfolio-analytics.export
xn1finance.portfolio-analytics.scenario.run
xn1finance.portfolio-analytics.market-data.read
xn1finance.portfolio-analytics.market-data.write
xn1finance.portfolio-analytics.macro-data.read
xn1finance.portfolio-analytics.macro-data.write
xn1finance.portfolio-analytics.models.read
xn1finance.portfolio-analytics.models.write
```

Recommended endpoint mapping:

| Endpoint Area | Permission |
| --- | --- |
| Portfolio list and report | `xn1finance.portfolio-analytics.read` |
| Account portfolio analytics settings read | `xn1finance.portfolio-analytics.read` |
| Account portfolio analytics settings write | `xn1finance.portfolio-analytics.write` |
| Portfolio create/update | `xn1finance.portfolio-analytics.write` |
| Portfolio delete | `xn1finance.portfolio-analytics.delete` |
| Holding import | `xn1finance.portfolio-analytics.import` |
| Report export | `xn1finance.portfolio-analytics.export` |
| Scenario run | `xn1finance.portfolio-analytics.scenario.run` |
| Instrument and price reads | `xn1finance.portfolio-analytics.market-data.read` |
| Data provider/admin writes | `xn1finance.portfolio-analytics.market-data.write` |
| Macro series/event reads | `xn1finance.portfolio-analytics.macro-data.read` |
| Macro ingestion/admin writes | `xn1finance.portfolio-analytics.macro-data.write` |
| Model version reads | `xn1finance.portfolio-analytics.models.read` |
| Model, provider, and system refresh settings activation | `xn1finance.portfolio-analytics.models.write` |

## 10. Core Use Cases

### UC-001 Create Portfolio

An authenticated user creates a portfolio under the active account scope.

Minimum input:

- portfolio name
- base currency
- optional description
- optional investment objective

Result:

- a new account-owned portfolio exists
- initial status is `Draft` or `Active`

### UC-002 Add Manual Holding

The user adds a holding manually.

Input:

- symbol
- exchange or market
- quantity
- average cost
- currency
- optional acquisition date
- optional notes

Result:

- the system resolves or creates a `SecurityInstrument`
- the holding is linked to the portfolio
- current market value can be estimated when price data exists

### UC-003 Resolve Instrument

The system resolves a user-entered symbol to a canonical instrument.

The resolver should return:

- symbol
- exchange
- name
- asset class
- country
- currency
- sector
- industry
- data source
- confidence

If the symbol is ambiguous, the UI must ask the user to select the correct match.

### UC-004 Analyze Exposure

The system calculates portfolio exposure by:

- sector
- industry
- asset class
- instrument type
- currency
- country
- region
- single name

Output:

- exposure percentage
- market value
- target range comparison when profile exists

### UC-005 Select Investor Profile

The user selects an investor profile.

Initial profiles:

- Defensive
- Balanced
- Growth
- Aggressive
- Income
- Macro Sensitive

Each profile defines target allocation ranges and risk thresholds.

### UC-006 Compare Against Target Allocation

The system compares portfolio exposure against the selected profile.

Example output:

```text
Technology current 62%, target 25% to 40%, status AboveRange
Health Care current 0%, target 8% to 15%, status BelowRange
Cash current 3%, target 2% to 10%, status InRange
```

### UC-007 Calculate Risk Score

The system calculates a portfolio risk report.

Initial risk dimensions:

- concentration risk
- sector risk
- single-name risk
- currency risk
- volatility risk
- drawdown risk
- interest-rate sensitivity
- inflation sensitivity
- macro event sensitivity
- data quality risk

Each dimension should expose:

- score
- severity
- explanation key
- contributing holdings

### UC-008 Track Macro Series

The platform stores macro series definitions.

Examples:

- US CPI
- US Core CPI
- US PCE
- US Core PCE
- Nonfarm Payrolls
- Unemployment Rate
- Fed Funds Target Rate
- US 10Y Treasury Yield
- US 2Y Treasury Yield
- 2Y-10Y Spread
- ISM Manufacturing PMI
- ISM Services PMI
- Retail Sales
- Industrial Production
- WTI Oil
- Brent Oil
- DXY
- High Yield Credit Spread
- Euro Area CPI
- China PMI

### UC-009 Store Macro Release Event

The system stores macro release events.

Fields:

- series id
- release date and time
- actual value
- consensus value
- previous value
- revised previous value
- unit
- source
- release status

### UC-010 Calculate Macro Surprise

Preferred:

```text
surprise = actual - consensus
```

If consensus is not available:

```text
estimated_surprise = actual - model_expected_value
```

The system must mark whether surprise is consensus-based or model-estimated.

### UC-011 Estimate Sector Factor Sensitivity

The system estimates how sectors historically reacted to macro surprises.

Example:

```text
CPI surprise +1 percentage point -> Technology -0.85 percentage point over 5 trading days
Oil +10% -> Energy +2.10 percentage point over 5 trading days
10Y Yield +1 percentage point -> Real Estate -1.20 percentage point over 5 trading days
```

### UC-012 Run Portfolio Macro Scenario

The user selects a scenario.

Example:

```text
US CPI surprise +0.3 percentage point
US 10Y yield +0.15 percentage point
DXY +1.0%
```

The system estimates:

- portfolio-level impact
- sector-level impact
- holding-level contribution
- confidence level
- model version
- data quality warnings

### UC-013 View Macro Calendar

The user views upcoming macro releases.

The calendar should show:

- release date and time
- series name
- country or region
- importance
- previous value
- consensus when available
- affected sectors
- portfolio relevance score

### UC-014 View Portfolio Report

The user opens a report page with:

- summary score
- allocation charts
- concentration table
- target profile comparison
- macro sensitivity matrix
- upcoming event risks
- top contributors to risk
- data quality warnings

### UC-015 Export Report

The user exports an analysis report.

MVP output can be:

- printable HTML
- PDF later
- CSV for holdings/exposures

### UC-016 Manage Data Sources

An operator configures external data providers.

Examples:

- FRED
- OECD
- Stooq
- Financial Modeling Prep
- Twelve Data
- internal CSV import

MVP should support provider abstraction even if the first provider is simple.

### UC-017 Review Data Quality

The user or operator can see missing data:

- unresolved instrument
- missing sector
- stale price
- missing FX rate
- missing consensus
- model-estimated surprise
- insufficient history

The analysis must degrade visibly instead of silently producing false precision.

## 11. Functional Requirements

### FR-001 Portfolio CRUD

The system shall allow authorized users to create, read, update, and delete portfolios under the active account scope.

### FR-002 Holdings CRUD

The system shall allow authorized users to add, update, and remove holdings in a portfolio.

### FR-003 Instrument Resolution

The system shall resolve symbols against an instrument catalog and expose ambiguity to the user.

### FR-004 Sector Classification

The system shall store sector and industry classification for instruments.

Initial classification standard:

```text
GICS-like 11 sector model
```

Do not depend on licensed GICS data unless licensing is confirmed.

### FR-005 Exposure Calculation

The system shall calculate portfolio exposure based on latest known market values.

If price data is unavailable, the system shall fall back to cost basis and mark the valuation as estimated.

### FR-006 Investor Profile

The system shall allow each portfolio to use an investor analysis profile.

The profile may be:

- selected from system defaults
- account-customized in later phases

### FR-007 Target Ranges

The system shall compare exposure values against profile target ranges.

### FR-008 Risk Report

The system shall produce a risk report with scores, explanations, and contributing holdings.

### FR-009 Macro Series Catalog

The system shall maintain a macro series catalog with unit, frequency, region, source, and importance.

### FR-010 Macro Release Storage

The system shall store macro release events with actual, consensus, previous, revised previous, and release timestamp.

### FR-011 Surprise Calculation

The system shall calculate consensus surprise when consensus is available.

The system shall calculate estimated surprise when consensus is unavailable and label it clearly.

### FR-012 Factor Sensitivity

The system shall store sector factor sensitivity by macro factor, response window, model version, and confidence.

### FR-013 Scenario Engine

The system shall apply selected macro shocks to portfolio exposures and produce estimated impact.

### FR-014 Data Quality Warnings

The system shall expose warnings when analysis relies on stale, missing, estimated, or insufficient data.

### FR-015 Account Scope Enforcement

The backend shall enforce account scope using the active account context.

### FR-016 Permission Enforcement

Protected endpoints shall declare explicit permission requirements.

### FR-017 Product Service Layer

Blazor pages shall call product-local services, not raw HTTP calls.

### FR-018 Localization

Visible product text shall be localization-ready.

### FR-019 Report View

The product shall provide a report page suitable for printing or exporting in later phases.

### FR-020 Audit Metadata

Important records shall store created/updated timestamps and actor account identifiers.

## 12. Nonfunctional Requirements

### NFR-001 Accuracy Transparency

The product must show confidence, data source, model version, and limitations for analytics results.

### NFR-002 Performance

Portfolio report generation for a normal retail portfolio should complete within an interactive UI timeframe.

Initial target:

```text
under 2 seconds for portfolios with up to 100 holdings when data is already cached
```

### NFR-003 Resilience

External provider failures must not break the whole application.

The UI should show:

- stale data
- unavailable data
- provider error
- cached result

### NFR-004 Security

Backend authorization is mandatory for all portfolio and data-management endpoints.

### NFR-005 Privacy

Portfolio holdings are account-owned sensitive financial data.

Do not expose holdings across accounts unless an explicit relation/permission model supports it.

### NFR-006 Explainability

Risk and scenario outputs must expose enough explanation for the user to understand why a score was produced.

### NFR-007 No False Precision

When the model is weak or data is incomplete, show ranges and warnings rather than exact-looking forecasts.

### NFR-008 WASM Payload Control

Heavy charting, analytics visualization, and import components should be loaded only when the finance module is opened.

## 13. Domain Model

### 13.1 Portfolio

Represents an account-owned investment portfolio.

Suggested fields:

- `Id`
- `OwnerAccountId`
- `Name`
- `Description`
- `BaseCurrency`
- `Status`
- `InvestorProfileId`
- `CreatedByAccountId`
- `CreatedAtUtc`
- `UpdatedByAccountId`
- `UpdatedAtUtc`
- `Deleted`

### 13.2 PortfolioHolding

Represents a position in a portfolio.

Suggested fields:

- `Id`
- `PortfolioId`
- `OwnerAccountId`
- `InstrumentId`
- `Symbol`
- `ExchangeCode`
- `Quantity`
- `AverageCost`
- `CostCurrency`
- `AcquiredAtUtc`
- `Notes`
- `CreatedAtUtc`
- `UpdatedAtUtc`
- `Deleted`

### 13.3 PortfolioCashPosition

Represents cash or cash-like allocation.

Suggested fields:

- `Id`
- `PortfolioId`
- `OwnerAccountId`
- `Currency`
- `Amount`
- `Label`
- `CreatedAtUtc`
- `UpdatedAtUtc`

### 13.4 PortfolioImportBatch

Represents a batch import of holdings.

Suggested fields:

- `Id`
- `OwnerAccountId`
- `PortfolioId`
- `SourceType`
- `FileResourceId`
- `Status`
- `RowsTotal`
- `RowsAccepted`
- `RowsRejected`
- `ErrorSummary`
- `CreatedAtUtc`
- `CompletedAtUtc`

Use `XN1Resources` for uploaded files when possible.

### 13.5 SecurityInstrument

Canonical instrument catalog row.

Suggested fields:

- `Id`
- `Symbol`
- `ExchangeCode`
- `Name`
- `InstrumentType`
- `AssetClass`
- `Currency`
- `CountryCode`
- `Region`
- `IssuerCompanyId`
- `SectorClassificationId`
- `IndustryName`
- `DataProvider`
- `ProviderInstrumentId`
- `Active`
- `CreatedAtUtc`
- `UpdatedAtUtc`

### 13.6 IssuerCompany

Company-level issuer identity.

Suggested fields:

- `Id`
- `Name`
- `LegalName`
- `CountryCode`
- `WebsiteUrl`
- `SectorClassificationId`
- `IndustryName`
- `Description`
- `CreatedAtUtc`
- `UpdatedAtUtc`

### 13.7 SectorClassification

Internal sector taxonomy.

Suggested fields:

- `Id`
- `Standard`
- `Level`
- `ParentId`
- `Code`
- `Name`
- `DisplayName`
- `SortOrder`
- `Color`
- `IconKey`
- `Active`

Initial sectors:

- Energy
- Materials
- Industrials
- Consumer Discretionary
- Consumer Staples
- Health Care
- Financials
- Information Technology
- Communication Services
- Utilities
- Real Estate

### 13.8 PriceBar

Historical price data.

Suggested fields:

- `Id`
- `InstrumentId`
- `Date`
- `Interval`
- `Open`
- `High`
- `Low`
- `Close`
- `AdjustedClose`
- `Volume`
- `Currency`
- `Provider`
- `CreatedAtUtc`

MVP interval:

```text
Daily
```

### 13.9 FxRate

Foreign exchange rate for portfolio base currency conversion.

Suggested fields:

- `Id`
- `BaseCurrency`
- `QuoteCurrency`
- `Date`
- `Rate`
- `Provider`
- `CreatedAtUtc`

### 13.10 InvestorAnalysisProfile

Defines a user-selected risk and allocation profile.

Suggested fields:

- `Id`
- `OwnerAccountId`
- `Key`
- `Name`
- `Description`
- `ProfileType`
- `RiskLevel`
- `IsSystem`
- `Active`
- `CreatedAtUtc`
- `UpdatedAtUtc`

System profile examples:

- Defensive
- Balanced
- Growth
- Aggressive
- Income
- MacroSensitive

### 13.11 TargetAllocationPolicy

Target allocation ranges for a profile.

Suggested fields:

- `Id`
- `InvestorProfileId`
- `Dimension`
- `Key`
- `MinimumPercent`
- `TargetPercent`
- `MaximumPercent`
- `SeverityWhenBelow`
- `SeverityWhenAbove`

Dimensions:

- Sector
- AssetClass
- Region
- Currency
- SingleName
- Cash

### 13.12 PortfolioValuationSnapshot

Calculated portfolio valuation at a point in time.

Suggested fields:

- `Id`
- `PortfolioId`
- `OwnerAccountId`
- `AsOfDate`
- `BaseCurrency`
- `TotalMarketValue`
- `TotalCostBasis`
- `UnrealizedGainLoss`
- `DataQualityStatus`
- `CreatedAtUtc`

### 13.13 PortfolioRiskSnapshot

Calculated risk report snapshot.

Suggested fields:

- `Id`
- `PortfolioId`
- `OwnerAccountId`
- `AsOfDate`
- `ModelVersionId`
- `OverallScore`
- `ConcentrationScore`
- `SectorScore`
- `VolatilityScore`
- `DrawdownScore`
- `CurrencyScore`
- `MacroSensitivityScore`
- `DataQualityScore`
- `SummaryJson`
- `CreatedAtUtc`

Store structured details as explicit child rows later if query needs justify it.

### 13.14 MacroSeries

Macro time series definition.

Suggested fields:

- `Id`
- `Key`
- `Name`
- `CountryCode`
- `Region`
- `Category`
- `Frequency`
- `Unit`
- `SourceProvider`
- `SourceSeriesId`
- `Importance`
- `Active`
- `CreatedAtUtc`
- `UpdatedAtUtc`

### 13.15 MacroReleaseEvent

Specific scheduled or actual macro release.

Suggested fields:

- `Id`
- `SeriesId`
- `ReleaseAtUtc`
- `ActualValue`
- `ConsensusValue`
- `PreviousValue`
- `RevisedPreviousValue`
- `Unit`
- `Status`
- `SourceProvider`
- `SourceReleaseId`
- `CreatedAtUtc`
- `UpdatedAtUtc`

### 13.16 MacroObservation

Historical observation for a macro series.

Suggested fields:

- `Id`
- `SeriesId`
- `ObservationDate`
- `Value`
- `VintageDate`
- `IsInitialRelease`
- `SourceProvider`
- `CreatedAtUtc`

### 13.17 MacroConsensus

Consensus expectation before a release.

Suggested fields:

- `Id`
- `ReleaseEventId`
- `ConsensusValue`
- `ConsensusSource`
- `CapturedAtUtc`
- `Confidence`
- `CreatedAtUtc`

Consensus data may be limited or paid. MVP can use this table later while supporting estimated surprise first.

### 13.18 MacroSurprise

Calculated actual-vs-expected value.

Suggested fields:

- `Id`
- `ReleaseEventId`
- `SurpriseType`
- `ExpectedValue`
- `ActualValue`
- `SurpriseValue`
- `StandardizedSurprise`
- `Unit`
- `ModelVersionId`
- `Confidence`
- `CreatedAtUtc`

Surprise types:

- Consensus
- ModelEstimated
- PreviousDelta
- ZScore

### 13.19 MacroFactor

Normalized macro factor used by analytics.

Suggested fields:

- `Id`
- `Key`
- `Name`
- `Category`
- `Unit`
- `Transformation`
- `Active`

Examples:

- `us.cpi.surprise`
- `us.core-cpi.surprise`
- `us.pce.surprise`
- `us.nfp.surprise`
- `us.10y-yield.change`
- `oil.wti.change`
- `dxy.change`

### 13.20 SectorFactorSensitivity

Estimated relationship between a macro factor and a sector.

Suggested fields:

- `Id`
- `MacroFactorId`
- `SectorClassificationId`
- `ResponseWindow`
- `Beta`
- `Intercept`
- `R2`
- `PValue`
- `SampleStartDate`
- `SampleEndDate`
- `ObservationCount`
- `ModelVersionId`
- `Confidence`
- `CreatedAtUtc`

Interpretation:

```text
sector_return_percentage_points = intercept + beta * factor_shock
```

### 13.21 ScenarioRun

User-triggered or system-triggered scenario execution.

Suggested fields:

- `Id`
- `OwnerAccountId`
- `PortfolioId`
- `Name`
- `ScenarioType`
- `InputJson`
- `ModelVersionId`
- `Status`
- `CreatedByAccountId`
- `CreatedAtUtc`
- `CompletedAtUtc`

### 13.22 PortfolioImpactEstimate

Scenario result.

Suggested fields:

- `Id`
- `ScenarioRunId`
- `PortfolioId`
- `OwnerAccountId`
- `EstimatedImpactPercent`
- `EstimatedImpactAmount`
- `BaseCurrency`
- `LowerBoundPercent`
- `UpperBoundPercent`
- `Confidence`
- `TopRiskContributorJson`
- `SectorImpactJson`
- `HoldingImpactJson`
- `WarningsJson`
- `CreatedAtUtc`

### 13.23 AnalyticsModelVersion

Tracks analytics model identity.

Suggested fields:

- `Id`
- `Key`
- `Name`
- `Description`
- `Version`
- `Status`
- `TrainingStartDate`
- `TrainingEndDate`
- `CreatedAtUtc`
- `ActivatedAtUtc`

## 14. Shared Contract Models

Place shared API contracts in:

```text
platform/XN1Lab.Platform.CoreServices.Contracts/PortfolioAnalytics
```

Initial contracts:

```text
PortfolioContract
PortfolioWriteContract
PortfolioHoldingContract
PortfolioHoldingWriteContract
PortfolioCashPositionContract
SecurityInstrumentContract
InstrumentLookupResultContract
SectorClassificationContract
InvestorAnalysisProfileContract
TargetAllocationPolicyContract
PortfolioExposureContract
SectorExposureContract
PortfolioRiskReportContract
PortfolioRiskDimensionContract
MacroSeriesContract
MacroReleaseEventContract
MacroSurpriseContract
MacroCalendarItemContract
SectorFactorSensitivityContract
ScenarioRunContract
ScenarioRunRequest
PortfolioImpactEstimateContract
DataQualityWarningContract
```

Contract rules:

- use `*Contract` suffix where the model can be confused with domain/persistence entities
- request types may use natural request names
- contracts remain DTOs
- no EF Core dependency
- no product-only chart state
- no direct references to CoreServices data models

## 15. CoreServices Implementation Plan

Every new CoreServices context should follow the current implementation standard:

```text
ApiModel
  ->
DataModel
  ->
DbContext + Fluent API
  ->
EFQueryBuilder
  ->
Backend
  ->
BusinessLogic
  ->
Controller
  ->
APIBuilder activation
```

### 15.1 Domain Layer

Recommended folders:

```text
XN1Lab.CoreServices.Domain/PortfolioAnalytics
|-- ApiModels
|-- DataModels
`-- Enums
```

Domain responsibilities:

- simple API models if still needed by current CoreServices style
- persistence data models
- enum definitions
- conversion from data model to API model

Each real DataModel should implement:

```text
IAPIModelConvertible<TApiModel>
```

### 15.2 Application Layer

Recommended folders:

```text
XN1Lab.CoreServices.Application/PortfolioAnalytics
|-- PortfolioAnalyticsBusinessLogic.cs
|-- PortfolioAnalyticsPermissionKeys.cs
|-- PortfolioAnalyticsSeedData.cs
`-- Models
```

Responsibilities:

- portfolio CRUD orchestration
- holdings validation
- exposure calculation
- risk report calculation
- scenario run orchestration
- macro surprise calculation
- permission-aware business actions
- provider abstraction contracts when needed

Application-owned persistence abstractions:

```text
XN1Lab.CoreServices.Application/Abstractions/Persistence/IPortfolioAnalyticsBackend.cs
```

Provider abstractions:

```text
IMarketDataProvider
IMacroDataProvider
IInstrumentResolver
IFactorSensitivityEstimator
```

Only add these abstractions when implementation needs them. Do not create empty abstractions too early.

### 15.3 Infrastructure Layer

Recommended folders:

```text
XN1Lab.CoreServices.Infrastructure/PortfolioAnalytics/EF
XN1Lab.CoreServices.Infrastructure/PortfolioAnalytics/Providers
XN1Lab.CoreServices.Infrastructure/Persistence/PortfolioAnalytics
XN1Lab.CoreServices.Infrastructure/Persistence/QueryBuilders/PortfolioAnalytics
```

Responsibilities:

- EF backend implementation
- data provider implementations
- query builders
- persistence registration
- ingestion job implementation later

### 15.4 Persistence

Use EF Core and Fluent API.

Rules:

- lowercase table names
- lowercase column names
- explicit column names
- explicit column types
- enums stored as string
- `DateTime` stored as `timestamp without time zone` with `UsesUtc()`
- account ownership columns indexed
- soft-delete where needed

Example table names:

```text
portfolio_analytics_portfolios
portfolio_analytics_holdings
portfolio_analytics_cash_positions
portfolio_analytics_instruments
portfolio_analytics_sector_classifications
portfolio_analytics_price_bars
portfolio_analytics_fx_rates
portfolio_analytics_investor_profiles
portfolio_analytics_target_allocations
portfolio_analytics_valuation_snapshots
portfolio_analytics_risk_snapshots
portfolio_analytics_macro_series
portfolio_analytics_macro_release_events
portfolio_analytics_macro_observations
portfolio_analytics_macro_consensus
portfolio_analytics_macro_surprises
portfolio_analytics_macro_factors
portfolio_analytics_sector_factor_sensitivities
portfolio_analytics_scenario_runs
portfolio_analytics_impact_estimates
portfolio_analytics_model_versions
```

### 15.5 Backend

Suggested backend:

```text
PortfolioAnalyticsBackend
```

Responsibilities:

- query portfolios by owner account
- query holdings by portfolio and owner account
- query instrument catalog
- query macro data
- persist snapshots
- persist scenario runs
- persist impact estimates

### 15.6 Business Logic

Suggested service:

```text
PortfolioAnalyticsBusinessLogic
```

Responsibilities:

- validate active account access
- create/update/delete portfolio
- add/update/remove holdings
- resolve instruments
- calculate exposures
- calculate profile alignment
- calculate risk report
- calculate macro surprise
- run scenario
- produce report model

### 15.7 Controller

Suggested controller:

```text
PortfolioAnalyticsController
```

Base route:

```text
/api/xn1finance/portfolio-analytics
```

Initial endpoints:

```text
GET    /api/xn1finance/portfolio-analytics/portfolios
POST   /api/xn1finance/portfolio-analytics/portfolios
GET    /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}
PUT    /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}
DELETE /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}

GET    /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}/holdings
POST   /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}/holdings
PUT    /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}/holdings/{holdingId}
DELETE /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}/holdings/{holdingId}

GET    /api/xn1finance/portfolio-analytics/instruments/lookup?symbol={symbol}
GET    /api/xn1finance/portfolio-analytics/sector-classifications
GET    /api/xn1finance/portfolio-analytics/investor-profiles

GET    /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}/exposure
GET    /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}/risk-report
POST   /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}/scenario-runs
GET    /api/xn1finance/portfolio-analytics/scenario-runs/{scenarioRunId}

GET    /api/xn1finance/portfolio-analytics/macro/calendar
GET    /api/xn1finance/portfolio-analytics/macro/series
GET    /api/xn1finance/portfolio-analytics/macro/releases
GET    /api/xn1finance/portfolio-analytics/macro/factor-sensitivities
```

Admin/data endpoints can be added after the read/report loop works.

### 15.8 APIBuilder Activation

Register:

- business logic
- backend implementation
- provider implementations
- permission definitions
- seed data
- DB context configuration

Keep the registration consistent with existing CoreServices context patterns.

### 15.9 Implemented API And Database Slice

The first backend slice is implemented in CoreServices before UI work.

Implemented persistence:

```text
Connection string: PortfolioAnalyticsDb
Default database: xn1lab_xn1finance_portfolio_analytics
Tables:
  - xn1finance_portfolio_analytics_account_settings
  - xn1finance_portfolio_analytics_system_settings
  - xn1finance_portfolios
  - xn1finance_portfolio_holdings
  - xn1finance_security_instruments
  - xn1finance_macro_series
  - xn1finance_macro_release_events
```

Implemented CoreServices files:

```text
XN1Lab.CoreServices.Domain/PortfolioAnalytics
XN1Lab.CoreServices.Application/PortfolioAnalytics/PortfolioAnalyticsBusinessLogic.cs
XN1Lab.CoreServices.Application/Abstractions/Persistence/IPortfolioAnalyticsBackend.cs
XN1Lab.CoreServices.Infrastructure/Persistence/PortfolioAnalytics/PortfolioAnalyticsDatabaseContext.cs
XN1Lab.CoreServices.Infrastructure/Persistence/QueryBuilders/PortfolioAnalytics
XN1Lab.CoreServices.Infrastructure/PortfolioAnalytics/EF/PortfolioAnalyticsBackend.cs
XN1Lab.CoreServices.Infrastructure/PortfolioAnalytics/EF/PortfolioAnalyticsSchemaSeedService.cs
XN1Lab.CoreServices.Infrastructure/PortfolioAnalytics/EF/PortfolioAnalyticsReferenceDataSeedService.cs
XN1Lab.CoreServices.Host/Controllers/PortfolioAnalyticsController.cs
```

Implemented bootstrap behavior:

- CoreServices host seeds `PortfolioAnalyticsDb` through `PortfolioAnalyticsSchemaSeedService`
- bootstrap creates the Portfolio Analytics database when missing
- bootstrap creates or repairs Portfolio Analytics tables idempotently
- reference data seed upserts starter security instruments, sector mappings, macro series, and seed macro release events idempotently
- bootstrap reseeds permission definitions, XN1 application catalog rows, and XN1 application entitlements
- the 2026-05-17 smoke pass used the local `tmp/DbBootstrap` helper with `PortfolioAnalyticsDb` included in its workspace copy

Last DB and permission smoke result:

```text
Date: 2026-05-17 10:28 Europe/Vienna
PortfolioAnalyticsDb: passed schema checks, 7/7 expected tables present
PortfolioAnalyticsDb: passed reference data checks, 54 active instruments present
PortfolioAnalyticsDb: passed reference data checks, 20 active macro series present
PortfolioAnalyticsDb: passed reference data checks, 28 active macro release events present
PermissionDb: passed, 12/12 Portfolio Analytics permission definitions present
PermissionDb: passed, 12/12 active Portfolio Analytics account entitlements present
ProductCatalogDb: passed, active xn1finance.portfolio-analytics.web catalog row present
Warnings: none
```

Implemented endpoints:

```text
GET    /api/xn1finance/portfolio-analytics/settings/account
POST   /api/xn1finance/portfolio-analytics/settings/account
GET    /api/xn1finance/portfolio-analytics/settings/system
POST   /api/xn1finance/portfolio-analytics/settings/system

GET    /api/xn1finance/portfolio-analytics/portfolios
POST   /api/xn1finance/portfolio-analytics/portfolios
GET    /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}
PUT    /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}
DELETE /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}

GET    /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}/holdings
POST   /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}/holdings
GET    /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}/holdings/{holdingId}
PUT    /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}/holdings/{holdingId}
DELETE /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}/holdings/{holdingId}

GET    /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}/exposure
GET    /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}/earnings
POST   /api/xn1finance/portfolio-analytics/portfolios/{portfolioId}/scenarios/run

GET    /api/xn1finance/portfolio-analytics/instruments
GET    /api/xn1finance/portfolio-analytics/instruments/lookup?symbol={symbol}
GET    /api/xn1finance/portfolio-analytics/instruments/external-search?searchText={searchText}
GET    /api/xn1finance/portfolio-analytics/instruments/external-resolve?symbol={symbol}
GET    /api/xn1finance/portfolio-analytics/instruments/{instrumentId}
POST   /api/xn1finance/portfolio-analytics/instruments
PUT    /api/xn1finance/portfolio-analytics/instruments/{instrumentId}
DELETE /api/xn1finance/portfolio-analytics/instruments/{instrumentId}

GET    /api/xn1finance/portfolio-analytics/earnings/calendar

GET    /api/xn1finance/portfolio-analytics/macro/calendar
GET    /api/xn1finance/portfolio-analytics/macro/series
GET    /api/xn1finance/portfolio-analytics/macro/series/{macroSeriesId}
POST   /api/xn1finance/portfolio-analytics/macro/series
PUT    /api/xn1finance/portfolio-analytics/macro/series/{macroSeriesId}
DELETE /api/xn1finance/portfolio-analytics/macro/series/{macroSeriesId}

GET    /api/xn1finance/portfolio-analytics/macro/releases
GET    /api/xn1finance/portfolio-analytics/macro/releases/{releaseEventId}
GET    /api/xn1finance/portfolio-analytics/macro/series/{macroSeriesId}/releases
POST   /api/xn1finance/portfolio-analytics/macro/releases
PUT    /api/xn1finance/portfolio-analytics/macro/releases/{releaseEventId}
DELETE /api/xn1finance/portfolio-analytics/macro/releases/{releaseEventId}
```

Implemented security:

- active account scope is enforced in business logic
- owner account cannot be overridden by request payload
- account settings are scoped to the active account
- system refresh/provider settings require model read/write permissions
- exposure endpoint requires `xn1finance.portfolio-analytics.read`
- scenario run endpoint requires `xn1finance.portfolio-analytics.scenario.run`
- macro calendar endpoint requires `xn1finance.portfolio-analytics.macro-data.read`
- endpoints declare `xn1finance.portfolio-analytics.read`, `write`, `delete`, market-data, macro-data, model, and scenario permissions
- application key `xn1finance.portfolio-analytics.web` is registered in catalog/client seed data

Implemented refresh worker foundation:

```text
Options section: PortfolioAnalytics:Refresh
Hosted service: PortfolioAnalyticsDataRefreshHostedService
Refresh job: PortfolioAnalyticsDataRefreshJob
Macro provider abstraction: IMacroDataProvider
Market provider abstraction: IMarketDataProvider
Registered providers:
  - NoopMacroDataProvider
  - SeedMacroDataProvider
  - FredMacroDataProvider
  - NoopMarketDataProvider
  - AlphaVantageMarketDataProvider
Default provider key: seed
Default state: disabled
```

Implemented market data persistence:

```text
Latest prices: xn1finance_latest_market_prices
Daily/interval bars: xn1finance_market_price_bars
FX rates: xn1finance_fx_rates
Portfolio valuation snapshots: xn1finance_portfolio_valuation_snapshots
Earnings calendar: xn1finance_security_earnings_events
Earnings actual reports: xn1finance_security_earnings_reports
```

Rules:

- the worker must stay disabled unless a provider and schedule are explicitly configured
- `PortfolioAnalytics:Refresh:Enabled` remains the operator-level startup gate
- persisted system settings can disable macro refresh or change the macro provider used by the refresh job
- market refresh stays disabled by default through `PortfolioAnalytics:Refresh:RefreshMarketPrices = false`
- persisted `MarketDataProviderKey` can select a market provider when the worker is enabled
- provider implementations must map external macro releases through `MacroSeriesKey`
- `SeedMacroDataProvider` seeds the static macro series catalog and scheduled placeholder release events, not live actual/consensus values
- `FredMacroDataProvider` imports historical FRED observations as released macro events without consensus values
- releases are upserted by `(MacroSeriesId, ReleaseTimeUtc)`
- latest prices are upserted by `(SecurityInstrumentId, ProviderKey)`
- market price bars are upserted by `(SecurityInstrumentId, ProviderKey, Interval, BarTimeUtc)`
- earnings events are upserted by `(SecurityInstrumentId, ProviderKey, ReportDateUtc)`
- earnings reports are upserted by `SecurityEarningsEventId`
- consensus surprise is calculated when both actual and consensus values exist
- missing provider keys fall back to the no-op provider

Earnings calendar model:

```text
SecurityEarningsEvent
  - schedule/calendar record per security
  - stores report date, fiscal period ending, EPS estimate, report time, currency, provider key, and received timestamp
  - must remain focused on expected/reporting schedule data

SecurityEarningsReport
  - actual post-release report record per security earnings event
  - stores actual EPS, EPS surprise, actual revenue, revenue estimate, revenue surprise, report period, source metadata, and optional raw provider payload
  - belongs to one SecurityEarningsEvent and one SecurityInstrument
  - must not be embedded into portfolio holdings because holdings represent position state, not market/reporting facts

PortfolioEarningsEvent
  - read model derived from SecurityEarningsEvent, optional SecurityEarningsReport, and portfolio exposure
  - adds exposure value, exposure percent, days until report, UI severity, and actual report fields when available
```

Provider and refresh policy:

- earnings calendar data is never queried directly from the Blazor client
- CoreServices refreshes provider data into `xn1finance_security_earnings_events`
- UI and scenario pages read only from CoreServices API
- refresh job queries active stock instruments; ETFs/funds do not normally have company earnings dates
- missing symbols can be filled by instrument lookup first, then picked up by the scheduled refresh
- the default lookahead window is 90 days and can be changed through system settings
- production jobs must respect the external provider plan and rate limits

FRED provider configuration:

```json
{
  "PortfolioAnalytics": {
    "Refresh": {
      "MacroProviderKey": "fred"
    },
    "Providers": {
      "Fred": {
        "BaseUrl": "https://api.stlouisfed.org/fred",
        "ApiKey": "",
        "ApiKeyEnvironmentVariable": "FRED_API_KEY",
        "ObservationLookbackDays": 730,
        "RequestTimeoutSeconds": 30,
        "MaxSeriesPerRefresh": 0
      }
    }
  }
}
```

Secret handling:

- do not store `FRED_API_KEY` in committed appsettings files
- use environment variable `FRED_API_KEY` or local secret configuration
- FRED provides actual observations, not survey consensus; scenario surprise should use manual override or `actual - previous` until a consensus provider is added

Alpha Vantage provider configuration:

```json
{
  "PortfolioAnalytics": {
    "Refresh": {
      "MarketDataProviderKey": "alpha-vantage",
      "RefreshMarketPrices": true,
      "RefreshEarningsCalendar": true,
      "MarketPriceLookbackDays": 7,
      "EarningsCalendarLookaheadDays": 90,
      "MaxMarketInstrumentsPerRun": 10
    },
    "Providers": {
      "AlphaVantage": {
        "BaseUrl": "https://www.alphavantage.co/query",
        "ApiKey": "",
        "ApiKeyEnvironmentVariable": "ALPHA_VANTAGE_API_KEY",
        "RequestTimeoutSeconds": 30,
        "MaxBarsPerRequest": 100
      }
    }
  }
}
```

Alpha Vantage first slice:

- `SYMBOL_SEARCH` for instrument discovery
- `GLOBAL_QUOTE` for latest quote
- `TIME_SERIES_DAILY` for daily OHLCV bars
- `EARNINGS_CALENDAR` for upcoming earnings announcement dates and EPS estimates
- refresh job persists quotes to `xn1finance_latest_market_prices`
- refresh job persists daily bars to `xn1finance_market_price_bars`
- refresh job persists earnings dates to `xn1finance_security_earnings_events`
- provider key: `alpha-vantage`
- free-tier calls are rate limited; production SaaS must use a licensed plan before broad refresh jobs are enabled

Secret handling:

- do not store `ALPHA_VANTAGE_API_KEY` in committed appsettings files
- use environment variable `ALPHA_VANTAGE_API_KEY` or local secret configuration

Initial seed series:

```text
us-cpi
us-core-cpi
us-pce
us-core-pce
us-nonfarm-payrolls
us-unemployment-rate
us-fed-funds-rate
us-10y-treasury-yield
us-2y-treasury-yield
us-2y-10y-spread
us-retail-sales
us-industrial-production
ism-manufacturing-pmi
ism-services-pmi
wti-crude-oil
brent-crude-oil
dxy
high-yield-credit-spread
euro-area-cpi
china-manufacturing-pmi
```

Initial instrument seed:

```text
Seed count: 54 active instruments
Data source: xn1seed
Coverage: US mega-cap equities, sector ETFs, bond ETFs, commodities, crypto, cash, and FX
Sector taxonomy: internal GICS-like keys such as technology, financials, energy, health-care, utilities, real-estate, fixed-income, commodity, crypto, cash
```

Initial macro release seed:

```text
Seed count after 2026-05-17 smoke: 28 active release events
Status: Scheduled
Source: seed
Purpose: development and QA placeholders for macro calendar and scenario override workflows
Production rule: replace placeholder events with provider data before using them as market calendar truth
```

Build verification:

```text
dotnet build XN1Lab.CoreServices.sln -c Debug
Result: succeeded, 0 warnings, 0 errors

dotnet build XN1Lab.XN1Finance.PortfolioAnalytics.sln -c Debug
Result: succeeded, 0 warnings, 0 errors
```

### 15.10 Implemented Settings Slice

The settings slice is implemented as the first product-management surface.

Account settings:

- stored in `xn1finance_portfolio_analytics_account_settings`
- scoped by `owner_account_id`
- stores default base currency
- stores investor profile
- stores target allocation percentages
- stores rebalance and macro surprise thresholds

Account provider settings:

- stored in `xn1finance_portfolio_analytics_account_provider_settings`
- scoped by `owner_account_id`
- stores the selected macro and market provider keys for that account
- stores encrypted provider API keys for account-owned provider usage
- stores account-level request policy values such as minimum refresh interval, max symbols per refresh, and daily request limit
- never returns raw provider API keys from CoreServices responses

Account provider request usage:

- stored in `xn1finance_portfolio_analytics_provider_request_usage`
- keyed by `owner_account_id`, `provider_key`, and UTC usage date
- increments before an external provider request is made for account-scoped market data access
- blocks account-scoped external provider requests when the account daily request limit is reached
- exposes a read summary with `RequestCount`, `DailyRequestLimit`, and `RemainingRequests` for Settings UI visibility
- requires an account market data API key before interactive external symbol resolution/search is allowed

Holding price sync:

- the holdings table reads latest price, market value, and unrealized P/L from CoreServices response fields
- the UI must not call external providers while rendering rows
- the selected portfolio has an explicit sync action that calls CoreServices and refreshes stored latest prices
- the sync action respects account-scoped provider keys, max symbols per refresh, and daily request limits
- the sync action requires market data write permission

System settings:

- stored in `xn1finance_portfolio_analytics_system_settings`
- keyed by `settings_key`, initially `default`
- stores macro refresh enabled state
- stores macro series/release refresh switches
- stores macro and market provider keys
- stores refresh interval and release lookback/lookahead windows

The Blazor settings page uses the standard module page tab pattern:

- `Profile` tab for account allocation and investor profile settings
- `Providers` tab for account-scoped provider access, request usage, and request policy
- `System` tab for model/system refresh settings that require model permissions

The Blazor settings page uses `IPortfolioAnalyticsService`, which wraps `IPlatformApiClient`.
Pages must continue to avoid direct raw HTTP calls.

## 16. Blazor WebAssembly Product Implementation

### 16.1 Host Type

Use:

```text
Blazor WebAssembly
```

Project:

```text
products/xn1finance/portfolio-analytics/src/XN1Lab.XN1Finance.PortfolioAnalytics.Web
```

The host should use the platform SaaS composition pattern where possible:

```csharp
builder.Services.AddXN1SaaSHost(builder.Configuration, options =>
{
    options.ApplicationSection = "Application";
    options.AuthenticationSection = "Authentication";
    options.ApiSection = "CoreServicesApi";
});
```

### 16.2 Application Configuration

Recommended `wwwroot/appsettings.json` values:

```json
{
  "Application": {
    "Name": "XN1Finance Portfolio Analytics",
    "Key": "xn1finance.portfolio-analytics.web"
  },
  "CoreServicesApi": {
    "BaseUrl": "https://api.example.local"
  },
  "Authentication": {
    "ApplicationKey": "xn1finance.portfolio-analytics.web"
  }
}
```

The product host should use CoreServices identity client resolution instead of hardcoded host-to-client maps.

Runtime identity standard:

- `ApplicationKey`: `xn1finance.portfolio-analytics.web`
- XN1Lab shared account Keycloak `ClientId`: `895ed50b-c808-4f5f-9bb4-2fc6175592bc.xn1finance.portfolio-analytics.web`
- The browser host resolves the runtime `ClientId` from CoreServices with `GET /api/identity/client-resolution?host={host}&applicationKey=xn1finance.portfolio-analytics.web`.
- The product appsettings must not map host names to client ids or start login with the short application key as a static fallback.
- CoreServices account-client seed owns the `xn1_account_clients` mapping; Keycloak must also contain a public browser client with the same `ClientId` and the product redirect origins.

### 16.3 Product Feature Structure

Recommended structure:

```text
Features
|-- Dashboard
|-- Portfolios
|   |-- Pages
|   |-- Components
|   |-- Models
|   `-- Services
|-- Holdings
|   |-- Components
|   `-- Models
|-- Instruments
|   |-- Components
|   `-- Services
|-- InvestorProfiles
|-- RiskReports
|-- MacroCalendar
|-- ScenarioLab
|-- Reports
`-- Settings
```

### 16.4 Product Services

Pages and components must call product-local services.

Initial interfaces:

```text
IPortfolioAnalyticsService
IInstrumentLookupService
IMacroCalendarService
IScenarioService
```

Initial implementations:

```text
ApiPortfolioAnalyticsService
ApiInstrumentLookupService
ApiMacroCalendarService
ApiScenarioService
```

These services should use:

```text
IPlatformApiClient
```

Pages must not compose raw HTTP requests directly.

### 16.5 Pages

Initial pages:

```text
/finance/portfolio
/finance/portfolio/portfolios
/finance/portfolio/portfolios/{portfolioId}
/finance/portfolio/portfolios/{portfolioId}/holdings
/finance/portfolio/portfolios/{portfolioId}/risk
/finance/portfolio/portfolios/{portfolioId}/scenario
/finance/portfolio/macro/calendar
/finance/portfolio/settings
```

### 16.6 Shell And Navigation

Register module metadata through the platform module catalog.

Manifest:

```text
Key: xn1finance.portfolio-analytics
DisplayNameKey: xn1finance.portfolio_analytics.nav
RoutePrefix: /finance/portfolio
Icon: chart line or briefcase
Group: Finance
RequiredPermissions:
  - xn1finance.portfolio-analytics.read
AssemblyName: XN1Lab.XN1Finance.PortfolioAnalytics.Web
```

The product should render navigation with `PlatformNavigationMenu`.

### 16.7 UI Pattern

Use existing Platform UI primitives:

- `AppShell`
- `WorkspacePage`
- `WorkspacePageContent`
- shared tables
- shared flyouts
- shared loaders
- shared empty states
- platform alert service

Do not create a marketing landing page as the first screen.

The first screen after login should be the portfolio analytics workspace.

### 16.8 Charting

MVP can start with:

- CSS-based bars
- simple SVG charts
- lightweight custom components

If a chart library is added later, load it only inside analytics/report modules.

Chart types:

- sector exposure bar chart
- asset class allocation chart
- risk dimension score bars
- macro sensitivity heatmap
- scenario impact waterfall
- upcoming event relevance list

### 16.9 Localization

All visible strings should be localization-key based.

Recommended key prefix:

```text
xn1finance.portfolio_analytics
```

Examples:

```text
xn1finance.portfolio_analytics.nav
xn1finance.portfolio_analytics.portfolios.title
xn1finance.portfolio_analytics.holdings.add
xn1finance.portfolio_analytics.risk.concentration
xn1finance.portfolio_analytics.macro.calendar
xn1finance.portfolio_analytics.scenario.run
```

### 16.10 Validation And Feedback

Use platform validation and alert patterns:

- inline validation for forms
- platform alert for save/delete/run feedback
- normalized API errors
- localized validation messages
- no page-local ad hoc error banners unless the platform component cannot support the need

## 17. Analytics Design

### 17.1 Exposure Formula

For each holding:

```text
market_value_base = quantity * latest_price * fx_rate_to_base
exposure_percent = market_value_base / portfolio_total_market_value * 100
```

Current implementation status:

```text
market_value = quantity * latest_stored_market_price
```

If no latest market price exists, the implementation falls back to average cost and emits `MarketPriceMissing`.
If the latest stored price is stale, it emits `MarketPriceStale`.
FX conversion to portfolio base currency is the next valuation slice.

If latest price is missing:

```text
estimated_value_base = quantity * average_cost * fx_rate_to_base
```

The result must include a data quality warning.

### 17.2 Concentration Score

Initial concentration metrics:

- top holding percent
- top 5 holdings percent
- sector max percent
- Herfindahl-Hirschman Index style score

Example:

```text
hhi = sum(weight_percent_as_decimal ^ 2)
```

Map the result into a 0 to 100 score.

### 17.3 Target Range Comparison

Each exposure bucket can be:

- BelowRange
- InRange
- AboveRange
- NoTarget

### 17.4 Macro Surprise

Preferred calculation:

```text
surprise = actual - consensus
```

Fallback calculation:

```text
estimated_surprise = actual - expected_model_value
```

Expected model can start simple:

- previous value
- moving average
- seasonal moving average
- z-score baseline

The result must identify the method.

### 17.5 Sector Sensitivity

Initial model:

```text
sector_return = alpha + beta * macro_factor_shock + error
```

Response windows:

- 1 trading day
- 5 trading days
- 20 trading days

Store:

- beta
- R2
- p-value
- sample dates
- model version
- confidence

### 17.6 Portfolio Scenario Impact

For each sector:

```text
sector_impact_percent = beta * factor_shock
portfolio_contribution = sector_weight * sector_impact_percent
```

Portfolio impact:

```text
portfolio_impact_percent = sum(portfolio_contribution)
```

For holding-level approximation:

```text
holding_impact = holding_weight * sector_impact_percent
```

Later phases can add instrument beta, style factor beta, and market beta.

### 17.7 Confidence Score

Confidence should consider:

- observation count
- R2
- p-value
- data freshness
- whether consensus is real or estimated
- whether instrument mapping is complete
- whether price history is sufficient

## 18. Data Providers

### 18.1 Macro Data

Candidate free or low-cost sources:

- FRED
- OECD
- ECB
- World Bank
- TCMB EVDS for Turkey macro data
- manual CSV import

### 18.2 Market Data

Candidate free or low-cost sources:

- Stooq
- Financial Modeling Prep
- Twelve Data
- Alpha Vantage
- Polygon free tier where appropriate
- manual CSV import

### 18.3 Provider Rules

Provider integration must be behind explicit provider classes.

Provider records should capture:

- provider name
- provider symbol
- source series id
- source timestamp
- ingestion timestamp
- license notes where needed

Do not assume real-time data rights.

MVP should treat prices as delayed/end-of-day unless a data license explicitly allows more.

## 19. Data Quality Model

Every report should include warnings.

Warning examples:

```text
InstrumentUnresolved
SectorMissing
PriceMissing
PriceStale
FxRateMissing
ConsensusMissing
SurpriseEstimated
HistoryInsufficient
ModelLowConfidence
ProviderUnavailable
DataLicenseLimited
```

Each warning should have:

- code
- severity
- message key
- affected entity id
- affected entity type

## 20. MVP Phases

### Phase 0 - Documentation And Decisions

Deliver:

- architecture and requirements document
- naming decision
- boundary decision
- domain object list
- API draft
- MVP scope

### Phase 1 - Product Shell

Deliver:

- Blazor WebAssembly host
- platform SaaS host composition
- authentication/session hydration
- navigation module registration
- empty portfolio workspace
- route structure

### Phase 2 - Portfolio CRUD

Deliver:

- CoreServices PortfolioAnalytics context skeleton
- portfolio API
- holding API
- product service wrappers
- portfolio list page
- portfolio detail page
- holding editor flyout

### Phase 3 - Instrument And Exposure

Deliver:

- instrument catalog
- simple instrument lookup
- sector classification seed data
- exposure calculation
- sector/asset/currency allocation view
- data quality warnings

### Phase 4 - Investor Profile And Risk

Deliver:

- system investor profiles
- target allocation policies
- target comparison
- concentration risk score
- risk report endpoint
- risk report page

### Phase 5 - Macro Data And Calendar

Deliver:

- macro series catalog
- macro release event model
- initial provider or CSV import
- macro calendar page
- actual/consensus/surprise model

### Phase 6 - Scenario Lab

Deliver:

- factor sensitivity model
- first static/calibrated beta table
- scenario run endpoint
- scenario page
- portfolio impact estimate
- confidence and warning output

### Phase 7 - Reports And Export

Deliver:

- report page
- printable report layout
- CSV export
- later PDF export

## 21. Acceptance Criteria For MVP

MVP is acceptable when:

- a user can log in through the standard XN1Lab account/session flow
- a user can create a portfolio under active account scope
- a user can add at least 5 holdings manually
- the system resolves or flags each instrument
- the portfolio report shows sector exposure
- the portfolio report shows profile target comparison
- the portfolio report shows a concentration risk score
- the macro calendar shows at least seeded macro events
- the user can run at least one macro scenario
- the scenario result shows estimated portfolio impact and warnings
- backend endpoints enforce permissions
- product pages use product-local services over `IPlatformApiClient`
- no page directly builds raw HTTP calls
- no direct buy/sell recommendation language appears in the UI

## 22. Implementation Order

Recommended first implementation order:

1. Create Blazor WASM product host skeleton.
2. Add application configuration and SaaS host composition.
3. Register finance module navigation metadata.
4. Add CoreServices permissions.
5. Add shared contracts.
6. Add CoreServices data models for portfolio and holding.
7. Add EF mapping and migration.
8. Add backend/business/controller for portfolio CRUD.
9. Add product service wrappers.
10. Build portfolio list/detail/holding editor UI.
11. Add instrument lookup and sector seed data.
12. Add exposure endpoint and UI.
13. Add investor profile seed data.
14. Add risk report endpoint and UI.
15. Add macro series/release model.
16. Add scenario engine first version.
17. Add report view and export.

## 23. Open Decisions

### OD-001 First Market Data Provider

Need to choose first source:

- manual CSV only
- Stooq
- FMP
- Twelve Data

Recommendation:

```text
Start with manual seed/CSV plus one provider adapter.
```

### OD-002 Consensus Data Source

Consensus data is often paid.

Recommendation:

```text
Support consensus fields in the model, but MVP can use estimated surprise.
```

### OD-003 Sector Classification License

GICS may require licensing for direct use.

Recommendation:

```text
Use an internal GICS-like sector taxonomy until licensing is clear.
```

### OD-004 Separate Finance Service

Recommendation:

```text
Do not create a separate service at MVP. Start inside CoreServices.
```

### OD-005 Recommendation Engine

Recommendation:

```text
Do not build direct buy/sell recommendation features until legal/compliance review.
```

## 24. Final Architecture Decision

The agreed first product direction is:

```text
Product family: XN1Finance
Application: PortfolioAnalytics
Full product name: XN1Finance Portfolio Analytics
Project: XN1Lab.XN1Finance.PortfolioAnalytics.Web
ApplicationKey: xn1finance.portfolio-analytics.web
Backend context: CoreServices/PortfolioAnalytics
Contracts: Platform.CoreServices.Contracts/PortfolioAnalytics
Route prefix: /finance/portfolio
Permission prefix: xn1finance.portfolio-analytics
Host technology: Blazor WebAssembly
Positioning: portfolio risk intelligence and macro sensitivity analytics
```
