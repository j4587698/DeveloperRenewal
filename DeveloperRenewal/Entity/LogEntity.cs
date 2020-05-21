using System;
using GraphLib.Entity;

namespace DeveloperRenewal.Entity
{
    public class LogEntity: BaseEntity
    {
        public int ApplicationId { get; set; }

        public DateTime CreateDate { get; set; }

        public string Status { get; set; }

        public string Operation { get; set; }

        public string Message { get; set; }
    }
}