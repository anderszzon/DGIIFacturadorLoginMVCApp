using ConexionDGII;
using DGIIFacturadorLoginMVCApp.Data;
using DGIIFacturadorLoginMVCApp.Data.Migrations;
using DGIIFacturadorLoginMVCApp.Enums;
using DGIIFacturadorLoginMVCApp.Models;
using DGIIFacturadorLoginMVCApp.Extensions;
using iText.Barcodes;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace DGIIFacturadorLoginMVCApp.Controllers
{
    public class FacturacionDGIIController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        private const string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
        private const string passCert = "LD271167";
        private const string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
        private const string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
        private const string urlRecepcionResumenFactura = "https://ecf.dgii.gov.do/certecf/recepcionfc/api/recepcion/ecf";
        private const string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";
        private const string urlRecepcionFacturaAprobacionComercial = "https://ecf.dgii.gov.do/certecf/AprobacionComercial/api/AprobacionComercial";

        public FacturacionDGIIController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public ActionResult RegistrarComprobante()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GenerarPDF(int id, string codigoSeguridad)
        {
            var factura = _context.FacturasDGII
                    .Include(f => f.Items)
                    .FirstOrDefault(f => f.Id == id);

            if (factura == null)
                return NotFound();

            byte[] pdfBytes = CrearFacturaPDFInMemory(factura, codigoSeguridad, _env.WebRootPath);

            return File(pdfBytes, "application/pdf");
        }

        [HttpGet]
        public IActionResult GenerarPDFDownloads(int id, string codigoSeguridad)
        {
            var factura = _context.FacturasDGII
                    .Include(f => f.Items)
                    .FirstOrDefault(f => f.Id == id);

            if (factura == null)
                return NotFound();

            byte[] pdfBytes = CrearFacturaPDFInMemory(factura, codigoSeguridad, _env.WebRootPath);

            return File(pdfBytes, "application/pdf", $"Factura_{factura.ENCF}.pdf");
        }

        private byte[] CrearFacturaPDFInMemory(FacturasDGII factura, string codigoSeguridad, string webRootPath)
        {

            using (var ms = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(ms);
                PdfDocument pdf = new PdfDocument(writer);
                Document doc = new Document(pdf);

                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                PdfFont boldFont2 = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                string logoPath = Path.Combine(webRootPath, "images", "logo.jpeg");

                doc.Add(new Paragraph(" "));

                Table headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 48, 30, 48 }));
                headerTable.SetWidth(UnitValue.CreatePercentValue(100));
                headerTable.SetMarginBottom(10);
                headerTable.SetFont(boldFont);

                ImageData imageData = ImageDataFactory.Create(logoPath);
                Image logo = new Image(imageData);
                logo.ScaleToFit(150, 150);
                logo.SetMarginBottom(5);
                logo.SetHorizontalAlignment(HorizontalAlignment.LEFT);

                Cell leftCell = new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).SetFont(boldFont);

                leftCell.Add(logo);

                leftCell.Add(new Paragraph("Mora Tapia Peralta & Asociado, SRL").SetFontSize(9));
                leftCell.Add(new Paragraph($"RNC: {factura.RNCEmisor}").SetFontSize(9));
                leftCell.Add(new Paragraph("Dirección: Calle Ciudad Heredia de Costa Rica No.37 Local 303 Hondura La Feria").SetFontSize(9));
                leftCell.Add(new Paragraph("Teléfono: (829)-435-9277").SetFontSize(9));
                leftCell.Add(new Paragraph("Email: mtp@mtpasociados.com").SetFontSize(9));

                Cell spacerCell = new Cell().SetBorder(Border.NO_BORDER);

                Cell rightCell = new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).SetFont(boldFont);

                rightCell.Add(
                    new Paragraph("Página 1 de 1")
                        .SetFontSize(9)
                        .SetTextAlignment(TextAlignment.RIGHT)
                        .SetMarginBottom(10)
                );

                if (Enum.TryParse(factura.TipoeCF, out TipoECFEnum tipoEnum))
                {
                    string nombreFactura = tipoEnum.GetDisplayName();
                    rightCell.Add(new Paragraph(nombreFactura).SetFontSize(11).SetFont(boldFont2));
                }

                rightCell.Add(new Paragraph($"NCF: {factura.ENCF}").SetFontSize(9));
                rightCell.Add(new Paragraph($"NCF Modificado: {factura.NCFModificado}").SetFontSize(9));


                rightCell.Add(new Paragraph($"Fecha Vencimiento: {factura.FechaVencimientoSecuencia}").SetFontSize(9));
                rightCell.Add(new Paragraph($"Fecha: {factura.FechaEmision}").SetFontSize(9));
                rightCell.Add(new Paragraph($"Número Factura: {factura.NumeroFacturaInterna}").SetFontSize(9));

                headerTable.AddCell(leftCell);
                headerTable.AddCell(spacerCell);
                headerTable.AddCell(rightCell);

                doc.Add(headerTable);

                Table clienteTable = new Table(1);
                clienteTable.SetWidth(UnitValue.CreatePercentValue(40));
                clienteTable.SetHorizontalAlignment(HorizontalAlignment.LEFT);
                clienteTable.SetMarginBottom(10);
                clienteTable.SetBorder(new SolidBorder(0.5f));

                clienteTable.AddHeaderCell(new Cell()
                    .Add(new Paragraph("Cliente")
                    .SetFontSize(8)
                    .SetFont(boldFont)
                    .SetTextAlignment(TextAlignment.CENTER))
                    .SetPadding(5)
                );

                clienteTable.AddCell(new Cell().Add(new Paragraph($"RNC: {factura.RNCComprador}").SetFontSize(8)).SetBorder(Border.NO_BORDER).SetPadding(2));
                clienteTable.AddCell(new Cell().Add(new Paragraph($"CLIENTE: {factura.RazonSocialComprador}").SetFontSize(8)).SetBorder(Border.NO_BORDER).SetPadding(2));
                clienteTable.AddCell(new Cell().Add(new Paragraph($"DIRECCIÓN: {factura.DireccionComprador}").SetFontSize(8)).SetBorder(Border.NO_BORDER).SetPadding(2));

                doc.Add(clienteTable);

                Table table = new Table(UnitValue.CreatePercentArray(new float[] { 10, 30, 20, 20, 20 }))
                    .UseAllAvailableWidth()
                    .SetFontSize(9)
                    .SetFont(boldFont)
                    .SetTextAlignment(TextAlignment.RIGHT);

                for (int i = 0; i < 5; i++)
                {
                    string titulo = i == 0 ? "ITEM" :
                                    i == 1 ? "DESCRIPCIÓN" :
                                    i == 2 ? "CANTIDAD" :
                                    i == 3 ? "PRECIO" : "MONTO";

                    Cell headerCell = new Cell()
                        .Add(new Paragraph(titulo));

                    table.AddHeaderCell(headerCell);
                }

                foreach (var linea in factura.Items)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        string content = i == 0 ? linea.CantidadItem.ToString("N0") :
                                        i == 1 ? linea.NombreItem ?? "" :
                                        i == 2 ? Convert.ToDecimal(linea.UnidadMedida).ToString("N2") :
                                        i == 3 ? linea.PrecioUnitarioItem.ToString("N2") :
                                        linea.MontoItem.ToString("N2");

                        Cell dataCell = new Cell()
                            .Add(new Paragraph(content))
                            .SetBorderTop(Border.NO_BORDER)
                            .SetBorderBottom(Border.NO_BORDER);

                        table.AddCell(dataCell);
                    }
                }

                Cell totalesCell = new Cell(1, 5)
                    .SetBorderBottom(Border.NO_BORDER)
                    .SetBorderLeft(Border.NO_BORDER)
                    .SetBorderRight(Border.NO_BORDER)
                    .SetPadding(0)
                    .SetMargin(0)
                    .SetTextAlignment(TextAlignment.RIGHT);

                Table innerTable = new Table(UnitValue.CreatePercentArray(new float[] { 10, 10 }))
                    .SetWidth(UnitValue.CreatePercentValue(40))
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                    .SetBorderBottom(Border.NO_BORDER);

                innerTable.AddCell(
                    new Cell()
                        .Add(new Paragraph("Subtotal:").SetFontSize(9))
                        .SetBorder(new SolidBorder(0.5f))
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFont(boldFont2)

                );
                innerTable.AddCell(
                    new Cell()
                        .Add(new Paragraph($"{factura.MontoGravadoTotal:N2}").SetFontSize(9))
                        .SetBorder(new SolidBorder(0.5f))
                        .SetTextAlignment(TextAlignment.RIGHT)
                );

                innerTable.AddCell(
                    new Cell()
                        .Add(new Paragraph("ITBIS:").SetFontSize(9))
                        .SetBorder(new SolidBorder(0.5f))
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFont(boldFont2)

                );
                innerTable.AddCell(
                    new Cell()
                        .Add(new Paragraph($"{factura.TotalITBIS:N2}").SetFontSize(9))
                        .SetBorder(new SolidBorder(0.5f))
                        .SetTextAlignment(TextAlignment.RIGHT)
                );

                innerTable.AddCell(
                    new Cell()
                        .Add(new Paragraph("Total:").SetFontSize(9))
                        .SetBorder(new SolidBorder(0.5f))
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFont(boldFont2)
                );
                innerTable.AddCell(
                    new Cell()
                        .Add(new Paragraph($"{factura.MontoTotal:N2}").SetFontSize(9))
                        .SetBorder(new SolidBorder(0.5f))
                        .SetTextAlignment(TextAlignment.RIGHT)
                );

                totalesCell.Add(innerTable);
                table.AddCell(totalesCell);

                doc.Add(table);

                doc.Add(new Paragraph(" "));

                Table finalTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                    .UseAllAvailableWidth()
                    .SetMarginTop(20);

                Cell leftCell1 = new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT);

                Paragraph lineaFirma = new Paragraph("_____________________________________")
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetFontSize(9)
                    .SetMarginBottom(0);

                Paragraph autorizadoPor = new Paragraph("Autorizado por")
                    .SetFontSize(9)
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetFont(boldFont)
                    .SetMarginTop(2);

                leftCell1.Add(lineaFirma);
                leftCell1.Add(autorizadoPor);

                Cell rightCell1 = new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT);

                DateTime fechaFirma = DateTime.ParseExact(factura.FechaHoraFirma, "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                string soloFecha = fechaFirma.ToString("dd-MM-yyyy");

                string fechaFirmaFormateada = Uri.EscapeDataString(fechaFirma.ToString("dd-MM-yyyy HH:mm:ss"));

                string url = $"https://ecf.dgii.gov.do/certecf/ConsultaTimbre?RncEmisor={factura.RNCEmisor}&RncComprador={factura.RNCComprador}&ENCF={factura.ENCF}&FechaEmision={factura.FechaEmision}&MontoTotal={factura.MontoTotal}&FechaFirma={fechaFirmaFormateada}&CodigoSeguridad={codigoSeguridad}";

                BarcodeQRCode qrCode = new BarcodeQRCode(url);
                Image qrCodeImage = new Image(qrCode.CreateFormXObject(pdf));
                qrCodeImage.ScaleToFit(100, 100);
                qrCodeImage.SetHorizontalAlignment(HorizontalAlignment.RIGHT);

                rightCell1.Add(qrCodeImage);
                rightCell1.Add(new Paragraph($"Código de Seguridad: {codigoSeguridad}").SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT).SetMarginTop(5));
                rightCell1.Add(new Paragraph($"FechaHoraFirma: {factura.FechaHoraFirma}").SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));

                finalTable.AddCell(leftCell1);
                finalTable.AddCell(rightCell1);

                doc.Add(finalTable);

                doc.Close();
                return ms.ToArray();
            }
        }

        [HttpGet]
        public IActionResult comprobanteE31A()
        {
            var model = new FacturaDGIIModel1
            {
                ECF = new ECFModel1
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel1
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel1
                        {
                            TipoeCF = "",
                            eNCF = "E310000000001",
                            FechaVencimientoSecuencia = "31-12-2028",
                            IndicadorEnvioDiferido = "1",
                            IndicadorMontoGravado = "0",
                            TipoIngresos = "01",
                            TipoPago = "1"
                        },
                        Emisor = new EmisorModel1
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010101",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORT",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel1
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        Totales = new TotalesModel1
                        {
                            MontoGravadoTotal = "6000.00",
                            MontoGravadoI1 = "6000.00",
                            ITBIS1 = "18",
                            TotalITBIS = "1080.00",
                            TotalITBIS1 = "1080.00",
                            MontoTotal = "7080.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel1
                    {
                        Item = new List<ItemModel1>
                {
                    new ItemModel1
                    {
                        NumeroLinea = "1",
                        IndicadorFacturacion = "1",
                        NombreItem = "ASW DTU",
                        IndicadorBienoServicio = "1",
                        CantidadItem = "15.00",
                        UnidadMedida = "31",
                        PrecioUnitarioItem = "400.00",
                        MontoItem = "6000.00"
                    }
                }
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE31A(FacturaDGIIModel1 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {

                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);
                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteACECF()
        {
            var model = new FacturaDGIIModelACECF
            {
                ACECF = new ECFModelACECF
                {
                    DetalleAprobacionComercial = new DetalleAprobacionComercialACECF
                    {
                        Version = "",
                        RNCEmisor = "131880681",
                        eNCF = "E310000000001",
                        FechaEmision = "01-04-2020",
                        MontoTotal = "7080",
                        RNCComprador = "130322791",
                        Estado = "1",
                        FechaHoraAprobacionComercial = "09-06-2026 16:11:06",
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteACECF(FacturaDGIIModelACECF model)
        {
            string jsonFactura = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string factura = FacturacionElectronicaDGII.ObtenerFacturaAprobacionComercialSincrona(urlSemilla, passCert, jsonFactura);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaAprobacionComercialSincrona(urlValidarSemilla, urlRecepcionFacturaAprobacionComercial, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(factura);
                JObject jsonObjectResponse = JObject.Parse(response);

                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString()
                };

                if (respuesta.CodigoRespuesta == "01")
                {
                    respuesta.CodigoRespuesta = "1";
                    return View("verFactura", respuesta);
                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }
            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE31B()
        {
            var model = new FacturaDGIIModel2
            {
                ECF = new ECFModel2
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel2
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel2
                        {
                            TipoeCF = "",
                            eNCF = "E310000000011",
                            FechaVencimientoSecuencia = "31-12-2028",
                            IndicadorEnvioDiferido = "1",
                            IndicadorMontoGravado = "0",
                            TipoIngresos = "01",
                            TipoPago = "1"
                        },
                        Emisor = new EmisorModel2
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel2
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        Totales = new TotalesModel2
                        {
                            MontoGravadoTotal = "81015.00",
                            MontoGravadoI1 = "81015.00",
                            ITBIS1 = "18",
                            TotalITBIS = "14582.70",
                            TotalITBIS1 = "14582.70",
                            MontoPeriodo = "95597.70",
                            ValorPagar = "95597.70",
                            MontoTotal = "95597.70",
                        }
                    },
                    DetallesItems = new DetallesItemsModel2
                    {
                        Item = new List<ItemModel2>
                    {
                        new ItemModel2
                        {
                            NumeroLinea = "1",
                            TablaCodigosItem = new TablaCodigosItem7
                            {
                                CodigosItem = new List<CodigosItem7>
                                {
                                    new CodigosItem7
                                    {
                                        TipoCodigo = "Interno",
                                        CodigoItem = "1561"
                                    }
                                }
                            },
                            IndicadorFacturacion = "1",
                            NombreItem = "ZAPATOS",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "23.00",
                            UnidadMedida = "43",
                            PrecioUnitarioItem = "35.0000",
                            MontoItem = "805.00"
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "2",
                            TablaCodigosItem = new TablaCodigosItem7
                            {
                                CodigosItem = new List<CodigosItem7>
                                {
                                    new CodigosItem7
                                    {
                                        TipoCodigo = "Interno",
                                        CodigoItem = "1561"
                                    }
                                }
                            },
                            IndicadorFacturacion = "1",
                            NombreItem = "GALLETAS",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "547.00",
                            UnidadMedida = "6",
                            PrecioUnitarioItem = "145.0000",
                            MontoItem = "79315.00"
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "3",
                            TablaCodigosItem = new TablaCodigosItem7
                            {
                                CodigosItem = new List<CodigosItem7>
                                {
                                    new CodigosItem7
                                    {
                                        TipoCodigo = "Interno",
                                        CodigoItem = "1561"
                                    }
                                }
                            },
                            IndicadorFacturacion = "1",
                            NombreItem = "CAF¿",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "14.00",
                            UnidadMedida = "31",
                            PrecioUnitarioItem = "55.0000",
                            MontoItem = "770.00"
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "4",
                            TablaCodigosItem = new TablaCodigosItem7
                            {
                                CodigosItem = new List<CodigosItem7>
                                {
                                    new CodigosItem7
                                    {
                                        TipoCodigo = "Interno",
                                        CodigoItem = "1561"
                                    }
                                }
                            },
                            IndicadorFacturacion = "1",
                            NombreItem = "LECHE",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "25.00",
                            UnidadMedida = "24",
                            PrecioUnitarioItem = "65.0000",
                            MontoItem = "1625.00"
                            //// FechaElaboracion
                            //// FechaVencimiento
                        }
                      }
                    },
                    DescuentosORecargos = new DescuentosORecargosModel2
                    {
                        DescuentoORecargo = new List<DescuentosORecargo2>
                        {
                            new DescuentosORecargo2
                            {
                                NumeroLinea = "1",
                                TipoAjuste = "D",
                                DescripcionDescuentooRecargo = "Pronto pago",
                                TipoValor = "$",
                                MontoDescuentooRecargo = "1500.00",
                                IndicadorFacturacionDescuentooRecargo = "1"
                            }
                        }
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE310000000007(FacturaDGIIModel2 model)
        {
            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };

                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE310000000007()
        {
            var model = new FacturaDGIIModel2
            {
                ECF = new ECFModel2
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel2
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel2
                        {
                            TipoeCF = "",
                            eNCF = "E310000000007",
                            FechaVencimientoSecuencia = "31-12-2028",
                            IndicadorMontoGravado = "0",
                            TipoIngresos = "01",
                            TipoPago = "1",
                        },
                        Emisor = new EmisorModel2
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel2
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        Totales = new TotalesModel2
                        {
                            MontoGravadoTotal = "133975.00",
                            MontoGravadoI1 = "69975.00",
                            MontoGravadoI2 = "64000.00",
                            MontoExento = "71650.00",
                            ITBIS1 = "18",
                            ITBIS2 = "16",
                            TotalITBIS = "22835.50",
                            TotalITBIS1 = "12595.50",
                            TotalITBIS2 = "10240.00",
                            MontoPeriodo = "228460.50",
                            ValorPagar = "228460.50",
                            MontoTotal = "228460.50",
                        }
                    },
                    DetallesItems = new DetallesItemsModel2
                    {
                        Item = new List<ItemModel2>
                    {
                        new ItemModel2
                        {
                            NumeroLinea = "1",
                            IndicadorFacturacion = "4",
                            NombreItem = "ARROZ LA GARZA",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "20.00",
                            UnidadMedida = "46",
                            PrecioUnitarioItem = "1500.0000",
                            MontoItem = "30000.00"
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "2",
                            IndicadorFacturacion = "2",
                            NombreItem = "AZUCAR CREMA",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "40.00",
                            UnidadMedida = "46",
                            PrecioUnitarioItem = "1300.0000",
                            MontoItem = "52000.00"
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "3",
                            IndicadorFacturacion = "1",
                            NombreItem = "ESPAGUETIS MILANO",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "50.00",
                            UnidadMedida = "14",
                            PrecioUnitarioItem = "900.0000",
                            MontoItem = "45000.00"
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "4",
                            IndicadorFacturacion = "4",
                            NombreItem = "LECHE MILEX",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "25.00",
                            UnidadMedida = "47",
                            PrecioUnitarioItem = "450.0000",
                            MontoItem = "11250.00"
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "5",
                            IndicadorFacturacion = "1",
                            NombreItem = "SALSA LA FAMOSA",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "35.00",
                            UnidadMedida = "47",
                            PrecioUnitarioItem = "200.0000",
                            MontoItem = "7000.00"
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "6",
                            IndicadorFacturacion = "1",
                            NombreItem = "GALLETAS SALADAS GUARINA",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "55.00",
                            UnidadMedida = "14",
                            PrecioUnitarioItem = "95.0000",
                            MontoItem = "5225.00"
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "7",
                            IndicadorFacturacion = "4",
                            NombreItem = "SALAMI INDUVECA",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "60.00",
                            UnidadMedida = "14",
                            PrecioUnitarioItem = "115.0000",
                            MontoItem = "6900.00"
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "8",
                            IndicadorFacturacion = "1",
                            NombreItem = "JUGO DE NARANJA RICA",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "65.00",
                            UnidadMedida = "15",
                            PrecioUnitarioItem = "100.0000",
                            MontoItem = "6500.00"
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "9",
                            IndicadorFacturacion = "1",
                            NombreItem = "ACEITE CRISOL",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "25.00",
                            UnidadMedida = "47",
                            PrecioUnitarioItem = "250.0000",
                            MontoItem = "6250.00"
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "10",
                            IndicadorFacturacion = "4",
                            NombreItem = "HUEVOS CASCARON",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "55.00",
                            UnidadMedida = "20",
                            PrecioUnitarioItem = "300.0000",
                            MontoItem = "16500.00"
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "11",
                            IndicadorFacturacion = "4",
                            NombreItem = "MAIZ EL MAIZAL",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "20.00",
                            UnidadMedida = "46",
                            PrecioUnitarioItem = "350.0000",
                            MontoItem = "7000.00"
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "12",
                            IndicadorFacturacion = "2",
                            NombreItem = "CAF¿ SANTO DOMINGO",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "30.00",
                            UnidadMedida = "46",
                            PrecioUnitarioItem = "400.0000",
                            MontoItem = "12000.00"
                        }
                      }
                    }
                }
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE310000000009(FacturaDGIIModel2 model)
        {
            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };

                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE310000000009()
        {
            var model = new FacturaDGIIModel2
            {
                ECF = new ECFModel2
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel2
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel2
                        {
                            TipoeCF = "",
                            eNCF = "E310000000009",
                            FechaVencimientoSecuencia = "31-12-2028",
                            TipoIngresos = "01",
                            TipoPago = "1",
                        },
                        Emisor = new EmisorModel2
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel2
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        Totales = new TotalesModel2
                        {
                            MontoExento = "41450.00",
                            MontoTotal = "41450.00",
                        }
                    },
                    DetallesItems = new DetallesItemsModel2
                    {
                        Item = new List<ItemModel2>
                    {
                        new ItemModel2
                        {
                            NumeroLinea = "1",
                            IndicadorFacturacion = "4",
                            NombreItem = "Leche 12/24OZ",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "50.00",
                            UnidadMedida = "5",
                            PrecioUnitarioItem = "350.0000",
                            DescuentoMonto = "100.00",
                            TablaSubDescuento = new TablaSubDescuento2
                            {
                                SubDescuento = new List<SubDescuento2>
                                {
                                    new SubDescuento2
                                    {
                                        TipoSubDescuento = "$",
                                        MontoSubDescuento = "100.00"
                                    }
                                }
                            },
                            MontoItem = "17400.00"
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "2",
                            IndicadorFacturacion = "4",
                            NombreItem = "Huevo 18",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "20.00",
                            UnidadMedida = "6",
                            PrecioUnitarioItem = "500.0000",
                            DescuentoMonto = "100.00",
                            TablaSubDescuento = new TablaSubDescuento2
                            {
                                SubDescuento = new List<SubDescuento2>
                                {
                                    new SubDescuento2
                                    {
                                        TipoSubDescuento = "$",
                                        MontoSubDescuento = "100.00"
                                    }
                                }
                            },
                            MontoItem = "9900.00" // (20 * 500) - 100 = 9900.00
                        },
                        new ItemModel2
                        {
                            NumeroLinea = "3",
                            IndicadorFacturacion = "4",
                            NombreItem = "Carnes paq 2lb",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "30.00",
                            UnidadMedida = "31",
                            PrecioUnitarioItem = "475.0000",
                            DescuentoMonto = "100.00",
                            TablaSubDescuento = new TablaSubDescuento2
                            {
                                SubDescuento = new List<SubDescuento2>
                                {
                                    new SubDescuento2
                                    {
                                        TipoSubDescuento = "$",
                                        MontoSubDescuento = "100.00"
                                    }
                                }
                            },
                            MontoItem = "14150.00" // (30 * 475) - 100 = 14150.00
                        }
                      }
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE31B(FacturaDGIIModel2 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };

                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE31C()
        {
            var model = new FacturaDGIIModel3
            {
                ECF = new ECFModel3
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel3
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel3
                        {
                            TipoeCF = "",
                            eNCF = "E310000000003",
                            FechaVencimientoSecuencia = "31-12-2028",
                            IndicadorEnvioDiferido = "1",
                            IndicadorMontoGravado = "0",
                            TipoIngresos = "01",
                            TipoPago = "1"
                        },
                        Emisor = new EmisorModel3
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel3
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "CONSEJO NACIONAL DE SEGURIDAD SOCIAL",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        Totales = new TotalesModel3
                        {
                            MontoGravadoTotal = "118464.21",
                            MontoGravadoI1 = "118464.21",
                            ITBIS1 = "18",
                            TotalITBIS = "21323.56",
                            TotalITBIS1 = "21323.56",
                            MontoImpuestoAdicional = "14215.71",
                            MontoTotal = "154003.47",
                            ImpuestosAdicionales = new ImpuestosAdicionalesModel3
                            {
                                ImpuestoAdicional = new List<ImpuestoAdicionalTotalesModel3>
                                {
                                    new ImpuestoAdicionalTotalesModel3
                                    {
                                        TipoImpuesto = "002",
                                        TasaImpuestoAdicional = "2",
                                        OtrosImpuestosAdicionales = "2369.28"
                                    },
                                    new ImpuestoAdicionalTotalesModel3
                                    {
                                        TipoImpuesto = "004",
                                        TasaImpuestoAdicional = "10",
                                        OtrosImpuestosAdicionales = "11846.42"
                                    }
                                }
                            }
                        }
                    },
                    DetallesItems = new DetallesItemsModel3
                    {
                        Item = new List<ItemModel3>
                {
                    new ItemModel3
                    {
                        NumeroLinea = "1",
                        IndicadorFacturacion = "1",
                        NombreItem = "Renta Total",
                        IndicadorBienoServicio = "2",
                        CantidadItem = "1.00",
                        UnidadMedida = "43",
                        PrecioUnitarioItem = "107766.57",
                        MontoItem = "107766.57",
                        TablaImpuestoAdicional = new TablaImpuestoAdicionalModel3
                        {
                            ImpuestoAdicional = new List<ImpuestoAdicionalItemModel3>
                            {
                                new ImpuestoAdicionalItemModel3 { TipoImpuesto = "002" },
                                new ImpuestoAdicionalItemModel3 { TipoImpuesto = "004" }
                            }
                        }
                    },
                    new ItemModel3
                    {
                        NumeroLinea = "2",
                        IndicadorFacturacion = "1",
                        NombreItem = "Uso total",
                        IndicadorBienoServicio = "2",
                        CantidadItem = "1.0",
                        UnidadMedida = "43",
                        PrecioUnitarioItem = "10697.64",
                        MontoItem = "10697.64",
                        TablaImpuestoAdicional = new TablaImpuestoAdicionalModel3
                        {
                            ImpuestoAdicional = new List<ImpuestoAdicionalItemModel3>
                            {
                                new ImpuestoAdicionalItemModel3 { TipoImpuesto = "002" },
                                new ImpuestoAdicionalItemModel3 { TipoImpuesto = "004" }
                            }
                        }
                    }
                }
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE31C(FacturaDGIIModel3 model)
        {
            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };

                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }


        [HttpGet]
        public IActionResult comprobanteE31D()
        {
            var model = new FacturaDGIIModel4
            {
                ECF = new ECFModel4
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel4
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel4
                        {
                            TipoeCF = "",
                            eNCF = "E310000000004",
                            FechaVencimientoSecuencia = "31-12-2028",
                            IndicadorEnvioDiferido = "1",
                            IndicadorMontoGravado = "1",
                            TipoIngresos = "01",
                            TipoPago = "1"
                        },
                        Emisor = new EmisorModel4
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010101",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORT",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel4
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "CONSEJO NACIONAL DE SEGURIDAD SOCIAL",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        InformacionesAdicionales = new InformacionesAdicionales4
                        {
                            NumeroContenedor = "8019289",
                            NumeroReferencia = "1447"
                        },
                        Totales = new TotalesModel4
                        {
                            MontoGravadoTotal = "15548.04",
                            MontoGravadoI1 = "12363.56",
                            MontoGravadoI2 = "3184.48",
                            ITBIS1 = "18",
                            ITBIS2 = "16",
                            TotalITBIS = "2734.96",
                            TotalITBIS1 = "2225.44",
                            TotalITBIS2 = "509.52",
                            MontoTotal = "18283.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel4
                    {
                        Item = new List<ItemModel4>
                        {
                            new ItemModel4
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "1",
                                NombreItem = "MESAS INDUSTRIALES",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "50.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "220.00",
                                MontoItem = "11000.00"
                            },
                            new ItemModel4
                            {
                                NumeroLinea = "2",
                                IndicadorFacturacion = "1",
                                NombreItem = "JARRAS ACERO INOXIDABLE",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "45.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "85.00",
                                MontoItem = "3825.00"
                            },
                            new ItemModel4
                            {
                                NumeroLinea = "3",
                                IndicadorFacturacion = "2",
                                NombreItem = "YOGURT",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "56.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "67.00",
                                MontoItem = "3752"
                            }
                        }
                    },
                    DescuentosORecargos = new DescuentosORecargosModel4
                    {
                        DescuentoORecargo = new List<DescuentosORecargo4>
                        {
                            new DescuentosORecargo4
                            {
                                NumeroLinea = "1",
                                TipoAjuste = "D",
                                DescripcionDescuentooRecargo = "N",
                                TipoValor = "$",
                                MontoDescuentooRecargo = "200.00",
                                IndicadorFacturacionDescuentooRecargo = "1"
                            },
                            new DescuentosORecargo4
                            {
                                NumeroLinea = "2",
                                TipoAjuste = "D",
                                DescripcionDescuentooRecargo = "D",
                                TipoValor = "$",
                                MontoDescuentooRecargo = "50.00",
                                IndicadorFacturacionDescuentooRecargo = "2"
                            }
                        }
                    }
                }
            };

            return View(model);
        }



        [HttpPost]
        public IActionResult comprobanteE31D(FacturaDGIIModel4 model)
        {
            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }


        [HttpGet]
        public IActionResult comprobanteE31E()
        {
            var model = new FacturaDGIIModel5
            {
                ECF = new ECFModel5
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel5
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel5
                        {
                            TipoeCF = "",
                            eNCF = "E310000000005",
                            FechaVencimientoSecuencia = "31-12-2028",
                            IndicadorEnvioDiferido = "1",
                            IndicadorMontoGravado = "0",
                            TipoIngresos = "01",
                            TipoPago = "1",
                            TablaFormasPago = new TablaFormasPago5
                            {
                                FormaDePago = new List<FormaDePago5>
                                {
                                    new FormaDePago5
                                    {
                                        FormaPago = "1",
                                        MontoPago = "45253.00"
                                    }
                                }
                            }
                        },
                        Emisor = new EmisorModel5
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel5
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        Totales = new TotalesModel5
                        {
                            MontoGravadoTotal = "38350.00",
                            MontoGravadoI1 = "38350.00",
                            ITBIS1 = "18",
                            TotalITBIS = "6903.00",
                            TotalITBIS1 = "6903.00",
                            MontoTotal = "45253.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel5
                    {
                        Item = new List<ItemModel5>
                {
                    new ItemModel5
                    {
                        NumeroLinea = "1",
                        IndicadorFacturacion = "1",
                        NombreItem = "Servicio domiciliario",
                        IndicadorBienoServicio = "2",
                        CantidadItem = "5.00",
                        UnidadMedida = "43",
                        PrecioUnitarioItem = "2000.00",
                        DescuentoMonto = "1000.00",
                        TablaSubDescuento = new TablaSubDescuento5
                        {
                            SubDescuento = new List<SubDescuento5>
                            {
                                new SubDescuento5
                                {
                                    TipoSubDescuento = "$",
                                    MontoSubDescuento = "500.00"
                                },
                                new SubDescuento5
                                {
                                    TipoSubDescuento = "$",
                                    MontoSubDescuento = "500.00"
                                }
                            }
                        },
                        MontoItem = "9000.00"
                    },
                    new ItemModel5
                    {
                        NumeroLinea = "2",
                        IndicadorFacturacion = "1",
                        NombreItem = "Servicio presencial",
                        IndicadorBienoServicio = "1",
                        CantidadItem = "10.00",
                        UnidadMedida = "43",
                        PrecioUnitarioItem = "3000.00",
                        DescuentoMonto = "700.00",
                        TablaSubDescuento = new TablaSubDescuento5
                        {
                            SubDescuento = new List<SubDescuento5>
                            {
                                new SubDescuento5
                                {
                                    TipoSubDescuento = "$",
                                    MontoSubDescuento = "700.00"
                                }
                            }
                        },
                        RecargoMonto = "50.00",
                        TablaSubRecargo = new TablaSubRecargo5
                        {
                            SubRecargo = new List<SubRecargo5>
                            {
                                new SubRecargo5
                                {
                                    TipoSubRecargo = "$",
                                    MontoSubRecargo = "50.00"
                                }
                            }
                        },
                        MontoItem = "29350.00"
                    }
                }
                    }
                }
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE31E(FacturaDGIIModel5 model)
        {
            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubDescuento?.SubDescuento != null)
                {
                    item.TablaSubDescuento.SubDescuento = item.TablaSubDescuento.SubDescuento
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubDescuento) && !string.IsNullOrWhiteSpace(ci.MontoSubDescuento))
                        .ToList();

                    if (!item.TablaSubDescuento.SubDescuento.Any())
                    {
                        item.TablaSubDescuento = null;
                    }
                }
            }

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubRecargo?.SubRecargo != null)
                {
                    item.TablaSubRecargo.SubRecargo = item.TablaSubRecargo.SubRecargo
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubRecargo) && !string.IsNullOrWhiteSpace(ci.MontoSubRecargo))
                        .ToList();

                    if (!item.TablaSubRecargo.SubRecargo.Any())
                    {
                        item.TablaSubRecargo = null;
                    }
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE320000000006()
        {
            var model = new FacturaDGIIModelE32
            {
                ECF = new ECFModelE32
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModelE32
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModelE32
                        {
                            TipoeCF = "",
                            eNCF = "E320000000006",
                            TipoIngresos = "01",
                            TipoPago = "1",
                            IndicadorEnvioDiferido = "1",
                            IndicadorMontoGravado = "0",
                        },
                        Emisor = new EmisorModelE32
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModelE32
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        Totales = new TotalesModelE32
                        {
                            MontoGravadoTotal = "350765.00",
                            MontoGravadoI1 = "269805.00",
                            MontoGravadoI2 = "80190.00",
                            MontoGravadoI3 = "770.00",
                            MontoExento = "1625.00",
                            ITBIS1 = "18",
                            ITBIS2 = "16",
                            ITBIS3 = "0",
                            TotalITBIS = "61395.30",
                            TotalITBIS1 = "48564.90",
                            TotalITBIS2 = "12830.40",
                            TotalITBIS3 = "0.00",
                            MontoTotal = "413785.30",
                            MontoPeriodo = "413785.30",
                            ValorPagar = "413785.30",
                        }
                    },
                    DetallesItems = new DetallesItemsModelE32
                    {
                        Item = new List<ItemModelE32>
                        {
                            new ItemModelE32
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "1",
                                NombreItem = "LAPICES",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "23.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "35.0000",
                                MontoItem = "805.00"
                            },
                            new ItemModelE32
                            {
                                NumeroLinea = "2",
                                IndicadorFacturacion = "2",
                                NombreItem = "GALLETAS",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "547.00",
                                UnidadMedida = "6",
                                PrecioUnitarioItem = "145.0000",
                                MontoItem = "79315.00"
                            },
                            new ItemModelE32
                            {
                                NumeroLinea = "3",
                                IndicadorFacturacion = "3",
                                NombreItem = "PAN",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "14.00",
                                UnidadMedida = "31",
                                PrecioUnitarioItem = "55.0000",
                                MontoItem = "770.00"
                            },
                            new ItemModelE32
                            {
                                NumeroLinea = "4",
                                IndicadorFacturacion = "4",
                                NombreItem = "LECHE",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "25.00",
                                UnidadMedida = "47",
                                PrecioUnitarioItem = "65.0000",
                                MontoItem = "1625.00"
                            },
                            new ItemModelE32
                            {
                                NumeroLinea = "5",
                                IndicadorFacturacion = "2",
                                NombreItem = "SALSA",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "35.00",
                                UnidadMedida = "47",
                                PrecioUnitarioItem = "25.0000",
                                MontoItem = "875.00"
                            },
                            new ItemModelE32
                            {
                                NumeroLinea = "6",
                                IndicadorFacturacion = "1",
                                NombreItem = "TV LG 57",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "2.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "57000.0000",
                                MontoItem = "114000.00"
                            },
                            new ItemModelE32
                            {
                                NumeroLinea = "7",
                                IndicadorFacturacion = "1",
                                NombreItem = "LAVADORA-SECADORA  WESTINGHOUSE",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "1.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "75000.0000",
                                MontoItem = "75000.00"
                            },
                            new ItemModelE32
                            {
                                NumeroLinea = "8",
                                IndicadorFacturacion = "1",
                                NombreItem = "ESTUFA MABE",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "1.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "45000.0000",
                                MontoItem = "45000.00"
                            },
                            new ItemModelE32
                            {
                                NumeroLinea = "9",
                                IndicadorFacturacion = "1",
                                NombreItem = "LAPICES",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "1.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "35000.0000",
                                MontoItem = "35000.00"
                            }
                        }
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE320000000006(FacturaDGIIModelE32 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };

                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);
                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE320000000011ECF()
        {
            var model = new FacturaDGIIModelE32
            {
                ECF = new ECFModelE32
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModelE32
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModelE32
                        {
                            TipoeCF = "",
                            eNCF = "E320000000011",
                            TipoIngresos = "01",
                            TipoPago = "1",
                        },
                        Emisor = new EmisorModelE32
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS PRUEBA FACTURA DE CONSUMO MENOR 250MIL",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOS@123.COM",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModelE32
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            CorreoComprador = "DOCUMENTOSELECTRONICOSDE0612345678969789@123.COM",
                            DireccionComprador = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            MunicipioComprador = "170203",
                            ProvinciaComprador = "170000",
                            TelefonoAdicional = "809-472-7676"
                        },
                        Totales = new TotalesModelE32
                        {
                            MontoGravadoTotal = "34000.00",
                            MontoGravadoI1 = "34000.00",
                            ITBIS1 = "18",
                            TotalITBIS = "6120.00",
                            TotalITBIS1 = "6120.00",
                            MontoTotal = "40120.00",
                        }
                    },
                    DetallesItems = new DetallesItemsModelE32
                    {
                        Item = new List<ItemModelE32>
                        {
                            new ItemModelE32
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "1",
                                NombreItem = "Cargador",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "1",
                                UnidadMedida = "55",
                                PrecioUnitarioItem = "5000.00",
                                MontoItem = "5000.00"
                            },
                            new ItemModelE32
                            {
                                NumeroLinea = "2",
                                IndicadorFacturacion = "1",
                                NombreItem = "FREEZER",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "1",
                                UnidadMedida = "23",
                                PrecioUnitarioItem = "29000.00",
                                MontoItem = "29000.00"
                            }
                        }
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE320000000011ECF(FacturaDGIIModelE32 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);

                JObject jsonObject = JObject.Parse(invoice);

                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    root = jsonObject.GetValue("root")?.ToString()
                };

                // Guardar en Session
                HttpContext.Session.SetString("CodigoSeguridad",respuesta.CodigoSeguridad ?? string.Empty);

                if (respuesta.root == "ECF")
                {
                    respuesta.CodigoRespuesta = "1";

                    return View("verFactura", respuesta);
                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE320000000011()
        {
            string codigoSeguridad = HttpContext.Session.GetString("CodigoSeguridad");

            var model = new FacturaDGIIModelE32RFCE
            {
                RFCE = new ECFModelE32RFCE
                {
                    Encabezado = new EncabezadoModelE32RFCE
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModelE32RFCE
                        {
                            TipoeCF = "",
                            eNCF = "E320000000011",
                            TipoIngresos = "01",
                            TipoPago = "1",
                        },
                        Emisor = new EmisorModelE32RFCE
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS PRUEBA FACTURA DE CONSUMO MENOR 250MIL",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModelE32RFCE
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                        },
                        Totales = new TotalesModelE32RFCE
                        {
                            MontoGravadoTotal = "34000.00",
                            MontoGravadoI1 = "34000.00",
                            TotalITBIS = "6120.00",
                            TotalITBIS1 = "6120.00",
                            MontoTotal = "40120.00",
                        },
                        CodigoSeguridadeCF = codigoSeguridad
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE320000000011(FacturaDGIIModelE32RFCE model)
        {
            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionResumenFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.RFCE?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.RFCE?.Encabezado?.IdDoc?.eNCF,
                    TipoPago = model?.RFCE?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.RFCE?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    //IndicadorMontoGravado = model?.RFCE?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.RFCE?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.RFCE?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.RFCE?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.RFCE?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.RFCE?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.RFCE?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.RFCE?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.RFCE?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.RFCE?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.RFCE?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.RFCE?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.RFCE?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.RFCE?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.RFCE?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.RFCE?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.RFCE?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.RFCE?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.RFCE?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.RFCE?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.RFCE?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.RFCE?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.RFCE?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.RFCE?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.RFCE?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.RFCE?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    //FechaHoraFirma = model?.RFCE?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };

                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);
                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }


        [HttpGet]
        public IActionResult comprobanteE320000000013ECF()
        {
            var model = new FacturaDGIIModelE32
            {
                ECF = new ECFModelE32
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModelE32
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModelE32
                        {
                            TipoeCF = "",
                            eNCF = "E320000000013",
                            TipoIngresos = "01",
                            TipoPago = "1",
                        },
                        Emisor = new EmisorModelE32
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS PRUEBA FACTURA DE CONSUMO MENOR 250MIL",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOS@123.COM",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModelE32
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            CorreoComprador = "DOCUMENTOSELECTRONICOSDE0612345678969789@123.COM",
                            DireccionComprador = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            MunicipioComprador = "170203",
                            ProvinciaComprador = "170000",
                            TelefonoAdicional = "809-472-7676"
                        },
                        Totales = new TotalesModelE32
                        {
                            MontoGravadoTotal = "95000.00",
                            MontoGravadoI1 = "95000.00",
                            ITBIS1 = "18",
                            TotalITBIS = "17100.00",
                            TotalITBIS1 = "17100.00",
                            MontoTotal = "112100.00",
                        }
                    },
                    DetallesItems = new DetallesItemsModelE32
                    {
                        Item = new List<ItemModelE32>
                        {
                            new ItemModelE32
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "1",
                                NombreItem = "Nevera",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "1",
                                UnidadMedida = "55",
                                PrecioUnitarioItem = "95000.00",
                                MontoItem = "95000.00"
                            }
                        }
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE320000000013ECF(FacturaDGIIModelE32 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);

                JObject jsonObject = JObject.Parse(invoice);

                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    root = jsonObject.GetValue("root")?.ToString()
                };

                // Guardar en Session
                HttpContext.Session.SetString("CodigoSeguridad", respuesta.CodigoSeguridad ?? string.Empty);

                if (respuesta.root == "ECF")
                {
                    respuesta.CodigoRespuesta = "1";

                    return View("verFactura", respuesta);
                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE320000000013()
        {
            string codigoSeguridad = HttpContext.Session.GetString("CodigoSeguridad");

            var model = new FacturaDGIIModelE32RFCE
            {
                RFCE = new ECFModelE32RFCE
                {
                    Encabezado = new EncabezadoModelE32RFCE
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModelE32RFCE
                        {
                            TipoeCF = "",
                            eNCF = "E320000000013",
                            TipoIngresos = "01",
                            TipoPago = "1",
                        },
                        Emisor = new EmisorModelE32RFCE
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS PRUEBA FACTURA DE CONSUMO MENOR 250MIL",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModelE32RFCE
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                        },
                        Totales = new TotalesModelE32RFCE
                        {
                            MontoGravadoTotal = "95000.00",
                            MontoGravadoI1 = "95000.00",
                            TotalITBIS = "17100.00",
                            TotalITBIS1 = "17100.00",
                            MontoTotal = "112100.00",
                        },
                        CodigoSeguridadeCF = codigoSeguridad
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE320000000013(FacturaDGIIModelE32RFCE model)
        {
            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionResumenFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.RFCE?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.RFCE?.Encabezado?.IdDoc?.eNCF,
                    TipoPago = model?.RFCE?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.RFCE?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    //IndicadorMontoGravado = model?.RFCE?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.RFCE?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.RFCE?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.RFCE?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.RFCE?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.RFCE?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.RFCE?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.RFCE?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.RFCE?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.RFCE?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.RFCE?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.RFCE?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.RFCE?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.RFCE?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.RFCE?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.RFCE?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.RFCE?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.RFCE?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.RFCE?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.RFCE?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.RFCE?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.RFCE?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.RFCE?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.RFCE?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.RFCE?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.RFCE?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    //FechaHoraFirma = model?.RFCE?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };

                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);
                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }


        [HttpGet]
        public IActionResult comprobanteE320000000014ECF()
        {
            var model = new FacturaDGIIModelE32
            {
                ECF = new ECFModelE32
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModelE32
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModelE32
                        {
                            TipoeCF = "",
                            eNCF = "E320000000014",
                            TipoIngresos = "01",
                            TipoPago = "1",
                        },
                        Emisor = new EmisorModelE32
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS PRUEBA FACTURA DE CONSUMO MENOR 250MIL",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOS@123.COM",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModelE32
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            CorreoComprador = "DOCUMENTOSELECTRONICOSDE0612345678969789@123.COM",
                            DireccionComprador = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            MunicipioComprador = "170203",
                            ProvinciaComprador = "170000",
                            TelefonoAdicional = "809-472-7676"
                        },
                        Totales = new TotalesModelE32
                        {
                            MontoGravadoTotal = "10100.00",
                            MontoGravadoI1 = "10100.00",
                            ITBIS1 = "18",
                            TotalITBIS = "1818.00",
                            TotalITBIS1 = "1818.00",
                            MontoTotal = "11918.00",
                        }
                    },
                    DetallesItems = new DetallesItemsModelE32
                    {
                        Item = new List<ItemModelE32>
                        {
                            new ItemModelE32
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "1",
                                NombreItem = "Articulos de belleza",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "1",
                                UnidadMedida = "55",
                                PrecioUnitarioItem = "10000.00",
                                MontoItem = "10000.00"
                            },
                                                        new ItemModelE32
                            {
                                NumeroLinea = "2",
                                IndicadorFacturacion = "1",
                                NombreItem = "Queso",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "1",
                                UnidadMedida = "23",
                                PrecioUnitarioItem = "100.00",
                                MontoItem = "100.00"
                            }
                        }
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE320000000014ECF(FacturaDGIIModelE32 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);

                JObject jsonObject = JObject.Parse(invoice);

                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    root = jsonObject.GetValue("root")?.ToString()
                };

                // Guardar en Session
                HttpContext.Session.SetString("CodigoSeguridad", respuesta.CodigoSeguridad ?? string.Empty);

                if (respuesta.root == "ECF")
                {
                    respuesta.CodigoRespuesta = "1";

                    return View("verFactura", respuesta);
                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }


        [HttpGet]
        public IActionResult comprobanteE320000000014()
        {
            string codigoSeguridad = HttpContext.Session.GetString("CodigoSeguridad");

            var model = new FacturaDGIIModelE32RFCE
            {
                RFCE = new ECFModelE32RFCE
                {
                    Encabezado = new EncabezadoModelE32RFCE
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModelE32RFCE
                        {
                            TipoeCF = "",
                            eNCF = "E320000000014",
                            TipoIngresos = "01",
                            TipoPago = "1",
                        },
                        Emisor = new EmisorModelE32RFCE
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS PRUEBA FACTURA DE CONSUMO MENOR 250MIL",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModelE32RFCE
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                        },
                        Totales = new TotalesModelE32RFCE
                        {
                            MontoGravadoTotal = "10100.00",
                            MontoGravadoI1 = "10100.00",
                            TotalITBIS = "1818.00",
                            TotalITBIS1 = "1818.00",
                            MontoTotal = "11918.00",
                        },
                        CodigoSeguridadeCF = codigoSeguridad

                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE320000000014(FacturaDGIIModelE32RFCE model)
        {
            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionResumenFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.RFCE?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.RFCE?.Encabezado?.IdDoc?.eNCF,
                    TipoPago = model?.RFCE?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.RFCE?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    //IndicadorMontoGravado = model?.RFCE?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.RFCE?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.RFCE?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.RFCE?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.RFCE?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.RFCE?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.RFCE?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.RFCE?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.RFCE?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.RFCE?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.RFCE?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.RFCE?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.RFCE?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.RFCE?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.RFCE?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.RFCE?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.RFCE?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.RFCE?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.RFCE?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.RFCE?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.RFCE?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.RFCE?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.RFCE?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.RFCE?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.RFCE?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.RFCE?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    //FechaHoraFirma = model?.RFCE?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };

                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);
                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }


        [HttpGet]
        public IActionResult comprobanteE320000000015ECF()
        {
            var model = new FacturaDGIIModelE32
            {
                ECF = new ECFModelE32
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModelE32
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModelE32
                        {
                            TipoeCF = "",
                            eNCF = "E320000000015",
                            TipoIngresos = "01",
                            TipoPago = "1",
                        },
                        Emisor = new EmisorModelE32
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS PRUEBA FACTURA DE CONSUMO MENOR 250MIL",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOS@123.COM",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModelE32
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            CorreoComprador = "DOCUMENTOSELECTRONICOSDE0612345678969789@123.COM",
                            DireccionComprador = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            MunicipioComprador = "170203",
                            ProvinciaComprador = "170000",
                            TelefonoAdicional = "809-472-7676"
                        },
                        Totales = new TotalesModelE32
                        {
                            MontoGravadoTotal = "55000.00",
                            MontoGravadoI1 = "55000.00",
                            ITBIS1 = "18",
                            TotalITBIS = "9900.00",
                            TotalITBIS1 = "9900.00",
                            MontoTotal = "64900.00",
                        }
                    },
                    DetallesItems = new DetallesItemsModelE32
                    {
                        Item = new List<ItemModelE32>
                        {
                            new ItemModelE32
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "1",
                                NombreItem = "Celular",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "1",
                                UnidadMedida = "55",
                                PrecioUnitarioItem = "50000.00",
                                MontoItem = "50000.00"
                            },
                            new ItemModelE32
                            {
                                NumeroLinea = "2",
                                IndicadorFacturacion = "1",
                                NombreItem = "Cargador",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "1",
                                UnidadMedida = "23",
                                PrecioUnitarioItem = "5000.00",
                                MontoItem = "5000.00"
                            }
                        }
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE320000000015ECF(FacturaDGIIModelE32 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);

                JObject jsonObject = JObject.Parse(invoice);

                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    root = jsonObject.GetValue("root")?.ToString()
                };

                // Guardar en Session
                HttpContext.Session.SetString("CodigoSeguridad", respuesta.CodigoSeguridad ?? string.Empty);

                if (respuesta.root == "ECF")
                {
                    respuesta.CodigoRespuesta = "1";

                    return View("verFactura", respuesta);
                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }


        [HttpGet]
        public IActionResult comprobanteE320000000015()
        {
            string codigoSeguridad = HttpContext.Session.GetString("CodigoSeguridad");

            var model = new FacturaDGIIModelE32RFCE
            {
                RFCE = new ECFModelE32RFCE
                {
                    Encabezado = new EncabezadoModelE32RFCE
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModelE32RFCE
                        {
                            TipoeCF = "",
                            eNCF = "E320000000015",
                            TipoIngresos = "01",
                            TipoPago = "1",
                        },
                        Emisor = new EmisorModelE32RFCE
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS PRUEBA FACTURA DE CONSUMO MENOR 250MIL",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModelE32RFCE
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                        },
                        Totales = new TotalesModelE32RFCE
                        {
                            MontoGravadoTotal = "55000.00",
                            MontoGravadoI1 = "55000.00",
                            TotalITBIS = "9900.00",
                            TotalITBIS1 = "9900.00",
                            MontoTotal = "64900.00",
                        },
                        CodigoSeguridadeCF = codigoSeguridad
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE320000000015(FacturaDGIIModelE32RFCE model)
        {
            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionResumenFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.RFCE?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.RFCE?.Encabezado?.IdDoc?.eNCF,
                    TipoPago = model?.RFCE?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.RFCE?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    //IndicadorMontoGravado = model?.RFCE?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.RFCE?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.RFCE?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.RFCE?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.RFCE?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.RFCE?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.RFCE?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.RFCE?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.RFCE?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.RFCE?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.RFCE?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.RFCE?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.RFCE?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.RFCE?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.RFCE?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.RFCE?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.RFCE?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.RFCE?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.RFCE?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.RFCE?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.RFCE?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.RFCE?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.RFCE?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.RFCE?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.RFCE?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.RFCE?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.RFCE?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    //FechaHoraFirma = model?.RFCE?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };

                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);
                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE320000000005()
        {
            var model = new FacturaDGIIModel6
            {
                ECF = new ECFModel6
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel6
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel6
                        {
                            TipoeCF = "",
                            eNCF = "E320000000005",
                            TipoIngresos = "01",
                            TipoPago = "1"
                        },
                        Emisor = new EmisorModel6
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel6
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        InformacionesAdicionales = new InformacionesAdicionales6
                        {
                            FechaEmbarque = "08-06-2020",
                            NumeroEmbarque = "1550523",
                            NumeroContenedor = "1025536",
                            NumeroReferencia = "121517",
                            PesoBruto = "25.00",
                            PesoNeto = "24.50",
                            UnidadPesoBruto = "23",
                            UnidadPesoNeto = "23",
                            CantidadBulto = "1.00",
                            UnidadBulto = "6",
                            VolumenBulto = "1.00",
                            UnidadVolumen = "6"
                        },
                        Totales = new TotalesModel6
                        {
                            MontoGravadoTotal = "1971544.00",
                            MontoGravadoI1 = "8260.00",
                            MontoGravadoI2 = "1935966.00",
                            MontoGravadoI3 = "27318.00",
                            ITBIS1 = "18",
                            ITBIS2 = "16",
                            ITBIS3 = "0",
                            TotalITBIS = "311241.36",
                            TotalITBIS1 = "1486.80",
                            TotalITBIS2 = "309754.56",
                            TotalITBIS3 = "0.00",
                            MontoTotal = "2282785.36",
                            MontoPeriodo = "2282785.36",
                            ValorPagar  = "2282785.36"
                        }
                    },
                    DetallesItems = new DetallesItemsModel6
                    {
                        Item = new List<ItemModel6>
                        {
                            new ItemModel6
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "1",
                                NombreItem = "LAPICES",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "236.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "35.0000",
                                MontoItem = "8260.00"
                            },
                            new ItemModel6
                            {
                                NumeroLinea = "2",
                                IndicadorFacturacion = "2",
                                NombreItem = "GALLETAS",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "527.00",
                                UnidadMedida = "6",
                                PrecioUnitarioItem = "3650.0000",
                                MontoItem = "1923550.00"
                            },
                            new ItemModel6
                            {
                                NumeroLinea = "3",
                                IndicadorFacturacion = "3",
                                NombreItem = "PAN",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "174.00",
                                UnidadMedida = "31",
                                PrecioUnitarioItem = "157.0000",
                                MontoItem = "27318.00"
                            },
                            new ItemModel6
                            {
                                NumeroLinea = "4",
                                IndicadorFacturacion = "2",
                                NombreItem = "LECHE",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "128.00",
                                UnidadMedida = "47",
                                PrecioUnitarioItem = "97.0000",
                                MontoItem = "12416.00"
                            }
                        }
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE320000000005(FacturaDGIIModel6 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }
            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE32A()
        {
            var model = new FacturaDGIIModel6
            {
                ECF = new ECFModel6
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel6
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel6
                        {
                            TipoeCF = "",
                            eNCF = "E320000000001",
                            TipoIngresos = "01",
                            TipoPago = "1"
                        },
                        Emisor = new EmisorModel6
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "320301",
                            Provincia = "320000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel6
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        InformacionesAdicionales = new InformacionesAdicionales6
                        {
                            NumeroContenedor = "8019289",
                            NumeroReferencia = "1447"
                        },
                        Totales = new TotalesModel6
                        {
                            MontoExento = "300000.00",
                            MontoTotal = "300000.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel6
                    {
                        Item = new List<ItemModel6>
                {
                    new ItemModel6
                    {
                        NumeroLinea = "1",
                        IndicadorFacturacion = "4",
                        NombreItem = "LECHE",
                        IndicadorBienoServicio = "1",
                        CantidadItem = "7500.00",
                        UnidadMedida = "47",
                        PrecioUnitarioItem = "40.00",
                        MontoItem = "300000.00"
                    }
                }
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE32A(FacturaDGIIModel6 model)
        {
            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE32B()
        {
            var model = new FacturaDGIIModel7
            {
                ECF = new ECFModel7
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel7
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel7
                        {
                            TipoeCF = "",
                            eNCF = "E320000000002",
                            IndicadorMontoGravado = "0",
                            TipoIngresos = "01",
                            TipoPago = "1"
                        },
                        Emisor = new EmisorModel7
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "320301",
                            Provincia = "320000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel7
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        Totales = new TotalesModel7
                        {
                            MontoGravadoTotal = "152800.00",
                            MontoGravadoI1 = "27625.00",
                            MontoGravadoI2 = "125175.00",
                            MontoExento = "82750.00",
                            ITBIS1 = "18",
                            ITBIS2 = "16",
                            TotalITBIS = "25000.50",
                            TotalITBIS1 = "4972.50",
                            TotalITBIS2 = "20028.00",
                            MontoTotal = "260550.50",
                            ValorPagar = "260550.50"
                        }
                    },
                    DetallesItems = new DetallesItemsModel7
                    {
                        Item = new List<ItemModel7>
                {
                    new ItemModel7
                    {
                        NumeroLinea = "1",
                        TablaCodigosItem = new TablaCodigosItem7
                        {
                            CodigosItem = new List<CodigosItem7>
                            {
                                new CodigosItem7
                                {
                                    TipoCodigo = "INTERNA",
                                    CodigoItem = "ASDFJKL"
                                }
                            }
                        },
                        IndicadorFacturacion = "1",
                        NombreItem = "ALIMENTOS ENTEROS",
                        IndicadorBienoServicio = "1",
                        CantidadItem = "5.00",
                        UnidadMedida = "6",
                        PrecioUnitarioItem = "1100.00",
                        RecargoMonto = "25.00",
                        TablaSubRecargo = new TablaSubRecargo7
                        {
                            SubRecargo = new List<SubRecargo7>
                            {
                                new SubRecargo7
                                {
                                    TipoSubRecargo = "$",
                                    MontoSubRecargo = "25.00"
                                }
                            }
                        },
                        MontoItem = "5525.00"
                    },
                }
                    }
                }
            };


            for (int i = 1; i <= 14; i++)
            {
                var item = new ItemModel7
                {
                    NumeroLinea = i.ToString(),
                    IndicadorFacturacion = (i <= 4) ? "1" : (i <= 9) ? "2" : "4",
                    NombreItem = (i <= 4) ? "ALIMENTOS ENTEROS" : (i <= 9) ? "LECHE" : "MAH",
                    IndicadorBienoServicio = "1",
                    CantidadItem = (i <= 4) ? "5.00" : (i <= 9) ? "10.00" : "15.00",
                    UnidadMedida = "6",
                    PrecioUnitarioItem = (i <= 4 || i >= 10) ? "1100.00" : "2500.00",
                    RecargoMonto = (i <= 4) ? "25.00" : (i <= 9) ? "35.00" : "50.00",
                    TablaSubRecargo = new TablaSubRecargo7
                    {
                        SubRecargo = new List<SubRecargo7>
                        {
                            new SubRecargo7
                            {
                                TipoSubRecargo = "$",
                                MontoSubRecargo = (i <= 4) ? "25.00" : (i <= 9) ? "35.00" : "50.00"
                            }
                        }
                    },
                    MontoItem = (i <= 4) ? "5525.00" : (i <= 9) ? "25035.00" : "16550.00"
                };

                if (i <= 4)
                {
                    item.TablaCodigosItem = new TablaCodigosItem7
                    {
                        CodigosItem = new List<CodigosItem7>
                        {
                            new CodigosItem7
                            {
                                TipoCodigo = "INTERNA",
                                CodigoItem = "ASDFJKL"
                            }
                        }
                    };
                }

                model.ECF.DetallesItems.Item.Add(item);
            }

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE32B(FacturaDGIIModel7 model)
        {
            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaCodigosItem?.CodigosItem != null)
                {
                    item.TablaCodigosItem.CodigosItem = item.TablaCodigosItem.CodigosItem
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoCodigo) && !string.IsNullOrWhiteSpace(ci.CodigoItem))
                        .ToList();

                    if (!item.TablaCodigosItem.CodigosItem.Any())
                    {
                        item.TablaCodigosItem = null;
                    }
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();

                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE33()
        {
            var model = new FacturaDGIIModel8
            {
                ECF = new ECFModel8
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel8
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel8
                        {
                            TipoeCF = "",
                            eNCF = "E330000000001",
                            FechaVencimientoSecuencia = "31-12-2028",
                            TipoIngresos = "01",
                            TipoPago = "1",
                            TablaFormasPago = new TablaFormasPago8
                            {
                                FormaDePago = new List<FormaDePago8>
                            {
                                new FormaDePago8
                                {
                                    FormaPago = "1",
                                    MontoPago = "400000.00"
                                }
                            }
                            }
                        },
                        Emisor = new EmisorModel8
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "02-04-2020"
                        },
                        Comprador = new CompradorModel8
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "11-11-2020",
                            FechaOrdenCompra = "10-11-2020",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        InformacionesAdicionales = new InformacionesAdicionales8
                        {
                            NumeroContenedor = "8019289",
                            NumeroReferencia = "1447"
                        },
                        Totales = new TotalesModel8
                        {
                            MontoExento = "400000.00",
                            MontoTotal = "400000.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel8
                    {
                        Item = new List<ItemModel8>
                {
                    new ItemModel8
                    {
                        NumeroLinea = "1",
                        IndicadorFacturacion = "4",
                        NombreItem = "LECHE",
                        IndicadorBienoServicio = "1",
                        CantidadItem = "10000.00",
                        UnidadMedida = "47",
                        PrecioUnitarioItem = "40.00",
                        MontoItem = "400000.00"
                    }
                }
                    },
                    InformacionReferencia = new InformacionReferencia8
                    {
                        NCFModificado = "E320000000006",
                        FechaNCFModificado = "01-04-2020",
                        CodigoModificacion = "3"
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE33(FacturaDGIIModel8 model)
        {

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubDescuento?.SubDescuento != null)
                {
                    item.TablaSubDescuento.SubDescuento = item.TablaSubDescuento.SubDescuento
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubDescuento) && !string.IsNullOrWhiteSpace(ci.MontoSubDescuento))
                        .ToList();

                    if (!item.TablaSubDescuento.SubDescuento.Any())
                    {
                        item.TablaSubDescuento = null;
                    }
                }
            }

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubRecargo?.SubRecargo != null)
                {
                    item.TablaSubRecargo.SubRecargo = item.TablaSubRecargo.SubRecargo
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubRecargo) && !string.IsNullOrWhiteSpace(ci.MontoSubRecargo))
                        .ToList();

                    if (!item.TablaSubRecargo.SubRecargo.Any())
                    {
                        item.TablaSubRecargo = null;
                    }
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),
                    
                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado,

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };

                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE34()
        {
            var model = new FacturaDGIIModel9
            {
                ECF = new ECFModel9
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel9
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel9
                        {
                            TipoeCF = "",
                            eNCF = "E340000000001",
                            IndicadorNotaCredito = "0",
                            IndicadorMontoGravado = "0",
                            TipoIngresos = "01",
                            TipoPago = "1"
                        },
                        Emisor = new EmisorModel9
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "02-04-2020"
                        },
                        Comprador = new CompradorModel9
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        InformacionesAdicionales = new InformacionesAdicionales9
                        {
                            NumeroContenedor = "8019289",
                            NumeroReferencia = "1447"
                        },
                        Totales = new TotalesModel9
                        {
                            MontoGravadoTotal = "0.00",
                            MontoGravadoI1 = "0.00",
                            ITBIS1 = "18",
                            TotalITBIS = "0.00",
                            TotalITBIS1 = "0.00",
                            MontoTotal = "0.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel9
                    {
                        Item = new List<ItemModel9>
                {
                    new ItemModel9
                    {
                        NumeroLinea = "1",
                        IndicadorFacturacion = "1",
                        NombreItem = "TOP BOWL 1",
                        IndicadorBienoServicio = "1",
                        CantidadItem = "15",
                        UnidadMedida = "31",
                        PrecioUnitarioItem = "0.00",
                        MontoItem = "0.00"
                    }
                }
                    },
                    InformacionReferencia = new InformacionReferencia9
                    {
                        NCFModificado = "E310000000001",
                        FechaNCFModificado = "01-04-2020",
                        CodigoModificacion = "2",
                        RazonModificacion = "Error en datos"
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE34(FacturaDGIIModel9 model)
        {

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubDescuento?.SubDescuento != null)
                {
                    item.TablaSubDescuento.SubDescuento = item.TablaSubDescuento.SubDescuento
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubDescuento) && !string.IsNullOrWhiteSpace(ci.MontoSubDescuento))
                        .ToList();

                    if (!item.TablaSubDescuento.SubDescuento.Any())
                    {
                        item.TablaSubDescuento = null;
                    }
                }
            }

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubRecargo?.SubRecargo != null)
                {
                    item.TablaSubRecargo.SubRecargo = item.TablaSubRecargo.SubRecargo
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubRecargo) && !string.IsNullOrWhiteSpace(ci.MontoSubRecargo))
                        .ToList();

                    if (!item.TablaSubRecargo.SubRecargo.Any())
                    {
                        item.TablaSubRecargo = null;
                    }
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE34A()
        {
            var model = new FacturaDGIIModel9
            {
                ECF = new ECFModel9
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel9
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel9
                        {
                            TipoeCF = "",
                            eNCF = "E340000000015",
                            IndicadorNotaCredito = "0",
                            //IndicadorMontoGravado = "0",
                            //TipoIngresos = "01",
                            TipoPago = "1"
                        },
                        Emisor = new EmisorModel9
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            //NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            //Municipio = "010100",
                            //Provincia = "010000",
                            //CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            //WebSite = "www.facturaelectronica.com",
                            //CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            //NumeroFacturaInterna = "123456789016",
                            //NumeroPedidoInterno = "123456789016",
                            //ZonaVenta = "NORTE",
                            FechaEmision = "02-04-2020"
                        },
                        Comprador = new CompradorModel9
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 02",
                            //ContactoComprador = "MARCOS LATIPLOL",
                            //CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            //DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            //MunicipioComprador = "010100",
                            //ProvinciaComprador = "010000",
                            //FechaEntrega = "10-10-2020",
                            //FechaOrdenCompra = "10-11-2018",
                            //NumeroOrdenCompra = "4500352238",
                            //CodigoInternoComprador = "10633440"
                        },
                        InformacionesAdicionales = new InformacionesAdicionales9
                        {
                            NumeroContenedor = "8019289",
                            NumeroReferencia = "1447"
                        },
                        Totales = new TotalesModel9
                        {
                            //MontoGravadoTotal = "0.00",
                            //MontoGravadoI1 = "0.00",
                            //ITBIS1 = "18",
                            //TotalITBIS = "0.00",
                            //TotalITBIS1 = "0.00",
                            MontoTotal = "0.00",
                            MontoNoFacturable = "1.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel9
                    {
                        Item = new List<ItemModel9>
                    {
                        new ItemModel9
                        {
                            NumeroLinea = "1",
                            IndicadorFacturacion = "0",
                            NombreItem = "SERVICIO PUBLICIDAD ACTUALIZADO",
                            IndicadorBienoServicio = "2",
                            CantidadItem = "1.00",
                            UnidadMedida = "",
                            PrecioUnitarioItem = "1.00",
                            MontoItem = "1.00"
                        }
                    }
                    },
                    InformacionReferencia = new InformacionReferencia9
                    {
                        NCFModificado = "E410000000001",
                        FechaNCFModificado = "01-04-2020",
                        CodigoModificacion = "2",
                        RazonModificacion = ""
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE34A(FacturaDGIIModel9 model)
        {

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubDescuento?.SubDescuento != null)
                {
                    item.TablaSubDescuento.SubDescuento = item.TablaSubDescuento.SubDescuento
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubDescuento) && !string.IsNullOrWhiteSpace(ci.MontoSubDescuento))
                        .ToList();

                    if (!item.TablaSubDescuento.SubDescuento.Any())
                    {
                        item.TablaSubDescuento = null;
                    }
                }
            }

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubRecargo?.SubRecargo != null)
                {
                    item.TablaSubRecargo.SubRecargo = item.TablaSubRecargo.SubRecargo
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubRecargo) && !string.IsNullOrWhiteSpace(ci.MontoSubRecargo))
                        .ToList();

                    if (!item.TablaSubRecargo.SubRecargo.Any())
                    {
                        item.TablaSubRecargo = null;
                    }
                }
            }

            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = (item.UnidadMedida ?? "0"),
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE41()
        {
            var model = new FacturaDGIIModel10
            {
                ECF = new ECFModel10
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel10
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel10
                        {
                            TipoeCF = "",
                            eNCF = "E410000000001",
                            FechaVencimientoSecuencia = "31-12-2028",
                            IndicadorMontoGravado = "0",
                            TipoPago = "1",
                            TablaFormasPago = new TablaFormasPago10
                            {
                                FormaDePago = new List<FormaDePago10>
                        {
                            new FormaDePago10
                            {
                                FormaPago = "1",
                                MontoPago = "9000.00"
                            }
                        }
                            }
                        },
                        Emisor = new EmisorModel10
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010101",
                            Provincia = "010000",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel10
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 02",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000"
                        },
                        Totales = new TotalesModel10
                        {
                            MontoGravadoTotal = "10000.00",
                            MontoGravadoI1 = "10000.00",
                            ITBIS1 = "18",
                            TotalITBIS = "1800.00",
                            TotalITBIS1 = "1800.00",
                            MontoTotal = "11800.00",
                            ValorPagar = "11800.00",
                            TotalITBISRetenido = "1800.00",
                            TotalISRRetencion = "1000.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel10
                    {
                        Item = new List<ItemModel10>
                {
                    new ItemModel10
                    {
                        NumeroLinea = "1",
                        IndicadorFacturacion = "1",
                        Retencion = new Retencion10
                        {
                            IndicadorAgenteRetencionoPercepcion = "1",
                            MontoITBISRetenido = "1800.00",
                            MontoISRRetenido = "1000.00"
                        },
                        NombreItem = "SERVICIO PUBLICIDAD",
                        IndicadorBienoServicio = "2",
                        DescripcionItem = "LOREM IPSUM DOLOR SITI AMET, CONSECTETUR ADIPISCI IT. VESTIBULUM 1234 FERMENTUM E-X, CONSEQUAT (IACULIS) ARCU. PELLENTESQUE RUTRUM DUI EGET SAPIEN DICTUM, EU MOLLIS LECTUS AUCTOR. NUNC ORNARE ERAT QUIS NISL IMPERDIET PORTA. NULLAM VEL PHARETRA LEO, PELLENTESQUE FERMENTUM LECTUS. VIVAMUS ORCI IPSUM, SCELERISQUE QUIS VEHICULA QUIS, TEMPUS VITAE PURUS. ALIQUAM SAGITTIS EROS VITAE ANTE FAUCIBUS AUCTOR. MAECENAS PELLENTESQUE VEL EST IN CONGUE. FUSCE ARCU LIGULA, HENDRERIT EU DOLOR A, FACILISIS GRAVIDA DOLOR. PELLENTESQUE SED ALIQUET DOLOR. MAURIS BIBENDUM VEHICULA DICTUM. ETIAM TEMPUS, ODIO NEC CONSECTETUR IACULIS, ODIO NIBH EGESTAS FELIS, SED VIVERRA MAGNA EX SUSCIPIT AUGUE. PELLENTESQUE VESTIBULUM, LACUS NON MATTIS MOLESTIE, NEQUE LEO FACILISIS URNA, AC SUSCIPIT ERAT NISI ET MAGNA. PRAESENT PLACERAT SED LEO A GRAVIDA. MORBI ID ELIT LACUS. CLASS APTENT TACITI SOCIOSQU AD LITORA TORQUENT PER CONUBIA NOSTRA, PER INCEPTOS HIMENAEOS, CONSECTETUR ADIPISCING ELIT. NUNC ORNARE ERAT QUIS NISL IMP.",
                        CantidadItem = "1.00",
                        UnidadMedida = "43",
                        PrecioUnitarioItem = "10000.00",
                        MontoItem = "10000.00"
                    }
                }
                    }
                }
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE41(FacturaDGIIModel10 model)
        {

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubDescuento?.SubDescuento != null)
                {
                    item.TablaSubDescuento.SubDescuento = item.TablaSubDescuento.SubDescuento
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubDescuento) && !string.IsNullOrWhiteSpace(ci.MontoSubDescuento))
                        .ToList();

                    if (!item.TablaSubDescuento.SubDescuento.Any())
                    {
                        item.TablaSubDescuento = null;
                    }
                }
            }

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubRecargo?.SubRecargo != null)
                {
                    item.TablaSubRecargo.SubRecargo = item.TablaSubRecargo.SubRecargo
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubRecargo) && !string.IsNullOrWhiteSpace(ci.MontoSubRecargo))
                        .ToList();

                    if (!item.TablaSubRecargo.SubRecargo.Any())
                    {
                        item.TablaSubRecargo = null;
                    }
                }
            }

            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    //NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    //CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    //WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    //CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    //NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    //NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    //ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    //ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    //FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    //FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    //NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    //CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }


        [HttpGet]
        public IActionResult comprobanteE410000000008()
        {
            var model = new FacturaDGIIModel10
            {
                ECF = new ECFModel10
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel10
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel10
                        {
                            TipoeCF = "",
                            eNCF = "E410000000008",
                            FechaVencimientoSecuencia = "31-12-2028",
                            IndicadorMontoGravado = "0",
                            TipoPago = "1",
                            TablaFormasPago = new TablaFormasPago10
                            {
                                FormaDePago = new List<FormaDePago10>
                                {
                                    new FormaDePago10
                                    {
                                        FormaPago = "1",
                                        MontoPago = "17565.78"
                                    }
                                }
                            }
                        },
                        Emisor = new EmisorModel10
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel10
                        {
                            RNCComprador = "533445861",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 11",
                        },
                        Totales = new TotalesModel10
                        {
                            MontoGravadoTotal = "14886.25",
                            MontoGravadoI1 = "14886.25",
                            ITBIS1 = "18",
                            TotalITBIS = "2679.53",
                            TotalITBIS1 = "2679.53",
                            MontoTotal = "17565.78",
                            TotalITBISRetenido = "2634.53",
                            TotalISRRetencion = "1488.63"
                        }
                    },
                    DetallesItems = new DetallesItemsModel10
                    {
                        Item = new List<ItemModel10>
                        {
                            new ItemModel10
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "1",
                                Retencion = new Retencion10
                                {
                                    IndicadorAgenteRetencionoPercepcion = "1",
                                    MontoITBISRetenido = "961.20",
                                    MontoISRRetenido = "539.00"
                                },
                                NombreItem = "Servicio Profesional Legislativo",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "15.00",
                                UnidadMedida = "23",
                                PrecioUnitarioItem = "385.0000",
                                DescuentoMonto = "385.00",
                                TablaSubDescuento = new TablaSubDescuento10
                                {
                                    SubDescuento = new List<SubDescuento10>
                                    {
                                        new SubDescuento10
                                        {
                                            TipoSubDescuento = "$",
                                            MontoSubDescuento = "385.00"
                                        }
                                    }
                                },
                                MontoItem = "5390.00"
                            },
                            new ItemModel10
                            {
                                NumeroLinea = "2",
                                IndicadorFacturacion = "1",
                                Retencion = new Retencion10
                                {
                                    IndicadorAgenteRetencionoPercepcion = "1",
                                    MontoITBISRetenido = "436.50",
                                    MontoISRRetenido = "247.50"
                                },
                                NombreItem = "Asesoria Legal",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "5.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "550.0000",
                                DescuentoMonto = "275.00",
                                TablaSubDescuento = new TablaSubDescuento10
                                {
                                    SubDescuento = new List<SubDescuento10>
                                    {
                                        new SubDescuento10
                                        {
                                            TipoSubDescuento = "%",
                                            SubDescuentoPorcentaje = "10.00",
                                            MontoSubDescuento = "275.00"
                                        }
                                    }
                                },
                                MontoItem = "2475.00"
                            },
                            new ItemModel10
                            {
                                NumeroLinea = "3",
                                IndicadorFacturacion = "1",
                                Retencion = new Retencion10
                                {
                                    IndicadorAgenteRetencionoPercepcion = "1",
                                    MontoITBISRetenido = "369.00",
                                    MontoISRRetenido = "210.00"
                                },
                                NombreItem = "Gestiones Legales",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "9.00",
                                UnidadMedida = "13",
                                PrecioUnitarioItem = "250.0000",
                                DescuentoMonto = "150.00",
                                TablaSubDescuento = new TablaSubDescuento10
                                {
                                    SubDescuento = new List<SubDescuento10>
                                    {
                                        new SubDescuento10
                                        {
                                            TipoSubDescuento = "$",
                                            MontoSubDescuento = "150.00"
                                        }
                                    }
                                },
                                MontoItem = "2100.00"
                            },
                            new ItemModel10
                            {
                                NumeroLinea = "4",
                                IndicadorFacturacion = "1",
                                Retencion = new Retencion10
                                {
                                    IndicadorAgenteRetencionoPercepcion = "1",
                                    MontoITBISRetenido = "720.90",
                                    MontoISRRetenido = "405.50"
                                },
                                NombreItem = "Legalizacion de documentos",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "23.00",
                                UnidadMedida = "15",
                                PrecioUnitarioItem = "185.0000",
                                DescuentoMonto = "200.00",
                                TablaSubDescuento = new TablaSubDescuento10
                                {
                                    SubDescuento = new List<SubDescuento10>
                                    {
                                        new SubDescuento10
                                        {
                                            TipoSubDescuento = "$",
                                            MontoSubDescuento = "200.00"
                                        }
                                    }
                                },
                                MontoItem = "4055.00"
                            },
                            new ItemModel10
                            {
                                NumeroLinea = "5",
                                IndicadorFacturacion = "1",
                                Retencion = new Retencion10
                                {
                                    IndicadorAgenteRetencionoPercepcion = "1",
                                    MontoITBISRetenido = "146.93",
                                    MontoISRRetenido = "86.63"
                                },
                                NombreItem = "Servicios ambulatorio",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "7.00",
                                UnidadMedida = "23",
                                PrecioUnitarioItem = "125.0000",
                                DescuentoMonto = "8.75",
                                TablaSubDescuento = new TablaSubDescuento10
                                {
                                    SubDescuento = new List<SubDescuento10>
                                    {
                                        new SubDescuento10
                                        {
                                            TipoSubDescuento = "%",
                                            SubDescuentoPorcentaje = "1.00",
                                            MontoSubDescuento = "8.75"
                                        }
                                    }
                                },
                                MontoItem = "866.25"
                            }
                        }
                    }
                }
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE410000000008(FacturaDGIIModel10 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubDescuento?.SubDescuento != null)
                {
                    item.TablaSubDescuento.SubDescuento = item.TablaSubDescuento.SubDescuento
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubDescuento) && !string.IsNullOrWhiteSpace(ci.MontoSubDescuento))
                        .ToList();

                    if (!item.TablaSubDescuento.SubDescuento.Any())
                    {
                        item.TablaSubDescuento = null;
                    }
                }
            }

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubRecargo?.SubRecargo != null)
                {
                    item.TablaSubRecargo.SubRecargo = item.TablaSubRecargo.SubRecargo
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubRecargo) && !string.IsNullOrWhiteSpace(ci.MontoSubRecargo))
                        .ToList();

                    if (!item.TablaSubRecargo.SubRecargo.Any())
                    {
                        item.TablaSubRecargo = null;
                    }
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    //NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    //CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    //WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    //CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    //NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    //NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    //ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    //ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    //FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    //FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    //NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    //CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }


        [HttpGet]
        public IActionResult comprobanteE43()
        {
            var model = new FacturaDGIIModel11
            {
                ECF = new ECFModel11
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel11
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel11
                        {
                            TipoeCF = "",
                            eNCF = "E430000000001",
                            FechaVencimientoSecuencia = "31-12-2028"
                        },
                        Emisor = new EmisorModel11
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010101",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            FechaEmision = "01-04-2020"
                        },
                        Totales = new TotalesModel11
                        {
                            MontoExento = "700.00",
                            MontoTotal = "700.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel11
                    {
                        Item = new List<ItemModel11>
                {
                    new ItemModel11
                    {
                        NumeroLinea = "1",
                        IndicadorFacturacion = "4",
                        NombreItem = "Peajes viaje semana I",
                        IndicadorBienoServicio = "2",
                        CantidadItem = "7.00",
                        UnidadMedida = "43",
                        PrecioUnitarioItem = "100.00",
                        MontoItem = "700.00"
                    }
                }
                    }
                }
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE43(FacturaDGIIModel11 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }
            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE430000000012()
        {
            var model = new FacturaDGIIModel11
            {
                ECF = new ECFModel11
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel11
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel11
                        {
                            TipoeCF = "",
                            eNCF = "E430000000012",
                            FechaVencimientoSecuencia = "31-12-2028",
                            TipoPago = "1"
                        },
                        Emisor = new EmisorModel11
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            FechaEmision = "01-04-2020"
                        },
                        Totales = new TotalesModel11
                        {
                            MontoExento = "32300.00",
                            MontoTotal = "32300.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel11
                    {
                        Item = new List<ItemModel11>
                        {
                            new ItemModel11
                            {
                                NumeroLinea = "1",
                                TablaCodigosItem = new TablaCodigosItem11
                                {
                                    CodigosItem = new List<CodigosItem11>
                                    {
                                        new CodigosItem11
                                        {
                                            TipoCodigo = "Interno",
                                            CodigoItem = "1521"
                                        }
                                    }
                                },
                                IndicadorFacturacion = "4",
                                NombreItem = "Gastos de Oficina",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "1",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "10000.0000",
                                MontoItem = "10000.00"
                            },
                            new ItemModel11
                            {
                                NumeroLinea = "2",
                                TablaCodigosItem = new TablaCodigosItem11
                                {
                                    CodigosItem = new List<CodigosItem11>
                                    {
                                        new CodigosItem11
                                        {
                                            TipoCodigo = "Interno",
                                            CodigoItem = "1522"
                                        }
                                    }
                                },
                                IndicadorFacturacion = "4",
                                NombreItem = "Gastos de Transporte",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "1",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "5000.0000",
                                MontoItem = "5000.00"
                            },
                            new ItemModel11
                            {
                                NumeroLinea = "3",
                                TablaCodigosItem = new TablaCodigosItem11
                                {
                                    CodigosItem = new List<CodigosItem11>
                                    {
                                        new CodigosItem11
                                        {
                                            TipoCodigo = "Interno",
                                            CodigoItem = "1523"
                                        }
                                    }
                                },
                                IndicadorFacturacion = "4",
                                NombreItem = "Mantenimiento",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "1",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "3500.0000",
                                MontoItem = "3500.00"
                            },
                            new ItemModel11
                            {
                                NumeroLinea = "4",
                                TablaCodigosItem = new TablaCodigosItem11
                                {
                                    CodigosItem = new List<CodigosItem11>
                                    {
                                        new CodigosItem11
                                        {
                                            TipoCodigo = "Interno",
                                            CodigoItem = "1524"
                                        }
                                    }
                                },
                                IndicadorFacturacion = "4",
                                NombreItem = "Gastos varios",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "2",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "6500.0000",
                                MontoItem = "13000.00"
                            },
                            new ItemModel11
                            {
                                NumeroLinea = "5",
                                TablaCodigosItem = new TablaCodigosItem11
                                {
                                    CodigosItem = new List<CodigosItem11>
                                    {
                                        new CodigosItem11
                                        {
                                            TipoCodigo = "Interno",
                                            CodigoItem = "1526"
                                        }
                                    }
                                },
                                IndicadorFacturacion = "4",
                                NombreItem = "Gastos menor cuanta",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "1",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "800.0000",
                                MontoItem = "800.00"
                            }
                        }
                    }
                }
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE430000000012(FacturaDGIIModel11 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE44()
        {
            var model = new FacturaDGIIModel12
            {
                ECF = new ECFModel12
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel12
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel12
                        {
                            TipoeCF = "",
                            eNCF = "E440000000002",
                            FechaVencimientoSecuencia = "31-12-2028",
                            TipoIngresos = "01",
                            TipoPago = "1",
                            TipoCuentaPago = "CT",
                            NumeroCuentaPago = "0301678890090",
                            BancoPago = "BANCO XDRFT",
                            TablaFormasPago = new TablaFormasPago12
                            {
                                FormaDePago = new List<FormaDePago12>
                                {
                                    new FormaDePago12
                                    {
                                        FormaPago = "2",
                                        MontoPago = "248292.00"
                                    }
                                }
                            }
                        },
                        Emisor = new EmisorModel12
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010101",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORT",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel12
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        Totales = new TotalesModel12
                        {
                            MontoExento = "248292.00",
                            MontoTotal = "248292.00",
                            ValorPagar = "248292.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel12
                    {
                        Item = Enumerable.Range(1, 57).Select(i => new ItemModel12
                        {
                            NumeroLinea = i.ToString(),
                            IndicadorFacturacion = "4",
                            NombreItem = i == 50 ? "COMBUSTIBLE AWS juk" : "COMBUSTIBLE AWSO",
                            IndicadorBienoServicio = "1",
                            CantidadItem = "22.00",
                            UnidadMedida = "15",
                            PrecioUnitarioItem = "220.00",
                            MontoItem = "4840.00"
                        }).ToList()
                    },
                    DescuentosORecargos = new DescuentosORecargosModel12
                    {
                        DescuentoORecargo = new List<DescuentosORecargo12>
                        {
                            new DescuentosORecargo12
                            {
                                NumeroLinea = "1",
                                TipoAjuste = "D",
                                DescripcionDescuentooRecargo = "DESCUENTO ADMINISTRATIVO",
                                TipoValor = "%",
                                ValorDescuentooRecargo = "10.00",
                                MontoDescuentooRecargo = "27588.00",
                                IndicadorFacturacionDescuentooRecargo = "4"
                            }
                        }
                    }
                }
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE44(FacturaDGIIModel12 model)
        {

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubDescuento?.SubDescuento != null)
                {
                    item.TablaSubDescuento.SubDescuento = item.TablaSubDescuento.SubDescuento
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubDescuento) && !string.IsNullOrWhiteSpace(ci.MontoSubDescuento))
                        .ToList();

                    if (!item.TablaSubDescuento.SubDescuento.Any())
                    {
                        item.TablaSubDescuento = null;
                    }
                }
            }

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubRecargo?.SubRecargo != null)
                {
                    item.TablaSubRecargo.SubRecargo = item.TablaSubRecargo.SubRecargo
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubRecargo) && !string.IsNullOrWhiteSpace(ci.MontoSubRecargo))
                        .ToList();

                    if (!item.TablaSubRecargo.SubRecargo.Any())
                    {
                        item.TablaSubRecargo = null;
                    }
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE440000000008()
        {
            var model = new FacturaDGIIModel12
            {
                ECF = new ECFModel12
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel12
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel12
                        {
                            TipoeCF = "",
                            eNCF = "E440000000008",
                            FechaVencimientoSecuencia = "31-12-2028",
                            TipoIngresos = "01",
                            TipoPago = "1",
                            TablaFormasPago = new TablaFormasPago12
                            {
                                FormaDePago = new List<FormaDePago12>
                                {
                                    new FormaDePago12
                                    {
                                        FormaPago = "1",
                                        MontoPago = "432000.00"
                                    }
                                }
                            }
                        },
                        Emisor = new EmisorModel12
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel12
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        Totales = new TotalesModel12
                        {
                            MontoExento = "432000.00",
                            MontoTotal = "432000.00",
                            MontoPeriodo = "432000.00",
                            ValorPagar = "432000.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel12
                    {
                        Item = new List<ItemModel12>
                        {
                            new ItemModel12
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "4",
                                NombreItem = "PTE. CJ 24/12OZ",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "20.00",
                                UnidadMedida = "18",
                                PrecioUnitarioItem = "900.0000",
                                MontoItem = "18000.00"
                            },
                            new ItemModel12
                            {
                                NumeroLinea = "2",
                                IndicadorFacturacion = "4",
                                NombreItem = "PTE. CJ 48/12OZ",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "180.00",
                                UnidadMedida = "34",
                                PrecioUnitarioItem = "1800.0000",
                                MontoItem = "324000.00"
                            },
                            new ItemModel12
                            {
                                NumeroLinea = "3",
                                IndicadorFacturacion = "4",
                                NombreItem = "PTE. CJ 48/12OZ",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "50.00",
                                UnidadMedida = "34",
                                PrecioUnitarioItem = "1800.0000",
                                MontoItem = "90000.00"
                            }
                        }
                    }
                    //DescuentosORecargos = new DescuentosORecargosModel12
                    //{
                    //    DescuentoORecargo = new List<DescuentosORecargo12>
                    //    {
                    //        new DescuentosORecargo12
                    //        {
                    //            NumeroLinea = "1",
                    //            TipoAjuste = "D",
                    //            DescripcionDescuentooRecargo = "DESCUENTO ADMINISTRATIVO",
                    //            TipoValor = "%",
                    //            ValorDescuentooRecargo = "10.00",
                    //            MontoDescuentooRecargo = "27588.00",
                    //            IndicadorFacturacionDescuentooRecargo = "4"
                    //        }
                    //    }
                    //}
                }
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE440000000008(FacturaDGIIModel12 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE440000000010()
        {
            var model = new FacturaDGIIModel12
            {
                ECF = new ECFModel12
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel12
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel12
                        {
                            TipoeCF = "",
                            eNCF = "E440000000010",
                            FechaVencimientoSecuencia = "31-12-2028",
                            TipoIngresos = "01",
                            TipoPago = "1",
                            TablaFormasPago = new TablaFormasPago12
                            {
                                FormaDePago = new List<FormaDePago12>
                                {
                                    new FormaDePago12
                                    {
                                        FormaPago = "1",
                                        MontoPago = "170150.00"
                                    }
                                }
                            }
                        },
                        Emisor = new EmisorModel12
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel12
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        Totales = new TotalesModel12
                        {
                            MontoExento = "170150.00",
                            MontoTotal = "170150.00",
                            MontoPeriodo = "170150.00",
                            ValorPagar = "170150.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel12
                    {
                        Item = new List<ItemModel12>
                        {
                            new ItemModel12
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "4",
                                NombreItem = "ZAPATOS",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "40.00",
                                UnidadMedida = "6",
                                PrecioUnitarioItem = "350.0000",
                                DescuentoMonto = "500.00",
                                TablaSubDescuento = new TablaSubDescuento12
                                {
                                    SubDescuento = new List<SubDescuento12>
                                    {
                                        new SubDescuento12
                                        {
                                            TipoSubDescuento = "$",
                                            MontoSubDescuento = "500.00"
                                        }
                                    }
                                },
                                MontoItem = "13500.00"
                            },
                            new ItemModel12
                            {
                                NumeroLinea = "2",
                                IndicadorFacturacion = "4",
                                NombreItem = "CARTERAS",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "40.00",
                                UnidadMedida = "6",
                                PrecioUnitarioItem = "450.0000",
                                DescuentoMonto = "500.00",
                                TablaSubDescuento = new TablaSubDescuento12
                                {
                                    SubDescuento = new List<SubDescuento12>
                                    {
                                        new SubDescuento12
                                        {
                                            TipoSubDescuento = "$",
                                            MontoSubDescuento = "500.00"
                                        }
                                    }
                                },
                                MontoItem = "17500.00"
                            },
                            new ItemModel12
                            {
                                NumeroLinea = "3",
                                IndicadorFacturacion = "4",
                                NombreItem = "BLUSAS",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "50.00",
                                UnidadMedida = "6",
                                PrecioUnitarioItem = "550.0000",
                                DescuentoMonto = "500.00",
                                TablaSubDescuento = new TablaSubDescuento12
                                {
                                    SubDescuento = new List<SubDescuento12>
                                    {
                                        new SubDescuento12
                                        {
                                            TipoSubDescuento = "$",
                                            MontoSubDescuento = "500.00"
                                        }
                                    }
                                },
                                MontoItem = "27000.00"
                            },
                            new ItemModel12
                            {
                                NumeroLinea = "4",
                                IndicadorFacturacion = "4",
                                NombreItem = "CALCETINES",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "25.00",
                                UnidadMedida = "6",
                                PrecioUnitarioItem = "350.0000",
                                DescuentoMonto = "500.00",
                                TablaSubDescuento = new TablaSubDescuento12
                                {
                                    SubDescuento = new List<SubDescuento12>
                                    {
                                        new SubDescuento12
                                        {
                                            TipoSubDescuento = "$",
                                            MontoSubDescuento = "500.00"
                                        }
                                    }
                                },
                                MontoItem = "8250.00"
                            },
                            new ItemModel12
                            {
                                NumeroLinea = "5",
                                IndicadorFacturacion = "4",
                                NombreItem = "TIRANTES",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "35.00",
                                UnidadMedida = "6",
                                PrecioUnitarioItem = "250.0000",
                                DescuentoMonto = "500.00",
                                TablaSubDescuento = new TablaSubDescuento12
                                {
                                    SubDescuento = new List<SubDescuento12>
                                    {
                                        new SubDescuento12
                                        {
                                            TipoSubDescuento = "$",
                                            MontoSubDescuento = "500.00"
                                        }
                                    }
                                },
                                MontoItem = "8250.00"
                            },
                            new ItemModel12
                            {
                                NumeroLinea = "6",
                                IndicadorFacturacion = "4",
                                NombreItem = "TENIS",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "34.00",
                                UnidadMedida = "6",
                                PrecioUnitarioItem = "350.0000",
                                DescuentoMonto = "500.00",
                                TablaSubDescuento = new TablaSubDescuento12
                                {
                                    SubDescuento = new List<SubDescuento12>
                                    {
                                        new SubDescuento12
                                        {
                                            TipoSubDescuento = "$",
                                            MontoSubDescuento = "500.00"
                                        }
                                    }
                                },
                                MontoItem = "11400.00"
                            },
                            new ItemModel12
                            {
                                NumeroLinea = "7",
                                IndicadorFacturacion = "4",
                                NombreItem = "CALIZOS",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "50.00",
                                UnidadMedida = "6",
                                PrecioUnitarioItem = "400.0000",
                                DescuentoMonto = "500.00",
                                TablaSubDescuento = new TablaSubDescuento12
                                {
                                    SubDescuento = new List<SubDescuento12>
                                    {
                                        new SubDescuento12
                                        {
                                            TipoSubDescuento = "$",
                                            MontoSubDescuento = "500.00"
                                        }
                                    }
                                },
                                MontoItem = "19500.00"
                            },
                            new ItemModel12
                            {
                                NumeroLinea = "8",
                                IndicadorFacturacion = "4",
                                NombreItem = "BOLZOS",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "60.00",
                                UnidadMedida = "6",
                                PrecioUnitarioItem = "350.0000",
                                DescuentoMonto = "500.00",
                                TablaSubDescuento = new TablaSubDescuento12
                                {
                                    SubDescuento = new List<SubDescuento12>
                                    {
                                        new SubDescuento12
                                        {
                                            TipoSubDescuento = "$",
                                            MontoSubDescuento = "500.00"
                                        }
                                    }
                                },
                                MontoItem = "20500.00"
                            },
                            new ItemModel12
                            {
                                NumeroLinea = "9",
                                IndicadorFacturacion = "4",
                                NombreItem = "MEDIAS",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "45.00",
                                UnidadMedida = "6",
                                PrecioUnitarioItem = "450.0000",
                                DescuentoMonto = "500.00",
                                TablaSubDescuento = new TablaSubDescuento12
                                {
                                    SubDescuento = new List<SubDescuento12>
                                    {
                                        new SubDescuento12
                                        {
                                            TipoSubDescuento = "$",
                                            MontoSubDescuento = "500.00"
                                        }
                                    }
                                },
                                MontoItem = "19750.00"
                            },
                            new ItemModel12
                            {
                                NumeroLinea = "10",
                                IndicadorFacturacion = "4",
                                NombreItem = "SUETER",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "50.00",
                                UnidadMedida = "6",
                                PrecioUnitarioItem = "500.0000",
                                DescuentoMonto = "500.00",
                                TablaSubDescuento = new TablaSubDescuento12
                                {
                                    SubDescuento = new List<SubDescuento12>
                                    {
                                        new SubDescuento12
                                        {
                                            TipoSubDescuento = "$",
                                            MontoSubDescuento = "500.00"
                                        }
                                    }
                                },
                                MontoItem = "24500.00"
                            }
                        }
                    }
                    //DescuentosORecargos = new DescuentosORecargosModel12
                    //{
                    //    DescuentoORecargo = new List<DescuentosORecargo12>
                    //    {
                    //        new DescuentosORecargo12
                    //        {
                    //            NumeroLinea = "1",
                    //            TipoAjuste = "D",
                    //            DescripcionDescuentooRecargo = "DESCUENTO ADMINISTRATIVO",
                    //            TipoValor = "%",
                    //            ValorDescuentooRecargo = "10.00",
                    //            MontoDescuentooRecargo = "27588.00",
                    //            IndicadorFacturacionDescuentooRecargo = "4"
                    //        }
                    //    }
                    //}
                }
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE440000000010(FacturaDGIIModel12 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }


        [HttpGet]
        public IActionResult comprobanteE45()
        {
            var model = new FacturaDGIIModel13
            {
                ECF = new ECFModel13
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel13
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel13
                        {
                            TipoeCF = "",
                            eNCF = "E450000000001",
                            FechaVencimientoSecuencia = "31-12-2028",
                            IndicadorMontoGravado = "0",
                            TipoIngresos = "01",
                            TipoPago = "1"
                        },
                        Emisor = new EmisorModel13
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010101",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORT",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel13
                        {
                            RNCComprador = "131880657",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        InformacionesAdicionales = new InformacionesAdicionales13
                        {
                            NumeroContenedor = "8019289",
                            NumeroReferencia = "1447"
                        },
                        Totales = new TotalesModel13
                        {
                            MontoGravadoTotal = "30000.00",
                            MontoGravadoI1 = "30000.00",
                            ITBIS1 = "18",
                            TotalITBIS = "5400.00",
                            TotalITBIS1 = "5400.00",
                            MontoTotal = "35400.00",
                            ValorPagar = "35400.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel13
                    {
                        Item = new List<ItemModel13>
                        {
                            new ItemModel13
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "1",
                                NombreItem = "SERVICIO PUBLICIDAD",
                                IndicadorBienoServicio = "2",
                                DescripcionItem = "prestación de servicios relacionados con la creación, ejecución y distribución de campañas publicitarias.",
                                CantidadItem = "1.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "30000.00",
                                MontoItem = "30000.00"
                            }
                        }
                    }
                }
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE45(FacturaDGIIModel13 model)
        {

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubDescuento?.SubDescuento != null)
                {
                    item.TablaSubDescuento.SubDescuento = item.TablaSubDescuento.SubDescuento
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubDescuento) && !string.IsNullOrWhiteSpace(ci.MontoSubDescuento))
                        .ToList();

                    if (!item.TablaSubDescuento.SubDescuento.Any())
                    {
                        item.TablaSubDescuento = null;
                    }
                }
            }

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                if (item.TablaSubRecargo?.SubRecargo != null)
                {
                    item.TablaSubRecargo.SubRecargo = item.TablaSubRecargo.SubRecargo
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubRecargo) && !string.IsNullOrWhiteSpace(ci.MontoSubRecargo))
                        .ToList();

                    if (!item.TablaSubRecargo.SubRecargo.Any())
                    {
                        item.TablaSubRecargo = null;
                    }
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE450000000007()
        {
            var model = new FacturaDGIIModel13
            {
                ECF = new ECFModel13
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel13
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel13
                        {
                            TipoeCF = "",
                            eNCF = "E450000000007",
                            FechaVencimientoSecuencia = "31-12-2028",
                            IndicadorMontoGravado = "0",
                            TipoIngresos = "01",
                            TipoPago = "1"
                        },
                        Emisor = new EmisorModel13
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel13
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        InformacionesAdicionales = new InformacionesAdicionales13
                        {
                            FechaEmbarque = "08-06-2020",
                            NumeroEmbarque = "1550523",
                            NumeroContenedor = "1025536",
                            NumeroReferencia = "121517",
                            PesoBruto = "25.00",
                            PesoNeto = "24.50",
                            UnidadPesoBruto = "23",  
                            UnidadPesoNeto = "23",
                            CantidadBulto = "1.00",    
                            UnidadBulto = "6",
                            VolumenBulto = "1.00",
                            UnidadVolumen = "6"
                        },
                        Totales = new TotalesModel13
                        {
                            MontoGravadoTotal = "180000.00",
                            MontoGravadoI1 = "180000.00",
                            ITBIS1 = "18",
                            TotalITBIS = "49457.67",
                            TotalITBIS1 = "49457.67",
                            MontoImpuestoAdicional = "94764.83",
                            ImpuestosAdicionales = new ImpuestosAdicionalesModel13
                            {
                                ImpuestoAdicional = new List<ImpuestoAdicionalTotalesModel13>
                                {
                                    new ImpuestoAdicionalTotalesModel13
                                    {
                                        TipoImpuesto = "006",
                                        TasaImpuestoAdicional = "633.85",
                                        MontoImpuestoSelectivoConsumoEspecifico = "54004.02"
                                    },
                                    new ImpuestoAdicionalTotalesModel13
                                    {
                                        TipoImpuesto = "023",
                                        TasaImpuestoAdicional = "10",
                                        MontoImpuestoSelectivoConsumoAdvalorem = "40760.81"
                                    }
                                }
                            },
                            MontoTotal = "324222.49",
                            MontoPeriodo = "324222.49",
                            ValorPagar = "324222.49"
                        }
                    },
                    DetallesItems = new DetallesItemsModel13
                    {
                        Item = new List<ItemModel13>
                        {
                            new ItemModel13
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "1",
                                NombreItem = "PTE. CJ 24/12OZ",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "20.00",
                                UnidadMedida = "6",
                                CantidadReferencia = "24",
                                UnidadReferencia = "5",
                                TablaSubcantidad = new TablaSubcantidadModel13
                                {
                                    SubcantidadItem = new List<SubcantidadItemModel13>
                                    {
                                        new SubcantidadItemModel13
                                        {
                                            Subcantidad = "0.355",
                                            CodigoSubcantidad = "24",
                                        }
                                    }
                                },
                                PrecioUnitarioItem = "900.0000",
                                MontoItem = "18000.00",
                                TablaImpuestoAdicional = new TablaImpuestoAdicionalModel13
                                {
                                    ImpuestoAdicional = new List<ImpuestoAdicionalItemModel13>
                                    {
                                        new ImpuestoAdicionalItemModel13 { TipoImpuesto = "006" },
                                        new ImpuestoAdicionalItemModel13 { TipoImpuesto = "023" }
                                    }
                                },
                                OtraMonedaDetalle = new OtraMonedaDetalle13
                                {
                                    PrecioOtraMoneda = "16.27",
                                    MontoItemOtraMoneda = "325.50"
                                },
                                GradosAlcohol = "5.00",
                                PrecioUnitarioReferencia = "65.00",
                            },
                            new ItemModel13
                            {
                                NumeroLinea = "2",
                                IndicadorFacturacion = "1",
                                NombreItem = "PTE. CJ 48/12OZ",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "40.00",
                                UnidadMedida = "6",
                                CantidadReferencia = "48",
                                UnidadReferencia = "5",
                                TablaSubcantidad = new TablaSubcantidadModel13
                                {
                                    SubcantidadItem = new List<SubcantidadItemModel13>
                                    {
                                        new SubcantidadItemModel13
                                        {
                                            Subcantidad = "0.355",
                                            CodigoSubcantidad = "24",
                                        }
                                    }
                                },
                                PrecioUnitarioItem = "1800.0000",
                                MontoItem = "72000.00",
                                TablaImpuestoAdicional = new TablaImpuestoAdicionalModel13
                                {
                                    ImpuestoAdicional = new List<ImpuestoAdicionalItemModel13>
                                    {
                                        new ImpuestoAdicionalItemModel13 { TipoImpuesto = "006" },
                                        new ImpuestoAdicionalItemModel13 { TipoImpuesto = "023" }
                                    }
                                },
                                OtraMonedaDetalle = new OtraMonedaDetalle13
                                {
                                    PrecioOtraMoneda = "32.55",
                                    MontoItemOtraMoneda = "1301.99"
                                },
                                GradosAlcohol = "5.00",
                                PrecioUnitarioReferencia = "130.00"
                            },
                            new ItemModel13
                            {
                                NumeroLinea = "3",
                                IndicadorFacturacion = "1",
                                NombreItem = "PTE. CJ 48/12OZ",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "50.00",
                                UnidadMedida = "6",
                                CantidadReferencia = "48",
                                UnidadReferencia = "5",
                                TablaSubcantidad = new TablaSubcantidadModel13
                                {
                                    SubcantidadItem = new List<SubcantidadItemModel13>
                                    {
                                        new SubcantidadItemModel13
                                        {
                                            Subcantidad = "0.355",
                                            CodigoSubcantidad = "24",
                                        }
                                    }
                                },
                                PrecioUnitarioItem = "1800.0000",
                                MontoItem = "90000.00",
                                TablaImpuestoAdicional = new TablaImpuestoAdicionalModel13
                                {
                                    ImpuestoAdicional = new List<ImpuestoAdicionalItemModel13>
                                    {
                                        new ImpuestoAdicionalItemModel13 { TipoImpuesto = "006" },
                                        new ImpuestoAdicionalItemModel13 { TipoImpuesto = "023" }
                                    }
                                },
                                OtraMonedaDetalle = new OtraMonedaDetalle13
                                {
                                    PrecioOtraMoneda = "32.55",
                                    MontoItemOtraMoneda = "1627.49"
                                },
                                GradosAlcohol = "5.00",
                                PrecioUnitarioReferencia = "130.00"
                            }
                        }
                    }
                }
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE450000000007(FacturaDGIIModel13 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE450000000010()
        {
            var model = new FacturaDGIIModel13
            {
                ECF = new ECFModel13
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel13
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel13
                        {
                            TipoeCF = "",
                            eNCF = "E450000000010",
                            FechaVencimientoSecuencia = "31-12-2028",
                            IndicadorMontoGravado = "0",
                            TipoIngresos = "01",
                            TipoPago = "1"
                        },
                        Emisor = new EmisorModel13
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel13
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        Totales = new TotalesModel13
                        {
                            MontoGravadoTotal = "794000.00",
                            MontoGravadoI1 = "794000.00",
                            ITBIS1 = "18",
                            TotalITBIS = "142920.00",
                            TotalITBIS1 = "142920.00",
                            MontoTotal = "936920.00",
                            MontoPeriodo = "936920.00",
                            ValorPagar = "936920.00"
                        },
                        OtraMoneda = new OtraMoneda13
                        {
                            TipoMoneda = "USD",
                            TipoCambio = "56.3000",
                            MontoGravadoTotalOtraMoneda = "14103.02",
                            MontoGravado1OtraMoneda = "14103.02",
                            TotalITBISOtraMoneda = "2538.54",
                            TotalITBIS1OtraMoneda = "2538.54",
                            MontoTotalOtraMoneda = "16641.56"
                        }
                    },
                    DetallesItems = new DetallesItemsModel13
                    {
                        Item = new List<ItemModel13>
                        {
                            new ItemModel13
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "1",
                                NombreItem = "RADIO CASETTE",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "20.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "1500.0000",
                                MontoItem = "30000.00",
                                OtraMonedaDetalle = new OtraMonedaDetalle13
                                {
                                    PrecioOtraMoneda = "26.64",
                                    MontoItemOtraMoneda = "532.86"
                                }
                            },
                            new ItemModel13
                            {
                                NumeroLinea = "2",
                                IndicadorFacturacion = "1",
                                NombreItem = "VIDEO GRABADORA",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "20.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "2500.0000",
                                MontoItem = "50000.00",
                                OtraMonedaDetalle = new OtraMonedaDetalle13
                                {
                                    PrecioOtraMoneda = "44.40",
                                    MontoItemOtraMoneda = "888.10"
                                }
                            },
                            new ItemModel13
                            {
                                NumeroLinea = "3",
                                IndicadorFacturacion = "1",
                                NombreItem = "BOCINAS",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "20.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "3700.0000",
                                MontoItem = "74000.00",
                                OtraMonedaDetalle = new OtraMonedaDetalle13
                                {
                                    PrecioOtraMoneda = "65.72",
                                    MontoItemOtraMoneda = "1314.39"
                                }
                            },
                            new ItemModel13
                            {
                                NumeroLinea = "4",
                                IndicadorFacturacion = "1",
                                NombreItem = "ABANICOS",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "20.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "4500.0000",
                                MontoItem = "90000.00",
                                OtraMonedaDetalle = new OtraMonedaDetalle13
                                {
                                    PrecioOtraMoneda = "79.93",
                                    MontoItemOtraMoneda = "1598.58"
                                }
                            },
                            new ItemModel13
                            {
                                NumeroLinea = "5",
                                IndicadorFacturacion = "1",
                                NombreItem = "CABLES ELECTRONICOS",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "20.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "3750.0000",
                                MontoItem = "75000.00",
                                OtraMonedaDetalle = new OtraMonedaDetalle13
                                {
                                    PrecioOtraMoneda = "66.61",
                                    MontoItemOtraMoneda = "1332.15"
                                }
                            },
                            new ItemModel13
                            {
                                NumeroLinea = "6",
                                IndicadorFacturacion = "1",
                                NombreItem = "NEVERA NEDOCA",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "20.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "4000.0000",
                                MontoItem = "80000.00",
                                OtraMonedaDetalle = new OtraMonedaDetalle13
                                {
                                    PrecioOtraMoneda = "71.05",
                                    MontoItemOtraMoneda = "1420.96"
                                }
                            },
                            new ItemModel13
                            {
                                NumeroLinea = "7",
                                IndicadorFacturacion = "1",
                                NombreItem = "ESTUFA",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "20.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "3700.0000",
                                MontoItem = "74000.00",
                                OtraMonedaDetalle = new OtraMonedaDetalle13
                                {
                                    PrecioOtraMoneda = "65.72",
                                    MontoItemOtraMoneda = "1314.39"
                                }
                            },
                            new ItemModel13
                            {
                                NumeroLinea = "8",
                                IndicadorFacturacion = "1",
                                NombreItem = "LICUADORA",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "20.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "4500.0000",
                                MontoItem = "90000.00",
                                OtraMonedaDetalle = new OtraMonedaDetalle13
                                {
                                    PrecioOtraMoneda = "79.93",
                                    MontoItemOtraMoneda = "1598.58"
                                }
                            },
                            new ItemModel13
                            {
                                NumeroLinea = "9",
                                IndicadorFacturacion = "1",
                                NombreItem = "TOSTADORA",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "20.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "4550.0000",
                                MontoItem = "91000.00",
                                OtraMonedaDetalle = new OtraMonedaDetalle13
                                {
                                    PrecioOtraMoneda = "80.82",
                                    MontoItemOtraMoneda = "1616.34"
                                }
                            },
                            new ItemModel13
                            {
                                NumeroLinea = "10",
                                IndicadorFacturacion = "1",
                                NombreItem = "MICROONDAS",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "20.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "7000.0000",
                                MontoItem = "140000.00",
                                OtraMonedaDetalle = new OtraMonedaDetalle13
                                {
                                    PrecioOtraMoneda = "124.33",
                                    MontoItemOtraMoneda = "2486.68"
                                }
                            }
                        }
                    }
                }
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE450000000010(FacturaDGIIModel13 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            if (model?.ECF?.DetallesItems?.Item != null)
            {
                foreach (var item in model.ECF.DetallesItems.Item)
                {
                    // 1. Limpieza de TablaSubcantidad
                    if (item.TablaSubcantidad?.SubcantidadItem != null)
                    {
                        item.TablaSubcantidad.SubcantidadItem = item.TablaSubcantidad.SubcantidadItem
                            .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.Subcantidad) && !string.IsNullOrWhiteSpace(ci.CodigoSubcantidad))
                            .ToList();

                        if (!item.TablaSubcantidad.SubcantidadItem.Any())
                        {
                            item.TablaSubcantidad = null;
                        }
                    }

                    // 2. Limpieza de TablaImpuestoAdicional
                    if (item.TablaImpuestoAdicional?.ImpuestoAdicional != null)
                    {
                        item.TablaImpuestoAdicional.ImpuestoAdicional = item.TablaImpuestoAdicional.ImpuestoAdicional
                            .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoImpuesto))
                            .ToList();

                        if (!item.TablaImpuestoAdicional.ImpuestoAdicional.Any())
                        {
                            item.TablaImpuestoAdicional = null;
                        }
                    }

                    // 3. Tu lógica de TablaSubDescuento
                    if (item.TablaSubDescuento?.SubDescuento != null)
                    {
                        item.TablaSubDescuento.SubDescuento = item.TablaSubDescuento.SubDescuento
                            .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubDescuento) && !string.IsNullOrWhiteSpace(ci.MontoSubDescuento))
                            .ToList();

                        if (!item.TablaSubDescuento.SubDescuento.Any())
                        {
                            item.TablaSubDescuento = null;
                        }
                    }

                    // 4. Tu lógica de TablaSubRecargo
                    if (item.TablaSubRecargo?.SubRecargo != null)
                    {
                        item.TablaSubRecargo.SubRecargo = item.TablaSubRecargo.SubRecargo
                            .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubRecargo) && !string.IsNullOrWhiteSpace(ci.MontoSubRecargo))
                            .ToList();

                        if (!item.TablaSubRecargo.SubRecargo.Any())
                        {
                            item.TablaSubRecargo = null;
                        }
                    }
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE46()
        {
            var model = new FacturaDGIIModel14
            {
                ECF = new ECFModel14
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel14
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel14
                        {
                            TipoeCF = "",
                            eNCF = "E460000000001",
                            FechaVencimientoSecuencia = "31-12-2028",
                            TipoIngresos = "01",
                            TipoPago = "2",
                            FechaLimitePago = "06-05-2020",
                            TerminoPago = "1 mes",
                            TablaFormasPago = new TablaFormasPago14
                            {
                                FormaDePago = new List<FormaDePago14>
                        {
                            new FormaDePago14
                            {
                                FormaPago = "2",
                                MontoPago = "1800000.00"
                            }
                        }
                            }
                        },
                        Emisor = new EmisorModel14
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010101",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel14
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "ZONA FRANCA LOI",
                            ContactoComprador = "MARCOS LLUBERES",
                            CorreoComprador = "MARCOSLLUBERES@KKKK.COM",
                            DireccionComprador = "ZONA HAINA",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "07-04-2020",
                            ContactoEntrega = "JACINTO MANON",
                            DireccionEntrega = "ZONA HAINA",
                            TelefonoAdicional = "809-555-5050",
                            FechaOrdenCompra = "10-03-2020",
                            NumeroOrdenCompra = "4500352230",
                            CodigoInternoComprador = "10633441"
                        },
                        InformacionesAdicionales = new InformacionesAdicionales14
                        {
                            FechaEmbarque = "10-04-2020",
                            NumeroEmbarque = "10010-1207-000254",
                            NumeroContenedor = "ERTY227958722",
                            NumeroReferencia = "1448",
                            NombrePuertoEmbarque = "ZONA HAINA",
                            CondicionesEntrega = "FOB",
                            TotalFob = "1800.00",
                            Seguro = "250.00",
                            Flete = "22.00",
                            TotalCif = "2000.00",
                            RegimenAduanero = "EXPORTACION NACIONAL",
                            NombrePuertoSalida = "DOSDQ",
                            NombrePuertoDesembarque = "PTO RICO",
                            PesoBruto = "25000.00",
                            PesoNeto = "24878.00",
                            UnidadPesoBruto = "21",
                            UnidadPesoNeto = "21",
                            CantidadBulto = "250.00",
                            UnidadBulto = "25",
                            VolumenBulto = "45",
                            UnidadVolumen = "27"
                        },
                        Transporte = new Transporte14
                        {
                            ViaTransporte = "02",
                            PaisOrigen = "REPUBLICA DOMINICANA",
                            DireccionDestino = "CALLE GUALLUBI NO. 09",
                            PaisDestino = "PUERTO RICO",
                            NumeroAlbaran = "56789UJILLL"
                        },
                        Totales = new TotalesModel14
                        {
                            MontoGravadoTotal = "1800000.00",
                            MontoGravadoI3 = "1800000.00",
                            ITBIS3 = "0",
                            TotalITBIS = "0.00",
                            TotalITBIS3 = "0.00",
                            MontoTotal = "1800000.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel14
                    {
                        Item = new List<ItemModel14>
                {
                    new ItemModel14
                    {
                        NumeroLinea = "1",
                        TablaCodigosItem = new TablaCodigosItem14
                        {
                            CodigosItem = new List<CodigosItem14>
                            {
                                new CodigosItem14
                                {
                                    TipoCodigo = "INTERNA",
                                    CodigoItem = "123456"
                                }
                            }
                        },
                        IndicadorFacturacion = "3",
                        NombreItem = "AGUACATE CRIOLLO",
                        IndicadorBienoServicio = "1",
                        CantidadItem = "100.00",
                        UnidadMedida = "43",
                        PrecioUnitarioItem = "18000.00",
                        MontoItem = "1800000.00"
                    }
                }
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE46(FacturaDGIIModel14 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE460000000007()
        {
            var model = new FacturaDGIIModel14
            {
                ECF = new ECFModel14
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel14
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel14
                        {
                            TipoeCF = "",
                            eNCF = "E460000000007",
                            FechaVencimientoSecuencia = "31-12-2028",
                            TipoIngresos = "01",
                            TipoPago = "1",
                            TablaFormasPago = new TablaFormasPago14
                            {
                                FormaDePago = new List<FormaDePago14>
                                {
                                    new FormaDePago14
                                    {
                                        FormaPago = "1",
                                        MontoPago = "117500.00"
                                    }
                                }
                            }
                        },
                        Emisor = new EmisorModel14
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            CodigoVendedor = "AA0000000100000000010000000002000000000300000000050000000006",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            ZonaVenta = "NORTE",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel14
                        {
                            RNCComprador = "131880681",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03",
                            ContactoComprador = "MARCOS LATIPLOL",
                            CorreoComprador = "MARCOSLATIPLOL@KKKK.COM",
                            DireccionComprador = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
                            MunicipioComprador = "010100",
                            ProvinciaComprador = "010000",
                            FechaEntrega = "10-10-2020",
                            FechaOrdenCompra = "10-11-2018",
                            NumeroOrdenCompra = "4500352238",
                            CodigoInternoComprador = "10633440"
                        },
                        Totales = new TotalesModel14
                        {
                            MontoGravadoTotal = "117500.00",
                            MontoGravadoI3 = "117500.00",
                            ITBIS3 = "0",
                            TotalITBIS = "0.00",
                            TotalITBIS3 = "0.00",
                            MontoTotal = "117500.00",
                            MontoPeriodo = "117500.00",
                            ValorPagar = "117500.00"
                        },
                        OtraMoneda = new OtraMoneda14
                        {
                            TipoMoneda = "USD",
                            TipoCambio = "57.0000",
                            MontoGravadoTotalOtraMoneda = "2061.40",
                            MontoGravado3OtraMoneda = "2061.40",
                            TotalITBISOtraMoneda = "0.00",
                            TotalITBIS3OtraMoneda = "0.00",
                            MontoTotalOtraMoneda = "2061.40",
                        },
                    },
                    DetallesItems = new DetallesItemsModel14
                    {
                        Item = new List<ItemModel14>
                        {
                            new ItemModel14
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "3",
                                NombreItem = "Silla",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "150.00",
                                UnidadMedida = "6",
                                PrecioUnitarioItem = "450.0000",
                                MontoItem = "67500.00",
                                OtraMonedaDetalle = new OtraMonedaDetalle14
                                {
                                    PrecioOtraMoneda = "7.89",
                                    MontoItemOtraMoneda = "1184.21"
                                }
                            },
                            new ItemModel14
                            {
                                NumeroLinea = "2",
                                IndicadorFacturacion = "3",
                                NombreItem = "Mesa",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "100.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "200.0000",
                                MontoItem = "20000.00",
                                OtraMonedaDetalle = new OtraMonedaDetalle14
                                {
                                    PrecioOtraMoneda = "3.51",
                                    MontoItemOtraMoneda = "350.88"
                                }
                            },
                            new ItemModel14
                            {
                                NumeroLinea = "3",
                                IndicadorFacturacion = "3",
                                NombreItem = "Manteles",
                                IndicadorBienoServicio = "1",
                                CantidadItem = "120.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "250.0000",
                                MontoItem = "30000.00",
                                OtraMonedaDetalle = new OtraMonedaDetalle14
                                {
                                    PrecioOtraMoneda = "4.39",
                                    MontoItemOtraMoneda = "526.32"
                                }
                            }
                        }
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE460000000007(FacturaDGIIModel14 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE47()
        {
            var model = new FacturaDGIIModel15
            {
                ECF = new ECFModel15
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel15
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel15
                        {
                            TipoeCF = "",
                            eNCF = "E470000000001",
                            FechaVencimientoSecuencia = "31-12-2028",
                            NumeroCuentaPago = "BB00058745214789635111111111",
                            BancoPago = "BB0111111111111111111111111111111111111111111111111111111111111111111111111"
                        },
                        Emisor = new EmisorModel15
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010101",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel15
                        {
                            IdentificadorExtranjero = "533445888",
                            RazonSocialComprador = "ALEJA FERMIN SANTOS"
                        },
                        Totales = new TotalesModel15
                        {
                            MontoExento = "180000.00",
                            MontoTotal = "180000.00",
                            TotalISRRetencion = "48600.00"
                        },
                        OtraMoneda = new OtraMoneda15
                        {
                            TipoMoneda = "USD",
                            TipoCambio = "60.0000",
                            MontoExentoOtraMoneda = "3000.00",
                            MontoTotalOtraMoneda = "3000.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel15
                    {
                        Item = new List<ItemModel15>
                        {
                            new ItemModel15
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "4",
                                NombreItem = "LICENCIA WYI",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "1.00",
                                UnidadMedida = "43",
                                PrecioUnitarioItem = "180000.00",
                                MontoItem = "180000.00",
                                Retencion = new RetencionItem15
                                {
                                    IndicadorAgenteRetencionoPercepcion = "1",
                                    MontoISRRetenido = "48600.00"
                                },
                                OtraMonedaDetalle = new OtraMonedaDetalle15
                                {
                                    PrecioOtraMoneda = "3000.0000",
                                    MontoItemOtraMoneda = "3000.00"
                                }
                            }
                        }
                    },
                    Subtotales = new Subtotales15
                    {
                        Subtotal = new List<Subtotal15>
                        {
                            new Subtotal15
                            {
                                NumeroSubTotal = "1",
                                DescripcionSubtotal = "N/A",
                                Orden = "1",
                                SubTotalExento = "180000.00",
                                MontoSubTotal = "180000.00",
                                Lineas = "1"
                            }
                        }
                    }
                }
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE47(FacturaDGIIModel15 model)
        {

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE470000000010()
        {
            var model = new FacturaDGIIModel15
            {
                ECF = new ECFModel15
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel15
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel15
                        {
                            TipoeCF = "",
                            eNCF = "E470000000010",
                            FechaVencimientoSecuencia = "31-12-2028",
                            TipoPago = "1",
                            //TablaFormasPago = new TablaFormasPago15
                            //{
                            //    FormaDePago = new List<FormaDePago15>
                            //    {
                            //        new FormaDePago15
                            //        {
                            //            FormaPago = "1",
                            //            MontoPago = "347100.00"
                            //        }
                            //    }
                            //}
                        },
                        Emisor = new EmisorModel15
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel15
                        {
                            IdentificadorExtranjero = "350555123",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03"
                        },
                        Totales = new TotalesModel15
                        {
                            MontoExento = "348000.00",
                            MontoTotal = "348000.00",
                            MontoPeriodo = "348000.00",
                            ValorPagar = "348000.00",
                            TotalISRRetencion = "93960.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel15
                    {
                        Item = new List<ItemModel15>
                        {
                            new ItemModel15
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "4",
                                NombreItem = "Asesoria Legal P/H",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "24.00",
                                UnidadMedida = "19",
                                PrecioUnitarioItem = "1500.0000",
                                MontoItem = "36000.00",
                                Retencion = new RetencionItem15
                                {
                                    IndicadorAgenteRetencionoPercepcion = "1",
                                    MontoISRRetenido = "9720.00"
                                }
                            },
                            new ItemModel15
                            {
                                NumeroLinea = "2",
                                IndicadorFacturacion = "4",
                                NombreItem = "Asesoria Legal P/H",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "48.00",
                                UnidadMedida = "19",
                                PrecioUnitarioItem = "2500.0000",
                                MontoItem = "120000.00",
                                Retencion = new RetencionItem15
                                {
                                    IndicadorAgenteRetencionoPercepcion = "1",
                                    MontoISRRetenido = "32400.00"
                                }
                            },
                            new ItemModel15
                            {
                                NumeroLinea = "3",
                                IndicadorFacturacion = "4",
                                NombreItem = "Asesoria Legal P/H",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "64.00",
                                UnidadMedida = "19",
                                PrecioUnitarioItem = "3000.0000",
                                MontoItem = "192000.00",
                                Retencion = new RetencionItem15
                                {
                                    IndicadorAgenteRetencionoPercepcion = "1",
                                    MontoISRRetenido = "51840.00"
                                }
                            }
                        }
                    }
                }
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE470000000010(FacturaDGIIModel15 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }
            if (model?.ECF?.DetallesItems?.Item != null)
            {
                foreach (var item in model.ECF.DetallesItems.Item)
                {
                    // Limpieza de OtraMonedaDetalle
                    if (item.OtraMonedaDetalle != null)
                    {
                        // Si ambos campos están vacíos o son nulos, anulamos el objeto completo
                        if (string.IsNullOrWhiteSpace(item.OtraMonedaDetalle.PrecioOtraMoneda) &&
                            string.IsNullOrWhiteSpace(item.OtraMonedaDetalle.MontoItemOtraMoneda))
                        {
                            item.OtraMonedaDetalle = null;
                        }
                    }
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        [HttpGet]
        public IActionResult comprobanteE470000000009()
        {
            var model = new FacturaDGIIModel15
            {
                ECF = new ECFModel15
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel15
                    {
                        Version = "",
                        IdDoc = new VersionIdDocModel15
                        {
                            TipoeCF = "",
                            eNCF = "E470000000009",
                            FechaVencimientoSecuencia = "31-12-2028",
                            TipoPago = "1",
                        },
                        Emisor = new EmisorModel15
                        {
                            RNCEmisor = "130322791",
                            RazonSocialEmisor = "DOCUMENTOS ELECTRONICOS DE 02",
                            NombreComercial = "DOCUMENTOS ELECTRONICOS DE 02",
                            DireccionEmisor = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
                            Municipio = "010100",
                            Provincia = "010000",
                            CorreoEmisor = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
                            WebSite = "www.facturaelectronica.com",
                            NumeroFacturaInterna = "123456789016",
                            NumeroPedidoInterno = "123456789016",
                            FechaEmision = "01-04-2020"
                        },
                        Comprador = new CompradorModel15
                        {
                            IdentificadorExtranjero = "350555123",
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 03"
                        },
                        Totales = new TotalesModel15
                        {
                            MontoExento = "66000.00",
                            MontoTotal = "66000.00",
                            MontoPeriodo = "66000.00",
                            ValorPagar = "66000.00",
                            TotalISRRetencion = "17820.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel15
                    {
                        Item = new List<ItemModel15>
                        {
                            new ItemModel15
                            {
                                NumeroLinea = "1",
                                IndicadorFacturacion = "4",
                                NombreItem = "Asesoria Legal P/H",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "60.00",
                                UnidadMedida = "19",
                                PrecioUnitarioItem = "450.0000",
                                MontoItem = "27000.00",
                                Retencion = new RetencionItem15
                                {
                                    IndicadorAgenteRetencionoPercepcion = "1",
                                    MontoISRRetenido = "7290.00"
                                }
                            },
                            new ItemModel15
                            {
                                NumeroLinea = "2",
                                IndicadorFacturacion = "4",
                                NombreItem = "Asesoria Legal P/H",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "70.00",
                                UnidadMedida = "19",
                                PrecioUnitarioItem = "200.0000",
                                MontoItem = "14000.00",
                                Retencion = new RetencionItem15
                                {
                                    IndicadorAgenteRetencionoPercepcion = "1",
                                    MontoISRRetenido = "3780.00"
                                }
                            },
                            new ItemModel15
                            {
                                NumeroLinea = "3",
                                IndicadorFacturacion = "4",
                                NombreItem = "Asesoria Legal P/H",
                                IndicadorBienoServicio = "2",
                                CantidadItem = "100.00",
                                UnidadMedida = "19",
                                PrecioUnitarioItem = "250.0000",
                                MontoItem = "25000.00",
                                Retencion = new RetencionItem15
                                {
                                    IndicadorAgenteRetencionoPercepcion = "1",
                                    MontoISRRetenido = "6750.00"
                                }
                            }
                        }
                    }
                }
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult comprobanteE470000000009(FacturaDGIIModel15 model)
        {
            if (model?.ECF?.InformacionReferencia != null)
            {
                if (string.IsNullOrWhiteSpace(model.ECF.InformacionReferencia.NCFModificado))
                {
                    model.ECF.InformacionReferencia = null;
                }
            }
            if (model?.ECF?.DetallesItems?.Item != null)
            {
                foreach (var item in model.ECF.DetallesItems.Item)
                {
                    // Limpieza de OtraMonedaDetalle
                    if (item.OtraMonedaDetalle != null)
                    {
                        // Si ambos campos están vacíos o son nulos, anulamos el objeto completo
                        if (string.IsNullOrWhiteSpace(item.OtraMonedaDetalle.PrecioOtraMoneda) &&
                            string.IsNullOrWhiteSpace(item.OtraMonedaDetalle.MontoItemOtraMoneda))
                        {
                            item.OtraMonedaDetalle = null;
                        }
                    }
                }
            }

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                string mensajeValor = jsonObjectResponse["mensajes"]?[0]?["valor"]?.ToString();


                var respuesta = new FacturaDGIIResponseModel
                {
                    JsonInvoice = jsonObject.GetValue("json")?.ToString(),
                    ENCF = jsonObject.GetValue("encf")?.ToString(),
                    XmlSemilla = jsonObject.GetValue("xmlsemilla")?.ToString(),
                    XmlSemillaFirmada = jsonObject.GetValue("xmlsemillafirmada")?.ToString(),
                    Token = jsonObject.GetValue("token")?.ToString(),
                    XmlFactura = jsonObject.GetValue("xmlfactura")?.ToString(),
                    XmlFacturaFirmada = jsonObject.GetValue("xmlfacturafirmada")?.ToString(),
                    CodigoSeguridad = jsonObject.GetValue("codigoseguridad")?.ToString(),
                    CodigoRespuesta = jsonObjectResponse.GetValue("codigo")?.ToString(),
                    EstadoRespuesta = jsonObjectResponse.GetValue("estado")?.ToString(),
                    Mensaje = mensajeValor

                };

                var registro = new FacturasDGII
                {
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    RNCEmisor = model?.ECF?.Encabezado?.Emisor?.RNCEmisor,
                    RazonSocialEmisor = model?.ECF?.Encabezado?.Emisor?.RazonSocialEmisor,
                    NombreComercial = model?.ECF?.Encabezado?.Emisor?.NombreComercial,
                    DireccionEmisor = model?.ECF?.Encabezado?.Emisor?.DireccionEmisor,
                    Municipio = model?.ECF?.Encabezado?.Emisor?.Municipio,
                    Provincia = model?.ECF?.Encabezado?.Emisor?.Provincia,
                    CorreoEmisor = model?.ECF?.Encabezado?.Emisor?.CorreoEmisor,
                    WebSite = model?.ECF?.Encabezado?.Emisor?.WebSite,
                    CodigoVendedor = model?.ECF?.Encabezado?.Emisor?.CodigoVendedor,
                    NumeroFacturaInterna = model?.ECF?.Encabezado?.Emisor?.NumeroFacturaInterna,
                    NumeroPedidoInterno = model?.ECF?.Encabezado?.Emisor?.NumeroPedidoInterno,
                    ZonaVenta = model?.ECF?.Encabezado?.Emisor?.ZonaVenta,
                    FechaEmision = model?.ECF?.Encabezado?.Emisor?.FechaEmision,

                    RNCComprador = model?.ECF?.Encabezado?.Comprador?.RNCComprador,
                    RazonSocialComprador = model?.ECF?.Encabezado?.Comprador?.RazonSocialComprador,
                    ContactoComprador = model?.ECF?.Encabezado?.Comprador?.ContactoComprador,
                    CorreoComprador = model?.ECF?.Encabezado?.Comprador?.CorreoComprador,
                    DireccionComprador = model?.ECF?.Encabezado?.Comprador?.DireccionComprador,
                    MunicipioComprador = model?.ECF?.Encabezado?.Comprador?.MunicipioComprador,
                    ProvinciaComprador = model?.ECF?.Encabezado?.Comprador?.ProvinciaComprador,
                    FechaEntrega = model?.ECF?.Encabezado?.Comprador?.FechaEntrega,
                    FechaOrdenCompra = model?.ECF?.Encabezado?.Comprador?.FechaOrdenCompra,
                    NumeroOrdenCompra = model?.ECF?.Encabezado?.Comprador?.NumeroOrdenCompra,
                    CodigoInternoComprador = model?.ECF?.Encabezado?.Comprador?.CodigoInternoComprador,

                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    NCFModificado = model?.ECF?.InformacionReferencia?.NCFModificado ?? "",

                    FechaHoraFirma = model?.ECF?.FechaHoraFirma,
                    FechaRegistro = DateTime.Now
                };


                _context.FacturasDGII.Add(registro);
                _context.SaveChanges();

                respuesta.FacturaId = registro.Id;

                if (model?.ECF?.DetallesItems?.Item != null)
                {
                    foreach (var item in model.ECF.DetallesItems.Item)
                    {
                        var detalle = new ItemFactura
                        {
                            FacturaId = registro.Id,
                            NumeroLinea = item.NumeroLinea,
                            IndicadorFacturacion = item.IndicadorFacturacion,
                            NombreItem = item.NombreItem,
                            IndicadorBienoServicio = item.IndicadorBienoServicio,
                            CantidadItem = Convert.ToDecimal(item.CantidadItem ?? "0"),
                            UnidadMedida = item.UnidadMedida,
                            PrecioUnitarioItem = Convert.ToDecimal(item.PrecioUnitarioItem ?? "0"),
                            MontoItem = Convert.ToDecimal(item.MontoItem ?? "0")
                        };

                        _context.ItemsFactura.Add(detalle);
                    }
                }
                _context.SaveChanges();

                if (respuesta.CodigoRespuesta == "1")
                {
                    return View("verFactura", respuesta);

                }
                else
                {
                    ViewBag.MensajeError = respuesta.Mensaje;
                    return View("verFactura", respuesta);
                }

            }
            catch (DbUpdateException ex)
            {
                string error = ex.Message;

                if (ex.InnerException != null)
                    error += " | Inner Exception: " + ex.InnerException.Message;

                ViewBag.Error = error;
                return View(null);
            }

        }

        public IActionResult RegistrarEmisor()
        {
            return View();
        }

        public IActionResult ProbarCertificado()
        {
            string thumbprint = "5F5017E1810EBEAF9DAE0AD482C252F4AC19CA91";
            var resultado = FacturacionElectronicaDGII.FindCertificateFromWINDOWS(thumbprint);

            var model = new CertCheckResult
            {
                Existe = resultado.Existe,
                Mensaje = resultado.Mensaje,
                Subject = resultado.Subject,
                Thumbprint = resultado.Thumbprint
            };

            return View(model);
        }

        public IActionResult ListarCertificados()
        {
            var listaDGII = FacturacionElectronicaDGII.ListAllCertificates();

            var listaMVC = listaDGII.Select(c => new CertCheckResult
            {
                Existe = c.Existe,
                Mensaje = c.Mensaje,
                Subject = c.Subject,
                Thumbprint = c.Thumbprint
            }).ToList();

            return View(listaMVC);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarEmisor(EmisorInfo emisorInfo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Console.WriteLine($"RNC: {emisorInfo.RNCEmisor}");
                    Console.WriteLine($"Razón Social: {emisorInfo.RazonSocialEmisor}");

                    _context.EmisorInfo.Add(emisorInfo);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Emisor registrado exitosamente!";
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                }
            }

            return View(emisorInfo);
        }

    }
}
