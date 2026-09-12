# XN1Finance Tracking Plans UI

The tracking workspace lets each account define an explainable market-monitoring plan without hard-coding a single RSI strategy. It is available at `/finance/portfolio-management/tracking`; the former `/finance/portfolio/tracking` route remains as a compatibility alias.

## Plan model

A plan contains one or more provider-backed instruments, versioned indicator bindings, weighted score components, entry and exit rules, timing, notification preferences and execution assumptions. Each indicator binding selects its own candle timeframe, so a plan can combine, for example, RSI(14) on two-hour candles with EMA(50) on daily candles.

Rules support comparison, ALL, ANY and NOT nodes. Operands may use constants, indicator outputs or candle fields. Entry and exit rules may additionally use the calculated total score. The UI limits nesting and node count, keeps indicator references consistent when bindings are renamed and prevents removal of a binding that is still referenced.

The backend remains authoritative. The editor loads the indicator/provider capability catalogs, validates the complete definition before every save and sends the revision that was loaded. A conflict preserves the user's input instead of silently overwriting a newer version. Saving creates a draft version; activation is a separate, explicit action after a fresh validation.

## Runtime views

The workspace exposes plan overview, side-effect-free preview, evaluations, signals, paper portfolio and trades, in-app notifications, and immutable version inspection. Preview calculates the current decision and weighted contribution details but does not create an order, signal or notification.

Observe and Paper modes are editable. Paper mode uses a persistent simulated wallet with budget, position, stop-loss, take-profit, fee and slippage assumptions. Existing Live or Limit values can be inspected without being silently downgraded, but this UI does not enable real brokerage orders. Live execution requires a separately reviewed broker adapter, credential storage, exchange constraints, idempotency and operational controls.

## Access and availability

The page checks account read permission before making API calls and disables mutations when write permission is absent. Account switches recreate the routed page, clearing account-scoped view state. Backend capability flags independently control evaluation, scheduling, paper trading and notification actions; unavailable services are shown as unavailable rather than replaced with example data.

## Verification

`XN1Lab.XN1Finance.Tracking.Tests` covers the authenticated API client contract, permissions, loading and error states, preview safety, activation confirmation, optimistic concurrency, validation, recursive rule editing and paper/notification behavior. The test project is part of the application solution and runs in the development workflow.

`XN1Lab.XN1Finance.Tracking.Preview` is a local-only interactive host for responsive browser checks with visible example-data labeling. It never connects to CoreServices, identity or a brokerage account and must not be deployed.
