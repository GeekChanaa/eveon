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
          catch (Exception ex)
          {
            Console.WriteLine("this is an error in the write message tofile function");
          }
      }
  }
}