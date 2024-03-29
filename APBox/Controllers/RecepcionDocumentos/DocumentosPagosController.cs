﻿using APBox.Context;
using APBox.Control;
using API.Enums;
using API.Models.DocumentosRecibidos;
using API.Models.Dto;
using API.Operaciones.OperacionesProveedores;
using Aplicacion.LogicaPrincipal.Correos;
using Aplicacion.LogicaPrincipal.DocumentosRecibidos;
using Aplicacion.LogicaPrincipal.Facturas;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Utilerias.LogicaPrincipal;
using System.Data.Entity;
using System.Web;
using Aplicacion.LogicaPrincipal.CargasMasivas.CSV;
using API.Models.DocumentosPagos;
using Aplicacion.LogicaPrincipal.DocumentosPagos;
using Aplicacion.LogicaPrincipal.Validacion;

namespace APBox.Controllers.Operaciones
{
    [SessionExpire]
    public class DocumentosPagosController : Controller
    {
        private readonly APBoxContext _db = new APBoxContext();
        private readonly ProcesaDocumentoRecibido _procesaDocumentoRecibido = new ProcesaDocumentoRecibido();
        private readonly Decodificar _decodifica = new Decodificar();
        private readonly EnviosEmails _envioEmail = new EnviosEmails();
        private readonly CargaPagosDR _cargaPagosDR = new CargaPagosDR();
        private readonly ProcesaDocumentoPago _procesaDocumentoPago = new ProcesaDocumentoPago();
        private readonly ValidacionesPagos _validaPagos = new ValidacionesPagos();

