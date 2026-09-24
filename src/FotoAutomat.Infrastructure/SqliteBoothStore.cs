using FotoAutomat.Core;
using Microsoft.Data.Sqlite;

namespace FotoAutomat.Infrastructure;

/// <summary>Small durable simulator store. One booth process owns this database.</summary>
public sealed class SqliteBoothStore : IBoothStore
{
    private readonly string connectionString;

    public SqliteBoothStore(string databasePath)
    {
        var fullPath = Path.GetFullPath(databasePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
            DefaultTimeout = 5
        }.ToString();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode = WAL;
            CREATE TABLE IF NOT EXISTS sessions (
                id TEXT PRIMARY KEY,
                stage TEXT NOT NULL,
                image_path TEXT,
                revision INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS demo_receipts (
                receipt_id TEXT PRIMARY KEY,
                session_id TEXT NOT NULL REFERENCES sessions(id)
            );
            """;
        command.ExecuteNonQuery();
    }

    public BoothSession Create()
    {
        var session = new BoothSession(Guid.NewGuid(), SessionStage.AwaitingPayment, null, 0);
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO sessions (id, stage) VALUES ($id, $stage)";
        command.Parameters.AddWithValue("$id", session.Id.ToString());
        command.Parameters.AddWithValue("$stage", session.Stage.ToString());
        command.ExecuteNonQuery();
        return session;
    }

    public BoothSession Get(Guid id)
    {
        using var connection = Open();
        return Read(connection, id);
    }

    public BoothSession? GetLatest()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, stage, image_path, revision FROM sessions ORDER BY rowid DESC LIMIT 1";
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public BoothSession ConfirmDemoPayment(Guid sessionId, string receiptId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receiptId);
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var receipt = connection.CreateCommand();
        receipt.Transaction = transaction;
        receipt.CommandText = "SELECT session_id FROM demo_receipts WHERE receipt_id = $receipt";
        receipt.Parameters.AddWithValue("$receipt", receiptId);
        var owner = receipt.ExecuteScalar() as string;
        var session = Read(connection, sessionId, transaction);
        if (owner is not null)
        {
            if (owner != sessionId.ToString())
                throw new InvalidOperationException("Receipt belongs to a different session.");
            transaction.Commit();
            return session;
        }
        if (session.Stage != SessionStage.AwaitingPayment)
            throw new InvalidOperationException("This session is not awaiting a demo payment.");

        receipt.CommandText = "INSERT INTO demo_receipts (receipt_id, session_id) VALUES ($receipt, $session)";
        receipt.Parameters.AddWithValue("$session", sessionId.ToString());
        receipt.ExecuteNonQuery();
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = "UPDATE sessions SET stage = 'Paid', revision = revision + 1 WHERE id = $id";
        update.Parameters.AddWithValue("$id", sessionId.ToString());
        update.ExecuteNonQuery();
        transaction.Commit();
        return session with { Stage = SessionStage.Paid, Revision = session.Revision + 1 };
    }

    public BoothSession Transition(BoothSession expected, SessionStage next, string? imagePath = null)
    {
        var updated = expected.MoveTo(next, imagePath);
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE sessions SET stage = $next, image_path = $path, revision = revision + 1
            WHERE id = $id AND stage = $expected AND revision = $revision
            """;
        command.Parameters.AddWithValue("$id", expected.Id.ToString());
        command.Parameters.AddWithValue("$expected", expected.Stage.ToString());
        command.Parameters.AddWithValue("$revision", expected.Revision);
        command.Parameters.AddWithValue("$next", next.ToString());
        command.Parameters.AddWithValue("$path", (object?)updated.ImagePath ?? DBNull.Value);
        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("Session changed. Reload it before taking another action.");
        return updated;
    }

    public int RecoverInterruptedSessions()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE sessions SET stage = 'NeedsReview', revision = revision + 1
            WHERE stage IN ('Capturing', 'Printing')
            """;
        return command.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON; PRAGMA synchronous = FULL;";
        command.ExecuteNonQuery();
        return connection;
    }

    private static BoothSession Read(SqliteConnection connection, Guid id, SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT id, stage, image_path, revision FROM sessions WHERE id = $id";
        command.Parameters.AddWithValue("$id", id.ToString());
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new KeyNotFoundException($"Session {id} not found.");
        return Map(reader);
    }

    private static BoothSession Map(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(0)),
        Enum.Parse<SessionStage>(reader.GetString(1)),
        reader.IsDBNull(2) ? null : reader.GetString(2),
        reader.GetInt32(3));
}
