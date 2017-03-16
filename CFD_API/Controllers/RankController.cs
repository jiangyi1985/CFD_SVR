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
                    winRate = (decimal)o.Count(p => p.PL > 0) / o.Count(),
                    roi = o.Sum(p => p.PL.Value) / o.Sum(p => p.InvestUSD.Value),
                }).OrderByDescending(o=>o.roi).ToList();

            //move myself to the top
            var findIndex = userDTOs.FindIndex(o => o.id == UserId);
            if (findIndex > 0)
            {
                var me = userDTOs.First(o => o.id == UserId);
                userDTOs.RemoveAt(findIndex);
                userDTOs.Insert(0, me);
            }
            else if (findIndex == 0)
            {
                //do nothing
            }
            else
            {
                var me = GetUser();
                userDTOs.Insert(0, new UserDTO()
                {
                    id = me.Id,

                    posCount = 0,
                    roi = 0,
                    winRate = 0,
                });
            }

            ////100 at max
            //userDTOs = userDTOs.Take(100).ToList();

            //only return users with positive ROIs
            var result = userDTOs.Take(1).Concat(userDTOs.Skip(1).Where(o => o.roi > 0).Take(99)).ToList();

            //populate nickname/picUrl
            var userIds = result.Select(o => o.id).ToList();
            var users = db.Users.Where(o => userIds.Contains(o.Id)).ToList();
            foreach (var userDto in result)
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

            return result;

            //return null;
        } 
    }
}