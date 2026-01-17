namespace VoltaXApi.Exceptions.AuthExceptions
{
    public class InvalidResetPasswordCodeException : Exception
    {
        public InvalidResetPasswordCodeException()
            : base("The reset password code is incorrect or invalid.")
        {
        }

        public InvalidResetPasswordCodeException(string message)
            : base(message)
        {
        }
    }
}