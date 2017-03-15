﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using AutoMapper;
using CFD_API.Controllers.Attributes;
using CFD_API.DTO;
using CFD_COMMON.Models.Context;
using CFD_COMMON.Utils;

namespace CFD_API.Controllers
{
    [RoutePrefix("api/rank")]
    public class RankController : CFDController
    {
        public RankController(CFDEntities db, IMapper mapper)
            : base(db, mapper)
        {
        }

        [HttpGet]
        [Route("live/user/plClosed/2w")]
        [BasicAuth]
        public List<UserDTO> GetUserRankPL2w()
        {
            var twoWeeksAgo = DateTimes.GetChinaToday().AddDays(-13);
            var twoWeeksAgoUtc = twoWeeksAgo.AddHours(-8);

            var userDTOs = db.NewPositionHistory_live.Where(o => o.ClosedAt != null && o.ClosedAt >= twoWeeksAgoUtc)
                .GroupBy(o=>o.UserId).Select(o=>new UserDTO()
                {
                    id = o.Key.Value,

                    posCount = o.Count(),
                    winRate = o.Count(p => p.PL > 0) / o.Count(),
                    roi = o.Sum(p => p.PL.Value) / o.Sum(p => p.InvestUSD.Value),
                }).OrderByDescending(o=>o.roi).ToList();

            var userIds = userDTOs.Select(o => o.id).ToList();
            var users = db.Users.Where(o => userIds.Contains(o.Id)).ToList();

            foreach (var userDto in userDTOs)
            {
                var user = users.First(o => o.Id == userDto.id);
                userDto.nickname = user.Nickname;
                userDto.picUrl = user.PicUrl;
            }

            //var userDTOs = positions.GroupBy(o => o.UserId).Select(o => new UserDTO()
            //{
            //    id = o.Key.Value,
            //    nickname = o.First().User.Nickname,
            //    picUrl = o.First().User.PicUrl,

            //    posCount = o.Count(),
            //    winRate = o.Count(p => p.PL > 0)/o.Count(),
            //    roi = o.Sum(p => p.PL.Value)/o.Sum(p => p.InvestUSD.Value),
            //}).OrderByDescending(o=>o.roi).ToList();

            var findIndex = userDTOs.FindIndex(o => o.id == UserId);
            if (findIndex > 0)
            {
                var me = userDTOs.First(o => o.id == UserId);
                userDTOs.RemoveAt(findIndex);
                userDTOs.Insert(0, me);
            }

            return userDTOs;

            //return null;
        } 
    }
}
