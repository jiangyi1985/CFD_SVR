﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CFD_API.DTO;
using CFD_COMMON;
using CFD_COMMON.Models.Cached;
using CFD_COMMON.Utils;

namespace CFD_API.Caching
{
    public class WebCache
    {
        private static Timer _timerProdDef;
        private static Timer _timerQuote;
        private static Timer _timerTick;

        private static TimeSpan _updateIntervalProdDef = TimeSpan.FromSeconds(3);
        private static TimeSpan _updateIntervalQuote = TimeSpan.FromMilliseconds(500);
        private static TimeSpan _updateIntervalTick = TimeSpan.FromSeconds(10);

        public static IList<ProdDef> ProdDefs { get; private set; }
        public static IList<Quote> Quotes { get; private set; }
        public static ConcurrentDictionary<int, List<TickDTO>> TickToday { get; private set; }
        public static ConcurrentDictionary<int, List<TickDTO>> TickWeek { get; private set; }
        public static ConcurrentDictionary<int, List<TickDTO>> TickMonth { get; private set; }

        static WebCache()
        {
            //initialize
            ProdDefs = new List<ProdDef>();
            Quotes = new List<Quote>();
            TickToday = new ConcurrentDictionary<int, List<TickDTO>>();
            TickWeek = new ConcurrentDictionary<int, List<TickDTO>>();
            TickMonth = new ConcurrentDictionary<int, List<TickDTO>>();

            //get value from Redis
            using (var redisClient = CFDGlobal.PooledRedisClientsManager.GetClient())
            {
                try
                {
                    ProdDefs = redisClient.As<ProdDef>().GetAll();
                    Quotes = redisClient.As<Quote>().GetAll();
                }
                catch (Exception e)
                {
                    CFDGlobal.LogExceptionAsInfo(e);
                }
            }

            //set timer
            _timerProdDef = new Timer(UpdateProdDefs, null, _updateIntervalProdDef, TimeSpan.FromMilliseconds(-1));
            _timerQuote = new Timer(UpdateQuotes, null, _updateIntervalQuote, TimeSpan.FromMilliseconds(-1));
            _timerTick = new Timer(UpdateTicks, null, _updateIntervalTick, TimeSpan.FromMilliseconds(-1));
        }

        private static void UpdateTicks(object state)
        {
            while (true)
            {
                //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //the logic here must be identical as CFD_JOBS.Ayondo.TickChartWorker
                //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                UpdateTicksByConditions(TickToday, TickSize.OneMinute, TimeSpan.FromHours(12));
                UpdateTicksByConditions(TickWeek, TickSize.TenMinute, TimeSpan.FromDays(7));
                UpdateTicksByConditions(TickMonth, TickSize.OneHour, TimeSpan.FromDays(30));

                Thread.Sleep(_updateIntervalTick);
            }
        }

        private static void UpdateTicksByConditions(ConcurrentDictionary<int, List<TickDTO>> dicTicks, TickSize tickSize, TimeSpan tsTickListLength)
        {
            try
            {
                foreach (var pair in dicTicks)
                {
                    //var count = TickToday[pair.Key].Count;
                    //var first = TickToday[pair.Key].First();
                    //var last = TickToday[pair.Key].Last();

                    var id = pair.Key;
                    //var tickDTOs = pair.Value;

                    var prodDef = ProdDefs.FirstOrDefault(o => o.Id == id);
                    if (prodDef == null ||
                        prodDef.QuoteType == enmQuoteType.Closed || prodDef.QuoteType == enmQuoteType.Inactive) //not opening
                        continue;

                    var quote = Quotes.FirstOrDefault(o => o.Id == id);
                    if (quote == null)
                        continue;

                    var newTick = new TickDTO()
                    {
                        p = CFD_COMMON.Utils.Quotes.GetLastPrice(quote),
                        //time = quote.Time
                        time = Quotes.Max(o => o.Time) //为了在价格不变的时候补点
                    };

                    if (pair.Value.Count == 0)
                    {
                        pair.Value.Add(newTick);
                        continue;
                    }

                    var lastInList = pair.Value.Last();
                    if (lastInList.time >= newTick.time)
                        continue;

                    if (Ticks.IsTickEqual(lastInList.time, newTick.time, tickSize))
                        pair.Value[pair.Value.Count - 1] = newTick; //update
                    else
                    {
                        pair.Value.Add(newTick); //append
                    }

                    //delete old (before xxx hours ago)
                    var dtLast = pair.Value.Last().time;
                    var dtFirst = pair.Value.First().time;
                    if (dtLast - dtFirst > tsTickListLength)
                    {
                        dicTicks[pair.Key] = pair.Value.Where(o => dtLast - o.time <= tsTickListLength).ToList();
                    }

                    //var count2 = TickToday[pair.Key].Count;
                    //var first2 = TickToday[pair.Key].First();
                    //var last2 = TickToday[pair.Key].Last();
                    //CFDGlobal.LogLine(count + " => " + count2 + "   " + first.time + " ~ " + last.time + "  =>  " + first2.time + " ~ " + last2.time);
                }
            }
            catch (Exception e)
            {
                CFDGlobal.LogExceptionAsInfo(e);
            }
        }

        private static void UpdateProdDefs(object state)
        {
            while (true)
            {
                //CFDGlobal.LogLine("Updating WebCache ProdDefs...");
                using (var redisClient = CFDGlobal.PooledRedisClientsManager.GetClient())
                {
                    try
                    {
                        ProdDefs = redisClient.As<ProdDef>().GetAll();
                    }
                    catch (Exception e)
                    {
                        CFDGlobal.LogExceptionAsInfo(e);
                    }
                }

                Thread.Sleep(_updateIntervalProdDef);
            }
        }

        private static void UpdateQuotes(object state)
        {
            while (true)
            {
                //CFDGlobal.LogLine("Updating WebCache Quotes...");
                using (var redisClient = CFDGlobal.PooledRedisClientsManager.GetClient())
                {
                    try
                    {
                        Quotes = redisClient.As<Quote>().GetAll();
                    }
                    catch (Exception e)
                    {
                        CFDGlobal.LogExceptionAsInfo(e);
                    }
                }

                Thread.Sleep(_updateIntervalQuote);
            }
        }
    }
}