using System.Data;
using System.Data.SQLite;

namespace Rin.Framework.Archives;

public class SqliteArchive : IReadWriteArchive
{
    private readonly SQLiteConnection _connection;

    // Create an archive at a specific location
    public SqliteArchive(string filename)
    {
        _connection = new SQLiteConnection($"Data Source={filename};Version=3;");
        InitDatabase();
    }

    /// <summary>
    ///     Create an in-memory database
    /// </summary>
    public SqliteArchive()
    {
        _connection = new SQLiteConnection("Data Source=:memory:;Version=3;New=True;");
        InitDatabase();
    }

    public void Dispose()
    {
        _connection.Close();
    }

    public bool Contains(string key)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT key FROM store WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key);

        using var reader = cmd.ExecuteReader();
        return reader.Read();
    }

    public void Write(string key, Stream data)
    {
        var bytes = data is MemoryStream ms ? ms.ToArray() : null;
        if (bytes == null)
        {
            using var memoryStream = new MemoryStream();
            data.CopyTo(memoryStream);
            bytes = memoryStream.ToArray();
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
        INSERT INTO store (key, data)
        VALUES (@key, @data)
        ON CONFLICT(key) DO UPDATE SET data = excluded.data";
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.Add(new SQLiteParameter
        {
            ParameterName = "@data",
            Value = bytes,
            DbType = DbType.Binary
        });
        cmd.ExecuteNonQuery();
    }

    public void Remove(string key)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM store WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key);
        cmd.ExecuteNonQuery();
    }

    public IEnumerable<string> Keys
    {
        get
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT key FROM store ORDER BY key DESC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) yield return reader.GetString(0);
        }
    }

    public int Count
    {
        get
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM store";
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }

    public Stream CreateReadStream(string key)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT data FROM store WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key);

        using var reader = cmd.ExecuteReader();
        if (reader.Read()) return new MemoryStream((byte[])reader["data"]);

        throw new KeyNotFoundException();
    }

    private void InitDatabase()
    {
        _connection.Open();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS store (key TEXT PRIMARY KEY, data BLOB NOT NULL)";
        cmd.ExecuteNonQuery();
    }
}