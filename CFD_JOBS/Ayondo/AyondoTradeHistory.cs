﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using CFD_COMMON;
using CFD_COMMON.Models.Context;
using ServiceStack.Text;

namespace CFD_JOBS.Ayondo
{
    public class AyondoTradeHistory
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
        private static DateTime? _lastEndTime = null;

        public static void Run()
        {
            while (true)
            {
                try
                {
                    DateTime dtStart;
                    DateTime dtEnd = DateTime.UtcNow;

                    if (_lastEndTime == null)
                    {
                        dtStart = dtEnd - Interval; //fetch interval length of period
                    }
                    else
                    {
                        dtStart = _lastEndTime.Value; //fetch data since last fetch
                    }

                    var tsStart = dtStart.ToUnixTimeMs();
                    var tsEnd = dtEnd.ToUnixTimeMs();

                    var webClient = new WebClient();

                    CFDGlobal.LogLine("Fetching data " + dtStart + " ~ " + dtEnd);

                    var dtDownloadStart = DateTime.UtcNow;
                    var downloadString = webClient.DownloadString(
                        "http://thvm-prod4.cloudapp.net:14535/demo/reports/tradehero/cn/tradehistory?start="
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
                        using (var db = CFDEntities.Create())
                        {
                            var dbMaxCreateTime = db.AyondoTradeHistories.Max(o => o.CreateTime);

                            var entities = new List<CFD_COMMON.Models.Entities.AyondoTradeHistory>();

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

                                var tradeHistory = new CFD_COMMON.Models.Entities.AyondoTradeHistory()
                                {
                                    PositionId = posId,
                                    TradeId = tradeId,
                                    AccountId = accountId,
                                    FirstName = firstName,
                                    LastName = lastName,
                                    TradeTime = time,
                                    SecurityId = secIdD,
                                    SecurityName = secName,
                                    Direction = direction,
                                    Quantity = qty,
                                    TradePrice = price,
                                    PL = pl,
                                    GUID = guid,
                                    StopLoss = stopLoss,
                                    TakeProfit = takeProfit,
                                    CreateTime = createTime,
                                    UpdateType = updateType,
                                    DeviceType = deviceType,
                                };

                                if (tradeHistory.CreateTime <= dbMaxCreateTime)
                                    continue; //skip old data

                                entities.Add(tradeHistory);
                            }

                            CFDGlobal.LogLine("maxCreateTime: " + dbMaxCreateTime + " data:" + lineArrays.Count +
                                              " newData:" + entities.Count);

                            if (entities.Count > 0)
                            {
                                CFDGlobal.LogLine("saving to db...");
                                db.AyondoTradeHistories.AddRange(entities);
                                db.SaveChanges();
                            }
                        }
                    }

                    _lastEndTime = dtEnd;
                }
                catch (Exception e)
                {
                    CFDGlobal.LogException(e);
                }

                Thread.Sleep(Interval);
            }
        }
    }
}