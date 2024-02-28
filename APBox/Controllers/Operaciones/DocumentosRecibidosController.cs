﻿using APBox.Context;
using APBox.Control;
using API.Catalogos;
using API.Enums;
using API.Models.DocumentosRecibidos;
using API.Models.Dto;
using API.Operaciones.OperacionesProveedores;
using Aplicacion.LogicaPrincipal.Correos;
using Aplicacion.LogicaPrincipal.DocumentosRecibidos;
using Aplicacion.LogicaPrincipal.Facturas;
using SW.Services.Authentication;
using SW.Services.Validate;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Utilerias.LogicaPrincipal;

namespace APBox.Controllers.Operaciones
{
    [SessionExpire]
    public class DocumentosRecibidosController : Controller
    {
        private readonly APBoxContext _db = new APBoxContext();
        private readonly ProcesaDocumentoRecibido _procesaDocumentoRecibido = new ProcesaDocumentoRecibido();
        private readonly Decodificar _decodifica = new Decodificar();
        private readonly EnviosEmails _envioEmail = new EnviosEmails();

        #region Consultas

        //Metodo de Validacion E-Mail para usuarios solicitantes
        [HttpPost]
        public ActionResult ValidadorEmail(DocumentosRecibidosDR documentoRecibidoDr)
        {
            string email = documentoRecibidoDr.VerificarEmail;
            var grupoid = ObtenerGrupo();
            var usuarioSolicitante = _db.Usuarios.Where(c => c.Email == email && c.GrupoId == grupoid).FirstOrDefault();

            if (usuarioSolicitante == null)
            {
                return Json(new { success = false, message = "El Email no fue encontrado, favor de verificar" });
            }
            else
            {
                var usuarioSolicitanteNombre = usuarioSolicitante.NombreCompleto;
                var usuarioSolicitanteDepartamento = usuarioSolicitante.Departamento.Nombre;

                TempData["AprobadorId"] = usuarioSolicitante.Id;
                TempData["DepartamentoId"] = usuarioSolicitante.Departamento.Id;

                return Json(new
                {
                    success = true,
                    UsuarioSolicitanteEmail = email,
                    UsuarioSolicitanteNombre = usuarioSolicitanteNombre,
                    UsuarioSolicitanteDepartamento = usuarioSolicitanteDepartamento
                });
            }
        }

        #endregion

        // GET: DocumentosRecibidos/Index
        public ActionResult Index()
        {
            ViewBag.Controller = "DocumentosRecibidos";
            ViewBag.Action = "Index";
            ViewBag.ActionES = "Índice";
            ViewBag.Button = "CargaDocumentoRecibido";
            ViewBag.NameHere = "Documentos Recibidos";
            //get usaurio

            var usuario = _db.Usuarios.Find(ObtenerUsuario());

            if (usuario.esProveedor)
            {
                ViewBag.isProveedor = "Proveedor";
            }
            else
            {
                ViewBag.isProveedor = "Usuario";
            }


            var documentosRecibidosModel = new DocumentosRecibidosModel();
            var fechaInicial = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0);
            var fechaFinal = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
            documentosRecibidosModel.FechaInicial = fechaInicial;
            documentosRecibidosModel.FechaFinal = fechaFinal;
            if (usuario.esProveedor)
            {
                documentosRecibidosModel.DocumentosRecibidos = _procesaDocumentoRecibido.Filtrar(fechaInicial, fechaFinal, usuario.Id, (int)usuario.SocioComercialID);
            }
            else
            {
                documentosRecibidosModel.DocumentosRecibidos = _procesaDocumentoRecibido.Filtrar(fechaInicial, fechaFinal, usuario.Id, null);
            }
            return View(documentosRecibidosModel);
        }

