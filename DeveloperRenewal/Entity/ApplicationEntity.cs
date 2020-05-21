using System;
using System.Collections.Generic;
using GraphLib.Entity;
using LiteDB;
using Microsoft.Graph;

namespace DeveloperRenewal.Entity
{
    public class ApplicationEntity: BaseEntity
    {
        public string UserId { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public DateTime LastExecTime { get; set; } = DateTime.MinValue;

        public int MinExecInterval { get; set; } = 3600;

        public int MaxExecInterval { get; set; } = 7200;

        public bool AuthorizationStatus { get; set; }

        public bool IsEnable { get; set; }
    }
}