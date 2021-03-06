﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFD_COMMON.Models.Entities
{
    /// <summary>
    /// 该表记录了好友推荐奖励金
    /// 被推荐人模拟注册成功，可得30元奖励金
    /// 实盘入金后，推荐人可得30元奖励金
    /// </summary>
    [Table("ReferReward")]
    public class ReferReward
    {
        public int ID { get; set; }
        /// <summary>
        /// 推荐人ID
        /// </summary>
        public int UserID { get; set; }
       
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