        // POST: DocumentosRecibidos/Index
        [HttpPost]
        public ActionResult Index(DocumentosRecibidosModel documentosRecibidosModel)
        {
            ViewBag.Controller = "DocumentosRecibidos";
            ViewBag.Action = "Index";
            ViewBag.ActionES = "Index";
            ViewBag.Button = "CargaDocumentoRecibido";
            ViewBag.NameHere = "Documentos Recibidos";
            //get usaurio
            var usuario = _db.Usuarios.Find(ObtenerUsuario());
            if (usuario.esProveedor)
            {
                ViewBag.isProveedor = "Proveedor";
            }
            else
            {
                ViewBag.isProveedor = "Usuario";
            }
            DateTime fechaI = documentosRecibidosModel.FechaInicial;
            DateTime fechaF = documentosRecibidosModel.FechaFinal;

            var fechaInicial = new DateTime(fechaI.Year, fechaI.Month, fechaI.Day, 0, 0, 0);
            var fechaFinal = new DateTime(fechaF.Year, fechaF.Month, fechaF.Day, 23, 59, 59);
            if (usuario.esProveedor)
            {
                documentosRecibidosModel.DocumentosRecibidos = _procesaDocumentoRecibido.Filtrar(fechaInicial, fechaFinal, usuario.Id, (int)usuario.SocioComercialID);
            }
            else
            {
                documentosRecibidosModel.DocumentosRecibidos = _procesaDocumentoRecibido.Filtrar(fechaInicial, fechaFinal, usuario.Id, null);
            }

            return View(documentosRecibidosModel);
        }

        // GET: DocumentosRecibidos/CargaCfdi
        public ActionResult CargaCfdi()
        {
            ViewBag.Controller = "DocumentosRecibidos";
            ViewBag.Action = "CargaCfdi";
            ViewBag.NameHere = "Carga de CFDI";
            ViewBag.ActionES = "CargaCfdi";

            //get usaurio
            var usuario = _db.Usuarios.Find(ObtenerUsuario());
            if (usuario.esProveedor)
            {
                ViewBag.isProveedor = "Proveedor";
            }
            else
            {
                ViewBag.isProveedor = "Usuario";
            }
            DocumentosRecibidosDR documentoRecibidoDr = new DocumentosRecibidosDR()
            {
                Validaciones = new ValidacionesDR()
            };

            documentoRecibidoDr.Procesado = false;

            return View(documentoRecibidoDr);
        }

