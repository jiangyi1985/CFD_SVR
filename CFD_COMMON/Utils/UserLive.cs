﻿using System;

namespace CFD_COMMON.Utils
{
    public enum UserLiveStatus
    {
        None = 0,
        Active = 1,
        Pending = 2,
        Rejected = 3,
    }

    public class UserLive
    {
        public static UserLiveStatus GetUserLiveAccountStatus(string ayLiveAccountStatus)
        {
            switch (ayLiveAccountStatus)
            {
                //pending
                case null:
                case "PendingMifid":
                case "PendingClassification":
                case "PendingDocuments":
                case "PendingIdentityConflict":
                case "PendingReview":
                case "PendingRiskAssessment":
                case "PendingUnlock":
                case "PendingUnlockRetry":
                    return UserLiveStatus.Pending;
                    break;

                //rejected
                case "AbortedByExpiry":
                case "AbortedByPolicy":
                case "RejectedDD":
                case "RejectedMifid":
                case "RejectedDuplicate":
                    return UserLiveStatus.Rejected;
                    break;

                //created
                case "Active":
                case "Closed":
                case "Locked":
                case "PendingFunding":
                case "PendingLogin":
                case "PendingTrading":
                    return UserLiveStatus.Active;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(ayLiveAccountStatus), ayLiveAccountStatus, null);
            }
        }

        public static UserLiveStatus GetUserLiveAccountStatus(string ayLiveUsername, string ayLiveAccountStatus)
        {
            if (string.IsNullOrWhiteSpace(ayLiveUsername))
                return UserLiveStatus.None;

            return GetUserLiveAccountStatus(ayLiveAccountStatus);
        }
    }
}