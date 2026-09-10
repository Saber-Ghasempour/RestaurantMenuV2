namespace RestaurantMenu.Notifications.Presentation;

public static class OrderNotificationGroups
{
    public static string GuestOrder(Guid orderId) => $"guest-order:{orderId:D}";
    public static string StaffBranch(Guid restaurantId, Guid branchId) =>
        $"staff-branch:{restaurantId:D}:{branchId:D}";
}