        // POST: DocumentosRecibidos/CargaCfdi
        [HttpPost]
        public ActionResult CargaCfdi(DocumentosRecibidosDR documentoRecibidoDr)
        {
            ViewBag.Controller = "DocumentosRecibidos";
            ViewBag.Action = "CargaCfdi";
            ViewBag.NameHere = "Documentos Recibidos";
            //ViewBag.ActionES = "Index";
            //get usaurio
            var usuario = _db.Usuarios.Find(ObtenerUsuario());
            if (usuario.esProveedor)
            {
                ViewBag.isProveedor = "Proveedor";
            }
            else
            {
                ViewBag.isProveedor = "Usuario";
            }
            PathArchivosDto archivo;
            ValidateXmlResponse responseValidacion = new ValidateXmlResponse();
            AuthResponse responseAutenticacion = new AuthResponse();
            ComprobanteCFDI cfdi = new ComprobanteCFDI();
            documentoRecibidoDr.DetalleArrays = new List<String>();

            try
            {
                archivo = SubeArchivo();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", String.Format("No se pudo cargar el archivo: {0}", ex.Message));
                return View(documentoRecibidoDr);
            }
            try
            {
                //Deserealiza XML
                cfdi = _procesaDocumentoRecibido.DecodificaXML(archivo.PathDestinoXml);
                var timbreFiscalDigital = _decodifica.DecodificarTimbre(cfdi, null);
                var sucursal = _db.Sucursales.Find(ObtenerSucursal());
                var socioComercial = new SocioComercial();

                documentoRecibidoDr.Validaciones = new ValidacionesDR();
                if (usuario.esProveedor)
                {
                    socioComercial = _db.SociosComerciales.Where(s => s.Rfc == cfdi.Emisor.Rfc && s.SucursalId == sucursal.Id).FirstOrDefault();
                    var existUUID = _db.DocumentoRecibidoDr.Where(dr => dr.CfdiRecibidos_UUID == timbreFiscalDigital.UUID).FirstOrDefault();
                    if (usuario.SocioComercial.Rfc != cfdi.Emisor.Rfc)
                    {
                        throw new Exception("Error Validación : El archivo cargado no coincide con el Rfc emisor al socio comercial");

                    }
                    if (sucursal.Rfc != cfdi.Receptor.Rfc)
                    {
                        throw new Exception("Error Validación : El archivo cargado no coincide con el Rfc receptor ala empresa asignada");
                    }
                    if (existUUID != null)
                    {
                        throw new Exception("Error Validación : El archivo ya se encuentra cargado en el sistema");
                    }
                }
                else
                {
                    if (cfdi.Receptor.Rfc == "XEXX010101000" || cfdi.Receptor.Rfc == "XAXX010101000")
                    {
                        socioComercial = _db.SociosComerciales.Where(s => s.Rfc == cfdi.Receptor.Rfc && s.RazonSocial == cfdi.Receptor.Nombre && s.SucursalId == sucursal.Id).FirstOrDefault();
                    }
                    else
                    {
                        socioComercial = _db.SociosComerciales.Where(s => s.Rfc == cfdi.Receptor.Rfc && s.SucursalId == sucursal.Id).FirstOrDefault();
                    }

                }

                if (socioComercial == null)
                {
                    throw new Exception("Error Validación : El socio comercial no esta registrado en la BD");
                }

                var configuracionEmpresa = ConfiguracionEmpresa();
                //Parametro configuracion (Documentos Obligatorios)
                if (configuracionEmpresa == null)
                {
                    throw new Exception("Error Configuracion : No se a configuraro la validación de la empresa");
                }

                if (configuracionEmpresa.ValidacionDocumentosObligatoria)
                {
                    //autenticacion
                    responseAutenticacion = _procesaDocumentoRecibido.GetToken();

                    if (responseAutenticacion.data.token != null)
                    {
                        //Valida CFDI
                        responseValidacion = _procesaDocumentoRecibido.ValidaCfdi(responseAutenticacion.data.token, archivo.PathDestinoXml);
                        if (responseValidacion == null) { throw new Exception("Error response validación CFDI : null"); }
                        if (responseValidacion.status == "success")
                        {
                            documentoRecibidoDr.EstadoComercial = c_EstadoComercial.EnRevision;
                            documentoRecibidoDr.Procesado = true;
                            //Para iterar la lista sobre la validacion estructura
                            List<Detail> detail1 = responseValidacion.detail;
                            StringBuilder sb = new StringBuilder();
                            var count = detail1.Count();
                            var limite = 0;

                            foreach (var detalle in detail1)
                            {
                                limite++;
                                var limiteDetail = 0;
                                foreach (var nodedetalle in detalle.detail)
                                {
                                    limiteDetail++;
                                    sb.AppendLine(limite + "." + limiteDetail + " " + detalle.section + ":" + nodedetalle.message + ":" + nodedetalle.messageDetail);
                                    //add validaciones
                                    if (limite < count)
                                    {
                                        documentoRecibidoDr.DetalleArrays.Add(limite + "." + limiteDetail + " " + detalle.section + ":" + nodedetalle.message + ":" + nodedetalle.messageDetail + "\r\n");
                                    }
                                    else { documentoRecibidoDr.DetalleArrays.Add(limite + "." + limiteDetail + " " + detalle.section + ":" + nodedetalle.message + ":" + nodedetalle.messageDetail); }

                                }

                            }
                            documentoRecibidoDr.Validaciones.Detalle = sb.ToString();
                            documentoRecibidoDr.Validaciones_Detalle = sb.ToString();
                            documentoRecibidoDr.Validaciones.Fecha = DateTime.Now;
                        }
                    }
                }
                else
                {
                    documentoRecibidoDr.EstadoComercial = c_EstadoComercial.Aprobado;
                    documentoRecibidoDr.Procesado = true;
                    documentoRecibidoDr.Validaciones.Fecha = DateTime.Now;
                }


                //add socio comercial
                documentoRecibidoDr.SocioComercial_Id = socioComercial.Id;
                //add usuario
                documentoRecibidoDr.Usuario_Id = usuario.Id;
                documentoRecibidoDr.CfdiRecibidos_Serie = cfdi.Serie;
                documentoRecibidoDr.CfdiRecibidos_Folio = cfdi.Folio;
                documentoRecibidoDr.Moneda_Id = cfdi.Moneda;
                ViewBag.MetodoPago = cfdi.MetodoPago;
                ViewBag.FormaPago = cfdi.FormaPago;
                ViewBag.TipoComprobante = cfdi.TipoDeComprobante.ToString();
                ViewBag.TipoCambio = cfdi.TipoCambio;
                ViewBag.Moneda = cfdi.Moneda;
                ViewBag.UsoCFDI = cfdi.Receptor.UsoCFDI;
                ViewBag.Usuario = usuario.NombreCompleto;
                ViewBag.Emisor = cfdi.Emisor.Nombre;
                ViewBag.Receptor = cfdi.Receptor.Nombre;

                //Implemento validacion para recibir facturas dentro del mes en curso
                DateTime fechaActual = DateTime.Now;

                var facturaMesCorriente = ConfiguracionEmpresa().RecibirFacturasMesCorriente;
                documentoRecibidoDr.FechaComprobante = Convert.ToDateTime(cfdi.Fecha);

                if (facturaMesCorriente)
                {

                    //Primer dia del Mes Actual
                    DateTime primerDiaMesActual = new DateTime(fechaActual.Year, fechaActual.Month, 1);

                    //Ultimo dia del Mes Actual
                    DateTime ultimoDiaMesActual = primerDiaMesActual.AddMonths(1).AddDays(-1);

                    if (documentoRecibidoDr.FechaComprobante < primerDiaMesActual || documentoRecibidoDr.FechaComprobante > ultimoDiaMesActual)
                    {
                        throw new InvalidOperationException("La factura o el comprobante no corresponde al mes actual. Por favor, cargue una factura del mes actual.");
                    }

                }

                documentoRecibidoDr.CfdiRecibidos_UUID = timbreFiscalDigital.UUID;
                documentoRecibidoDr.FechaEntrega = DateTime.Now;
                documentoRecibidoDr.TipoDocumentoRecibido = c_TipoDocumentoRecibido.CFDI;
                documentoRecibidoDr.Monto = cfdi.Total;
                //add data validacion correcta

                documentoRecibidoDr.PathArchivoXml = archivo.PathDestinoXml;
                documentoRecibidoDr.PathArchivoPdf = archivo.PathDestinoPdf;

            }
            catch (Exception ex)
            {
                var errores = ex.Message.Split('|');
                foreach (var error in errores)
                {
                    ModelState.AddModelError("", error);
                }
                //delete Files
                if (archivo.PathDestinoXml != null)
                {
                    System.IO.File.Delete(archivo.PathDestinoXml);
                }
                if (archivo.PathDestinoPdf != null)
                {
                    System.IO.File.Delete(archivo.PathDestinoPdf);
                }
                documentoRecibidoDr.Procesado = false;
                documentoRecibidoDr.DetalleArrays = null;
            }

            return View(documentoRecibidoDr);
        }

