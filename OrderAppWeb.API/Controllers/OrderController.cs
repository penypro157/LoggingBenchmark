﻿using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MySqlX.XDevAPI.Common;
using OrderAppWeb.API.Context;
using OrderAppWeb.API.Models.Dtos;
using OrderAppWeb.API.Models.Entities;
using OrderAppWeb.API.Models.Results;
using Serilog;
using StackExchange.Redis;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OrderAppWeb.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IMemoryCache _memoryCache;
        private readonly OrderDbContext _context;
        private readonly IMapper mapper;
        private readonly Serilog.ILogger _logger;
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public OrderController(IMemoryCache memoryCache,
            OrderDbContext context,
            IMapper mapper
            , Serilog.ILogger logger,
            IConnectionMultiplexer connectionMultiplexer
            )
        {
            _memoryCache = memoryCache;
            _context = context;
            _logger = logger;
            this.mapper = mapper;
            _connectionMultiplexer = connectionMultiplexer;
        }

        // #region Memory Cache ile Cache işlemi
        // [HttpGet]
        //public async Task<IActionResult> Get(string? category)
        // {

        //     // Memory Cache sadece proje açıkken işlem görür.

        //     var result = new List<Product>();

        //     if (category is null)
        //     {
        //         result = _memoryCache.Get("products") as List<Product>;
        //         if(result is null)
        //         {
        //             result = await _context.Products.ToListAsync();
        //             _memoryCache.Set("products", result, TimeSpan.FromMinutes(10));
        //         }
        //     }
        //     else
        //     {
        //         result = _memoryCache.Get($"products-{category}") as List<Product>;
        //         if (result is null)
        //         {
        //             result = await _context.Products.Where(x => x.Category == category).ToListAsync();
        //             _memoryCache.Set($"products-{category}", result, TimeSpan.FromMinutes(10));
        //         }
        //     }

        //     var productDtos=mapper.Map<List<Product>,List<ProductDto>>(result);


        //     return Ok(new ApiResponse<List<ProductDto>>(StatusType.Success,productDtos));
        // }

        // #endregion

        #region Redis ile Cache işlemi
        //[HttpGet]
        //public async Task<IActionResult> Get(string? category)
        //{
        //    //var redisClient = new RedisClient("localhost", 6379);
        //    //IRedisTypedClient<List<Product>> redisProducts = redisClient.As<List<Product>>();

        //    //var result = new List<Product>();
        //    //if (category is null)
        //    //{
        //    //    result = redisClient.Get<List<Product>>("products");
        //    //    if (result is null)
        //    //    {
        //    //        result = await _context.Products.ToListAsync();
        //    //        redisClient.Set("products", result, TimeSpan.FromMinutes(10));
        //    //    }
        //    //}
        //    //else
        //    //{
        //    //    result = redisClient.Get<List<Product>>($"products-{category}");
        //    //    if (result is null)
        //    //    {
        //    //        result = await _context.Products.Where(x => x.Category == category).ToListAsync();
        //    //        redisClient.Set($"products-{category}", result, TimeSpan.FromMinutes(10));
        //    //    }
        //    //}

        //    //var productDtos = mapper.Map<List<Product>, List<ProductDto>>(result);


        //    //return Ok(new ApiResponse<List<ProductDto>>(StatusType.Success, productDtos));
        //}
        #endregion

        //[HttpPost] 
        //public async Task<IActionResult> CreateOrder(CreateOrderRequest createOrderRequest)
        //{
        //    //Order order=mapper.Map<Order>(createOrderRequest);
        //    //List<OrderDetail> orderDetails = mapper.Map<List<ProductDetailDto>, List<OrderDetail>>(createOrderRequest.ProductDetails);
        //    //order.TotalAmount = createOrderRequest.ProductDetails.Sum(x => x.Amount);
        //    //order.OrderDetails= orderDetails;
        //    //await _context.Orders.AddAsync(order);
        //    //await _context.SaveChangesAsync();

        //    return Ok(new ApiResponse<int>(StatusType.Success, order.Id));
        //}

        [HttpGet("{id:long}")]
        public async Task<IActionResult> TestBatchLogging([Required] int id)
        {
            var result = await _context.Products.Where(x => x.Id < 500).ToListAsync();
            var result1 = await _context.Products.Where(x => x.Id < 400).ToListAsync();
            var result2 = await _context.Products.Where(x => x.Id < 300).ToListAsync();
            var result3 = await _context.Products.Where(x => x.Id < 200).ToListAsync();
            var productList = new List<Product>();
            _logger.Information($" Test logging process start");
            for (int i = 0; i <= 50; i++)
            {
                Product product = new Product()
                {
                    Description = $"Description {i}",
                    Category = $"Category {i}",
                    Status = false,
                    Unit = i,
                    UnitPrice = i
                };
                productList.Add(product);
                _logger.Information($" This is Message ${i}");
            }
            await _context.Products.AddRangeAsync(productList);
            await _context.SaveChangesAsync();
            _logger.Information($" Test logging process end");
            return Ok(0);
        }

        [HttpGet]
        [Route("TestBatchLogging1")]
        public async Task<IActionResult> TestBatchLogging1([Required] int id)
        {
            var result = await _context.Products.Where(x => x.Id < 500).ToListAsync();
            var result1 = await _context.Products.Where(x => x.Id < 400).ToListAsync();
            var result2 = await _context.Products.Where(x => x.Id < 300).ToListAsync();
            var result3 = await _context.Products.Where(x => x.Id < 200).ToListAsync();
            var productList = new List<Product>();
            _logger.Information($" Test logging process start");
            for (int i = 0; i <= 50; i++)
            {
                Product product = new Product()
                {
                    Description = $"Description {i}",
                    Category = $"Category {i}",
                    Status = false,
                    Unit = i,
                    UnitPrice = i
                };
                productList.Append(product);
                _logger.Information($" This is Message ${i}");
            }
            await _context.Products.AddRangeAsync(productList);
            await _context.SaveChangesAsync();
            _logger.Information($" Test logging process end");
            return Ok(id);
        }

        #region 1000 tane fake data için
        //[HttpPost(Name = "DumpData")]

        //public async Task<IActionResult> Post()
        //{
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        Product product = new()
        //        {
        //            Category = $"Kategori {i}",
        //            CreateDate = DateTime.Now,
        //            Description = $"Açıklama {i}",
        //            Status = true,
        //            Unit = i,
        //            UnitPrice = i * 10
        //        };
        //        await _context.Products.AddAsync(product);
        //        await _context.SaveChangesAsync();
        //    }
        //    return Ok("Ürünler başarıyla oluşturuldu");
        //}
        #endregion
    }
}
