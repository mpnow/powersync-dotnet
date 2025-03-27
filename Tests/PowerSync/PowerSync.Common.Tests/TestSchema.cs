namespace PowerSync.Common.Tests;

using PowerSync.Common.DB.Schema;

public class TestSchema
{
    public static readonly Table Assets = new Table(new Dictionary<string, ColumnType>
        {
            { "created_at", ColumnType.TEXT },
            { "make", ColumnType.TEXT },
            { "model", ColumnType.TEXT },
            { "serial_number", ColumnType.TEXT },
            { "quantity", ColumnType.INTEGER },
            { "user_id", ColumnType.TEXT },
            { "customer_id", ColumnType.TEXT },
            { "description", ColumnType.TEXT },
        }, new TableOptions
        {
            Indexes = new Dictionary<string, List<string>> { { "makemodel", new List<string> { "make", "model" } } }
        });

    public static readonly Table Customers = new Table(new Dictionary<string, ColumnType>
        {
            { "name", ColumnType.TEXT },
            { "email", ColumnType.TEXT }
        });

    public static readonly Schema AppSchema = new Schema(new Dictionary<string, Table>
        {
            { "assets", Assets },
            { "customers", Customers }
        });
}