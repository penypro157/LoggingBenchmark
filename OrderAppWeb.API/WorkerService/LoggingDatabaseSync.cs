using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using OrderAppWeb.API.Models.Entities;
using AutoMapper;
using OrderAppWeb.API.Context;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using EFCore.BulkExtensions;
using System.Diagnostics;

public class LoggingDatabaseSync : BackgroundService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IMapper _mapper;
    private const string RedisListKey = "logs_infor";
    private const int BatchSize = 50000;
    private readonly IServiceProvider _serviceProvider;

    public LoggingDatabaseSync(IMapper mapper, IServiceProvider serviceProvider, IConnectionMultiplexer connectionMultiplexer)
    {
        _mapper = mapper;
        _serviceProvider = serviceProvider;
        _connectionMultiplexer = connectionMultiplexer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var stopwatch = new Stopwatch();
        while (!stoppingToken.IsCancellationRequested)
        {
            var redisDB = _connectionMultiplexer.GetDatabase();
            long listLength = redisDB.ListLength(RedisListKey);
            // Kiểm tra nếu Redis chứa đủ logs để xử lý
            if (listLength >= BatchSize)
            {
                var logs = new List<string>();
                RedisValue[] redisValues = redisDB.ListRange(RedisListKey, 0, BatchSize); ;
                try
                {
                    using (var scope = _serviceProvider.CreateAsyncScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                        Console.WriteLine($"------------------------------");
                        // serialize to list
                        var insertLogs = new List<Log>();
                        stopwatch.Restart();
                        for (int i = 0; i < BatchSize; i++)
                        {
                            insertLogs.Add(JsonConvert.DeserializeObject<Log>(redisValues[i].ToString()));
                        }
                        stopwatch.Stop();
                        Console.WriteLine($"serialize to list time: {stopwatch.ElapsedMilliseconds} ms");


                        // bulk insert
                        stopwatch.Restart();
                        await context.BulkInsertAsync(insertLogs, cancellationToken : stoppingToken);
                        Console.WriteLine($"bulk insert time: {stopwatch.ElapsedMilliseconds} ms");
                        stopwatch.Stop();

                        // remove all cached 
                        stopwatch.Restart();
                        redisDB.ListTrim(RedisListKey, BatchSize, -1);
                        stopwatch.Stop();
                        Console.WriteLine($"remove all cached time: {stopwatch.ElapsedMilliseconds} ms");
                        Console.WriteLine($"Bulk Insert: {BatchSize} records, Remains : {listLength-BatchSize}");
                    }

                }
                catch (Exception ex)
                {
                    // Ghi lại log vào Redis nếu có lỗi
                    redisDB.ListLeftPush(RedisListKey, redisValues);

                    // Bạn có thể logging error tại đây nếu muốn
                    Console.WriteLine($"Error processing logs: {ex.Message}");
                }
            }

            // Tạm dừng một chút để giảm tải
            await Task.Delay(5000, stoppingToken);
        }
    }
}

