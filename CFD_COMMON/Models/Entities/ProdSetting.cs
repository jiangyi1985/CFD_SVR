﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CFD_COMMON.Models.Entities
{
    [Table("ProdSetting_Live")]
    public class ProdSetting_Live
    {
        [Key]
        public int ProdID { get; set; }
        public decimal? MinInvestUSD { get; set; }
        public int? PriceDownInterval { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }
    }
}
