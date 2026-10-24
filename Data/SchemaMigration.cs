using FluentMigrator;
using Nop.Core;
using Nop.Data.Extensions;
using Nop.Data.Mapping;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.NopwaraWebhooks.Domain;

namespace Nop.Plugin.Misc.NopwaraWebhooks.Data;

[NopMigration("2026-10-22 16:00:00", "Nop.Plugin.Misc.NopwaraWebhooks schema", MigrationProcessType.Installation)]
public class SchemaMigration : Migration
{
    public override void Up()
    {
        CreateTableIfNotExists<WebhookSubscription>();
        CreateTableIfNotExists<WebhookDelivery>();
    }

    public override void Down()
    {
        DeleteTableIfExists<WebhookDelivery>();
        DeleteTableIfExists<WebhookSubscription>();
    }

    //nopCommerce 4.90 has no CreateTableIfNotExists/DeleteTableIfExists migration extensions (5.00 does)
    private void CreateTableIfNotExists<TEntity>() where TEntity : BaseEntity
    {
        if (!Schema.Table(NameCompatibilityManager.GetTableName(typeof(TEntity))).Exists())
            Create.TableFor<TEntity>();
    }

    private void DeleteTableIfExists<TEntity>() where TEntity : BaseEntity
    {
        var tableName = NameCompatibilityManager.GetTableName(typeof(TEntity));
        if (Schema.Table(tableName).Exists())
            Delete.Table(tableName);
    }
}
