namespace VoltaXApi.Services
{
  public interface IFileWriter
  {
      void WriteMessageToFile(string directory, string fileName, byte[] message);
  }
}