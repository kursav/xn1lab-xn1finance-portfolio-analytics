using System.ComponentModel.DataAnnotations;

namespace XN1Lab.XN1Finance.PortfolioAnalytics.Web.Features.Tracking.Models;

public sealed class ImportFinanceInstrumentRequest
{
    [Required, StringLength(80, MinimumLength = 1)]
    public string ProviderKey { get; set; } = "binance-spot";
    [Required, StringLength(80, MinimumLength = 1)]
    public string ProviderSymbol { get; set; } = string.Empty;
    public Guid? MarketDataConnectionId { get; set; }
}
