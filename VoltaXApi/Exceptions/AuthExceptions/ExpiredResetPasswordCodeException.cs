namespace VoltaXApi.Exceptions
{
    public class ExpiredResetPasswordCodeException : Exception
    {
        public DateTime ExpirationTime { get; }

        public ExpiredResetPasswordCodeException(DateTime expirationTime) 
            : base("The reset password code has expired. Please request a new code.")
        {
            ExpirationTime = expirationTime;
        }

        public ExpiredResetPasswordCodeException(string message) 
            : base(message)
        {
        }
    }
}
