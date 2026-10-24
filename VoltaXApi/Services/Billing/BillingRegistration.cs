namespace VoltaXApi.Services.Billing
{
    /// <summary>Invoices, credit notes, refunds and partner settlements (see docs/billing.md).</summary>
    public static class BillingRegistration
    {
        public static void AddBillingOptions(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<InvoicingOptions>(configuration.GetSection("Invoicing"));
            services.Configure<SettlementOptions>(configuration.GetSection("Settlements"));
        }

        public static void AddBilling(IServiceCollection services)
        {
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<InvoiceQueries>();
            services.AddScoped<BillingMailer>();
            services.AddScoped<IBillingEvents, BillingEvents>();
            // Needs the payment gateway (ServiceRegistration.ConfigurePayments).
            services.AddScoped<RefundService>();
            services.AddHostedService<InvoiceSweeper>();
            services.AddScoped<PartnerSettlementService>();
            services.AddScoped<SettlementDocuments>();
            services.AddHostedService<PartnerSettlementJob>();
        }
    }
}
