namespace Echo.Core
{
    public enum EndingOrder
    {
        GoodThenSecret
    }

    public sealed class EndingPlan
    {
        public bool HasGood { get; }
        public bool HasSecret { get; }
        public EndingOrder Order { get; }

        public EndingPlan(bool hasGood, bool hasSecret, EndingOrder order)
        {
            HasGood = hasGood;
            HasSecret = hasSecret;
            Order = order;
        }
    }

    /// <summary>
    /// Pure C# ending plan evaluator.
    /// Good Ending is always triggered; Secret Ending is triggered sequentially afterwards
    /// if and only if Morse was transmitted (Progress.MorseSent).
    /// </summary>
    public static class EndingRules
    {
        public static EndingPlan Evaluate(bool morseSent)
        {
            return new EndingPlan(
                hasGood: true,
                hasSecret: morseSent,
                order: EndingOrder.GoodThenSecret
            );
        }
    }
}

