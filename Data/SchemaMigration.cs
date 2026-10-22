using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.NopwaraWebhooks.Domain;

namespace Nop.Plugin.Misc.NopwaraWebhooks.Data;

[NopMigration("2026-10-22 16:00:00", "Nop.Plugin.Misc.NopwaraWebhooks schema", MigrationProcessType.Installation)]
public class SchemaMigration : Migration
{
    public override void Up()
    {
        this.CreateTableIfNotExists<WebhookSubscription>();
        this.CreateTableIfNotExists<WebhookDelivery>();
    }

    public override void Down()
    {
        this.DeleteTableIfExists<WebhookDelivery>();
        this.DeleteTableIfExists<WebhookSubscription>();
    }
}
