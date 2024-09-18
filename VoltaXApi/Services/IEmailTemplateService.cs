namespace VoltaXApi.Services
{
  public interface IEmailTemplateService
  {
      string GetFooterTemplate();
      string GetHeaderTemplate();
  }
}