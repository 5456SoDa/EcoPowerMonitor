using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoPowerMonitor.Models;
using Microsoft.Data.Sqlite;

namespace EcoPowerMonitor.Services
{
    public class DatabaseService
    {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private readonly SemaphoreSlim _dbLock = new(1, 1);

        public DatabaseService()
        {
            string appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EcoPowerMonitor");

            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }

            _dbPath = Path.Combine(appDataDir, "power_history.db");
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    PRAGMA journal_mode = WAL;
                    
                    CREATE TABLE IF NOT EXISTS DailySummaries (
                        Date TEXT PRIMARY KEY,
                        TotalKWh REAL NOT NULL DEFAULT 0,
                        TotalCost REAL NOT NULL DEFAULT 0,
                        PeakWatts REAL NOT NULL DEFAULT 0,
                        AverageWatts REAL NOT NULL DEFAULT 0,
                        ActiveSeconds INTEGER NOT NULL DEFAULT 0
                    );

                    CREATE TABLE IF NOT EXISTS Settings (
                        Key TEXT PRIMARY KEY,
                        Value TEXT NOT NULL
                    );
                ";
                command.ExecuteNonQuery();

                // Auto-migrate legacy Thai Buddhist Era dates (e.g. 2569-09-30 -> 2026-09-30) for ISO compliance
                MigrateThaiBuddhistDates(connection);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database init error: {ex.Message}");
            }
        }

        private void MigrateThaiBuddhistDates(SqliteConnection connection)
        {
            try
            {
                var rowsToMigrate = new List<(string oldDate, string newDate, double kwh, double cost, double peak, double avg, int sec)>();
                using (var selectCmd = connection.CreateCommand())
                {
                    selectCmd.CommandText = "SELECT Date, TotalKWh, TotalCost, PeakWatts, AverageWatts, ActiveSeconds FROM DailySummaries;";
                    using var reader = selectCmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string d = reader.GetString(0);
                        string[] parts = d.Split('-');
                        if (parts.Length == 3 && int.TryParse(parts[0], out int year) && year > 2400)
                        {
                            int gregorianYear = year - 543;
                            string newDate = $"{gregorianYear:D4}-{parts[1]}-{parts[2]}";
                            rowsToMigrate.Add((d, newDate, reader.GetDouble(1), reader.GetDouble(2), reader.GetDouble(3), reader.GetDouble(4), reader.GetInt32(5)));
                        }
                    }
                }

                foreach (var r in rowsToMigrate)
                {
                    using var tx = connection.BeginTransaction();
                    using var upCmd = connection.CreateCommand();
                    upCmd.Transaction = tx;
                    upCmd.CommandText = @"
                        INSERT INTO DailySummaries (Date, TotalKWh, TotalCost, PeakWatts, AverageWatts, ActiveSeconds)
                        VALUES ($newDate, $kwh, $cost, $peak, $avg, $sec)
                        ON CONFLICT(Date) DO UPDATE SET
                            TotalKWh = TotalKWh + $kwh,
                            TotalCost = TotalCost + $cost,
                            PeakWatts = MAX(PeakWatts, $peak),
                            ActiveSeconds = ActiveSeconds + $sec;
                        DELETE FROM DailySummaries WHERE Date = $oldDate;
                    ";
                    upCmd.Parameters.AddWithValue("$newDate", r.newDate);
                    upCmd.Parameters.AddWithValue("$oldDate", r.oldDate);
                    upCmd.Parameters.AddWithValue("$kwh", r.kwh);
                    upCmd.Parameters.AddWithValue("$cost", r.cost);
                    upCmd.Parameters.AddWithValue("$peak", r.peak);
                    upCmd.Parameters.AddWithValue("$avg", r.avg);
                    upCmd.Parameters.AddWithValue("$sec", r.sec);
                    upCmd.ExecuteNonQuery();
                    tx.Commit();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Date migration notice: {ex.Message}");
            }
        }

        public async Task SaveDailySummaryAsync(DateTime date, double addedKWh, double addedCost, double peakWatts, double currentWatts, int addedSeconds)
        {
            await _dbLock.WaitAsync();
            try
            {
                string dateStr = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // Check existing record for today
                double existingKWh = 0;
                double existingCost = 0;
                double existingPeak = 0;
                int existingSeconds = 0;

                await using (var selectCmd = connection.CreateCommand())
                {
                    selectCmd.CommandText = "SELECT TotalKWh, TotalCost, PeakWatts, ActiveSeconds FROM DailySummaries WHERE Date = $date;";
                    selectCmd.Parameters.AddWithValue("$date", dateStr);
                    await using var reader = await selectCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        existingKWh = reader.GetDouble(0);
                        existingCost = reader.GetDouble(1);
                        existingPeak = reader.GetDouble(2);
                        existingSeconds = reader.GetInt32(3);
                    }
                }

                double newTotalKWh = existingKWh + addedKWh;
                double newTotalCost = existingCost + addedCost;
                double newPeak = Math.Max(existingPeak, peakWatts);
                int newSeconds = existingSeconds + addedSeconds;
                double newAvg = newSeconds > 0 ? (newTotalKWh * 1000.0) / (newSeconds / 3600.0) : currentWatts;

                await using (var upsertCmd = connection.CreateCommand())
                {
                    upsertCmd.CommandText = @"
                        INSERT INTO DailySummaries (Date, TotalKWh, TotalCost, PeakWatts, AverageWatts, ActiveSeconds)
                        VALUES ($date, $kwh, $cost, $peak, $avg, $sec)
                        ON CONFLICT(Date) DO UPDATE SET
                            TotalKWh = $kwh,
                            TotalCost = $cost,
                            PeakWatts = $peak,
                            AverageWatts = $avg,
                            ActiveSeconds = $sec;
                    ";
                    upsertCmd.Parameters.AddWithValue("$date", dateStr);
                    upsertCmd.Parameters.AddWithValue("$kwh", newTotalKWh);
                    upsertCmd.Parameters.AddWithValue("$cost", newTotalCost);
                    upsertCmd.Parameters.AddWithValue("$peak", newPeak);
                    upsertCmd.Parameters.AddWithValue("$avg", newAvg);
                    upsertCmd.Parameters.AddWithValue("$sec", newSeconds);

                    await upsertCmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving daily summary: {ex.Message}");
            }
            finally
            {
                _dbLock.Release();
            }
        }

        public async Task<List<DailyPowerRecord>> GetDailySummariesAsync(int limit = 30)
        {
            await _dbLock.WaitAsync();
            var results = new List<DailyPowerRecord>();
            try
            {
                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Date, TotalKWh, TotalCost, PeakWatts, AverageWatts, ActiveSeconds 
                    FROM DailySummaries 
                    ORDER BY Date DESC 
                    LIMIT $limit;
                ";
                cmd.Parameters.AddWithValue("$limit", limit);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new DailyPowerRecord
                    {
                        DateString = reader.GetString(0),
                        TotalKWh = reader.GetDouble(1),
                        TotalCost = reader.GetDouble(2),
                        PeakWatts = reader.GetDouble(3),
                        AverageWatts = reader.GetDouble(4),
                        ActiveMinutes = reader.GetInt32(5) / 60
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading daily summaries: {ex.Message}");
            }
            finally
            {
                _dbLock.Release();
            }
            return results;
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            await _dbLock.WaitAsync();
            try
            {
                string json = JsonSerializer.Serialize(settings);
                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Settings (Key, Value) VALUES ('AppSettings', $val)
                    ON CONFLICT(Key) DO UPDATE SET Value = $val;
                ";
                cmd.Parameters.AddWithValue("$val", json);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
            finally
            {
                _dbLock.Release();
            }
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            await _dbLock.WaitAsync();
            try
            {
                await using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Value FROM Settings WHERE Key = 'AppSettings';";
                var val = await cmd.ExecuteScalarAsync();
                if (val is string json)
                {
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null) return settings;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
            finally
            {
                _dbLock.Release();
            }
            return new AppSettings();
        }

        public async Task<bool> ExportToCsvAsync(string filePath)
        {
            try
            {
                var records = await GetDailySummariesAsync(365);
                var sb = new StringBuilder();
                sb.AppendLine("Date,TotalEnergy_kWh,TotalCost_THB,PeakWatts_W,AverageWatts_W,ActiveMinutes");

                foreach (var r in records)
                {
                    sb.AppendLine($"{r.DateString},{r.TotalKWh:F4},{r.TotalCost:F2},{r.PeakWatts:F1},{r.AverageWatts:F1},{r.ActiveMinutes}");
                }

                await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting to CSV: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExportToJsonAsync(string filePath)
        {
            try
            {
                var records = await GetDailySummariesAsync(365);
                string json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting to JSON: {ex.Message}");
                return false;
            }
        }
    }
}