        // GET: DocumentosRecibidos/Create
        public ActionResult Create()
        {
            ViewBag.NameHere = "Crear";
            //get usaurio
            var usuario = _db.Usuarios.Find(ObtenerUsuario());
            if (usuario.esProveedor)
            {
                ViewBag.isProveedor = "Proveedor";
            }
            else
            {
                ViewBag.isProveedor = "Usuario";
            }
            return View();
        }

        // POST: DocumentosRecibidos/Create
        [HttpPost]
        public ActionResult Create(DocumentosRecibidosDR documentoRecibidoDr)
        {
            //get usaurio
            var usuario = _db.Usuarios.Find(ObtenerUsuario());
            ComprobanteCFDI cfdi = new ComprobanteCFDI();
            ViewBag.NameHere = "proveedor";
            if (usuario.esProveedor)
            {
                ViewBag.isProveedor = "Proveedor";
            }
            else
            {
                ViewBag.isProveedor = "Usuario";
            }
            try
            {

                // Obtener los datos guardados en TempData
                var aprobadorId = TempData["AprobadorId"] as int?;
                var departamentoId = TempData["DepartamentoId"] as int?;

                if (aprobadorId != null && departamentoId != null)
                {
                    documentoRecibidoDr.Aprobador_Id = aprobadorId.Value;
                    documentoRecibidoDr.Departamento_Id = departamentoId.Value;
                }

                //
                cfdi = _procesaDocumentoRecibido.DecodificaXML(documentoRecibidoDr.PathArchivoXml);
                var sucursal = _db.Sucursales.Where(s => s.Rfc == cfdi.Emisor.Rfc).FirstOrDefault();

                var timbreFiscalDigital = _decodifica.DecodificarTimbre(cfdi, null);

                documentoRecibidoDr.RecibidosXml = new RecibidosXMLDR();
                documentoRecibidoDr.RecibidosPdf = new RecibidosPDFDR();
                //insert files
                byte[] xmlFile = System.IO.File.ReadAllBytes(documentoRecibidoDr.PathArchivoXml);
                documentoRecibidoDr.RecibidosXml.Archivo = xmlFile;
                if (documentoRecibidoDr.PathArchivoPdf != null)
                {
                    byte[] pdfFile = System.IO.File.ReadAllBytes(documentoRecibidoDr.PathArchivoPdf);
                    documentoRecibidoDr.RecibidosPdf.Archivo = pdfFile;
                }
                else { documentoRecibidoDr.RecibidosPdf = null; documentoRecibidoDr.CfdiRecibidos_PDF_Id = null; }
                //table validaciones
                if (documentoRecibidoDr.Validaciones == null)
                {
                    documentoRecibidoDr.Validaciones = null; documentoRecibidoDr.Validaciones_Id = null;
                }
                // table recibidosComprobante
                documentoRecibidoDr.RecibidosComprobante = new RecibidosComprobanteDR()
                {
                    Sucursal_Id = sucursal.Id,
                    SocioComercial_Id = (int)documentoRecibidoDr.SocioComercial_Id,
                    Fecha = documentoRecibidoDr.FechaComprobante,
                    Serie = cfdi.Serie,
                    Folio = cfdi.Folio,
                    TipoComprobante = cfdi.TipoDeComprobante,
                    Version = cfdi.Version,
                    FormaPago = cfdi.FormaPago,
                    Moneda = cfdi.Moneda,
                    TipoCambio = (double)cfdi.TipoCambio,
                    LugarExpedicion = cfdi.LugarExpedicion,
                    MetodoPago = cfdi.MetodoPago,
                    Descuento = (double)cfdi.Descuento,
                    Subtotal = (double)cfdi.SubTotal,
                    Total = (double)cfdi.Total,
                    TotalImpuestosTrasladados = (double)cfdi.Impuestos.TotalImpuestosTrasladados,
                    TotalImpuestosRetenidos = (double)cfdi.Impuestos.TotalImpuestosTrasladados
                };
                documentoRecibidoDr.CfdiRecibidos_Id = null;
                // table solicitudes
                documentoRecibidoDr.Solicitudes = null;
                documentoRecibidoDr.Solicitud_Id = null;
                documentoRecibidoDr.EstadoComercial = c_EstadoComercial.EnRevision;
                documentoRecibidoDr.EstadoPago = c_EstadoPago.EnRevision;
                documentoRecibidoDr.Pagos = null;
                documentoRecibidoDr.Pagos_Id = null;
                documentoRecibidoDr.Referencia = documentoRecibidoDr.Referencia;
                _db.DocumentoRecibidoDr.Add(documentoRecibidoDr);
                _db.SaveChanges();
                return RedirectToAction("Index", "DocumentosRecibidos"); ;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return View(documentoRecibidoDr);

        }

        // GET: DocumentosRecibidos/Edit/5
        public ActionResult Revision(int id)
        {
            ViewBag.Controller = "DocumentosRecibidos";
            ViewBag.Action = "Edit";
            ViewBag.ActionES = "Editar";
            ViewBag.NameHere = "Revisión de Comprobante Recibido";

            var documentoRecibido = _db.DocumentoRecibidoDr.Find(id);
            var usuario = _db.Usuarios.Find(ObtenerUsuario());
            if (usuario.esProveedor)
            {
                documentoRecibido.isProveedor = true;
                ViewBag.isProveedor = "Proveedor";
            }
            else
            {
                documentoRecibido.isProveedor = false;
                ViewBag.isProveedor = "Usuario";
            }
            // Splitting the string into lines
            string[] lines = documentoRecibido.Validaciones_Detalle.Split('\n');
            documentoRecibido.DetalleArrays = lines.ToList();
            TempData["AprobadorId"] = null;
            TempData["DepartamentoId"] = null;

            return View(documentoRecibido);
        }

        // POST: DocumentosRecibidos/Edit/5
        [HttpPost]
        public ActionResult Revision(DocumentosRecibidosDR documentoRecibidoEdit)
        {
            try
            {
                var usuario = _db.Usuarios.Find(ObtenerUsuario());
                var documentoRecibido = _db.DocumentoRecibidoDr.Find(documentoRecibidoEdit.Id);
                if (usuario.esProveedor)
                {
                    // Obtener los datos guardados en TempData
                    var aprobadorId = TempData["AprobadorId"] as int?;
                    var departamentoId = TempData["DepartamentoId"] as int?;
                    if (aprobadorId != null && departamentoId != null)
                    {
                        documentoRecibido.Aprobador_Id = aprobadorId.Value;
                        documentoRecibido.Departamento_Id = departamentoId.Value;
                        _db.Entry(documentoRecibido).State = EntityState.Modified;
                        _db.SaveChanges();
                    }
                }
                else
                {
                    if (documentoRecibidoEdit.EstadoComercial == c_EstadoComercial.Rechazado)
                    {
                        documentoRecibido.MotivoRechazo = documentoRecibidoEdit.MotivoRechazo;
                    }
                    documentoRecibido.Solicitudes = null;
                    documentoRecibido.Solicitud_Id = null;
                    documentoRecibido.EstadoComercial = documentoRecibidoEdit.EstadoComercial;
                    _db.Entry(documentoRecibido).State = EntityState.Modified;
                    _db.SaveChanges();

                    //envio email rechazo
                    if (documentoRecibidoEdit.EstadoComercial == c_EstadoComercial.Rechazado)
                    {
                        _envioEmail.SendEmailNotifications(null, documentoRecibido, true, (int)ObtenerSucursal());
                    }
                }
                return RedirectToAction("Index", "DocumentosRecibidos");
            }
            catch
            {
                return View(documentoRecibidoEdit);
            }
        }

        // GET: DocumentosRecibidos/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: DocumentosRecibidos/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        #region Validaciones

        public ActionResult AprobarEstadoComercial(int id)
        {
            // Obtener el documento recibido con el ID proporcionado 
            // y asignarlo a la variable tuObjeto
            var tuObjeto = _db.DocumentoRecibidoDr.Find(id);

            // Verificar si el objeto no es nulo
            if (tuObjeto != null)
            {
                // Cambiar la propiedad EstadoComercial a aprobado
                tuObjeto.EstadoComercial = c_EstadoComercial.Aprobado;

                // Guardar los cambios 
                _db.SaveChanges();

                // Redirigir al método Index
                return RedirectToAction("Index");
            }

            // Manejar el caso en que el objeto no se encuentre
            return HttpNotFound();
        }

        public ActionResult RechazarEstadoComercial(int id)
        {
            // Obtener el documento recibido con el ID proporcionado 
            // y asignarlo a la variable tuObjeto
            var tuObjeto = _db.DocumentoRecibidoDr.Find(id);

            // Verificar si el objeto no es nulo
            if (tuObjeto != null)
            {
                // Cambiar la propiedad EstadoComercial a aprobado
                tuObjeto.EstadoComercial = c_EstadoComercial.Rechazado;

                // Guardar los cambios 
                _db.SaveChanges();

                // Redirigir al método Index
                return RedirectToAction("Index");
            }

            // Manejar el caso en que el objeto no se encuentre
            return HttpNotFound();
        }

        public static string FechaFormat(string fecha)
        {
            DateTime fechaConvertDate = Convert.ToDateTime(fecha);
            string fechaFormat = fechaConvertDate.ToString("dd/MM/yyyy");
            return fechaFormat;
        }
        public ConfiguracionesDR ConfiguracionEmpresa()
        {
            var sucursalId = _db.Sucursales.Find(ObtenerSucursal());
            var configuracion = _db.config.FirstOrDefault(c => c.Sucursal_Id == sucursalId.Id);


            if (configuracion == null)
            {
                return null;
            }

            return configuracion;
        }
        private void PopulaEstadoComercial()
        {
            List<SelectListItem> items = new List<SelectListItem>();
            items.Add(new SelectListItem { Text = "En Revision", Value = "0", Selected = true });
            items.Add(new SelectListItem { Text = "Aprobado", Value = "1" });
            items.Add(new SelectListItem { Text = "Rechazado", Value = "2" });
            ViewBag.estadoComercial = items;
        }
        private int ObtenerGrupo()
        {

            return Convert.ToInt32(Session["GrupoId"]);
        }
        private int ObtenerSucursal()
        {
            return Convert.ToInt32(Session["SucursalId"]);
        }
        private int ObtenerUsuario()
        {
            return Convert.ToInt32(Session["UsuarioId"]);
        }
        private PathArchivosDto SubeArchivo()
        {
            PathArchivosDto pathArchivos = new PathArchivosDto();
            if (Request.Files.Count > 0)
            {
                var operacionesStreams = new OperacionesStreams();
                for (var r = 0; r < Request.Files.Count; r++)
                {
                    var archivo = Request.Files[r];
                    var extencion = Path.GetExtension(archivo.FileName);
                    if (extencion == ".xml" || extencion == ".XML")
                    {
                        var pathDestinoXml = Path.Combine(Server.MapPath("~/Archivos/DocumentosProveedores/"), archivo.FileName);
                        Stream fileStream = archivo.InputStream;
                        var mStreamer = new MemoryStream();
                        mStreamer.SetLength(fileStream.Length);
                        fileStream.Read(mStreamer.GetBuffer(), 0, (int)fileStream.Length);
                        mStreamer.Seek(0, SeekOrigin.Begin);
                        operacionesStreams.StreamArchivo(mStreamer, pathDestinoXml);

                        pathArchivos.PathDestinoXml = pathDestinoXml;
                    }
                    if (extencion == ".pdf" || extencion == ".PDF")
                    {
                        var pathDestinoPdf = Path.Combine(Server.MapPath("~/Archivos/DocumentosProveedores/"), archivo.FileName);
                        Stream fileStream = archivo.InputStream;
                        var mStreamer = new MemoryStream();
                        mStreamer.SetLength(fileStream.Length);
                        fileStream.Read(mStreamer.GetBuffer(), 0, (int)fileStream.Length);
                        mStreamer.Seek(0, SeekOrigin.Begin);
                        operacionesStreams.StreamArchivo(mStreamer, pathDestinoPdf);

                        pathArchivos.PathDestinoPdf = pathDestinoPdf;
                    }

                }

                return pathArchivos;
            }
            throw new Exception("Favor de cargar por lo menos un archivo");
        }
        public ActionResult DescargaXml(int id)
        {
            byte[] archivoFisico = new byte[255];
            var documentoRecibido = _db.DocumentoRecibidoDr.Find(id);
            archivoFisico = documentoRecibido.RecibidosXml.Archivo;


            MemoryStream ms = new MemoryStream(archivoFisico, 0, 0, true, true);
            string nameArchivo = documentoRecibido.CfdiRecibidos_Serie + "-" + documentoRecibido.CfdiRecibidos_Folio + "-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            Response.AddHeader("content-disposition", "attachment;filename= " + nameArchivo + ".xml");
            Response.Buffer = true;
            Response.Clear();
            Response.OutputStream.Write(ms.GetBuffer(), 0, ms.GetBuffer().Length);
            Response.OutputStream.Flush();
            Response.End();


            return new FileStreamResult(Response.OutputStream, "application/xml");
        }
        public ActionResult DescargaPdf(int id)
        {
            byte[] archivoFisico = new byte[255];
            var documentoRecibido = _db.DocumentoRecibidoDr.Find(id);
            archivoFisico = documentoRecibido.RecibidosPdf.Archivo;


            MemoryStream ms = new MemoryStream(archivoFisico, 0, 0, true, true);
            string nameArchivo = documentoRecibido.CfdiRecibidos_Serie + "-" + documentoRecibido.CfdiRecibidos_Folio + "-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            Response.AddHeader("content-disposition", "attachment;filename= " + nameArchivo + ".pdf");
            Response.Buffer = true;
            Response.Clear();
            Response.OutputStream.Write(ms.GetBuffer(), 0, ms.GetBuffer().Length);
            Response.OutputStream.Flush();
            Response.End();


            return new FileStreamResult(Response.OutputStream, "application/pdf");
        }
        public ActionResult DescargaAdjunto(int id)
        {
            var documentoRecibido = _db.DocumentoRecibidoDr.Find(id);
            byte[] archivoFisicoXml = documentoRecibido.RecibidosXml.Archivo;
            byte[] archivoFisicoPdf = documentoRecibido.RecibidosPdf.Archivo;
            string nameArchivo = documentoRecibido.CfdiRecibidos_Serie + "-" + documentoRecibido.CfdiRecibidos_Folio + "-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

            var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                AddFileToZip(archive, archivoFisicoXml, nameArchivo + ".xml");
                AddFileToZip(archive, archivoFisicoPdf, nameArchivo + ".pdf");
            }

            memoryStream.Seek(0, SeekOrigin.Begin);

            var fileStreamResult = new FileStreamResult(memoryStream, "application/zip")
            {
                FileDownloadName = "adjuntos.zip"
            };

            return fileStreamResult;


        }
        private void AddFileToZip(ZipArchive archive, byte[] fileBytes, string entryName)
        {
            var entry = archive.CreateEntry(entryName);
            using (var entryStream = entry.Open())
            {
                entryStream.Write(fileBytes, 0, fileBytes.Length);
            }
        }
        #endregion
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
