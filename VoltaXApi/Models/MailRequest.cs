using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System;
namespace VoltaXApi.Models
{
    public class MailRequest
    {
        public string? Phone { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public List<string> ToEmails { get; set; }
        public string Subject { get; set; }
        public string? Body { get; set; }
        public List<IFormFile>? Attachments { get; set; }
    }
}