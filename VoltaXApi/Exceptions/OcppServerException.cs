namespace VoltaXApi.Exceptions;
public class OcppServerException : Exception
    {
        public OcppServerException(string message) : base(message) { }
    }