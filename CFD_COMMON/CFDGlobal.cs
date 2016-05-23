﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace CFD_COMMON
{
    public class CFDGlobal
    {
        public static string USER_PIC_BLOB_CONTAINER="user-picture";
        public static string USER_PIC_BLOB_CONTAINER_URL = "https://cfdstorage.blob.core.chinacloudapi.cn/" + USER_PIC_BLOB_CONTAINER+"/";

        public static string DATETIME_MASK_MILLI_SECOND = "yyyy-MM-dd HH:mm:ss.fff";

        public static string ASSET_CLASS_STOCK = "Single Stocks";
        public static string ASSET_CLASS_FX = "Currencies";
        public static string ASSET_CLASS_INDEX = "Stock Indices";
        public static string ASSET_CLASS_COMMODITY = "Commodities";

        /// <summary>
        /// the default application-wide BasicRedisClientManager, non-pooled, created for current application
        /// </summary>
        public static IRedisClientsManager BasicRedisClientManager;

        static CFDGlobal()
        {
            JsConfig.TreatEnumAsInteger = true;
            //JsConfig.DateHandler = JsonDateHandler.ISO8601;

            //create default application-wide BasicRedisClientManager
            BasicRedisClientManager = GetNewBasicRedisClientManager();
        }

        /// <summary>
        /// get a new BasicRedisClientManager (non-pooled)
        /// </summary>
        /// <returns></returns>
        public static IRedisClientsManager GetNewBasicRedisClientManager()
        {
            var redisConStr = CFDGlobal.GetConfigurationSetting("redisConnectionString");
            return new BasicRedisClientManager(redisConStr);
        }

        public static string GetConfigurationSetting(string key)
        {
            if (RoleEnvironment.IsAvailable)
            {
                ////throw exception if not exist
                //return RoleEnvironment.GetConfigurationSettingValue(key);

                string value=null;
                try
                {
                    value = CloudConfigurationManager.GetSetting(key);
                }
                catch (Exception e)
                {
                }

                //if there's no cloud config, return local config
                return value ?? ConfigurationManager.AppSettings[key];
            }
            else
            {
                return ConfigurationManager.AppSettings[key];
            }
        }

        public static T RetryMaxOrThrow<T>(Func<T> p, int sleepMilliSeconds = 10000, int retryMax = 3, bool verboseErrorLog = true)
        {
            int retryCount = 0;
            int currentSleepMilliSeconds = sleepMilliSeconds;
            for (; ; )
            {
                try
                {
                    return p();
                }
                catch (Exception ex)
                {
                    //if (verboseErrorLog)
                    //    Trace.Write("RetryMaxOrThrow: {" + LogAllExceptionsAndStack(ex) + "}... ");

                    //// backoff exponentially when the DB is under load
                    //if (IsDatabaseLoadRelated(ex))
                    //{
                    //    if (verboseErrorLog)
                    //        Trace.Write("DB load exception detected, will back off exponentially... stack: [" + Global.LogStack() + "] / " + Global.LogAllExceptionsAndStack(ex));
                    //    currentSleepSeconds = Math.Min(currentSleepSeconds * 2, 60 * 10); // cap at max 10 minute wait
                    //    retryMax = 5;
                    //}

                    if (++retryCount == retryMax)
                    {
                        //if (verboseErrorLog)
                        //{
                        //    Global.LogLine(
                        //       "### exceeded retry count; throwing: [(type)=" + ex.GetType().Name + ": " + ex.Message + "] " +
                        //       LogAllExceptionsAndStack(ex) +
                        //       " stack: [" + Global.LogStack() + "]");
                        //}

                        throw new System.ApplicationException("exceed retry count: " + ex.Message, ex);
                    }

                    //if (verboseErrorLog)
                    //    Trace.WriteLine("sleep " + currentSleepSeconds + "s...");

                    //#if !DEBUG
                    Thread.Sleep(currentSleepMilliSeconds);
                    //#endif
                }
            }
        }

        public static void LogError(string message)
        {
            Trace.TraceError(GetLogDatetimePrefix() + message);
        }
        public static void LogWarning(string message)
        {
            Trace.TraceWarning(GetLogDatetimePrefix() + message);
        }
        public static void LogInformation(string message)
        {
            Trace.TraceInformation(GetLogDatetimePrefix() + message);
        }

        public static void LogLine(string message)
        {
            Trace.WriteLine(GetLogDatetimePrefix() + message);
        }

        public static void LogException(Exception exception)
        {
            var ex = exception;
            while (ex!=null)
            {
                Trace.WriteLine(GetLogDatetimePrefix() + ex.Message);
                Trace.WriteLine(GetLogDatetimePrefix() + ex.StackTrace);

                ex = ex.InnerException;
            }
        }

        private static string GetLogDatetimePrefix()
        {
            return DateTime.Now.ToString(DATETIME_MASK_MILLI_SECOND)+" ";
        }
    }
}
