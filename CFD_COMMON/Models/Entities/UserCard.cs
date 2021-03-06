﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CFD_COMMON.Models.Entities
{
    public class UserCardBase {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 用户Id
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 卡片Id
        /// </summary>
        public int CardId { get; set; }

        public long PositionId { get; set; }

        /// <summary>
        /// 买涨、买跌
        /// </summary>
        public bool? IsLong { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public decimal? Qty { get; set; }

        public decimal? Invest { get; set; }

        public decimal? Leverage { get; set; }

        public decimal? TradePrice { get; set; }

        public decimal? SettlePrice { get; set; }

        public DateTime? TradeTime { get; set; }

        /// <summary>
        /// 鼓励金
        /// </summary>
        public decimal? Reward { get; set; }

        public int? SecurityId { get; set; }

        public decimal? PL { get; set; }

        public DateTime? ClosedAt { get; set; }

        /// <summary>
        /// 点赞数量
        /// </summary>
        public int? Likes { get; set; }

        public bool? IsNew { get; set; }

        /// <summary>
        /// 是否被分享到首页
        /// </summary>
        public bool? IsShared { get; set; }
        /// <summary>
        /// 是否已支付过交易金
        /// </summary>
        public bool? IsPaid { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? Expiration { get; set; }
    }

    /// <summary>
    /// 模拟盘的用户卡片
    /// </summary>
    [Table("UserCard")]
    public class UserCard : UserCardBase
    {
        
    }
}
