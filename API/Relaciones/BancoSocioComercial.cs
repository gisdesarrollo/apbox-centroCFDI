﻿using API.Catalogos;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Relaciones
{
    [Table("rel_bancossocioscomerciales")]
    public class BancoSocioComercial
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Campo Obligatorio")]
        public String Nombre { get; set; }

        [DisplayName("Banco")]
        [Required(ErrorMessage = "Campo Obligatorio")]
        public int BancoId { get; set; }
        [ForeignKey("BancoId")]
        public virtual Banco Banco { get; set; }

        [DisplayName("SocioComercial")]
        [Required(ErrorMessage = "Campo Obligatorio")]
        public int SocioComercialId { get; set; }
        [ForeignKey("SocioComercialId")]
        public virtual SocioComercial SocioComercial { get; set; }

        [Required(ErrorMessage = "Campo Obligatorio")]
        public String NumeroCuenta { get; set; }
    }
}
