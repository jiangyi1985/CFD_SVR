﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using CFD_COMMON;
using CFD_COMMON.Models.Context;
using ServiceStack.Text;
using System.Threading.Tasks;
using CFD_COMMON.Utils;
using CFD_COMMON.Utils.Extensions;
using CFD_COMMON.Localization;
using CFD_COMMON.Models.Entities;

namespace CFD_JOBS.Ayondo
{
    public class AyondoTradeHistoryImport
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
        private static DateTime? _lastEndTime = null;

        public static void Run(bool isLive = false)
        {
            var mapper = MapperConfig.GetAutoMapperConfiguration().CreateMapper();

            while (true)
            {
                try
                {
                    DateTime dtStart;
                    DateTime dtEnd;
                    var dtNow = DateTime.UtcNow;

                    //bool needSave = false;

                    if (_lastEndTime == null) //first time started
                    {
                        AyondoTradeHistoryBase lastDbRecord;
                        using (var db = CFDEntities.Create())//find last record in db
                        {
                            lastDbRecord = isLive
                                ? (AyondoTradeHistoryBase) db.AyondoTradeHistory_Live.OrderByDescending(o => o.Id).FirstOrDefault()
                                : db.AyondoTradeHistories.OrderByDescending(o => o.Id).FirstOrDefault();
                        }

                        if (lastDbRecord == null) //db is empty
                        {
                            dtEnd = dtNow;
                            dtStart = dtEnd - Interval;
                        }
                        else //last record in db is found
                        {
                            var dtLastDbRecord = lastDbRecord.TradeTime.Value;

                            dtStart = dtLastDbRecord.AddMilliseconds(1);

                            //如果上次同步时间超过24小时，则每次最多只取24小时
                            dtEnd = dtNow - dtLastDbRecord > TimeSpan.FromHours(24) ? dtStart.AddHours(24) : dtNow;
                        }
                    }
                    else
                    {
                        dtStart = _lastEndTime.Value.AddMilliseconds(1);
                        dtEnd = dtNow;
                    }

                    //using (var db = CFDEntities.Create())
                    //{
                    //    var lastTradeHistory = isLive
                    //        ? db.AyondoTradeHistory_Live.OrderByDescending(o => o.Id).FirstOrDefault()
                    //        : db.AyondoTradeHistories.OrderByDescending(o => o.Id).FirstOrDefault();

                    //    //如果上次同步时间超过24小时，则每次最多只取24小时
                    //    if ((DateTime.UtcNow - lastTradeHistory.TradeTime).Value.Hours > 24)
                    //    {
                    //        dtEnd = lastTradeHistory.TradeTime.Value.AddHours(24);
                    //    }

                    //    //最后一次结束时间为空，意味着服务重新开启，此时需要获取数据库中最后一条TradeHistory作为开始时间
                    //    if (_lastEndTime == null)
                    //    {
                    //        if(lastTradeHistory != null)
                    //        {
                    //            dtStart = lastTradeHistory.TradeTime.Value.AddMilliseconds(1);
                    //        }
                    //        else
                    //        {
                    //            dtStart = dtEnd - Interval; //fetch interval length of period
                    //        }
                    //    }
                    //    else
                    //    {
                    //        dtStart = _lastEndTime.Value.AddMilliseconds(1); //fetch data since last fetch
                    //    }
                    //}

                    var tsStart = dtStart.ToUnixTimeMs();
                    var tsEnd = dtEnd.ToUnixTimeMs();

                    var webClient = new WebClient();

                    CFDGlobal.LogLine("Fetching data " + dtStart + " ~ " + dtEnd);

                    var dtDownloadStart = DateTime.UtcNow;
                    var downloadString = webClient.DownloadString(
                        CFDGlobal.GetConfigurationSetting("ayondoTradeHistoryHost"+(isLive?"_Live":"")) 
                        + (isLive?"live":"demo") + "/reports/tradehero/cn/tradehistory?start="
                        + tsStart + "&end=" + tsEnd);

                    CFDGlobal.LogLine("Done. " + (DateTime.UtcNow - dtDownloadStart).TotalSeconds + "s");

                    var lines = downloadString.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);

                    var lineArrays = lines.Skip(1) //skip headers
                        .Select(o => o.Split(','))
                        //.Where(o => o.Last() == "NA") //DeviceType == NA
                        .ToList();

                    if (lineArrays.Count == 0)
                    {
                        CFDGlobal.LogLine("no data received");
                    }
                    else
                    {
                        var newTradeHistories = new List<AyondoTradeHistoryBase>();
                        using (var db = CFDEntities.Create())
                        {
                            //var dbMaxCreateTime = db.AyondoTradeHistories.Max(o => o.CreateTime);

                            //PositionID,TradeID,AccountID,FirstName,LastName,
                            //TradeTime,ProductID,ProductName,Direction,Trade Size,
                            //Trade Price,Realized P&L,GUID,StopLoss,TakeProfit,
                            //CreationTime,UpdateType,DeviceType
                            foreach (var arr in lineArrays)
                            {
                                var posId = Convert.ToInt64(arr[0]);
                                var tradeId = Convert.ToInt64(arr[1]);
                                var accountId = Convert.ToInt64(arr[2]);
                                var firstName = arr[3];
                                var lastName = arr[4];
                                var time = DateTime.ParseExact(arr[5], CFDGlobal.AYONDO_DATETIME_MASK,
                                    CultureInfo.CurrentCulture,
                                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                                var secIdD = Convert.ToInt32(arr[6]);
                                var secName = arr[7];
                                var direction = arr[8];
                                var qty = Convert.ToDecimal(arr[9]);
                                var price = Convert.ToDecimal(arr[10]);
                                var pl = Convert.ToDecimal(arr[11]);
                                var guid = arr[12];
                                decimal? stopLoss = arr[13] == ""
                                    ? (decimal?) null
                                    : decimal.Parse(arr[13], NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint);
                                //1.0E-6
                                decimal? takeProfit = arr[14] == "" ? (decimal?) null : Convert.ToDecimal(arr[14]);
                                var createTime = DateTime.ParseExact(arr[15], CFDGlobal.AYONDO_DATETIME_MASK,
                                    CultureInfo.CurrentCulture,
                                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                                var updateType = arr[16];
                                var deviceType = arr[17];

                                var tradeHistory = new AyondoTradeHistoryBase()
                                {
                                    PositionId = posId,
                                    TradeId = tradeId,
                                    AccountId = accountId,
                                    FirstName = firstName,
                                    LastName = lastName,
                                    TradeTime = time, //data time
                                    SecurityId = secIdD,
                                    SecurityName = secName,
                                    Direction = direction,
                                    Quantity = qty,
                                    TradePrice = price,
                                    PL = pl,
                                    GUID = guid,
                                    StopLoss = stopLoss,
                                    TakeProfit = takeProfit,
                                    CreateTime = createTime, //position created time
                                    UpdateType = updateType,
                                    DeviceType = deviceType,
                                };

                                //if (tradeHistory.TradeTime <= dbMaxCreateTime)
                                //    continue; //skip old data

                                newTradeHistories.Add(tradeHistory);
                            }

                            //if(tradeHistory.UpdateType == "DELETE")
                            //        {
                            //            var newPositionHistory = isLive
                            //                ? db.NewPositionHistory_live.FirstOrDefault(h => h.Id == tradeHistory.PositionId)
                            //                : db.NewPositionHistories.FirstOrDefault(h => h.Id == tradeHistory.PositionId);

                            //            if(newPositionHistory != null)
                            //            {
                            //                newPositionHistory.ClosedPrice = tradeHistory.TradePrice;
                            //                newPositionHistory.ClosedAt = tradeHistory.TradeTime;
                            //                newPositionHistory.PL = tradeHistory.PL;
                            //                needSave = true;
                            //            }
                            //        }
                            //}

                            //CFDGlobal.LogLine("maxCreateTime: " + dbMaxCreateTime + " data:" + lineArrays.Count +
                            //                  " newData:" + entities.Count);

                            var newClosedTradeHistories = newTradeHistories.Where(o => o.UpdateType == "DELETE").ToList();

                            if (newClosedTradeHistories.Count > 0)//update position table with new closed trade histories
                            {
                                var newClosedPosIDs = newClosedTradeHistories.Select(o => o.PositionId).ToList();
                                
                                var positionsToClose = isLive
                                    ? db.NewPositionHistory_live.OfType<NewPositionHistory>()
                                        .Where(o => newClosedPosIDs.Contains(o.Id))
                                        .ToList()
                                    : db.NewPositionHistories.Where(o => newClosedPosIDs.Contains(o.Id)).ToList();

                                if (positionsToClose.Count > 0)
                                {
                                    foreach (var pos in positionsToClose)
                                    {
                                        var trade = newClosedTradeHistories.FirstOrDefault(o => o.PositionId == pos.Id);

                                        //update db fields
                                        pos.ClosedPrice = trade.TradePrice;
                                        pos.ClosedAt = trade.TradeTime;
                                        pos.PL = trade.PL;
                                    }

                                    CFDGlobal.LogLine("updating position close time/price/pl...");
                                    db.SaveChanges();
                                }
                            }

                            if (newTradeHistories.Count > 0)
                            {
                                if (isLive)
                                    db.AyondoTradeHistory_Live.AddRange(newTradeHistories.Select(o=> mapper.Map<AyondoTradeHistory_Live>(o)));
                                else
                                    db.AyondoTradeHistories.AddRange(newTradeHistories.Select(o => mapper.Map<AyondoTradeHistory>(o)));

                                CFDGlobal.LogLine("saving trade histories...");
                                db.SaveChanges();
                            }
                        }

                            var autoClosedHistories = newTradeHistories.OfType<AyondoTradeHistory>().Where(x => x.UpdateType == "DELETE" && x.DeviceType == "NA").ToList();
                            Push(autoClosedHistories);
                    }

                    CFDGlobal.LogLine("");
                    _lastEndTime = dtEnd;
                }
                catch (Exception e)
                {
                    CFDGlobal.LogException(e);
                }

                Thread.Sleep(Interval);
            }
        }

        /// <summary>
        /// push auto-close notification
        /// </summary>
        /// <param name="systemCloseHistory"></param>
        private static void Push(List<CFD_COMMON.Models.Entities.AyondoTradeHistory> systemCloseHistorys)
        {
            if (systemCloseHistorys == null || systemCloseHistorys.Count == 0)
                return;

            string msgTemplate = "{{\"id\":{3}, \"type\":\"1\", \"title\":\"盈交易\", \"StockID\":{1}, \"CName\":\"{2}\", \"message\":\"{0}\"}}";

            //me
            string msgContentTemplate = "{0}于{1}平仓，价格为{2}，{3}美元";

            List<KeyValuePair<string, string>> getuiPushList = new List<KeyValuePair<string, string>>();
            List<long> ayondoAccountIds = systemCloseHistorys.Where(o => o.AccountId.HasValue).Select(o => o.AccountId.Value).Distinct().ToList();
            using (var db = CFDEntities.Create())
            {
                //原先是只获取开启了自动提醒的用户。现在为了消息中心，改为获取全部用户，在后面的循环里面再判断是否要发推送（消息中心一定要保存的）。
                var query = from u in db.Users
                            join d in db.Devices on u.Id equals d.userId
                            into x from y in x.DefaultIfEmpty()
                            where ayondoAccountIds.Contains(u.AyondoAccountId.Value) //&& u.AutoCloseAlert.HasValue && u.AutoCloseAlert.Value
                               select new {y.deviceToken, u.Id, u.AyondoAccountId, u.AutoCloseAlert   };

                var result = query.ToList();
                //因为一个用户可能有多台设备，所以要在循环的时候判断一下，是否一条Position的平仓消息已经被记录过
                //Key - Position Id
                //Value - 生成的Message Id
                Dictionary<long, int> messageSaved = new Dictionary<long, int>();
                foreach(var h in systemCloseHistorys)
                {
                    if (!h.PositionId.HasValue)
                        continue;
                  
                    foreach (var item in result)
                    {
                        if(item.AyondoAccountId == h.AccountId)
                        {
                            #region save Message
                            //针对每一个position id，只保存一次message
                            if (!messageSaved.ContainsKey(h.PositionId.Value))
                            {
                                Message msg = new Message();
                                msg.UserId = item.Id;
                                //如果PL大于零，则为止盈消息（因为系统自动平仓没有止盈，只有止损）
                                if (h.PL > 0)
                                {
                                    string msgFormat = "{0}已达到您设置的止盈价格:{1},盈利+{2}";
                                    msg.Title = "止盈消息";
                                    string pl = Math.Abs(Math.Round(h.PL.Value, 2)).ToString();
                                    msg.Body = string.Format(msgFormat, Translator.GetCName(h.SecurityName), Math.Round(h.TradePrice.Value, 2), pl + "美元");
                                    msg.CreatedAt = DateTime.UtcNow;
                                    msg.IsReaded = false;
                                }
                                else if(!isAutoClose(h, db))//如果是设置的止损
                                {
                                    string msgFormat = "{0}已达到您设置的止损价格:{1},亏损-{2}";
                                    msg.Title = "止损消息";
                                    string pl = Math.Abs(Math.Round(h.PL.Value, 2)).ToString();
                                    msg.Body = string.Format(msgFormat, Translator.GetCName(h.SecurityName), Math.Round(h.TradePrice.Value, 2), pl + "美元");
                                    msg.CreatedAt = DateTime.UtcNow;
                                    msg.IsReaded = false;
                                }
                                else//系统自动平仓
                                {
                                    string msgFormat = "{0}已经被系统自动平仓，平仓价格:{1},{2}";
                                    msg.Title = "平仓消息";
                                    string pl = h.PL.Value < 0 ? "亏损-" + Math.Abs(Math.Round(h.PL.Value, 2)).ToString() : "盈利+" + Math.Abs(Math.Round(h.PL.Value, 2)).ToString();
                                    msg.Body = string.Format(msgFormat, Translator.GetCName(h.SecurityName), Math.Round(h.TradePrice.Value, 2), pl + "美元");
                                    msg.CreatedAt = DateTime.UtcNow;
                                    msg.IsReaded = false;
                                }
                                
                                db.Messages.Add(msg);
                                db.SaveChanges();
                                messageSaved.Add(h.PositionId.Value, msg.Id);
                            }
                            #endregion

                            #region Push notification
                            if (item.AutoCloseAlert.HasValue && item.AutoCloseAlert.Value && !string.IsNullOrEmpty(item.deviceToken))
                            {
                                string msgPart4 = string.Empty;
                                if (h.PL.HasValue)
                                {
                                    if (h.PL.Value < 0)
                                    {
                                        msgPart4 = "亏损" + Math.Abs(Math.Round(h.PL.Value)).ToString();
                                    }
                                    else
                                    {
                                        msgPart4 = "盈利" + Math.Abs(Math.Round(h.PL.Value)).ToString();
                                    }
                                }

                                string message = string.Format(msgContentTemplate, Translator.GetCName(h.SecurityName), DateTimes.UtcToChinaTime(h.TradeTime.Value).ToString(CFDGlobal.DATETIME_MASK_SECOND), Math.Round(h.TradePrice.Value, 2), msgPart4);
                                getuiPushList.Add(new KeyValuePair<string, string>(item.deviceToken, string.Format(msgTemplate, message, h.SecurityId, Translator.GetCName(h.SecurityName), messageSaved[h.PositionId.Value])));
                            }
                            #endregion

                        }
                    }
                }
                
            }

            var splitedPushList = getuiPushList.SplitInChunks(1000);
            var push = new GeTui();
            foreach(var pushList in splitedPushList)
            {
                var response = push.PushBatch(pushList);
                CFDGlobal.LogLine("Auto close notification push response:" + response);
            }
        }

        /// <summary>
        /// 判断某条平仓记录是系统自动平仓，还是设置止损后的平仓
        /// </summary>
        /// <param name="closedHistory"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        private static bool isAutoClose(CFD_COMMON.Models.Entities.AyondoTradeHistory closedHistory, CFDEntities db)
        {
            //用平仓记录的PositionID去找到开仓记录
            var openHistory = db.AyondoTradeHistories.FirstOrDefault(o => o.PositionId == closedHistory.PositionId && o.UpdateType == "CREATE");
            if (openHistory == null) //如果没有找到开仓记录（原则上不会发生），就认为是系统自动平仓
            {
                return true;
            }

            if(!openHistory.Quantity.HasValue || !openHistory.TradePrice.HasValue)//如果开仓记录没有价格或数量，就认为是系统自动平仓
            {
                return true;
            }

            decimal investment = openHistory.Quantity.Value * openHistory.TradePrice.Value;
            //如果亏损/投资比，大于0.9就认为是系统自动平仓
            if(0.9M < Math.Abs((closedHistory.PL / investment).Value))
            {
                return true;
            }

            return false;
        }
    }
}