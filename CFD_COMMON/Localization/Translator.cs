﻿namespace CFD_COMMON.Localization
{
    public class Translator
    {
        public static string Translate(TransKey transKey)
        {
            if (Translations.Values.ContainsKey(transKey))
                return Translations.Values[transKey];
            else
                return transKey.ToString();
        }

        public static string GetCName(string name)
        {
            var str = name.Replace(" CFD", string.Empty).Replace(" TradeHero", string.Empty).Replace(" Mini", string.Empty);

            if (str.StartsWith("China 50 "))
                return "新华富时A50";
            //return str.Replace("China 50 ","新华富时A50 ");
            if (str.StartsWith("Japan 225 "))
                return "日经225";
            //return str.Replace("Japan 225 ", "日经225 ");

            if (Translations.ProdCNames.ContainsKey(str))
                return Translations.ProdCNames[str];
            else
                return name;
        }

        public static string AyondoOrderRejectMessageTranslate(string ayondoText)
        {
            if (ayondoText == "Order Delete: Not Sufficient Funds")
                return "剩余资金不足";
            if (ayondoText == "Server detected error: Above maximum lotsize")
                return "高于最大下单金额";
            if (ayondoText == "Server detected error: Below minimum lotsize")
                return "低于最小下单金额";
            if (ayondoText == "Server detected error: Leverage is above the maximum for this product")
                return "杠杆高于最大限制";
            if (ayondoText == "Server detected error: Trading not permitted outside of market hours")
                return "闭市时间不能交易";

            if (ayondoText == "No such order: StopLoss level within minimum stop distance")
                return "止损价与当前价间距过小";
            if (ayondoText == "No such order: Invalid stop level")
                return "不正确的止损价";

            if (ayondoText == "No such order: Invalid profit level")
                return "不正确的止盈价";
            if (ayondoText == "Request Failed")
                return "操作未成功";

            CFDGlobal.LogWarning("Cannot find ayondo translate for: [" + ayondoText + "]");
            return ayondoText;
        }
    }
}