using MySql.Data.MySqlClient;

namespace TaskManagerMVC.Data;

public class DatabaseSeeder
{
    private readonly DbConnectionFactory _dbFactory;

    public DatabaseSeeder(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public void Initialize()
    {
        using var conn = _dbFactory.CreateConnection();
        conn.Open();

        // Check if hourly_rate column exists, if not add it
        var checkSql = @"SELECT COUNT(*) 
                         FROM information_schema.COLUMNS 
                         WHERE TABLE_SCHEMA = DATABASE() 
                         AND TABLE_NAME = 'tasks' 
                         AND COLUMN_NAME = 'hourly_rate'";
        
        using var checkCmd = new MySqlCommand(checkSql, conn);
        var count = Convert.ToInt32(checkCmd.ExecuteScalar());

        if (count == 0)
        {
            var alterSql = "ALTER TABLE tasks ADD COLUMN hourly_rate DECIMAL(10,2) DEFAULT 0.00";
            using var alterCmd = new MySqlCommand(alterSql, conn);
            alterCmd.ExecuteNonQuery();
            Console.WriteLine("[MIGRATION] Added hourly_rate column to tasks table.");
        }
        
        // Fix notifications type column length
        var checkNotifSql = @"SELECT CHARACTER_MAXIMUM_LENGTH 
                              FROM information_schema.COLUMNS 
                              WHERE TABLE_SCHEMA = DATABASE() 
                              AND TABLE_NAME = 'notifications' 
                              AND COLUMN_NAME = 'type'";
                              
        using var checkNotifCmd = new MySqlCommand(checkNotifSql, conn);
        var typeLength = Convert.ToInt32(checkNotifCmd.ExecuteScalar()); // If DBNull or null, returns 0? Safer logic below.
        
        // ExecuteScalar might return null, use object then convert
        object? result = checkNotifCmd.ExecuteScalar();
        long length = 0;
        if (result != null && result != DBNull.Value)
        {
            length = Convert.ToInt64(result);
        }

        if (length < 50)
        {
            var alterNotifSql = "ALTER TABLE notifications MODIFY COLUMN type VARCHAR(50) NOT NULL";
            using var alterNotifCmd = new MySqlCommand(alterNotifSql, conn);
            alterNotifCmd.ExecuteNonQuery();
            Console.WriteLine("[MIGRATION] Expanded type column in notifications table.");
        }
    }
}
