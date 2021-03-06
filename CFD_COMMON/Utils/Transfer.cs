﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using CFD_COMMON.Models.Entities;

namespace CFD_COMMON.Utils
{
    public class Transfer
    {
        public static string[] UserVisibleTypes = {"EFT", "WeCollect - CUP", "Bank Wire", "Transaction Fee", "Adyen - Skrill", "Bonus", "Focal - CUP" };

        private static readonly string[] DepositTypes = { "WeCollect - CUP", "Adyen - Skrill", "Focal - CUP" };

        public static Expression<Func<AyondoTransferHistory_Live, bool>> IsDeposit()
        {
            return
                o =>
                    Transfer.DepositTypes.Contains(o.TransferType) ||
                    (o.TransferType == "Bank Wire" && o.Amount > 0 /*&& o.TransferId.Length == 36*/);
        }

        public static Expression<Func<AyondoTransferHistory_Live, bool>> IsDeposit(long? ayLiveAccountId)
        {
            return
                o => o.TradingAccountId == ayLiveAccountId && 
                    (Transfer.DepositTypes.Contains(o.TransferType) ||
                    (o.TransferType == "Bank Wire" && o.Amount > 0 /*&& o.TransferId.Length == 36*/));
        }

        public static Expression<Func<AyondoTransferHistory_Live, bool>> IsDeposit(List<long?> ayLiveAccountIdList)
        {
            return
                o => ayLiveAccountIdList.Contains(o.TradingAccountId) &&
                    (Transfer.DepositTypes.Contains(o.TransferType) ||
                    (o.TransferType == "Bank Wire" && o.Amount > 0 /*&& o.TransferId.Length == 36*/));
        }

        public static Expression<Func<AyondoTransferHistory_Live, bool>> IsRemittance(long? ayLiveAccountId)
        {
            return
                o =>
                    o.TradingAccountId == ayLiveAccountId &&
                    o.TransferType == "EFT";
        }

        public static bool IsDepositData(AyondoTransferHistoryBase o)
        {
            return Transfer.DepositTypes.Contains(o.TransferType) ||
                    (o.TransferType == "Bank Wire" && o.Amount > 0 /*&& o.TransferId.Length == 36*/);
        }

        //public static bool IsDeposit(string transferType)
        //{
        //    var lower = transferType.ToLower();
        //    return lower == "wecollect - cup" || lower == "adyen - skrill" || lower== "focal - cup";
        //}

        public static Tuple<string, string> getTransDescriptionColor(string transType, decimal amount)
        {
            Tuple<string, string> result = new Tuple<string, string>(string.Empty, string.Empty);
            switch (transType.ToLower().Trim())
            {
                case "eft":
                    result = new Tuple<string, string>("出金", "#000000");
                    break;
                case "wecollect - cup":
                case "adyen - skrill":
                case "focal - cup":
                    result = new Tuple<string, string>("入金", "#1c8d13");
                    break;
                case "bank wire":
                    result = amount >= 0? new Tuple<string, string>("转账", "#1c8d13") : new Tuple<string, string>("出金受理", "#1c8d13");
                    break;
                case "transaction fee":
                    result = new Tuple<string, string>("手续费", "#000000");
                    break;
                case "trade result":
                    result = new Tuple<string, string>("交易", "#000000");
                    break;
                case "financing":
                    result = new Tuple<string, string>("隔夜费", "#000000");
                    break;
                case "dividend":
                    result = new Tuple<string, string>("分红", "#000000");
                    break;
                case "bonus":
                    result = new Tuple<string, string>("交易金入金", "#1c8d13");
                    break;
            }

            return result;
        }
    }
}
