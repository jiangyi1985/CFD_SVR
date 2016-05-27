﻿using AutoMapper;
using CFD_API.DTO;
using CFD_COMMON.Localization;
using CFD_COMMON.Models.Cached;
using CFD_COMMON.Models.Entities;
using CFD_COMMON.Utils;

namespace CFD_API
{
    public class MapperConfig
    {
        public static MapperConfiguration GetAutoMapperConfiguration()
        {
            return new MapperConfiguration(cfg =>
            {
                //var r = new Random();

                cfg.CreateMap<User, UserDTO>();

                cfg.CreateMap<AyondoSecurity, SecurityDTO>()
                    //.ForMember(dest => dest.last, opt => opt.MapFrom(src => Quotes.GetLastPrice(src)))
                    ////tag
                    //.ForMember(dest => dest.tag, opt => opt.Condition(o => o.AssetClass == "Single Stocks"))
                    .ForMember(dest => dest.tag, opt => opt.MapFrom(src => src.Financing == "US Stocks" ? "US" : null))
                    ////name
                    ////                    .ForMember(dest => dest.name, opt => opt.MapFrom(src => src.CName ?? (r.Next(0, 2) == 0 ? "阿里巴巴" : "苹果")))
                    //.ForMember(dest => dest.name, opt => opt.MapFrom(src => src.CName ?? src.Name.TruncateMax(10)))
                    .ForMember(dest => dest.name, opt => opt.MapFrom(src => src.CName))
                    ////open
                    ////.ForMember(dest => dest.open, opt => opt.MapFrom(src => src.Ask*((decimal) r.Next(80, 121))/100))
                    ;
                cfg.CreateMap<AyondoSecurity, SecurityDetailDTO>()
                    .ForMember(dest => dest.tag, opt => opt.MapFrom(src => src.Financing == "US Stocks" ? "US" : null))
                    .ForMember(dest => dest.name, opt => opt.MapFrom(src => src.CName))
                    ;

                cfg.CreateMap<ProdDef, SecurityDTO>()
                    .ForMember(dest => dest.name, opt => opt.MapFrom(src => Translator.GetCName(src.Name)))
                    .ForMember(dest => dest.open, opt => opt.MapFrom(src => Quotes.GetOpenPrice(src)))
                    .ForMember(dest => dest.isOpen, opt => opt.MapFrom(src => src.QuoteType == enmQuoteType.Open))
                    .ForMember(dest => dest.tag, opt => opt.MapFrom(src => Products.IsUsStocks(src.Symbol) ? "US" : null));

                cfg.CreateMap<ProdDef, SecurityDetailDTO>()
                    .ForMember(dest => dest.last, opt => opt.MapFrom(src => Quotes.GetLastPrice(src)))
                    .ForMember(dest => dest.ask, opt => opt.MapFrom(src => src.Offer))
                    .ForMember(dest => dest.name, opt => opt.MapFrom(src => Translator.GetCName(src.Name)))
                    .ForMember(dest => dest.open, opt => opt.MapFrom(src => Quotes.GetOpenPrice(src)))
                    //.ForMember(dest => dest.preClose, opt => opt.MapFrom(src => src.CloseAsk))
                    .ForMember(dest => dest.isOpen, opt => opt.MapFrom(src => src.QuoteType == enmQuoteType.Open))
                    .ForMember(dest => dest.tag, opt => opt.MapFrom(src => Products.IsUsStocks(src.Symbol) ? "US" : null))
                    .ForMember(dest => dest.dcmCount, opt => opt.MapFrom(src => src.Prec));

                cfg.CreateMap<ProdDef, ProdDefDTO>();

                cfg.CreateMap<Tick, TickDTO>();


                cfg.CreateMap<Banner, BannerDTO>();
            });
        }
    }
}