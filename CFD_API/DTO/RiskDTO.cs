﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CFD_API.DTO
{
    public class UserRiskDTO
    {
        public UserRiskDTO()
        {

        }

        public int UserId { get; set; }

        public string NickName { get; set; }

        /// <summary>
        /// average leverage.
        /// </summary>
        public decimal Leverage { get; set; }

        /// <summary>
        /// average order frequency per hour.
        /// </summary>
        public decimal Frequency { get; set; }

        /// <summary>
        /// average hold time(second).
        /// </summary>
        public decimal HoldTime { get; set; }

        /// <summary>
        /// cv_invest std_invest/ave_invest.
        /// </summary>
        public decimal Invest { get; set; }

        private decimal correctIndex(decimal index)
        {
            if (index < 0)
            {
                return 0;
            }
            else if (index > 25)
            {
                return 25;
            }
            else
            {
                return index;
            }
        }

        public decimal LeverageIndex
        {
            get
            {
                decimal index = (this.Leverage - 10m) * 0.28m;
                return this.correctIndex(index);
            }
        }

        public decimal HoldTimeIndex
        {
            get
            {
                decimal index = (this.HoldTime - 9000m) * 0.000045m;
                return this.correctIndex(index);
            }
        }

        public decimal FrequencyIndex
        {
            get
            {
                decimal index = this.Frequency * 119m;
                return this.correctIndex(index);
            }
        }

        public decimal InvestIndex
        {
            get
            {
                decimal index = (this.Invest - 0.15m) * 27.5m;
                return this.correctIndex(index);
            }
        }

        public decimal Index
        {
            get
            {                
                return this.LeverageIndex + this.HoldTimeIndex + this.FrequencyIndex + this.InvestIndex;
            }
        }
    }
}