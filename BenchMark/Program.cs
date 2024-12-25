using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var client = new HttpClient();
        string url = "https://localhost:7188/api/Order/0"; // Thay bằng API của bạn

        int requestCount = 1000; // Số lượng request đồng thời
        var tasks = new Task[requestCount];
        var times = new long[requestCount]; // Lưu thời gian thực hiện của từng request

        for (int i = 0; i < requestCount; i++)
        {
            int index = i; // Biến cục bộ để tránh vấn đề capture biến `i` trong lambda
            tasks[i] = Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    var response = await client.GetAsync(url);
                    stopwatch.Stop();
                    times[index] = stopwatch.ElapsedMilliseconds; // Ghi lại thời gian
                    Console.WriteLine($"Request {index + 1}: {response.StatusCode} - {stopwatch.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    times[index] = stopwatch.ElapsedMilliseconds;
                    Console.WriteLine($"Request {index + 1} failed: {ex.Message} - {stopwatch.ElapsedMilliseconds}ms");
                }
            });
        }

        await Task.WhenAll(tasks);

        Console.WriteLine("\n=== Summary ===");
        Console.WriteLine($"Total Requests: {requestCount}");
        Console.WriteLine($"Fastest Request: {times.Min()}ms");
        Console.WriteLine($"Slowest Request: {times.Max()}ms");
        Console.WriteLine($"Average Time: {times.Average():0.00}ms");
        Console.ReadLine();
    }
}
