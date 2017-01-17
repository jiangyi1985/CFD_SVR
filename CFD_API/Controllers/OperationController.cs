﻿using AutoMapper;
using CFD_API.Controllers.Attributes;
using CFD_API.DTO;
using CFD_API.DTO.Form;
using CFD_COMMON.Models.Context;
using CFD_COMMON.Utils;
using Newtonsoft.Json.Linq;
using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace CFD_API.Controllers
{
    [RoutePrefix("api/operation")]
    public class OperationController : CFDController
    {
        public OperationController(CFDEntities db, IMapper mapper)
            : base(db, mapper)
        {
        }

        public string Test()
        {
            return null;
        }

        /// <summary>
        /// 由运营人员发起的消息推送
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        [HttpPost]
        [AdminAuth]
        public ResultDTO Push(OperationPushDTO form)
        {
            ResultDTO result = new ResultDTO() { success = true };
            var phoneList = form.phone.Split(',').ToList();//requestObj["phone"].Value<string>().Split(',').ToList();

            var tokenListQuery = from u in db.Users
                                             join d in db.Devices on u.Id equals d.userId
                                             where phoneList.Contains(u.Phone) 
                                             select new { d.deviceToken, u.Id, u.AyondoAccountId, u.AutoCloseAlert };

            var tokenList = tokenListQuery.ToList();
            string msg = form.message;
            string pushType = string.IsNullOrEmpty(form.deepLink) ? "0" : "3";

            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
            string format = "{{\"type\":\"{1}\", \"title\":\"盈交易测试\", \"StockID\":0, \"CName\":\"\", \"message\":\"{0}\", \"deepLink\":\"{2}\"}}";
            foreach(var token in tokenList)
            {
                list.Add(new KeyValuePair<string, string>(token.deviceToken, string.Format(format,msg, pushType, form.deepLink)));
            }

            var push = new GeTui();
            var response = push.PushBatch(list);
            result.message = response;
            return result;
        }

        [HttpPost]
        [Route("reward/transfer")]
        [AdminAuth]
        public List<RewardTransferDTO> GetRewardTransferHistory(RewardTransferSearchDTO form)
        {
            if (form == null || string.IsNullOrEmpty(form.startTime) || string.IsNullOrEmpty(form.startTime))
            {
                return null;
            }

            DateTime startTime = DateTime.Parse(form.startTime);
            DateTime endTime = DateTime.Parse(form.endTime);

            var rewardTransferHistory = (from x in db.RewardTransfers
                                         join y in db.Users on x.UserID equals y.Id
                                         join z in db.UserInfos on y.Id equals z.UserId
                                         into t1
                                         from t2 in t1.DefaultIfEmpty()
                                         where x.CreatedAt>startTime && x.CreatedAt < endTime
                                         select new RewardTransferDTO() { liveAccount = y.AyLiveUsername, liveAccountID = y.AyLiveAccountId.HasValue? y.AyLiveAccountId.Value.ToString() : string.Empty, name = t2.LastName + t2.FirstName, amount = x.Amount, date = x.CreatedAt }).ToList();
            return rewardTransferHistory;
        }
    }
}