        // GET: DocumentosPagos
        public ActionResult Index()
        {
            ViewBag.Controller = "DocumentosAprobados";
            ViewBag.Action = "Index";
            ViewBag.ActionES = "Índice";
            ViewBag.Button = "CargaDocumentoRecibido";
            ViewBag.NameHere = "Documentos Aprobados";
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

        // POST: DocumentosPagos filtrados por el rango de fecha
        [HttpPost]
        public ActionResult Index(DocumentosRecibidosModel documentosRecibidosModel)
        {
            ViewBag.Controller = "DocumentosAprobados";
            ViewBag.Action = "Index";
            ViewBag.ActionES = "Index";
            ViewBag.Button = "CargaDocumentoRecibido";
            ViewBag.NameHere = "Documentos Aprobados";
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

        // GET: DocumentosPagos/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DocumentosPagos/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: DocumentosPagos/Edit/5
        public ActionResult Revision(int id)
        {
            ViewBag.Controller = "DocumentosRecibidos";
            ViewBag.Action = "Edit";
            ViewBag.ActionES = "Editar";
            ViewBag.NameHere = "Revisión de Comprobante Recibido para Pago";

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

            return View(documentoRecibido);
        }

        // POST: DocumentosPagos/Edit/5
        [HttpPost]
        public ActionResult Revision(DocumentosRecibidosDR documentoRecibidoEdit)
        {
            try
            {
                var usuario = _db.Usuarios.Find(ObtenerUsuario());
                var documentoRecibido = _db.DocumentoRecibidoDr.Find(documentoRecibidoEdit.Id);
                var usuarioEntrega = _db.Usuarios.Find(documentoRecibido.AprobacionesDR.UsuarioEntrega_Id);
                var sucursal = _db.Sucursales.Find(ObtenerSucursal());

                documentoRecibido.EstadoComercial = documentoRecibidoEdit.EstadoComercial;
                documentoRecibido.EstadoPago = documentoRecibidoEdit.EstadoPago;
                documentoRecibido.AprobacionesDR.Observaciones = documentoRecibidoEdit.AprobacionesDR.Observaciones;

                if (documentoRecibidoEdit.EstadoComercial == c_EstadoComercial.EnRevision && documentoRecibidoEdit.AprobacionesDR.Observaciones != null)
                {
                    var usuarioAprobador = _db.Usuarios.Find(documentoRecibido.AprobacionesDR.UsuarioAprobacionComercial_id);

                    documentoRecibido.AprobacionesDR.FechaAprobacionComercial = DateTime.Now;
                    documentoRecibido.AprobacionesDR.UsuarioAprobacionComercial_id = usuario.Id;
                    _envioEmail.NotificacionRevisionComercial(usuarioAprobador, documentoRecibido, sucursal.Id);
                }

                if (documentoRecibidoEdit.EstadoPago == c_EstadoPago.Aprobado)
                {
                    documentoRecibido.AprobacionesDR.FechaAprobacionComercial = DateTime.Now;
                    documentoRecibido.AprobacionesDR.UsuarioAprobacionComercial_id = usuario.Id;

                    documentoRecibido.EstadoComercial = c_EstadoComercial.Aprobado;
                }

                if (documentoRecibidoEdit.EstadoPago == c_EstadoPago.Rechazado)
                {
                    documentoRecibido.AprobacionesDR.FechaRechazo = DateTime.Now;
                    documentoRecibido.AprobacionesDR.UsuarioRechazo_id = usuario.Id;
                    documentoRecibido.AprobacionesDR.DetalleRechazo = documentoRecibidoEdit.AprobacionesDR.DetalleRechazo;

                    documentoRecibido.EstadoPago = c_EstadoPago.Rechazado;

                    //Notificación al usuario que entrega
                    _envioEmail.NotificacionCambioEstadoComercial(usuarioEntrega, documentoRecibido, c_EstadoComercial.Rechazado, (int)ObtenerSucursal());
                }

                _db.Entry(documentoRecibido).State = EntityState.Modified;
                _db.SaveChanges();

                return RedirectToAction("Index", "DocumentosPagos");
            }
            catch
            {
                return View(documentoRecibidoEdit);
            }
        }

        public ActionResult Exportar()
        {
            var pathCompleto = _cargaPagosDR.Exportar();
            byte[] filedata = System.IO.File.ReadAllBytes(pathCompleto);
            string contentType = MimeMapping.GetMimeMapping(pathCompleto);

            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = Path.GetFileName(pathCompleto),
                Inline = false,
            };
            Response.AppendHeader("Content-Disposition", cd.ToString());
            return File(filedata, contentType);
        }
            
        public ActionResult Pagos()
        {
            ViewBag.Controller = "DocumentosPagos";
            ViewBag.Action = "Pagos";
            ViewBag.ActionES = "Pagos";
            ViewBag.NameHere = "Pagos Procesados";

            var usuario = _db.Usuarios.Find(ObtenerUsuario());
            var sucursal = _db.Sucursales.Find(ObtenerSucursal());
            DocumentosPagosModel pagosModel = new DocumentosPagosModel();
            DateTime dayI = DateTime.Now.AddDays(-6);
            var fechaInicial = new DateTime(DateTime.Now.Year, DateTime.Now.Month, dayI.Day, 0, 0, 0);
            var fechaFinal = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
            pagosModel.FechaInicial = fechaInicial;
            pagosModel.FechaFinal = fechaFinal;
            pagosModel.Pagos = _procesaDocumentoPago.Filtrar(fechaInicial,fechaFinal,false , null,sucursal.Id);

            return View(pagosModel);
        }
        [HttpPost]
        public ActionResult Pagos(DocumentosPagosModel pagosModel)
        {
            ViewBag.Controller = "DocumentosPagos";
            ViewBag.Action = "Pagos";
            ViewBag.ActionES = "Pagos";
            ViewBag.NameHere = "Pagos Procesados";

            var usuario = _db.Usuarios.Find(ObtenerUsuario());
            var sucursal = _db.Sucursales.Find(ObtenerSucursal());
            DateTime fechaI = pagosModel.FechaInicial;
            DateTime fechaF = pagosModel.FechaFinal;

            var fechaInicial = new DateTime(fechaI.Year, fechaI.Month, fechaI.Day, 0, 0, 0);
            var fechaFinal = new DateTime(fechaF.Year, fechaF.Month, fechaF.Day, 23, 59, 59);
            pagosModel.FechaInicial = fechaInicial;
            pagosModel.FechaFinal = fechaFinal;
            pagosModel.Pagos = _procesaDocumentoPago.Filtrar(fechaInicial, fechaFinal, false,null,sucursal.Id);

            return View(pagosModel);
        }
        
        public ActionResult CargaLayout()
        {
            ViewBag.Controller = "DocumentosPagos";
            ViewBag.Action = "CargaLayout";
            ViewBag.ActionES = "Carga Layout";
            ViewBag.NameHere = "Carga Layout de Pagos";

            DocumentosPagosModel documentoPagoModel = new DocumentosPagosModel();
            documentoPagoModel.Previsualizacion = true;

            return View(documentoPagoModel);
        }

        [HttpPost]
        public ActionResult CargaLayout(DocumentosPagosModel documentoPagoModel)
        {
            ViewBag.Controller = "DocumentosPagos";
            ViewBag.Action = "CargaLayout";
            ViewBag.ActionES = "Carga Layout";
            ViewBag.NameHere = "Carga Layout de Pagos";

            String archivo;
            try
            {
                archivo = SubeArchivo(0);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", String.Format("No se pudo cargar el archivo: {0}", ex.Message));
                return View(documentoPagoModel);
            }

            try
            {
                var usuario = _db.Usuarios.Find(ObtenerUsuario());
              documentoPagoModel.Pagos= _cargaPagosDR.Importar(archivo,ObtenerSucursal(),documentoPagoModel.Previsualizacion,usuario.Id);
                if (!documentoPagoModel.Previsualizacion)
                {
                    return RedirectToAction("Pagos");
                }
            }catch(Exception ex)
            {
                ModelState.AddModelError("", String.Format("Ocurrio un error: {0}", ex.Message));
            }

            documentoPagoModel.Previsualizacion = false;
            
            return View(documentoPagoModel);
        }

        public ActionResult ComplementosPagosCargados()
        {
            ViewBag.Controller = "DocumentosPagos";
            ViewBag.Action = "ComplementosPagosCargados";
            ViewBag.ActionES = "Complementos Pagos Cargados";
            ViewBag.NameHere = "Complementos Pagos Cargados";

            var usuario = _db.Usuarios.Find(ObtenerUsuario());
            var sucursal = _db.Sucursales.Find(ObtenerSucursal());
            DocumentosPagosModel pagosModel = new DocumentosPagosModel();
            DateTime dayI = DateTime.Now.AddDays(-6);
            var fechaInicial = new DateTime(DateTime.Now.Year, DateTime.Now.Month, dayI.Day, 0, 0, 0);
            var fechaFinal = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
            pagosModel.FechaInicial = fechaInicial;
            pagosModel.FechaFinal = fechaFinal;
            pagosModel.Pagos = _procesaDocumentoPago.Filtrar(fechaInicial, fechaFinal, false, null,sucursal.Id);

            return View(pagosModel);
        }
        [HttpPost]
        public ActionResult ComplementosPagosCargados(DocumentosPagosModel pagosModel)
        {
            ViewBag.Controller = "DocumentosPagos";
            ViewBag.Action = "ComplementosPagosCargados";
            ViewBag.ActionES = "Complementos Pagos Cargados";
            ViewBag.NameHere = "Complementos Pagos Cargados";

            var usuario = _db.Usuarios.Find(ObtenerUsuario());
            var sucursal = _db.Sucursales.Find(ObtenerSucursal());
            var fechaI = pagosModel.FechaInicial;
            var fechaF = pagosModel.FechaFinal;
            var fechaInicial = new DateTime(fechaI.Year, fechaI.Month, fechaI.Day, 0, 0, 0);
            var fechaFinal = new DateTime(fechaF.Year, fechaF.Month, fechaF.Day, 23, 59, 59);
            pagosModel.FechaInicial = fechaInicial;
            pagosModel.FechaFinal = fechaFinal;
            pagosModel.Pagos = _procesaDocumentoPago.Filtrar(fechaInicial, fechaFinal, false, null,sucursal.Id);

            return View(pagosModel);
        }
        public ActionResult CargaComplementoPago(int Id)
        {
            ViewBag.Controller = "DocumentosPagos";
            ViewBag.Action = "CargaComplementoPago";
            ViewBag.ActionES = "Carga Complemento Pago";
            ViewBag.NameHere = "Carga complemento Pago";
            var pago = _db.PagoDr.Find(Id);
            pago.Procesado = false;

            return View(pago);
        }
        [HttpPost]
        public ActionResult CargaComplementoPago(PagosDR pagoDR)
        {
            ViewBag.Controller = "DocumentosPagos";
            ViewBag.Action = "CargaComplementoPago";
            ViewBag.ActionES = "Carga Complemento Pago";
            ViewBag.NameHere = "Carga complemento Pago";
            ComprobanteCFDI cfdi = new ComprobanteCFDI();
            ComplementoPagoDto pagoDto = new ComplementoPagoDto();
            String archivo;
            try
            {
                archivo = SubeArchivo(0);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", String.Format("No se pudo cargar el archivo: {0}", ex.Message));
                return View(pagoDR);
            }
            
            var pago = _db.PagoDr.Find(pagoDR.Id);
            try
            {
                cfdi = _procesaDocumentoRecibido.DecodificaXML(archivo);
                if (cfdi != null)
                {
                    pago.ComplementoPago = new ComplementoPagoDto();
                    pago.ComplementoPago.NombreEmisor = cfdi.Emisor.Nombre;
                    pago.ComplementoPago.NombreReceptor = cfdi.Receptor.Nombre;
                    if(cfdi.Pagos.Pago.Length > 1)
                    {
                        ModelState.AddModelError("", "Error: se encontraron mas de 1 pago realizado");
                    }
                    foreach (var pagoXml in cfdi.Pagos.Pago)
                    {
                        pago.ComplementoPago.FormaPago = pagoXml.FormaDePagoP;
                        pago.ComplementoPago.TipoComprobante = cfdi.TipoDeComprobante;
                        pago.ComplementoPago.TipoCambio = pagoXml.TipoCambioP;
                        pago.ComplementoPago.Moneda = pagoXml.MonedaP;
                        pago.ComplementoPago.Monto = pagoXml.Monto;
                        pago.ComplementoPago.Serie = cfdi.Serie;
                        pago.ComplementoPago.Folio = cfdi.Folio;
                        pago.ComplementoPago.Uuid = cfdi.TimbreFiscalDigital.UUID;
                        pago.ComplementoPago.FechaPago = pagoXml.FechaPago.ToString("dd/MM/yyyy");
                    }
                   pago.Detalle_Validacion = _validaPagos.ValidaComplementoPago(pago, cfdi);
                }
                pago.Procesado = true;
            }catch(Exception ex)
            {
                ModelState.AddModelError("", String.Format("Error: {0}", ex.Message));
                return View(pagoDR);
            }
            return View(pago);
        }

        public ActionResult EstadoPago(PagosDR pagoDR)
        {
            try
            {
                var pago = _db.PagoDr.Find(pagoDR.Id);
                var usuario = _db.Usuarios.Find(ObtenerUsuario());
                var documentoRecibido = _db.DocumentoRecibidoDr.Find(pago.ComplementoPagoRecibido_Id);
                
                documentoRecibido.EstadoPago = c_EstadoPago.Completado;
                documentoRecibido.AprobacionesDR.FechaCompletaPagos = DateTime.Now;
                documentoRecibido.AprobacionesDR.UsuarioCompletaPagos_id = usuario.Id;
                
                _db.Entry(documentoRecibido).State = EntityState.Modified;
                _db.SaveChanges();

                return RedirectToAction("ComplementosPagosCargados");
            }
            catch
            {
                return View("CargaComplementoPago",pagoDR.Id);
            }
            
        }
    
        // GET: DocumentosPagos/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: DocumentosPagos/Delete/5
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
        private String SubeArchivo(int indice)
        {
            if (Request.Files.Count > 0)
            {
                var archivo = Request.Files[indice];
                if (archivo.ContentLength > 0)
                {
                    var operacionesStreams = new OperacionesStreams();
                    var nombreArchivo = Path.GetFileName(archivo.FileName);

                    var pathDestino = Path.Combine(Server.MapPath("~/Archivos/DocumentosProveedores/"), archivo.FileName);
                    Stream fileStream = archivo.InputStream;
                    var mStreamer = new MemoryStream();
                    mStreamer.SetLength(fileStream.Length);
                    fileStream.Read(mStreamer.GetBuffer(), 0, (int)fileStream.Length);
                    mStreamer.Seek(0, SeekOrigin.Begin);
                    operacionesStreams.StreamArchivo(mStreamer, pathDestino);
                    return pathDestino;
                }
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
