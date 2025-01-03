﻿using Newtonsoft.Json;
using OrderAppWeb.API.Models.Dtos;
using OrderAppWeb.API.Models.Entities;

namespace OrderAppWeb.API.Profile
{
    public class MapperProfile : AutoMapper.Profile
    {
        public MapperProfile()
        {
            CreateMap<Product, ProductDto>();
            CreateMap<ProductDto, Product>();
            CreateMap<Order, CreateOrderRequest>();
            CreateMap<CreateOrderRequest, Order>();
            CreateMap<ProductDetailDto, OrderDetail>();
            CreateMap<OrderDetail, ProductDetailDto>();
            CreateMap<StackExchange.Redis.RedisValue, Log>().ConvertUsing(src => JsonConvert.DeserializeObject<Log>(src.ToString())); ;
        }
    }
}
