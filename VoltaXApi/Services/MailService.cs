using VoltaXApi.Settings;
using VoltaXApi.Models;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.IO;
using System;
using MailKit;

namespace VoltaXApi.Services
{
    public class MailService : IMailService
    {
        private readonly MailSettings _mailSettings;
				private readonly IEmailTemplateService _emailTemplateService;
        public MailService(
					IOptions<MailSettings> mailSettings,
					IEmailTemplateService emailTemplateService)
        {
            _mailSettings = mailSettings.Value;
						_emailTemplateService = emailTemplateService;
        }

        public async Task SendEmailAsync(MailRequest mailRequest)
        {
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(_mailSettings.Mail);
            email.To.Add(MailboxAddress.Parse(mailRequest.ToEmail));
            email.Subject = mailRequest.Subject;
            var builder = new BodyBuilder();
            if (mailRequest.Attachments != null)
            {
                byte[] fileBytes;
                foreach (var file in mailRequest.Attachments)
                {
                    if (file.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            file.CopyTo(ms);
                            fileBytes = ms.ToArray();
                        }
                        builder.Attachments.Add(file.FileName, fileBytes, ContentType.Parse(file.ContentType));
                    }
                }
            }
            builder.HtmlBody = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>A simple, clean, and responsive HTML invoice template</title><style>.invoice-box {max-width: 800px;margin: auto;padding: 30px;border: 1px solid #eee;box-shadow: 0 0 10px rgba(0, 0, 0, .15);font-size: 16px;line-height: 24px;font-family: 'Helvetica Neue', 'Helvetica', Helvetica, Arial, sans-serif;color: #555;}.invoice-box table {width: 100%;line-height: inherit;text-align: left;}.invoice-box table td {padding: 5px;vertical-align: top;}.invoice-box table tr td:nth-child(2) {text-align: right;}.invoice-box table tr.top table td {padding-bottom: 20px;}.invoice-box table tr.top table td.title {font-size: 45px;line-height: 45px;color: #333;}.invoice-box table tr.information table td {padding-bottom: 40px;}.invoice-box table tr.heading td {background: #eee;border-bottom: 1px solid #ddd;font-weight: bold;}.invoice-box table tr.details td {padding-bottom: 20px;}.invoice-box table tr.item td{border-bottom: 1px solid #eee;}.invoice-box table tr.item.last td {border-bottom: none;}.invoice-box table tr.total td:nth-child(2) {border-top: 2px solid #eee;font-weight: bold;}@media only screen and (max-width: 600px) {.invoice-box table tr.top table td {width: 100%;display: block;text-align: center;}.invoice-box table tr.information table td {width: 100%;display: block;text-align: center;}}/** RTL **/.rtl {direction: rtl;font-family: Tahoma, 'Helvetica Neue', 'Helvetica', Helvetica, Arial, sans-serif;}.rtl table {text-align: right;}.rtl table tr td:nth-child(2) {text-align: left;}</style></head><body><div class=\"invoice-box\"><table cellpadding=\"0\" cellspacing=\"0\"><tr class=\"top\"><td colspan=\"2\"><table><tr><td class=\"title\"><img src=\"\" style=\"width:100%; max-width:300px;\"></td><td>Contact Us Message</td></tr></table></td></tr><tr class=\"information\"><td colspan=\"2\"><table><tr><td>  Promocups, Inc.<br>   Industrieweg 20-15 3846 BD<br>Harderwijk,Nederland</td><td><br>From : "+mailRequest.Name+" <br>Subject : "+mailRequest.Subject+" <br>Email : "+mailRequest.Email+"  <br>Phone : "+mailRequest.Phone+"</td></tr></table></td></tr></table><tr class=\"information\"><td colspan=\"2\"><table><tr><td>  Message</td><td><br>"+mailRequest.Body+"</td></tr></table></td></tr></div></body></html>";
            email.Body = builder.ToMessageBody();
            using var smtp = new SmtpClient();
            smtp.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(_mailSettings.Mail, _mailSettings.Password);
            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }

