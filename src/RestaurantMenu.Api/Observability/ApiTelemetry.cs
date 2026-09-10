using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RestaurantMenu.Api.Observability;

public static class ApiTelemetry
{
    public const string MeterName = "RestaurantMenu.Api.Business";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Operations =
        Meter.CreateCounter<long>("restaurantmenu.business.operations");
    private static readonly Histogram<double> Duration =
        Meter.CreateHistogram<double>("restaurantmenu.business.operation.duration", "ms");

    public static void RecordOperation(string method, string route, int statusCode,
        double elapsedMilliseconds)
    {
        var tags = new TagList
        {
            { "http.request.method", NormalizeMethod(method) },
            { "http.route", route },
            { "operation.outcome", Outcome(statusCode) }
        };
        Operations.Add(1, tags);
        Duration.Record(elapsedMilliseconds, tags);
    }

    private static string NormalizeMethod(string method) => method switch
    {
        "POST" or "PUT" or "PATCH" or "DELETE" => method,
        _ => "OTHER"
    };

    private static string Outcome(int statusCode) => statusCode switch
    {
        >= 200 and < 400 => "succeeded",
        401 or 403 => "denied",
        _ => "failed"
    };
}
