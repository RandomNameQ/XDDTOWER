namespace Core.BoardV2
{
    public class ShopBoardV2 : BoardGridV2
    {
        public int defaultPrice = 1;

        public virtual bool CanPickUp(PlaceableObject obj)
        {
            return true;
        }
    }
}


