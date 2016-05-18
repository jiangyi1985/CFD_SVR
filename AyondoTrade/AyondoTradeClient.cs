﻿using System;
using System.Collections.Generic;
using System.ServiceModel;
using AyondoTrade.Model;

namespace AyondoTrade
{
    public class AyondoTradeClient : ClientBase<IAyondoTradeService>, IAyondoTradeService
    {
        public AyondoTradeClient(System.ServiceModel.Channels.Binding binding, EndpointAddress edpAddr)
            : base(binding, edpAddr)
        {
        }

        public string Test(string text)
        {
            return base.Channel.Test(text);
        }

        public IList<Model.PositionReport> GetPositionReport(string username, string password)
        {
            return base.Channel.GetPositionReport(username, password);
        }

        public IList<PositionReport> GetPositionHistoryReport(string username, string password, DateTime startTime, DateTime endTime)
        {
            return base.Channel.GetPositionHistoryReport(username, password, startTime, endTime);
        }

        public PositionReport NewOrder(string username, string password, int securityId, bool isLong, decimal orderQty, //char? ordType = null, decimal? price = null, 
            decimal? leverage = null, decimal? stopPx = null, string nettingPositionId = null)
        {
            return base.Channel.NewOrder(username, password, securityId, isLong, orderQty, //ordType: ordType, price: price,
                 leverage: leverage, stopPx: stopPx, nettingPositionId: nettingPositionId);
        }

        public PositionReport NewTakeOrder(string username, string password, int securityId, decimal price, string nettingPositionId)
        {
            return base.Channel.NewTakeOrder(username, password, securityId, price, nettingPositionId);
        }

        public PositionReport ReplaceOrder(string username, string password, int securityId, string orderId, decimal price,string nettingPositionId)
        {
            return base.Channel.ReplaceOrder(username, password, securityId, orderId, price, nettingPositionId);
        }

        public PositionReport CancelOrder(string username, string password, int securityId, string orderId, string nettingPositionId)
        {
            return base.Channel.CancelOrder(username, password, securityId, orderId, nettingPositionId);
        }

        public decimal GetBalance(string username, string password)
        {
            return base.Channel.GetBalance(username, password);
        }
    }
}