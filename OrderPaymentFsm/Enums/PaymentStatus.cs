namespace OrderPaymentFsm.Enums
{
    public enum PaymentStatus // что нужно хранить здесь? какой enum?
    {
        New,
        Canceled,
        Authorized,
        Reversed,
        DeadlineExpired,
        Rejected,
        PartialReversed,
        Confirmed,
        PartialRefunded,
        Refunded,
        FormShowed

        // todo all statuses https://www.tinkoff.ru/kassa/develop/api/payments/
    }
}