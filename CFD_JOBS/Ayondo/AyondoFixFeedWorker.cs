﻿using System;
using System.Collections.Generic;
using System.Linq;
using CFD_COMMON;
using CFD_COMMON.Models.Cached;
using QuickFix;
using QuickFix.Transport;

namespace CFD_JOBS.Ayondo
{
    internal class AyondoFixFeedWorker
    {
        public static void Run()
        {
            SessionSettings settings = new SessionSettings(CFDGlobal.GetConfigurationSetting("ayondoFixFeedCfgFilePath"));
            AyondoFixFeedApp myApp = new AyondoFixFeedApp();
            IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
            //ILogFactory logFactory = new FileLogFactory(settings);
            SocketInitiator initiator = new SocketInitiator(myApp, storeFactory, settings
                //,logFactory
                );

            //var basicRedisClientManager = CFDGlobal.GetNewBasicRedisClientManager();
            var redisClient = CFDGlobal.BasicRedisClientManager.GetClient();
            var redisProdDefClient = redisClient.As<ProdDef>();
            var redisTickClient = redisClient.As<Tick>();

            initiator.Start();
            while (true)
            {
                //System.Console.WriteLine("o hai");
                System.Threading.Thread.Sleep(1000);

                try
                {
                    //save ProdDefs
                    if (!myApp.ProdDefs.IsEmpty)
                    {
                        //CFDGlobal.LogLine("Pending ProdDefs detected. Loading from queue...");

                        //new prod list from Ayondo MDS2
                        IList<ProdDef> listNew = new List<ProdDef>();

                        while (!myApp.ProdDefs.IsEmpty)
                        {
                            ProdDef obj;
                            var tryDequeue = myApp.ProdDefs.TryDequeue(out obj);
                            listNew.Add(obj);
                        }

                        //-----------------SAVING PROD DEF-------------------------------------------
                        CFDGlobal.LogLine("Saving " + listNew.Count + " ProdDefs to Redis...");

                        //current redis list
                        var listOld = redisProdDefClient.GetAll();

                        IList<ProdDef> listToSave = new List<ProdDef>();
                        var listToSaveAsQuote = new List<ProdDef>();

                        foreach (var newProdDef in listNew)
                        {
                            var old = listOld.FirstOrDefault(o => o.Id == newProdDef.Id);

                            if (old != null) //updating prod def in redis
                            {
                                //update open/close time/price depending on state change
                                if (old.QuoteType != enmQuoteType.Closed && newProdDef.QuoteType == enmQuoteType.Closed) //xxx -> close
                                {
                                    CFDGlobal.LogLine("PROD CLOSED " + newProdDef.Id + " time: " + newProdDef.Time);

                                    //close time
                                    old.LastClose = newProdDef.Time;

                                    //prod def will be treated as a new QUOTE when stock open/close
                                    listToSaveAsQuote.Add(newProdDef);
                                }
                                else if (old.QuoteType != enmQuoteType.Open && newProdDef.QuoteType == enmQuoteType.Open) //xxx -> open
                                {
                                    CFDGlobal.LogLine("PROD OPENED " + newProdDef.Id + " time: " + newProdDef.Time + " offer: " + newProdDef.Offer + " bid: " + newProdDef.Bid);

                                    //open time
                                    old.LastOpen = newProdDef.Time;

                                    //open prices
                                    old.OpenAsk = newProdDef.Offer;
                                    old.OpenBid = newProdDef.Bid;

                                    //preclose
                                    old.PreClose = newProdDef.CloseAsk;

                                    //prod def will be treated as a new QUOTE when stock open/close
                                    listToSaveAsQuote.Add(newProdDef);
                                }

                                //update fields
                                old.Time = newProdDef.Time;
                                old.QuoteType = newProdDef.QuoteType;
                                old.Name = newProdDef.Name;
                                old.Symbol = newProdDef.Symbol;
                                old.AssetClass = newProdDef.AssetClass;
                                old.Bid = newProdDef.Bid;
                                old.Offer = newProdDef.Offer;
                                old.CloseBid = newProdDef.CloseBid;
                                old.CloseAsk = newProdDef.CloseAsk;
                                old.Shortable = newProdDef.Shortable;
                                old.MinSizeShort = newProdDef.MinSizeShort;
                                old.MaxSizeShort = newProdDef.MaxSizeShort;
                                old.MinSizeLong = newProdDef.MinSizeLong;
                                old.MaxSizeLong = newProdDef.MaxSizeLong;
                                old.MaxLeverage = newProdDef.MaxLeverage;

                                listToSave.Add(old);
                            }
                            else //appending new prod def into redis
                            {
                                listToSave.Add(newProdDef);
                            }
                        }

                        redisProdDefClient.StoreAll(listToSave);

                        //-----------------SAVING QUOTE-------------------------------------------
                        if (listToSaveAsQuote.Count > 0)
                        {
                            var quotes = listToSaveAsQuote.Select(o => new Quote()
                            {
                                Bid = o.Bid.Value,
                                Id = o.Id,
                                Offer = o.Offer.Value,
                                Time = o.Time
                            }).ToList();
                            TickChartWorker.SaveQuoteTicks(quotes, redisTickClient, TickChartWorker.TickSize.OneMinute);
                            //var newCount = 0;
                            //var updateCount = 0;
                            //var appendCount = 0;
                            //var identicalCount = 0;
                            //var overdueCount = 0;
                            //foreach (var prodDef in listToSaveAsQuote)
                            //{
                            //    var listName = "tick:" + prodDef.Id;

                            //    IRedisList<Tick> list = redisTickClient.Lists[listName];

                            //    var newTick = new Tick()
                            //    {
                            //        P = prodDef.Offer.Value,
                            //        Time = prodDef.Time
                            //    };

                            //    if (list.Count == 0) //new quote?
                            //    {
                            //        newCount++;
                            //        list.Add(newTick);
                            //        continue;
                            //    }

                            //    var lastTick = list[list.Count - 1]; //last tick in cache
                            //    if (newTick.Time > lastTick.Time)
                            //    {
                            //        if (DateTimes.IsEqualDownToMinute(newTick.Time, lastTick.Time))
                            //        {
                            //            updateCount++;
                            //            list[list.Count - 1] = newTick; //update last tick to new tick
                            //        }
                            //        else
                            //        {
                            //            appendCount++;
                            //            list.Add(newTick); //append new tick
                            //        }
                            //    }
                            //    else
                            //    {
                            //        if (newTick.Time == lastTick.Time)
                            //            identicalCount++;
                            //        else
                            //            overdueCount++;
                            //    }
                            //}

                            //CFDGlobal.LogLine("ProdDef as Quotes: " + listToSaveAsQuote.Count + " update: " + updateCount + " append: " + appendCount + " identical: " + identicalCount +
                            //                  " new: " +
                            //                  newCount + " overdue: " + overdueCount);
                        }
                    }
                }
                catch (Exception e)
                {
                    CFDGlobal.LogException(e);
                }
            }
            //initiator.Stop();
        }
    }
}