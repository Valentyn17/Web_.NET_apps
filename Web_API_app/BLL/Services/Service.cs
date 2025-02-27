﻿using AutoMapper;
using BLL.DTO;
using BLL.Infrastructure;
using BLL.Interfaces;
using DAL.Entities;
using DAL.Enums;
using DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class Service : IService
    {

        IUnitOfWork Database { get; set; }

        public Service(IUnitOfWork unitOfWork)
        {
            Database = unitOfWork;
        }

        public void AddGoodsToStorage(int? id, int Count)
        {
            if (id == null)
            {
                throw new InvalidOperationException(message: "Good was null");
            }
            Good good = Database.Goods.Get(id.Value);
            if (good == null)
            {
                throw new InvalidOperationException(message: "Can't find such good");
            }
            good.Count += Count;
            Database.Goods.Update(good);

        }

        public void Dispose()
        {
            Database.Dispose();
        }

        public GoodDTO GetGood(int? id)
        {
            if (id == null)
                throw new InvalidOperationException(message: "Id was not setted");
            var good = Database.Goods.Get(id.Value);
            if (good == null)
                throw new InvalidOperationException("Can't find such good");

            return new GoodDTO { Count = good.Count, Descr = good.Descr, Id = good.Id, Name = good.Name, Price = good.Price };

        }

        public IEnumerable<GoodDTO> FindByName(string Name)
        {
            if (Name == null)
                throw new InvalidOperationException(message: "Name was not setted");
            var goods = Database.Goods.Find(i => i.Name.Contains(Name));
            var mapper = AutoMapperConfiguration.GetMapper();
            return mapper.Map<IEnumerable<Good>, List<GoodDTO>>(goods);
        }

        public IEnumerable<GoodDTO> GetGoods()
        {
            var mapper = AutoMapperConfiguration.GetMapper();
            return mapper.Map<IEnumerable<Good>, List<GoodDTO>>(Database.Goods.GetAll());
        }

        public IEnumerable<OrderDTO> GetOrders()
        {
            var mapper = AutoMapperConfiguration.GetMapper();
            var orders = mapper.Map<IEnumerable<Order>, List<OrderDTO>>(Database.Orders.GetAll());
            foreach (var item in orders)
            {
                item.GoodName = Database.Goods.Get(item.GoodId).Name;
            }
            return orders;
        }

        public void MakeOrder(OrderDTO orderDto)
        {
            Good good = Database.Goods.Get(orderDto.GoodId);

            // валидация
            if (good == null)
                throw new InvalidOperationException(message: "Can't find such good");
            Order order = new Order
            {
                Date = DateTime.Now,
                GoodId = good.Id,
                Sum = good.Price * orderDto.Count,
                Email= orderDto.Email,
                Good = good,
                Status=Status.New,
                Count = orderDto.Count
            };
            Database.Orders.Create(order);
            Database.Save();
        }

        public void ChangeOrderStatus(OrderDTO orderDTO)
        {
            int count = orderDTO.Count;
            Good good = Database.Goods.Get(orderDTO.GoodId);
            Order order1 = Database.Orders.Get(orderDTO.Id);
            if (orderDTO == null || order1==null || good==null)
                throw new InvalidOperationException(message: $"Some object was null");
            
            if (orderDTO.Status < order1.Status || orderDTO.Status==0)
            {
                throw new InvalidOperationException(message: "You can't change status from upper to lower");
            }
            if (count > good.Count)
            {
                if (orderDTO.Status == Status.Confirmed)
                {
                    throw new InvalidOperationException(message: "You can't set status confirmed to this order because you don't have enough goods in storage");
                }
                else
                {
                   
                    var mapper = AutoMapperConfiguration.GetMapper();
                    Order order = mapper.Map<OrderDTO, Order>(orderDTO);
                    
                    Database.Orders.Update(order);
                    Database.Save();
                }
            }
            else 
            {
                Order order = Database.Orders.Get(orderDTO.Id);
                if (order.Status != Status.Confirmed)
                {
                    good.Count -= orderDTO.Count;

                    Database.Goods.Update(good);
                    
                    var mapper = AutoMapperConfiguration.GetMapper();

                    order = mapper.Map<OrderDTO, Order>(orderDTO);
                    order.Sum = good.Price * count;
                    Database.Orders.Update(order);
                    Database.Save();
                }
            }

            
            
        }

        public void CreateGood(GoodDTO goodDTO)
        {
            if (goodDTO == null)
                throw new InvalidOperationException();
            var config = new MapperConfiguration(cfg => cfg.CreateMap<GoodDTO, Good>());
            var mapper = new Mapper(config);
            Good good = mapper.Map<GoodDTO, Good>(goodDTO);
            Database.Goods.Create(good);
            Database.Save();
        }

        public void DeleteGood(int id)
        {
            var good=Database.Goods.Find(i => i.Id == id);
            if (good == null)
                throw new ArgumentException("Such product does not exist");
            Database.Goods.Delete(id);
            Database.Save();

        }
        public void DeleteOrder(int id)
        {

            Database.Orders.Delete(id);
            Database.Save();

        }

        public void UpdateGood(GoodDTO goodDTO)
        {
            var config = new MapperConfiguration(cfg => cfg.CreateMap<GoodDTO, Good>());
            var mapper = new Mapper(config);
            Good good = mapper.Map<GoodDTO, Good>(goodDTO);
            Database.Goods.Update(good);
            Database.Save();
        }

        public OrderDTO GetOrder(int? id)
        {
            if (id == null)
                throw new InvalidOperationException(message: "Id was not setted");
            var order = Database.Orders.Get(id.Value);
            if (order == null)
                throw new InvalidOperationException("Can't find such order");
            var config = new MapperConfiguration(cfg => cfg.CreateMap<Order, OrderDTO>());
            var mapper = new Mapper(config);
            OrderDTO order1 = mapper.Map<Order, OrderDTO>(order);
            return order1;

        }
    }
}
