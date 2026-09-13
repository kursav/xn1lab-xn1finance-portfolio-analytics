using System.Globalization;

namespace XN1Lab.XN1Finance.Tracking.Preview;

internal static class PreviewPort
{
    public static int Parse(string[] args)
    {
        if (args.Length == 0) return 7547;
        if (args.Length != 2 || args[0] != "--preview-port" ||
            !int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out var port) ||
            port is < 1024 or > 65535)
            throw new ArgumentException("Use --preview-port with a port from 1024 to 65535.", nameof(args));
        return port;
    }
}
