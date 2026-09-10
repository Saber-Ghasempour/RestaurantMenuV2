namespace RestaurantMenu.Api.Infrastructure;

public sealed class IdempotencyMiddleware(RequestDelegate next)
{
    public const string HeaderName = "Idempotency-Key";
    public const string ItemName = "RestaurantMenu.IdempotencyKey";
    public const string InvalidItemName = "RestaurantMenu.InvalidIdempotencyKey";

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method) &&
            context.Request.Path == "/api/public/orders")
        {
            var values = context.Request.Headers[HeaderName];
            var key = values.Count == 1 ? values[0] : null;
            if (string.IsNullOrWhiteSpace(key) || key.Length > 128 ||
                key.Any(character => character < 0x21 || character > 0x7e))
            {
                context.Items[InvalidItemName] = true;
            }
            else context.Items[ItemName] = key;
        }

        await next(context);
    }
}
