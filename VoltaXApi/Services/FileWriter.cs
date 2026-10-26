namespace VoltaXApi.Services
{
  public class FileWriter
  {
      public void WriteMessageToFile(string directory, string fileName, byte[] message)
      {
          string fullPath = Path.Combine(directory, fileName);
          try
          {
              File.WriteAllBytes(fullPath, message);
          }
          catch (Exception)
          {
            // Message dumps are an opt-in debugging aid (MessageDumpDir): a failed write must not break the charger connection.
          }
      }
  }
}