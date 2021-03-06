﻿using CFD_COMMON;
using CFD_COMMON.Models.Cached;
using CFD_COMMON.Models.Context;
using CFD_COMMON.Models.Entities;
using CFD_COMMON.Utils;
using EntityFramework.Extensions;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CFD_JOBS
{
    public class QuizJob
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

        public static void Run()
        {
            while (true)
            {
                try
                {
                    var start = DateTime.UtcNow;
                    var end = DateTime.UtcNow.AddMinutes(5);

                    //北京时间上午6点(UTC 22点)统计前一天的竞猜结果
                    int timeToSendHour = 22;
                    try
                    {
                        using (var db = CFDEntities.Create())
                        {
                            timeToSendHour = int.Parse(db.Miscs.FirstOrDefault(o => o.Key == "QuizJob").Value);
                        }
                    }
                    catch
                    {
                        Console.WriteLine("读取发送时间失败");
                    }

                    var jobTime = DateTime.SpecifyKind(new DateTime(start.Year, start.Month, start.Day, timeToSendHour, 0, 0), DateTimeKind.Utc);
                    if (start < jobTime && end >= jobTime)
                    {
                        using (var db = CFDEntities.Create())
                        {                            
                            //找到当天交易日对应的竞猜活动
                            DateTime today = DateTime.Now.Date;
                            var quiz = db.Quizzes.FirstOrDefault(q => q.TradeDay.HasValue && q.TradeDay.Value == today && q.ExpiredAt == SqlDateTime.MaxValue.Value);

                            //DateTime nextDay = today.AddDays(1);
                            //var nextQuiz = db.Quizzes.FirstOrDefault(q => q.TradeDay.HasValue && q.TradeDay.Value == nextDay && q.ExpiredAt == SqlDateTime.MaxValue.Value);
                            //if(nextQuiz != null)
                            //{
                            //    //给下一个竞猜活动加注
                            //    int amouont = new Random().Next(500, 1000);
                            //    var quizBetShort = new QuizBet()
                            //    {
                            //        BetAmount = amouont,
                            //        BetDirection = "short",
                            //        PL = amouont,
                            //        CreatedAt = DateTime.Now,
                            //        QuizID = nextQuiz.ID,
                            //        UserID = 2031
                            //    };

                            //    var quizBetLong = new QuizBet()
                            //    {
                            //        BetAmount = amouont,
                            //        BetDirection = "long",
                            //        PL = amouont,
                            //        CreatedAt = DateTime.Now,
                            //        QuizID = nextQuiz.ID,
                            //        UserID = 2031
                            //    };

                            //    db.QuizBets.Add(quizBetShort);
                            //    db.QuizBets.Add(quizBetLong);
                            //    db.SaveChanges();
                            //}

                            if (quiz != null)
                            {                                
                                Console.WriteLine("Quiz found with trade day:" + quiz.TradeDay);
                                using (var redisClient = CFDGlobal.GetDefaultPooledRedisClientsManager(true).GetClient())
                                {
                                    var redisKLineClient = redisClient.As<KLine>();
                                    //var redisProdDefClient = redisClient.As<ProdDef>();

                                    var klines = redisKLineClient.Lists[KLines.GetKLineListNamePrefix(KLineSize.Day) + quiz.ProdID];

                                    if (klines.Count == 0)
                                    {
                                        CFDGlobal.LogWarning(string.Format("Kline for Security ID:{0} is empty", quiz.ProdID));
                                    }

                                    //get 20 records at max
                                    //var result = klines.GetRange(beginIndex < 0 ? 0 : beginIndex, klines.Count - 1)
                                    //    .FirstOrDefault(k=>k.Time.Date == quiz.TradeDay.Value.Date);
                                    var result = klines.FirstOrDefault(k => k.Time.Date == quiz.TradeDay.Value.Date);
                                    quiz.SettledAt = DateTime.Now;
                                    if (result != null)
                                    {
                                        decimal shortAmount = db.QuizBets.Where(qb => qb.QuizID == quiz.ID && qb.BetDirection == "short").Select(qb=>qb.BetAmount).DefaultIfEmpty(0).Sum().Value;
                                        decimal longAmount = db.QuizBets.Where(qb => qb.QuizID == quiz.ID && qb.BetDirection == "long").Select(qb => qb.BetAmount).DefaultIfEmpty(0).Sum().Value;
                                        quiz.OpenPrice = result.Open;
                                        quiz.ClosePrice = result.Close;
                                        if (result.Close > result.Open) //涨
                                        {
                                            //如果有人买涨，就把本金+买跌人的钱平分给买涨的人
                                            //如果没人买涨，不做任何操作
                                            if (db.QuizBets.Any(qb => qb.QuizID == quiz.ID && qb.BetDirection == "long"))
                                            {
                                                
                                                int longPersons = db.QuizBets.Where(qb => qb.QuizID == quiz.ID && qb.BetDirection == "long").Count();

                                                decimal rate = (longAmount + shortAmount) / longAmount;
                                                //把买跌的人的交易金平分给所有买涨的人
                                                db.QuizBets.Where(qb => qb.QuizID == quiz.ID && qb.BetDirection == "long")
                                                    .Update(qb => new QuizBet() { PL = qb.BetAmount * rate, SettledAt = DateTime.Now });

                                                //买跌的人PL清零
                                                db.QuizBets.Where(qb => qb.QuizID == quiz.ID && qb.BetDirection == "short")
                                                    .Update(qb => new QuizBet() { PL = 0, SettledAt = DateTime.Now });
                                            }
                                            else//如果无人买涨，就把买跌人的钱还回去
                                            {
                                                db.QuizBets.Where(qb => qb.QuizID == quiz.ID && qb.BetDirection == "short")
                                                    .Update(qb => new QuizBet() { PL = qb.BetAmount, SettledAt = DateTime.Now });
                                            }
                                           
                                            quiz.Result = "long";
                                        }
                                        else//跌
                                        {
                                            //如果有人买跌，就把本金+买涨人的钱平分给买跌的人
                                            //如果没人买跌，不做任何操作
                                            if (db.QuizBets.Any(qb => qb.QuizID == quiz.ID && qb.BetDirection == "short"))
                                            {
                                                
                                                int shortPersons = db.QuizBets.Where(qb => qb.QuizID == quiz.ID && qb.BetDirection == "short").Count();

                                                decimal rate = (longAmount + shortAmount) / shortAmount;
                                                //把买跌的人的交易金平分给所有买涨的人
                                                db.QuizBets.Where(qb => qb.QuizID == quiz.ID && qb.BetDirection == "short")
                                                    .Update(qb => new QuizBet() { PL = qb.BetAmount * rate, SettledAt = DateTime.Now });

                                                //买涨的人PL清零
                                                db.QuizBets.Where(qb => qb.QuizID == quiz.ID && qb.BetDirection == "long")
                                                    .Update(qb => new QuizBet() { PL = 0, SettledAt = DateTime.Now });
                                            }
                                            else//如果无人买跌，就把买涨人的钱还回去
                                            {
                                                db.QuizBets.Where(qb => qb.QuizID == quiz.ID && qb.BetDirection == "long")
                                                    .Update(qb => new QuizBet() { PL = qb.BetAmount, SettledAt = DateTime.Now });
                                            }

                                            quiz.Result = "short";
                                        }
                                    }

                                    db.SaveChanges();
                                }
                            }
                            else
                            {
                                Console.WriteLine("no quiz found");
                            }
                        }
                    }

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
