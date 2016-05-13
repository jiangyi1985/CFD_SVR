﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CFD_COMMON.Models.Cached;

namespace CFD_API.DTO
{
    public class SecurityDTO
    {
        public int id { get; set; }
        public string symbol { get; set; }
        public string name { get; set; }
        //public string picUrl { get; set; }
        public string tag { get; set; }

        public decimal? preClose { get; set; }
        public decimal? open { get; set; }
        public decimal? last { get; set; }

        public bool isOpen { get; set; }
    }

    public class SecurityDetailDTO : SecurityDTO
    {
        public int? dcmCount { get; set; }

        public DateTime? lastOpen { get; set; }
        public DateTime? lastClose { get; set; }

        public decimal? longPct { get; set; }

        public decimal? minValueLong { get; set; }
        public decimal? minValueShort { get; set; }
        public decimal? maxValueLong { get; set; }
        public decimal? maxValueShort { get; set; }
        public decimal? maxLeverage { get; set; }
    }

    /// <summary>
    /// for test api use only
    /// </summary>
    public class SecurityDetail2DTO : SecurityDetailDTO
    {
        public string assetClass { get; set; }
        public string ccy2 { get; set; }
        public enmQuoteType quoteType { get; set; }
        public string cname { get; set; }
    }
}