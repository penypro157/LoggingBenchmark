using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Elasticsearch.Net;
using Microsoft.Data.SqlClient;
using Nest;
using Newtonsoft.Json.Linq;

class Program
{
    private static readonly string _connectionString = "Data Source=LAPTOP-ERUTT96U;Initial Catalog=Logging;Integrated Security=True;Trust Server Certificate=True;Connection Timeout = 300";
    private const int _pageSize = 200000;
    static void Main(string[] args)
    {
        try
        {
            var stopwatch = new Stopwatch();
            var logIndex = "logs";
            // config elasticsearch
            var elasticSearchUrl = new Uri("http://localhost:9200");
            var connectionConfig = new ConnectionSettings(elasticSearchUrl)
                .RequestTimeout(TimeSpan.FromSeconds(10)).DefaultIndex(logIndex).EnableApiVersioningHeader();
            var elasticClient = new ElasticClient(connectionConfig);
            var pingResponse = elasticClient.Ping();
            if (!pingResponse.IsValid)
            {
                Console.WriteLine($"Elasticsearch connection failed: {pingResponse.DebugInformation}");
            }
            elasticClient.Indices.Delete("logs");
            if (!elasticClient.Indices.Exists(Indices.Parse(logIndex)).Exists)
            {
                var descriptor = new CreateIndexDescriptor(logIndex)
                .Mappings(ms => ms
                    .Map<Log>(m => m.AutoMap())
                );
                var res = elasticClient.Indices.Create(descriptor);
            }
            // config database


            // handle
            int pageIndex = 0; // Bắt đầu từ trang 0
            var isLoggedToClient = false;
            var totalLogCount = GetTotalRecord();
            var totalCounter = 0;
            while (true)
            {
                Console.WriteLine($"start round {pageIndex}");
                stopwatch.Restart();
                Console.WriteLine("----------------");
                var logs = GetLogs(pageIndex, _pageSize);
                if (logs.Count == 0)
                {
                    break; // Nếu không còn bản ghi nào, thoát vòng lặp
                }

                var x = elasticClient.BulkAll(logs, b => b.Index("logs")
                    .BackOffTime(TimeSpan.FromSeconds(30))
                    .BackOffRetries(1).RefreshOnCompleted().Size(1000)).Wait(TimeSpan.FromMinutes(15), (next) =>
                    {
                        isLoggedToClient = true;
                    });
                if (isLoggedToClient)
                {
                    Console.WriteLine($"Bulk inserted {totalCounter}/{totalLogCount} record.");
                }
                totalCounter += logs.Count;
                pageIndex++; // Tăng chỉ số trang
                Task.Delay(1000);

            }

            stopwatch.Stop();
            Console.WriteLine($"Bulk inserted {totalCounter}/{totalLogCount} record. ({stopwatch.ElapsedMilliseconds})");
            TruncateTable();
            Console.WriteLine($"Clear Log Table");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        Console.ReadLine();
    }

    private static List<Log> GetLogs(int pageIndex, int pageSize)
    {
        var logs = new List<Log>();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand("SELECT ID, Message, TimeStamp FROM Logs ORDER BY Id OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", connection))
            {
                command.Parameters.Add(new SqlParameter("@Offset", pageIndex * pageSize));
                command.Parameters.Add(new SqlParameter("@PageSize", pageSize));
                command.CommandTimeout = 60;
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var log = new Log
                        {
                            Id = reader.GetInt32(0).ToString(),
                            Message = reader.GetString(1), // Giả sử Message là cột thứ hai
                            TimeStamp = reader.GetDateTime(2) // Giả sử Date là cột thứ ba
                        };
                        logs.Add(log);
                    }
                }
            }
        }
        return logs;
    }

    private static int GetTotalRecord()
    {
        int totalRecord = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand("select count(*) from Logs ", connection))
            {
                command.CommandTimeout = 60;
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        totalRecord = reader.GetInt32(0);
                    }
                }
            }
        }
        return totalRecord;
    }
    private static void TruncateTable()
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            using (SqlCommand command = new SqlCommand("Truncate Table Logs", connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
    public class Log
    {
        public string Id { get; set; }
        public string Message { get; set; }
        public string MessageTemplate { get; set; }
        public string Level { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Exception { get; set; }
        public string Properties { get; set; }
    }
}
