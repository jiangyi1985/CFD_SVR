﻿using System;

namespace CFD_API.DTO.Form
{
    public class SignupByPhoneFormDTO
    {
        public string phone { get; set; }
        public string verifyCode { get; set; }
    }

    public class SignupByWeChatFormDTO
    {
        public string openid { get; set; }
        public string unionid { get; set; }
        public string nickname { get; set; }
        public string headimgurl { get; set; }
    }

    public class LoginFormDTO
    {
        public int userId { get; set; }
        public string token { get; set; }
    }

    public class BindPhoneDTO
    {
        public string phone { get; set; }
        public string verifyCode { get; set; }
    }

    public class OperationPushDTO
    {
        public string phone { get; set; }
        public string message { get; set; }
        public string deepLink { get; set; }
    }

    public class RewardTransferSearchDTO
    {
        public string startTime;
        public string endTime;
    }

    public class RewardTransferDTO
    {
        public string liveAccount;
        public string liveAccountID;
        public string name;
        public decimal amount;
        public DateTime date;
    }
}
