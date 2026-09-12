# Tracking UI preview fixture

This local test host renders the production `TrackingPlansPage`, editor and display components with an in-memory fixture. It is for interactive browser checks; it never connects to CoreServices, an identity provider or a brokerage account. BTC/USDT quotes and candles come from Binance's public market-data-only API; a failed live request is shown as an error instead of falling back to a fixed price. Changes disappear when the browser circuit or process is restarted.

The host binds only `127.0.0.1:7547`. Its test permission resolver and fixture service are registered exclusively in this test project; production authentication and HTTP clients are unchanged. Do not deploy this test project.

From the repository root, use the configured .NET 9 SDK:

```powershell
dotnet run --project tests/XN1Lab.XN1Finance.Tracking.Preview/XN1Lab.XN1Finance.Tracking.Preview.csproj
```

When validating against an isolated Platform checkout, append `-p:XN1LabPlatformRoot=<absolute-platform-path>`. Open `http://127.0.0.1:7547/finance/portfolio-management/tracking` and verify plan selection, preview, history tabs, notifications, draft editing, indicator settings and nested rule editing. The actual backend HTTP contract is independently checked by `FinanceTrackingClientTests`.
