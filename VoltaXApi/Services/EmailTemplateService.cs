namespace VoltaXApi.Services
{
  public class EmailTemplateService : IEmailTemplateService
  {
      private readonly IConfiguration _configuration;

      public EmailTemplateService(IConfiguration configuration)
      {
          _configuration = configuration;
      }

      public string GetFooterTemplate()
      {
          var instagram = _configuration["SocialLinks:Instagram"];
          var facebook = _configuration["SocialLinks:Facebook"];
          var twitter = _configuration["SocialLinks:Twitter"];
          var linkedin = _configuration["SocialLinks:Linkedin"];
					Console.WriteLine("this is the footer mail ");
					Console.WriteLine(instagram);
          return $@"<table class=""row row-4"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #fff;"">
						<tbody>
							<tr>
								<td>
									<table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; width: 500px; margin: 0 auto;"" width=""500"">
										<tbody>
											<tr>
												<td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; padding-top: 5px; vertical-align: top; border-top: 0px; border-right: 0px; border-bottom: 0px; border-left: 0px;"">
													<table class=""html_block block-1"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
														<tr>
															<td class=""pad"">
																<div style=""font-family:Arial, Helvetica Neue, Helvetica, sans-serif;text-align:center;"" align=""center""><div style=""height:30px;"">&nbsp;</div></div>
															</td>
														</tr>
													</table>
													<table class=""social_block block-2"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
														<tr>
															<td class=""pad"" style=""text-align:center;padding-right:0px;padding-left:0px;"">
																<div class=""alignment"" align=""center"">
																	<table class=""social-table"" width=""168px"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; display: inline-block;"">
																		<tr>
																			<td style=""padding:0 5px 0 5px;""><a href=""{facebook}"" target=""_blank""><img src=""https://app-rsrc.getbee.io/public/resources/social-networks-icon-sets/t-outline-circle-default-gray/facebook@2x.png"" width=""32"" height=""auto"" alt=""Facebook"" title=""Facebook"" style=""display: block; height: auto; border: 0;""></a></td>
																			<td style=""padding:0 5px 0 5px;""><a href=""{twitter}"" target=""_blank""><img src=""https://app-rsrc.getbee.io/public/resources/social-networks-icon-sets/t-outline-circle-default-gray/twitter@2x.png"" width=""32"" height=""auto"" alt=""Twitter"" title=""Twitter"" style=""display: block; height: auto; border: 0;""></a></td>
																			<td style=""padding:0 5px 0 5px;""><a href=""{instagram}"" target=""_blank""><img src=""https://app-rsrc.getbee.io/public/resources/social-networks-icon-sets/t-outline-circle-default-gray/instagram@2x.png"" width=""32"" height=""auto"" alt=""Instagram"" title=""Instagram"" style=""display: block; height: auto; border: 0;""></a></td>
																			<td style=""padding:0 5px 0 5px;""><a href=""{linkedin}"" target=""_blank""><img src=""https://app-rsrc.getbee.io/public/resources/social-networks-icon-sets/t-outline-circle-default-gray/linkedin@2x.png"" width=""32"" height=""auto"" alt=""LinkedIn"" title=""LinkedIn"" style=""display: block; height: auto; border: 0;""></a></td>
																		</tr>
																	</table>
																</div>
															</td>
														</tr>
													</table>
													<table class=""html_block block-3"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
														<tr>
															<td class=""pad"">
																<div style=""font-family:Arial, Helvetica Neue, Helvetica, sans-serif;text-align:center;"" align=""center""><div style=""margin-top: 25px;border-top:1px dashed #D6D6D6;margin-bottom: 20px;""></div></div>
															</td>
														</tr>
													</table>
													<table class=""paragraph_block block-4"" width=""100%"" border=""0"" cellpadding=""10"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; word-break: break-word;"">
														<tr>
															<td class=""pad"">
																<div style=""color:#C0C0C0;font-family:Montserrat,sans-serif;font-size:12px;line-height:120%;text-align:center;mso-line-height-alt:14.399999999999999px;"">
																	<p style=""margin: 0; word-break: break-word;"">Volta X is the fueling network of the future, delivering more places to charge, so that EV drivers can count on us for charging their EVs all day, every day.</p>
																	<p style=""margin: 0; word-break: break-word;"">&nbsp;</p>
																	<p style=""margin: 0; word-break: break-word;"">Résidence&nbsp;<em>Mas Palomas II</em>. Rue Omar Ibn Al Khattab. <br>
																	  4ème étage N°87. 90000 <em>Tanger</em>&nbsp;&nbsp;</p>
																	<p style=""margin: 0; word-break: break-word;"">&nbsp;&nbsp;support@voltaxcharging.com / +212 5 31 07 48 22</p>
<p style=""margin: 0; word-break: break-word;""><span style=""word-break: break-word; color: #c0c0c0;"">&nbsp;</span></p>
																</div>
															</td>
														</tr>
													</table>
													<table class=""html_block block-5"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
														<tr>
															<td class=""pad"">
																<div style=""font-family:Montserrat, sans-serif;text-align:center;"" align=""center""><div style=""height-top: 20px;"">&nbsp;</div></div>
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
					<table class=""row row-5"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #ffffff;"">
						<tbody>
							<tr> </tr>
						</tbody>
					</table>
				</td>
			</tr>
		</tbody>
	</table>
  </body>

</html>";
          
      }
  
      public string GetHeaderTemplate()
      {
        string headerString = @"<!DOCTYPE html>
<html xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:o=""urn:schemas-microsoft-com:office:office"" lang=""en"">

<head>
	<title>Volta X Password Reset</title>
	<meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
	<meta name=""viewport"" content=""width=device-width, initial-scale=1.0""><!--[if mso]><xml><o:OfficeDocumentSettings><o:PixelsPerInch>96</o:PixelsPerInch><o:AllowPNG/></o:OfficeDocumentSettings></xml><![endif]--><!--[if !mso]><!--><!--<![endif]-->
	 <!-- Add Montserrat font -->
    <link href=""https://fonts.googleapis.com/css2?family=Montserrat:wght@400;700&display=swap"" rel=""stylesheet"">
	<style>
		* {
			box-sizing: border-box;
			font-family: 'Montserrat', sans-serif;
		}

		body {
			margin: 0;
			padding: 0;
			font-family: 'Montserrat', sans-serif;
		}

		a[x-apple-data-detectors] {
			color: inherit !important;
			text-decoration: inherit !important;
		}

		#MessageViewBody a {
			color: inherit;
			text-decoration: none;
		}

		p {
			line-height: inherit
		}

		.desktop_hide,
		.desktop_hide table {
			mso-hide: all;
			display: none;
			max-height: 0px;
			overflow: hidden;
		}

		.image_block img+div {
			display: none;
		}

		sup,
		sub {
			line-height: 0;
			font-size: 75%;
		}

		@media (max-width:520px) {

			.desktop_hide table.icons-inner,
			.social_block.desktop_hide .social-table {
				display: inline-block !important;
			}

			.icons-inner {
				text-align: center;
			}

			.icons-inner td {
				margin: 0 auto;
			}

			.image_block div.fullWidth {
				max-width: 100% !important;
			}

			.mobile_hide {
				display: none;
			}

			.row-content {
				width: 100% !important;
			}

			.stack .column {
				width: 100%;
				display: block;
			}

			.mobile_hide {
				min-height: 0;
				max-height: 0;
				max-width: 0;
				overflow: hidden;
				font-size: 0px;
			}

			.desktop_hide,
			.desktop_hide table {
				display: table !important;
				max-height: none !important;
			}
		}
	</style><!--[if mso ]><style>sup, sub { font-size: 100% !important; } sup { mso-text-raise:10% } sub { mso-text-raise:-10% }</style> <![endif]-->
</head>
  
  <table class=""nl-container"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #FFFFFF;"">
		<tbody>
			<tr>
				<td>
					<table class=""row row-1"" align=""center"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #f5f5f5;"">
						<tbody>
							<tr>
								<td>
									<table class=""row-content stack"" align=""center"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; width: 500px; margin: 0 auto;"" width=""500"">
										<tbody>
											<tr>
												<td class=""column column-1"" width=""100%"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; padding-bottom: 5px; vertical-align: top; border-top: 0px; border-right: 0px; border-bottom: 0px; border-left: 0px;"">
													<table class=""image_block block-1"" width=""100%"" border=""0"" cellpadding=""0"" cellspacing=""0"" role=""presentation"" style=""mso-table-lspace: 0pt; mso-table-rspace: 0pt;"">
														<tr>
															<td class=""pad"" style=""padding-bottom:10px;width:100%;padding-right:0px;padding-left:0px;"">
																<div class=""alignment"" align=""center"" style=""line-height:10px"">
																	<div style=""max-width: 175px;"">
                                  <img src=""https://voltaxcharging.com/img/volta-logo.png"" style=""display: block; height: auto; border: 0; width: 100%;"" width=""175"" alt=""your-logo"" title=""your-logo"" height=""auto""></div>
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
					</table>";

          return headerString;
      }
  }
}