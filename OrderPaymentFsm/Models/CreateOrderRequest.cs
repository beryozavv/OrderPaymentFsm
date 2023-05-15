namespace OrderPaymentFsm.Models
{
    /// <summary>
    /// Запрос на создание заказа
    /// </summary>
    public class CreateOrderRequest
    {
        /// <summary>
        /// Электронный адрес покупателя
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// Промокод
        /// </summary>
        public string? Promocode { get; set; }
    }
}