namespace DropSystem
{
    public readonly struct GoldDropResult
    {
        public GoldDropResult(int amount)
        {
            Amount = amount > 0 ? amount : 0;
        }

        public int Amount { get; }
        public bool HasGold => Amount > 0;
    }
}