        public async Task SendVerificationEmailAsync(MailRequest mailRequest, string verificationLink)
				{
					var email = new MimeMessage();
					Console.WriteLine("this is the mail settings : ");
					Console.WriteLine("MAIL : " + _mailSettings.Mail);
					Console.WriteLine("MAIL : " + _mailSettings.Host);
					Console.WriteLine("MAIL : " + _mailSettings.Password);
					Console.WriteLine("MAIL : " + _mailSettings.Port);
					email.From.Add(new MailboxAddress("TESTER", _mailSettings.Mail));
					email.Sender = MailboxAddress.Parse(_mailSettings.Mail);
					email.To.Add(MailboxAddress.Parse(mailRequest.ToEmail));
					email.Subject = mailRequest.Subject;
					var builder = new BodyBuilder();
					if (mailRequest.Attachments != null)
					{
						byte[] fileBytes;
						foreach (var file in mailRequest.Attachments)
						{
							if (file.Length > 0)
							{
								using (var ms = new MemoryStream())
								{
									file.CopyTo(ms);
									fileBytes = ms.ToArray();
								}
								builder.Attachments.Add(file.FileName, fileBytes, ContentType.Parse(file.ContentType));
							}
						}
					}
					builder.HtmlBody = _emailTemplateService.GetHeaderTemplate() + $@"
					<table class=""row row-2"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #f5f5f5;"">
								<tbody>
									<tr>
										<td>
											<table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #ffffff; color: #000000; width: 500px; margin: 0 auto;"" width=""500"">
												<tbody>
													<tr>
														<td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 20px; padding-top: 15px; vertical-align: top; border-top: 0px; border-right: 0px; border-bottom: 0px; border-left: 0px;"">
															<table class=""image_block block-1"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
																<tr>
																	<td class=""pad"" style=""padding-bottom:5px;padding-left:5px;padding-right:5px;width:100%;"">
																		<div class=""alignment"" align=""center"" style=""line-height:10px"">
																			<div class=""fullWidth"" style=""max-width: 350px;""><img src=""https://d1oco4z2z1fhwp.cloudfront.net/templates/default/2966/gif-resetpass.gif"" style=""display: block; height: auto; border: 0; width: 100%;"" width=""350"" alt=""reset-password"" title=""reset-password"" height=""auto""></div>
																		</div>
																	</td>
																</tr>
															</table>
															<table class=""heading_block block-2"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
																<tr>
																	<td class=""pad"" style=""text-align:center;width:100%;"">
																		<h1 style=""margin: 0; color: #393d47; direction: ltr; font-family: Tahoma, Verdana, Segoe, sans-serif; font-size: 25px; font-weight: normal; letter-spacing: normal; line-height: 120%; text-align: center; margin-top: 0; margin-bottom: 0; mso-line-height-alt: 30px;""><strong>Forgot your password? </strong></h1>
																	</td>
																</tr>
															</table>
															<table class=""paragraph_block block-3"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; word-break: break-word;"">
																<tr>
																	<td class=""pad"">
																		<div style=""color:#393d47;font-family:Montserrat,sans-serif;font-size:14px;line-height:150%;text-align:center;mso-line-height-alt:21px;"">
																			<p style=""margin: 0; word-break: break-word;""><span style=""word-break: break-word;""><span style=""word-break: break-word;"">No worries, we've got you covered!&nbsp; </span><span style=""word-break: break-word;"">Let’s get you a new password.</span></span></p>
																		</div>
																	</td>
																</tr>
															</table>
															<table class=""button_block block-4"" width=""100%"" border=""0"" cellpadding=""15"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
																<tr>
																	<td class=""pad"">
																		<div class=""alignment"" align=""center""><!--[if mso]>
		<v:roundrect xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:w=""urn:schemas-microsoft-com:office:word"" href=""www.restpasswordlink.com"" style=""height:58px;width:270px;v-text-anchor:middle;"" arcsize=""35%"" strokeweight=""0.75pt"" strokecolor=""#FFC727"" fillcolor=""#e5b223"">
		<w:anchorlock/>
		<v:textbox inset=""0px,0px,0px,0px"">
		<center dir=""false"" style=""color:#393d47;font-family:Tahoma, Verdana, sans-serif;font-size:18px"">
		<![endif]--><a href=""{verificationLink}"" target=""_blank"" style=""background-color:#e5b223;border-bottom:1px solid #FFC727;border-left:1px solid #FFC727;border-radius:20px;border-right:1px solid #FFC727;border-top:1px solid #FFC727;color:#393d47;display:inline-block;font-family: Montserrat, sans-serif;font-size:18px;font-weight:undefined;mso-border-alt:none;padding-bottom:10px;padding-top:10px;text-align:center;text-decoration:none;width:auto;word-break:keep-all;""><span style=""word-break: break-word; padding-left: 50px; padding-right: 50px; font-size: 18px; display: inline-block; letter-spacing: normal;""><span style=""word-break: break-word;""><span style=""word-break: break-word; line-height: 36px;"" data-mce-style><strong>Verify Email</strong></span></span></span></a><!--[if mso]></center></v:textbox></v:roundrect><![endif]--></div>
																	</td>
																</tr>
															</table>
															<table class=""paragraph_block block-5"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; word-break: break-word;"">
																<tr>
																	<td class=""pad"" style=""padding-bottom:5px;padding-left:10px;padding-right:10px;padding-top:10px;"">
																		<div style=""color:#393d47;font-family:Montserrat,sans-serif;font-size:13px;line-height:150%;text-align:center;mso-line-height-alt:19.5px;"">
																			<p style=""margin: 0; word-break: break-word;""><span style=""word-break: break-word;"">If you didn’t request to change your password, simply ignore this email.</span></p>
																		</div>
																	</td>
																</tr>
															</table>
														</td>
													</tr>
												</tbody>
											</table>
										</td>
									</tr>
								</tbody>
							</table>
							<table class=""row row-3"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #f5f5f5;"">
								<tbody>
									<tr>
										<td>
											<table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; width: 500px; margin: 0 auto;"" width=""500"">
												<tbody>
													<tr>
														<td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top; border-top: 0px; border-right: 0px; border-bottom: 0px; border-left: 0px;"">
															<table class=""paragraph_block block-1"" width=""100%"" border=""0"" cellpadding=""15"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; word-break: break-word;"">
																<tr>
																	<td class=""pad"">
																		<div style=""color:#393d47;font-family:Montserrat,sans-serif;font-size:10px;line-height:120%;text-align:center;mso-line-height-alt:12px;"">
																			<p style=""margin: 0; word-break: break-word;""><span style=""word-break: break-word;"">This link will&nbsp;expire in 1 hour.&nbsp;If you continue to have problems</span><br><span style=""word-break: break-word;"">please feel free to contact us at <a href=""mailto:support@voltaxcharging.com"" target=""_blank"" title=""support@voltaxcharging.com"" style=""text-decoration: underline; color: #393d47;"" rel=""noopener"">support@voltaxcharging.com</a>. <a href=""voltaxcharging.com"" target=""_blank"" style=""text-decoration: underline; color: #393d47;""</a></span></p>
																		</div>
																	</td>
																</tr>
															</table>
														</td>
													</tr>
												</tbody>
											</table>
										</td>
									</tr>
								</tbody>
							</table>
				" + _emailTemplateService.GetFooterTemplate();

					email.Body = builder.ToMessageBody();
					using var smtp = new SmtpClient();
					smtp.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);

					smtp.Authenticate(_mailSettings.Mail, _mailSettings.Password);
					await smtp.SendAsync(email);
					smtp.Disconnect(true);
				}

    }
}