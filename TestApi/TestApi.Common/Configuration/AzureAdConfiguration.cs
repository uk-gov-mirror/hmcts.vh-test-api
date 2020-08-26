﻿namespace TestApi.Common.Configuration
{
    public class AzureAdConfiguration
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Authority { get; set; }
        public string ValidAudience { get; set; }
        public string TenantId { get; set; }
    }
}