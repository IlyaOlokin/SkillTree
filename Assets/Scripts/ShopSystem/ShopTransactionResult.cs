namespace ShopSystem
{
    public enum ShopTransactionResult
    {
        Success = 0,
        InvalidShop = 1,
        InvalidEntry = 2,
        OutOfStock = 3,
        NotEnoughGold = 4,
        InventoryFull = 5,
        InvalidPrice = 6,
        InvalidInventorySlot = 7
    }
}
