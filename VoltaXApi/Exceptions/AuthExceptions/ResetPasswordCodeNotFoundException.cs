namespace VoltaXApi.Exceptions.AuthExceptions
{
    public class ResetPasswordCodeNotFoundException : Exception
    {
        public ResetPasswordCodeNotFoundException() 
            : base("No reset password request found for this user. Please request a password reset first.")
        {
        }

        public ResetPasswordCodeNotFoundException(string message) 
            : base(message)
        {
        }
    }
}
