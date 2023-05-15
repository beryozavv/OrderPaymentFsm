using OrderPaymentFsm.Enums;

namespace OrderPaymentFsm.Models.Tickets
{
    public record BookedTicketItem
    {
        public long Id { get; init; }
        public long EventId { get; init; }
        public double Price { get; init; }
        public double OriginalPrice { get; init; }
        public long Fee { get; init; }
        public long OriginalFee { get; init; }
        public long Type { get; init; }
        public string Sector { get; init; } = null!;
        public long SectorId { get; init; }
        public long Row { get; init; }
        public long Place { get; init; }
        public long PlaceId { get; init; }
        public double DiscountPrice { get; init; }
        public TicketStatusEnum Status { get; init; }
        public string? Barcode { get; init; }
    }
}