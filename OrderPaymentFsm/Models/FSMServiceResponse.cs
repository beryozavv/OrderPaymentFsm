using OrderPaymentFsm.Enums;

namespace OrderPaymentFsm.Models
{
    public class FsmServiceResponse
    {
        public FsmResponseStatus Status { get; set; }

        public string? ErrorMessage { get; set; }
    }

    public class FsmServiceResponse<T> : FsmServiceResponse
    {
        public FsmServiceResponse()
        {
        }

        public FsmServiceResponse(T result)
        {
            Result = result;
        }

        public T? Result { get; set; }
    }
}