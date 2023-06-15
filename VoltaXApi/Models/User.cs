using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace VoltaXApi.Models
{
    public class User : IEntity
    {
        public int ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    
        // Salting the hashed password is one of the most secured ways to secure passwords
        public  byte[] PasswordHash { get; set; }
        public  byte[] PasswordSalt { get; set; }
        [NotMapped]
        public string Password { get; set; }
        public IEnumerable<Order>? Orders { get; set; }
        
        
    }
}