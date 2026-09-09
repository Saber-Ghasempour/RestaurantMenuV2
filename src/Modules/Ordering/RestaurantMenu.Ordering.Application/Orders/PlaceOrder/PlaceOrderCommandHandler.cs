using System.Security.Cryptography;
using System.Text.Json;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Application.DiningSessions.ResolveDiningSession;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Application.Orders.PlaceOrder;

public sealed class PlaceOrderCommandHandler(
    IQueryHandler<ResolveDiningSessionQuery, Result<DiningSessionScope>> sessionResolver,
    ICatalogOrderSnapshotProvider catalog,
    IDiningTableSnapshotProvider tables,
    IOrderRepository orders,
    IIdempotencyRepository idempotency,
    IOrderingUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<PlaceOrderCommand, Result<PlaceOrderResponse>>
{
    public async Task<Result<PlaceOrderResponse>> Handle(PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) ||
            command.IdempotencyKey.Length > IdempotencyRecord.MaxKeyLength ||
            command.IdempotencyKey.Any(character => character < 0x21 || character > 0x7e))
            return Result.Failure<PlaceOrderResponse>(PlaceOrderErrors.IdempotencyKeyRequired);

        var sessionResult = await sessionResolver.Handle(
            new ResolveDiningSessionQuery(command.DiningSessionToken), cancellationToken);
        if (sessionResult.IsFailure) return Result.Failure<PlaceOrderResponse>(sessionResult.Error);
        var scope = sessionResult.Value; var now = timeProvider.GetUtcNow();
        var idempotencyScope = $"dining-session:{scope.SessionId:N}";
        var requestHash = Hash(command.CustomerNote, command.Lines);
        var existing = await idempotency.GetAsync(idempotencyScope, command.IdempotencyKey, now, cancellationToken);
        if (existing is not null)
        {
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(existing.RequestHash), Convert.FromHexString(requestHash)))
                return Result.Failure<PlaceOrderResponse>(PlaceOrderErrors.IdempotencyKeyReused);
            var prior = await orders.GetByIdAsync(existing.ResourceId, cancellationToken);
            if (prior is not null) return Result.Success(Map(prior, true));
        }

        var inputLines = command.Lines ?? [];
        if (inputLines.Count == 0)
            return Result.Failure<PlaceOrderResponse>(OrderErrors.LinesRequired);
        if (inputLines.Count > Order.MaxLineCount)
            return Result.Failure<PlaceOrderResponse>(OrderErrors.TooManyLines);
        if (inputLines.Any(line => line.Quantity is < 1 or > OrderLine.MaxQuantity))
            return Result.Failure<PlaceOrderResponse>(OrderErrors.InvalidQuantity);
        if (inputLines.Any(line => !string.IsNullOrWhiteSpace(line.Note) && line.Note.Trim().Length > OrderLine.MaxNoteLength))
            return Result.Failure<PlaceOrderResponse>(OrderErrors.LineNoteTooLong);
        if (!string.IsNullOrWhiteSpace(command.CustomerNote) && command.CustomerNote.Trim().Length > Order.MaxCustomerNoteLength)
            return Result.Failure<PlaceOrderResponse>(OrderErrors.CustomerNoteTooLong);

        var snapshots = await catalog.GetOrderableAsync(scope.RestaurantId, scope.BranchId,
            inputLines.Select(line => new CatalogOrderLineRequest(line.MenuItemId, line.VariantId)).ToArray(), cancellationToken);
        if (snapshots is null || snapshots.Count != inputLines.Count)
            return Result.Failure<PlaceOrderResponse>(PlaceOrderErrors.ItemsNotOrderable);
        var tableName = await tables.GetDisplayNameAsync(scope.RestaurantId, scope.BranchId,
            scope.DiningTableId, cancellationToken);
        if (tableName is null)
            return Result.Failure<PlaceOrderResponse>(PlaceOrderErrors.DiningTableUnavailable);

        var trustedLines = snapshots.Select((snapshot, index) => new OrderLineSnapshot(
            snapshot.MenuItemId, snapshot.VariantId, snapshot.ItemName, snapshot.VariantName,
            snapshot.UnitPriceAmount, snapshot.Currency, inputLines[index].Quantity,
            inputLines[index].Note)).ToArray();
        var orderId = OrderId.New();
        var result = Order.Create(orderId, $"O-{orderId.Value:N}"[..20].ToUpperInvariant(),
            scope.RestaurantId, scope.BranchId, scope.DiningTableId, tableName, scope.SessionId,
            command.CustomerNote, trustedLines, now);
        if (result.IsFailure) return Result.Failure<PlaceOrderResponse>(result.Error);

        orders.Add(result.Value);
        idempotency.Add(new IdempotencyRecord(Guid.CreateVersion7(), idempotencyScope,
            command.IdempotencyKey, requestHash, orderId, 201, now, now.AddHours(24)));
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (IdempotencyKeyAlreadyExistsException)
        { return Result.Failure<PlaceOrderResponse>(PlaceOrderErrors.IdempotencyRace); }
        return Result.Success(Map(result.Value, false));
    }

    private static string Hash(string? note, IReadOnlyList<PlaceOrderLine>? lines)
    {
        var canonical = new
        {
            CustomerNote = Normalize(note),
            Lines = (lines ?? []).Select(line => new
            { line.MenuItemId, line.VariantId, line.Quantity, Note = Normalize(line.Note) }).ToArray()
        };
        return Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(canonical)));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static PlaceOrderResponse Map(Order order, bool replay) => new(order.Id.Value,
        order.PublicNumber, order.Status.ToString(), order.Currency, order.SubtotalAmount,
        order.TotalAmount, order.CreatedAtUtc, order.Lines.Select(line => new PlacedOrderLineResponse(
            line.Id.Value, line.MenuItemId!.Value, line.VariantId, line.ItemName, line.VariantName,
            line.UnitPriceAmount, line.Currency, line.Quantity, line.LineTotalAmount, line.Note)).ToArray(), replay);
}
