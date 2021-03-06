﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using CFD_COMMON.Models.Context;
using CFD_COMMON.Models.Entities;

namespace CFD_COMMON.Service
{
    public class UserService
    {
        public CFDEntities db { get; set; }

        public UserService(CFDEntities db)
        {
            this.db = db;
        }

        public static string NewToken()
        {
            return Guid.NewGuid().ToString("N");
        }

        public void CreateUserByPhone(string phone)
        {
            //creating new user if phone doesn't exist in a new transaction
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions {IsolationLevel = IsolationLevel.Serializable}))
            {
                using (var dbIsolated = CFDEntities.Create())
                {
                    var userIsolated = dbIsolated.Users.FirstOrDefault(o => o.Phone == phone);
                    if (userIsolated == null)
                    {
                        userIsolated = new User
                        {
                            CreatedAt = DateTime.UtcNow,
                            Phone = phone,
                            Token = NewToken(),

                            AutoCloseAlert = true,
                            AutoCloseAlert_Live = true,
                        };
                        dbIsolated.Users.Add(userIsolated);

                        dbIsolated.SaveChanges();
                    }
                }
                scope.Complete();
            }
        }

        public void CreateUserByWeChat(string openid, string unionid)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions {IsolationLevel = IsolationLevel.Serializable}))
            {
                using (var dbIsolated = CFDEntities.Create())
                {
                    var userIsolated = dbIsolated.Users.FirstOrDefault(o => o.WeChatOpenId == openid);
                    if (userIsolated == null)
                    {
                        userIsolated = new User
                        {
                            CreatedAt = DateTime.UtcNow,
                            WeChatOpenId = openid,
                            WeChatUnionId = unionid,
                            Token = NewToken(),

                            AutoCloseAlert = true,
                            AutoCloseAlert_Live = true,
                        };
                        dbIsolated.Users.Add(userIsolated);

                        dbIsolated.SaveChanges();
                    }
                }
                scope.Complete();
            }
        }

        public void BindWechat(int userId, string wechatOpenId)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
            {
                using (var dbIsolated = CFDEntities.Create())
                {
                    var userIsolated = dbIsolated.Users.FirstOrDefault(o => o.Id == userId);
                    if (userIsolated != null && userIsolated.WeChatOpenId == null
                        && !dbIsolated.Users.Any(o => o.WeChatOpenId == wechatOpenId))
                    {
                        userIsolated.WeChatOpenId = wechatOpenId;
                        dbIsolated.SaveChanges();
                    }
                }
                scope.Complete();
            }
        }

        public void BindPhone(int userId, string phone)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
            {
                using (var dbIsolated = CFDEntities.Create())
                {
                    var userIsolated = dbIsolated.Users.FirstOrDefault(o => o.Id == userId);
                    if (userIsolated != null && userIsolated.Phone == null
                        && !dbIsolated.Users.Any(o => o.Phone == phone))
                    {
                        userIsolated.Phone = phone;
                        dbIsolated.SaveChanges();
                    }
                }
                scope.Complete();
            }
        }
    }
}