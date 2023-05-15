namespace OrderPaymentFsm.Exceptions
{
    internal class UnsupportedResponseStatusException : Exception
    {
        public UnsupportedResponseStatusException()
        {
        }

        public UnsupportedResponseStatusException(string message) : base(message)
        {
        }
    }
}