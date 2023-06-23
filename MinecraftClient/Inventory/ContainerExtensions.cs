using System;

namespace MinecraftClient.Inventory;

using System.Linq;
using System.Linq.Expressions;

public enum ComparisonType
{
    Equals,
    NotEquals,
    Less,
    LessOrEqual,
    Greater,
    GreaterOrEqual
}

public static class ContainerExtensions
{
    public static bool IsFull(this Container container) =>
        container.Items.Values.All(item => item.Count >= item.Type.StackCount());

    public static int FirstEmptySlot(this Container container) =>
        container.Items.FirstOrDefault(slot => slot.Value.IsEmpty).Key;

    public static int FirstSlotWithItem(this Container container, ItemType type, int? count = null,
        ComparisonType comparison = ComparisonType.Equals) =>
        container.Items.FirstOrDefault(slot =>
            slot.Value.Type == type &&
            (!count.HasValue || CompareCounts(slot.Value.Count, count.Value, comparison))).Key;

    public static int LastSlotWithItem(this Container container, ItemType type, int? count = null,
        ComparisonType comparison = ComparisonType.Equals) =>
        container.Items.LastOrDefault(slot =>
                slot.Value.Type == type &&
                (!count.HasValue || CompareCounts(slot.Value.Count, count.Value, comparison)))
            .Key;

    // We could have used "Expression<Func<int, int, bool>> comparison = null" instead of "ComparisonType comparison", but it would look ugly to use
    private static bool CompareCounts(int value1, int value2, ComparisonType comparison)
    {
        var left = Expression.Constant(value1);
        var right = Expression.Constant(value2);

        Expression binaryExpression = comparison switch
        {
            ComparisonType.Less => Expression.LessThan(left, right),
            ComparisonType.LessOrEqual => Expression.LessThanOrEqual(left, right),
            ComparisonType.Greater => Expression.GreaterThan(left, right),
            ComparisonType.GreaterOrEqual => Expression.GreaterThanOrEqual(left, right),
            ComparisonType.Equals => Expression.Equal(left, right),
            ComparisonType.NotEquals => Expression.NotEqual(left, right),
            _ => Expression.Equal(left, right)
        };

        return Expression.Lambda<Func<bool>>(binaryExpression).Compile().Invoke();
    }
}