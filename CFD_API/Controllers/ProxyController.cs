﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using CFD_COMMON;
using Newtonsoft.Json.Linq;

namespace CFD_API.Controllers
{
    [RoutePrefix("api/proxy")]
    public class ProxyController : CFDController
    {
        [HttpGet]
        [Route("check-username")]
        public JObject CheckUsername(string AccountType, string UserName)
        {
            var httpWebRequest = WebRequest.CreateHttp(CFDGlobal.AMS_HOST + "check-username?AccountType=" + AccountType + "&UserName=" + UserName);
            httpWebRequest.Headers["Authorization"] = CFDGlobal.AMS_HEADER_AUTH;
            httpWebRequest.Proxy = null;

            var dtBegin = DateTime.UtcNow;

            var webResponse = httpWebRequest.GetResponse();
            var responseStream = webResponse.GetResponseStream();
            var sr = new StreamReader(responseStream);

            var str = sr.ReadToEnd();
            var ts = DateTime.UtcNow - dtBegin;
            CFDGlobal.LogInformation("AMS check-username called. Time: " + ts.TotalMilliseconds + "ms Url: " + httpWebRequest.RequestUri + " Response: " + str);

            var jObject = JObject.Parse(str);
            return jObject;
        }

        [HttpPost]
        [Route("DemoAccount")]
        public JObject DemoAccount(string username, string password)
        {
            var httpWebRequest = WebRequest.CreateHttp(CFDGlobal.AMS_HOST + "DemoAccount");
            httpWebRequest.Headers["Authorization"] = CFDGlobal.AMS_HEADER_AUTH;
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/json; charset=UTF-8";
            httpWebRequest.Proxy = null;
            var requestStream = httpWebRequest.GetRequestStream();
            var sw = new StreamWriter(requestStream);

            //Escape the "{", "}" (by duplicating them) in the format string:
            var json =
                @"{{
'AddressCity': 'TestCity',
'AddressCountry': 'CN',
'AddressLine1': 'Teststr. 123',
'AddressLine2': null,
'AddressZip': '12345',
'ClientIP': '127.0.0.1',
'Currency': 'USD',
'FirstName': 'User',
'Gender': 'Male',
'IsTestRecord': true,
'Language': 'EN',
'LastName': 'THCN',
'Password': '{1}',
'PhonePrimary': '0044 123445',
'SalesRepGuid':null,
'UserName': '{0}',
'ProductType': 'CFD'
}}";

            var s = string.Format(json, username, password);
            sw.Write(s);
            sw.Flush();
            sw.Close();

            var dtBegin = DateTime.UtcNow;

            var webResponse = httpWebRequest.GetResponse();
            var responseStream = webResponse.GetResponseStream();
            var sr = new StreamReader(responseStream);

            var str = sr.ReadToEnd();
            var ts = DateTime.UtcNow - dtBegin;
            CFDGlobal.LogInformation("AMS demo called. Time: " + ts.TotalMilliseconds + "ms Url: " +
                                     httpWebRequest.RequestUri + " Response: " + str + "Request:" + s);

            var jObject = JObject.Parse(str);
            return jObject;
        }
    }
}
