using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDDSCommunity.IntrusionDetection.Shared.Test;

[TestClass]
public sealed class DatabaseEncryptionTest
{
    private string testDirectory = null!;

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void Initialize()
    {
        testDirectory = Path.Combine(TestContext.TestRunDirectory ?? AppContext.BaseDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Database.Instance.Close();
        if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
    }

    [TestMethod]
    public void Configure_NewDatabaseEncryptsHeaderAndCreatesProtectedKey()
    {
        Database database = new();
        string path = Path.Combine(testDirectory, "encrypted.db");

        database.Configure(testDirectory, "encrypted.db");
        database.ExecuteNonQuery("CREATE TABLE Secret(Value TEXT NOT NULL)");
        database.ExecuteNonQuery("INSERT INTO Secret(Value) VALUES(@p0)", "sensitive-value");
        Assert.IsFalse(ContainsBytes(path, "sensitive-value"u8));
        if (File.Exists(path + "-wal"))
            Assert.IsFalse(ContainsBytes(path + "-wal", "sensitive-value"u8));
        database.Close();

        Assert.IsFalse(HasPlaintextHeader(path));
        Assert.IsTrue(File.Exists(path + ".key"));
        Assert.IsFalse(File.ReadAllText(path).Contains("sensitive-value", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Configure_PlaintextDatabaseMigratesDataWithoutLeavingPlaintextRollback()
    {
        SQLitePCL.Batteries_V2.Init();
        string path = Path.Combine(testDirectory, "legacy.db");
        using (SqliteConnection plaintext = new($"Data Source={path};Pooling=False"))
        {
            plaintext.Open();
            using SqliteCommand command = plaintext.CreateCommand();
            command.CommandText = "CREATE TABLE Legacy(Value TEXT NOT NULL); INSERT INTO Legacy(Value) VALUES('preserved');";
            command.ExecuteNonQuery();
        }
        Assert.IsTrue(HasPlaintextHeader(path));

        Database database = new();
        database.Configure(testDirectory, "legacy.db");

        Assert.AreEqual("preserved", Convert.ToString(database.ExecuteScalar("SELECT Value FROM Legacy")));
        database.Close();
        Assert.IsFalse(HasPlaintextHeader(path));
        Assert.IsEmpty(Directory.GetFiles(testDirectory, "*.plaintext-rollback-*"));
    }

    [TestMethod]
    public void Configure_EncryptedDatabaseWithoutKeyFailsClosed()
    {
        Database database = new();
        string path = Path.Combine(testDirectory, "locked.db");
        database.Configure(testDirectory, "locked.db");
        database.Close();
        File.Delete(path + ".key");

        Assert.ThrowsExactly<InvalidDataException>(() => new Database().Configure(testDirectory, "locked.db"));
        Assert.IsTrue(File.Exists(path));
        Assert.IsFalse(File.Exists(path + ".key"));
    }

    [TestMethod]
    public void EncryptedDatabaseRejectsIncorrectPassword()
    {
        Database database = new();
        string path = Path.Combine(testDirectory, "wrong-password.db");
        database.Configure(testDirectory, "wrong-password.db");
        database.Close();

        using SqliteConnection connection = new(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
            Password = "incorrect-password"
        }.ConnectionString);
        Assert.ThrowsExactly<SqliteException>(() => connection.Open());
    }

    private static bool HasPlaintextHeader(string path)
    {
        Span<byte> header = stackalloc byte[16];
        using FileStream stream = File.OpenRead(path);
        return stream.Read(header) == header.Length && header.SequenceEqual("SQLite format 3\0"u8);
    }

    private static bool ContainsBytes(string path, ReadOnlySpan<byte> value)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        byte[] content = new byte[checked((int)stream.Length)];
        stream.ReadExactly(content);
        return content.AsSpan().IndexOf(value) >= 0;
    }
}
