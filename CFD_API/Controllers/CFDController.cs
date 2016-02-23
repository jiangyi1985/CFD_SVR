﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using CFD_COMMON.Localization;
using CFD_COMMON.Models.Context;

namespace CFD_API.Controllers
{
    public class CFDController : ApiController
    {
        public CFDEntities db { get; protected set; }

        public CFDController(CFDEntities db)
        {
            this.db = db;
        }

        public string __(TransKeys transKey)
        {
            if (Translations.Values.ContainsKey(transKey))
                return Translations.Values[transKey];
            else
                return transKey.ToString();
        }

        //public CFDController()
        //{
        //    db=CFDEntities.Create();
        //}
    }
}