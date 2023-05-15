namespace OrderPaymentFsm.Enums
{
    internal enum OrderTrigger
    {
        CreateNewOrder,
        FindAndSaveReservation,
        CreateTicketOrder,
        CreateTicketOrderErr,
        FindRecentOrder,
        CreatePay,
        RecreatePay,
        CallbackSellOrder,
        CallbackFailOrder,
        SellTickets,
        SellTicketsRefundErr,
        FailSellTickets,
        TransitToFullPricePromo,

        RaiseLocalTransErr,
        TransitToCustomTransErr,
        TransitToGlobalErr
    }
}