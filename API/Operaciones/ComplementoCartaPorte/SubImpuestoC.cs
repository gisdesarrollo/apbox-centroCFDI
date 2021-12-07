﻿using CFDI.API.Enums.CFDI33;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.Operaciones.ComplementoCartaPorte
{
    [Table("cp_SubImpuestoC")]
    public class SubImpuestoC
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public Decimal Base { get; set; }

        public c_Impuesto Impuesto { get; set; }

        [DisplayName("Tipo de Factor")]
        public c_TipoFactor TipoFactor { get; set; }

        [DisplayName("Tasa o Cuota")]
        public  Decimal TasaOCuota { get; set; }

        public Decimal Importe { get; set; }

        public Decimal TotalImpuestosTR { get; set; }
    }
}