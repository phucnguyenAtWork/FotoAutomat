using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace FotoAutomat.Infrastructure;

/// <summary>booth.db trong thư mục quản trị: phiên khách, giao dịch, shot, lựa chọn, nhật ký và voucher.
/// Một tiến trình booth sở hữu tệp này. Ghi với WAL và synchronous=FULL như SqliteBoothStore.</summary>
internal static class BoothDatabase
{
    private static readonly HashSet<string> initialized = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object gate = new();

    public static string DefaultRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FotoAutomat", "Operator");

    public static SqliteConnection Open(string root)
    {
        string path = Path.GetFullPath(Path.Combine(root, "booth.db"));
        lock (gate)
        {
            if (!initialized.Contains(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var setup = Connect(path);
                Execute(setup, null, Schema);
                AddColumn(setup, "customer_sessions", "share_token", "TEXT");
                Execute(setup, null, "CREATE UNIQUE INDEX IF NOT EXISTS customer_sessions_share_token ON customer_sessions(share_token)");
                ImportLegacyVouchers(setup, root);
                initialized.Add(path);
            }
        }
        return Connect(path);
    }

    private static SqliteConnection Connect(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false, DefaultTimeout = 5 }.ToString());
        connection.Open();
        Execute(connection, null, "PRAGMA foreign_keys = ON; PRAGMA synchronous = FULL;");
        return connection;
    }

    private const string Schema = """
        PRAGMA journal_mode = WAL;
        CREATE TABLE IF NOT EXISTS customer_sessions (
            id TEXT PRIMARY KEY,
            started_at TEXT NOT NULL,
            stage TEXT NOT NULL,
            revision INTEGER NOT NULL,
            completed_at TEXT,
            compensates TEXT,
            snapshot TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS payments (
            session_id TEXT PRIMARY KEY REFERENCES customer_sessions(id),
            price INTEGER NOT NULL,
            discount INTEGER NOT NULL,
            amount_due INTEGER NOT NULL,
            method TEXT NOT NULL,
            voucher_code TEXT,
            reference TEXT,
            paid_at TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS shots (
            id TEXT PRIMARY KEY,
            session_id TEXT NOT NULL REFERENCES customer_sessions(id),
            seq INTEGER NOT NULL,
            path TEXT,
            state TEXT NOT NULL,
            at TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS selections (
            session_id TEXT PRIMARY KEY REFERENCES customer_sessions(id),
            data TEXT NOT NULL,
            saved_at TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS session_events (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            session_id TEXT NOT NULL,
            at TEXT NOT NULL,
            stage TEXT NOT NULL,
            note TEXT
        );
        CREATE TABLE IF NOT EXISTS shot_faces (
            shot_id TEXT PRIMARY KEY,
            faces TEXT NOT NULL,
            detector TEXT NOT NULL,
            at TEXT NOT NULL
        );
        CREATE TABLE IF NOT EXISTS photo_jobs (
            id TEXT PRIMARY KEY,
            session_id TEXT NOT NULL,
            shot_id TEXT,
            kind TEXT NOT NULL,
            state TEXT NOT NULL,
            output TEXT,
            error TEXT,
            attempts INTEGER NOT NULL DEFAULT 0,
            duration_ms INTEGER,
            working_set_mb INTEGER,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS photo_jobs_state ON photo_jobs(kind, state, created_at);
        CREATE TABLE IF NOT EXISTS vouchers (
            code TEXT PRIMARY KEY,
            discount INTEGER NOT NULL,
            free INTEGER NOT NULL,
            unlimited INTEGER NOT NULL,
            created_at TEXT NOT NULL,
            used_at TEXT,
            uses INTEGER NOT NULL DEFAULT 0,
            last_used_at TEXT,
            revoked INTEGER NOT NULL DEFAULT 0,
            compensates TEXT
        );
        """;

    // Cột thêm sau khi DB đã có dữ liệu: CREATE TABLE IF NOT EXISTS không thêm cột vào bảng cũ.
    private static void AddColumn(SqliteConnection connection, string table, string column, string type)
    {
        using var info = Command(connection, null, $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = $name", ("$name", column));
        if (Convert.ToInt64(info.ExecuteScalar(), CultureInfo.InvariantCulture) == 0) Execute(connection, null, $"ALTER TABLE {table} ADD COLUMN {column} {type}");
    }

    // Voucher trước đây lưu ở vouchers.json: nhập một lần rồi đổi tên tệp để không nhập lại.
    private static void ImportLegacyVouchers(SqliteConnection connection, string root)
    {
        string legacy = Path.Combine(root, "vouchers.json");
        if (!File.Exists(legacy)) return;
        var vouchers = JsonSerializer.Deserialize<List<Voucher>>(File.ReadAllText(legacy)) ?? [];
        using var transaction = connection.BeginTransaction();
        foreach (var v in vouchers) VoucherSql.Insert(connection, transaction, v, ignoreExisting: true);
        transaction.Commit();
        File.Move(legacy, legacy + ".imported", true);
    }

    public static int Execute(SqliteConnection connection, SqliteTransaction? transaction, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = Command(connection, transaction, sql, parameters);
        return command.ExecuteNonQuery();
    }

    public static SqliteCommand Command(SqliteConnection connection, SqliteTransaction? transaction, string sql, params (string Name, object? Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return command;
    }

    public static string Time(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);
    public static DateTimeOffset Time(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    public static DateTimeOffset? TimeOrNull(SqliteDataReader reader, int column) => reader.IsDBNull(column) ? null : Time(reader.GetString(column));
}
