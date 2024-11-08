using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupportPatriots.Model.Models
{
    [Table("Tenant", Schema = "dbo")]
    public class Tenant
    {
        [Key]
        public Guid TenantId { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }
        public Guid? ParentTenantId { get; set; }
        public bool IsSuperTenant { get; set; }

        public TenantSetting Setting { get; set; }

        [NotMapped]
        public bool IsParent => !ParentTenantId.HasValue;
        [NotMapped]
        public ICollection<Tenant> Tenants { get; set; }
    }
}
