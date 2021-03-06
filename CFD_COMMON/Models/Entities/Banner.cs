namespace CFD_COMMON.Models.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Banner")]
    public partial class Banner
    {
        //[DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        [StringLength(200)]
        public string Url { get; set; }

        [StringLength(200)]
        public string ImgUrl { get; set; }

        [StringLength(200)]
        public string Header { get; set; }

        [StringLength(int.MaxValue)]
        public string Body { get; set; }

        public int? IsTop { get; set; }

        public DateTime? TopAt { get; set; }

        [StringLength(200)]
        public string CreatedBy { get; set; }

        public DateTime? CreatedAt { get; set; }

        [StringLength(200)]
        public string ExpiredBy { get; set; }

        public DateTime? Expiration { get; set; }
    }
}
