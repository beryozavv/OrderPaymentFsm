namespace OrderPaymentFsm.Enums
{
    [Flags]
    public enum OrderState
    {
        Init = 1,
        NewOrder = 2,
        ReservedTicketSaved = 4,
        TicketOrderCreated = 8,
        TicketOrderErr = 16,
        PayLinkCreated = 32,
        OrderSold = 64,
        OrderSellFailed = 128,
        TicketsSold = 256,
        NeedRefund = 512,
        TicketsSellFailed = 1024,

        GlobalOrderErr = 2048,

        ReservationTransErr = 4096,
        TicketOrderTransErr = 8192,
        PayTransErr = 16384,
        RecentOrderTransErr = 32768,
        SellTicketsTransErr = 65536,
        FailSellTicketsTransErr = 131072,
        PayLinkCreatedTransErr = 262144,
        FullPricePromo = 524288,
        FullPricePromoTransErr = 1048576,

        AllTransientErrors = ReservationTransErr | TicketOrderTransErr | PayTransErr | RecentOrderTransErr | SellTicketsTransErr | FailSellTicketsTransErr |
                             PayLinkCreatedTransErr | FullPricePromoTransErr,
        AllErrors = AllTransientErrors | GlobalOrderErr | TicketOrderErr,

        AllFailed = TicketsSellFailed | OrderSellFailed | NeedRefund
    }

    internal static class OrderEnumExtensions
    {
        public static bool IsTransientErr(this OrderState state)
        {
            return (state & OrderState.AllTransientErrors) == state;
        }

        public static bool IsError(this OrderState state)
        {
            return (state & OrderState.AllErrors) == state;
        }

        public static bool IsFailed(this OrderState state)
        {
            return (state & OrderState.AllFailed) == state;
        }
    }
}