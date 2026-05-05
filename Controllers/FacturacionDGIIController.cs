using ConexionDGII;
using DGIIFacturadorLoginMVCApp.Data;
using DGIIFacturadorLoginMVCApp.Data.Migrations;
using DGIIFacturadorLoginMVCApp.Models;
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

        // Inyectar el contexto vía constructor
        public FacturacionDGIIController(ApplicationDbContext context)
        {
            _context = context;
        }


        public ActionResult RegistrarComprobante()
        {
            return View(); // Vista inicial con el selector
        }

        [HttpGet]
        public IActionResult verFactura()
        {
            // Datos necesarios
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";
            string jsonInvoiceFO = "{ \"facturaDesdeF&O\": \"datos\" }";

            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
                JObject jsonObject = JObject.Parse(invoice);
                JObject jsonObjectResponse = JObject.Parse(response);

                var model = new FacturaDGIIResponseModel
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

                return View(model);
                //return View("NombreDeLaVista", model);

            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(null);
            }
        }

        [HttpGet]
        public IActionResult GenerarPDF(int id, string codigoSeguridad)
        {
            // Obtener la factura desde la base de datos
            var factura = _context.FacturasDGII
                    .Include(f => f.Items)
                    .FirstOrDefault(f => f.Id == id);

            if (factura == null)
                return NotFound();

            byte[] pdfBytes = CrearFacturaPDFInMemory(factura, codigoSeguridad);

            //return File(pdfBytes, "application/pdf", $"Factura_{factura.ENCF}.pdf");
            //return File(pdfBytes, "application/pdf");
            //return Content("mensaje");
            //return File(pdfBytes, "application/pdf", $"Factura_{factura.ENCF}.pdf");

            //return View("verfacturaPDF");
            return File(pdfBytes, "application/pdf", $"Factura_{factura.ENCF}.pdf");

            //return View("verfacturaPDF", $"Factura_{factura.ENCF}.pdf");
            //return RedirectToAction("VerFacturaPDF", new { id = id });

        }

        [HttpGet]
        public IActionResult GenerarPDFinUSD(int id, string codigoSeguridad)
        {
            // Obtener la factura desde la base de datos
            var factura = _context.FacturasDGII
                    .Include(f => f.Items)
                    .FirstOrDefault(f => f.Id == id);

            if (factura == null)
                return NotFound();

            byte[] pdfBytes = CrearFacturaPDFInMemory(factura, codigoSeguridad);

            //return File(pdfBytes, "application/pdf", $"Factura_{factura.ENCF}.pdf");
            //return File(pdfBytes, "application/pdf");
            //return Content("mensaje");
            //return File(pdfBytes, "application/pdf", $"Factura_{factura.ENCF}.pdf");

            //return View("verfacturaPDF");
            return File(pdfBytes, "application/pdf", $"Factura_{factura.ENCF}.pdf");

            //return View("verfacturaPDF", $"Factura_{factura.ENCF}.pdf");
            //return RedirectToAction("VerFacturaPDF", new { id = id });

        }

        private byte[] CrearFacturaPDFInMemory(FacturasDGII factura, string codigoSeguridad)
        {
            using (var ms = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(ms); // ← usar memoria, NO disco
                PdfDocument pdf = new PdfDocument(writer);
                Document doc = new Document(pdf);

                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                PdfFont boldFont2 = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                string logoPath = "C:\\Users\\andersonmgordilloh\\source\\repos\\FacturacionElectronicaDGII\\ArchivosDGII\\logo.jpeg";


                //ImageData imageData = ImageDataFactory.Create(logoPath);
                //Image logo = new Image(imageData);
                //logo.ScaleToFit(150, 150); // Más pequeño
                //logo.SetMarginBottom(5);
                //logo.SetHorizontalAlignment(HorizontalAlignment.LEFT);
                //doc.Add(logo);

                doc.Add(new Paragraph(" "));

                // Crear la tabla con dos columnas más estrechas y espacio en el medio
                Table headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 48, 30, 48 })); // columna izquierda, espaciado, columna derecha
                headerTable.SetWidth(UnitValue.CreatePercentValue(100));
                headerTable.SetMarginBottom(10);
                headerTable.SetFont(boldFont);

                // Cargar el logo
                ImageData imageData = ImageDataFactory.Create(logoPath);
                Image logo = new Image(imageData);
                logo.ScaleToFit(150, 150); // Más pequeño para ajustarse bien dentro de la tabla
                logo.SetMarginBottom(5);
                logo.SetHorizontalAlignment(HorizontalAlignment.LEFT);

                // Celda izquierda - Emisor
                Cell leftCell = new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).SetFont(boldFont);

                leftCell.Add(logo);

                leftCell.Add(new Paragraph("Mora Tapia Peralta & Asociado, SRL").SetFontSize(9));
                leftCell.Add(new Paragraph($"RNC: {factura.RNCEmisor}").SetFontSize(9));
                leftCell.Add(new Paragraph("Dirección: Calle Ciudad Heredia de Costa Rica No.37 Local 303 Hondura La Feria").SetFontSize(9));
                leftCell.Add(new Paragraph("Teléfono: (829)-435-9277").SetFontSize(9));
                leftCell.Add(new Paragraph("Email: mtp@mtpasociados.com").SetFontSize(9));

                // Celda vacía como separador
                Cell spacerCell = new Cell().SetBorder(Border.NO_BORDER);

                // Celda derecha - Factura
                Cell rightCell = new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT).SetFont(boldFont);

                rightCell.Add(
                    new Paragraph("Página 1 de 1")
                        .SetFontSize(9)
                        //.SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                        .SetTextAlignment(TextAlignment.RIGHT)
                        .SetMarginBottom(10) // Espacio de 10 puntos debajo del texto
                );


                rightCell.Add(new Paragraph("Factura de Crédito Fiscal").SetFontSize(11).SetFont(boldFont2));
                rightCell.Add(new Paragraph($"NCF: {factura.ENCF}").SetFontSize(9));
                rightCell.Add(new Paragraph($"Fecha Vencimiento: {factura.FechaVencimientoSecuencia}").SetFontSize(9));
                rightCell.Add(new Paragraph($"Fecha: {factura.FechaEmision}").SetFontSize(9));
                rightCell.Add(new Paragraph($"Número Factura: {factura.NumeroFacturaInterna}").SetFontSize(9));
                //rightCell.Add(new Paragraph($"Orden de venta: {factura.NumeroOrdenCompra}").SetFontSize(9));
                //rightCell.Add(new Paragraph("Condición de pago: x ").SetFontSize(9));
                //rightCell.Add(new Paragraph("Moneda: x ").SetFontSize(9));

                // Agregar las celdas a la tabla
                headerTable.AddCell(leftCell);
                headerTable.AddCell(spacerCell); // espacio entre columnas
                headerTable.AddCell(rightCell);

                // Agregar la tabla al documento
                doc.Add(headerTable);












                //doc.Add(new Paragraph(" "));

                Table clienteTable = new Table(1);
                clienteTable.SetWidth(UnitValue.CreatePercentValue(40)); // Tamaño compacto
                clienteTable.SetHorizontalAlignment(HorizontalAlignment.LEFT); // Alineación izquierda
                clienteTable.SetMarginBottom(10);
                clienteTable.SetBorder(new SolidBorder(0.5f));

                // Celda del encabezado
                clienteTable.AddHeaderCell(new Cell()
                    .Add(new Paragraph("Cliente")
                    .SetFontSize(8)
                    .SetFont(boldFont)
                    .SetTextAlignment(TextAlignment.CENTER))
                    //.SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                    .SetPadding(5)
                );

                // Celdas de contenido
                clienteTable.AddCell(new Cell().Add(new Paragraph($"RNC: {factura.RNCComprador}").SetFontSize(8)).SetBorder(Border.NO_BORDER).SetPadding(2));
                clienteTable.AddCell(new Cell().Add(new Paragraph($"CLIENTE: {factura.RazonSocialComprador}").SetFontSize(8)).SetBorder(Border.NO_BORDER).SetPadding(2));
                clienteTable.AddCell(new Cell().Add(new Paragraph($"DIRECCIÓN: {factura.DireccionComprador}").SetFontSize(8)).SetBorder(Border.NO_BORDER).SetPadding(2));
                //clienteTable.AddCell(new Cell().Add(new Paragraph($"Contacto: {factura.ContactoComprador}").SetFontSize(8)).SetBorder(Border.NO_BORDER).SetPadding(2));
                //clienteTable.AddCell(new Cell().Add(new Paragraph($"Correo: {factura.CorreoComprador}").SetFontSize(8)).SetBorder(Border.NO_BORDER).SetPadding(2));

                doc.Add(clienteTable);








                // 1. Tabla principal (conserva bordes)
                Table table = new Table(UnitValue.CreatePercentArray(new float[] { 10, 30, 20, 20, 20 }))
                    .UseAllAvailableWidth()
                    .SetFontSize(9)
                    .SetFont(boldFont)
                    .SetTextAlignment(TextAlignment.RIGHT);

                // 2. Configurar encabezados (sin bordes visibles)
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


                // 3. Agregar filas de datos (sin bordes visibles)
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



                // 1. Celda de totales (ocupa todas las columnas pero alineada a la derecha)
                Cell totalesCell = new Cell(1, 5)
                    //.SetBorder(Border.NO_BORDER) // Sin borde exhttps://localhost:7088/FacturacionDGII/registrarfacturaE310000000002terior (se manejará en la tabla interna)
                    .SetBorderBottom(Border.NO_BORDER)
                    .SetBorderLeft(Border.NO_BORDER)    // ← Esta línea es clave
                    .SetBorderRight(Border.NO_BORDER)
                    .SetPadding(0) // Eliminar espacio interno
                    .SetMargin(0)  // Eliminar margen
                    .SetTextAlignment(TextAlignment.RIGHT); // Alinear contenido a la derecha

                // 2. Tabla interna para etiquetas y valores (ancho ajustado + bordes)
                Table innerTable = new Table(UnitValue.CreatePercentArray(new float[] { 10, 10 })) // Columnas más estrechas
                    .SetWidth(UnitValue.CreatePercentValue(40)) // Ocupa solo el 50% del espacio (ajustable)
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT) // Alinear tabla a la derecha
                    //.SetBorder(new SolidBorder(1))
                    .SetBorderBottom(Border.NO_BORDER); // Bordes completos

                // 3. Agregar filas con bordes:
                // - Subtotal
                innerTable.AddCell(
                    new Cell()
                        .Add(new Paragraph("Subtotal:").SetFontSize(9))
                        .SetBorder(new SolidBorder(0.5f))
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFont(boldFont2) // Negrita para el título   

                );
                innerTable.AddCell(
                    new Cell()
                        .Add(new Paragraph($"{factura.MontoGravadoTotal:N2}").SetFontSize(9))
                        .SetBorder(new SolidBorder(0.5f))
                        .SetTextAlignment(TextAlignment.RIGHT)
                );

                // - ITBIS
                innerTable.AddCell(
                    new Cell()
                        .Add(new Paragraph("ITBIS:").SetFontSize(9))
                        .SetBorder(new SolidBorder(0.5f))
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFont(boldFont2) // Negrita para el título   

                );
                innerTable.AddCell(
                    new Cell()
                        .Add(new Paragraph($"{factura.TotalITBIS:N2}").SetFontSize(9))
                        .SetBorder(new SolidBorder(0.5f))
                        .SetTextAlignment(TextAlignment.RIGHT)
                );

                // - Total RD
                innerTable.AddCell(
                    new Cell()
                        .Add(new Paragraph("Total:").SetFontSize(9))
                        .SetBorder(new SolidBorder(0.5f))
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetFont(boldFont2) // Negrita para el título   
                );
                innerTable.AddCell(
                    new Cell()
                        .Add(new Paragraph($"{factura.MontoTotal:N2}").SetFontSize(9))
                        .SetBorder(new SolidBorder(0.5f))
                        .SetTextAlignment(TextAlignment.RIGHT)
                );

                // 4. Integrar en la tabla principal
                totalesCell.Add(innerTable);
                table.AddCell(totalesCell);

                // Agregar al documento
                doc.Add(table);












                doc.Add(new Paragraph(" "));




                // Crear una tabla con 2 columnas: firma (izquierda) y QR/info (derecha)
                Table finalTable = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 }))
                    .UseAllAvailableWidth()
                    .SetMarginTop(20);

                // ---------- COLUMNA IZQUIERDA: Firma ----------
                Cell leftCell1 = new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT);

                // Línea para firmar
                Paragraph lineaFirma = new Paragraph("_____________________________________")
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetFontSize(9)
                    .SetMarginBottom(0);

                // Texto "Autorizado por"
                Paragraph autorizadoPor = new Paragraph("Autorizado por")
                    .SetFontSize(9)
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetFont(boldFont)  
                    .SetMarginTop(2);

                leftCell1.Add(lineaFirma);
                leftCell1.Add(autorizadoPor);

                // ---------- COLUMNA DERECHA: QR y detalles ----------
                Cell rightCell1 = new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT);

                // Crear código QR
                DateTime fechaFirma = DateTime.ParseExact(factura.FechaHoraFirma, "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                string soloFecha = fechaFirma.ToString("dd-MM-yyyy");

                string fechaFirmaFormateada = Uri.EscapeDataString(fechaFirma.ToString("dd-MM-yyyy HH:mm:ss"));

                string url = $"https://ecf.dgii.gov.do/certecf/ConsultaTimbre?RncEmisor={factura.RNCEmisor}&RncComprador={factura.RNCComprador}&ENCF={factura.ENCF}&FechaEmision={factura.FechaEmision}&MontoTotal={factura.MontoTotal}&FechaFirma={fechaFirmaFormateada}&CodigoSeguridad={codigoSeguridad}";

                BarcodeQRCode qrCode = new BarcodeQRCode(url);
                Image qrCodeImage = new Image(qrCode.CreateFormXObject(pdf));
                qrCodeImage.ScaleToFit(100, 100);
                qrCodeImage.SetHorizontalAlignment(HorizontalAlignment.RIGHT);

                // Agregar contenido
                //rightCell1.Add(new Paragraph("Código QR:").SetTextAlignment(TextAlignment.RIGHT));
                rightCell1.Add(qrCodeImage);
                rightCell1.Add(new Paragraph($"Código de Seguridad: {codigoSeguridad}").SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT).SetMarginTop(5));
                rightCell1.Add(new Paragraph($"FechaHoraFirma: {factura.FechaHoraFirma}").SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT));

                // Agregar celdas a la tabla
                finalTable.AddCell(leftCell1);
                finalTable.AddCell(rightCell1);

                // Agregar al documento
                doc.Add(finalTable);

                doc.Close();
                return ms.ToArray(); // ← ahora retorna el PDF generado en memoria
            }
        }



        private byte[] CrearFacturaPDFenLocal(FacturasDGII factura)
        {
            using (var ms = new MemoryStream())
            {
                String dest = "C:\\Users\\andersonmgordilloh\\source\\repos\\FacturacionElectronicaDGII\\ArchivosDGII\\sample.pdf";

                PdfWriter writer = new PdfWriter(dest);
                PdfDocument pdf = new PdfDocument(writer);
                Document doc = new Document(pdf);

                doc.Add(new Paragraph("FACTURA").SetFontSize(18));

                doc.Add(new Paragraph($"Tipo eCF: {factura.TipoeCF}"));
                doc.Add(new Paragraph($"eNCF: {factura.ENCF}"));
                doc.Add(new Paragraph($"FechaVencimientoSecuencia: {factura.FechaVencimientoSecuencia}"));
                doc.Add(new Paragraph($"IndicadorEnvioDiferido: {factura.IndicadorEnvioDiferido}"));
                doc.Add(new Paragraph($"IndicadorMontoGravado: {factura.IndicadorMontoGravado}"));

                doc.Add(new Paragraph(" "));

                // Tabla de productos
                Table table = new Table(4);
                table.AddHeaderCell("Producto");
                table.AddHeaderCell("Cantidad");
                table.AddHeaderCell("Precio Unitario");
                table.AddHeaderCell("Total");

                //foreach (var item in factura.Detalles)
                //{
                //    table.AddCell(item.Producto);
                //    table.AddCell(item.Cantidad.ToString());
                //    table.AddCell($"${item.PrecioUnitario:F2}");
                //    table.AddCell($"${(item.Cantidad * item.PrecioUnitario):F2}");
                //}

                doc.Add(table);
                doc.Add(new Paragraph(" "));
                //doc.Add(new Paragraph($"TOTAL: ${factura.Total:F2}").SetBold());

                // Generar el código QR
                // 1. Crear la URL que quieres codificar en el QR
                string url = $"https://ecf.dgii.gov.do/certecf/ConsultaTimbre?RncEmisor=130322791&RncComprador=131880681&ENCF=E310000000029&FechaEmision=01-04-2020&MontoTotal=7080.00&FechaFirma=01-03-2025%2005:07:00&CodigoSeguridad=p1NsBj"; // Ajusta esta URL

                // 2. Crear el objeto BarcodeQRCode
                BarcodeQRCode qrCode = new BarcodeQRCode(url);

                // 3. Convertir el QR code a una imagen de iText
                Image qrCodeImage = new Image(qrCode.CreateFormXObject(pdf));

                // 4. Ajustar el tamaño del QR (opcional)
                qrCodeImage.ScaleToFit(100, 100);

                // 5. Añadir el QR al documento
                doc.Add(new Paragraph("Código QR:"));
                doc.Add(qrCodeImage);

                doc.Close();
                return ms.ToArray();
            }
        }

        [HttpGet]
        public IActionResult VerFacturaPDFenLocal(int id)
        {
            // Obtener la factura desde la base de datos
            var factura = _context.FacturasDGII
                //.Include(f => f.)
                .FirstOrDefault(f => f.Id == 33);
                //.FirstOrDefault(f => f.Id == id);

            if (factura == null)
                return NotFound();

            string rutaPDF = $"C:\\Users\\andersonmgordilloh\\source\\repos\\FacturacionElectronicaDGII\\ArchivosDGII\\sample.pdf";

            if (System.IO.File.Exists(rutaPDF))
            {
                byte[] pdfBytes = System.IO.File.ReadAllBytes(rutaPDF);
                ViewBag.PdfData = $"data:application/pdf;base64,{Convert.ToBase64String(pdfBytes)}";
            }

            return View("verfacturaPDF");
        }

        [HttpGet]
        public IActionResult VerFacturaPDF(int id)
        {
            ViewBag.IdFactura = id;
            return View();
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
                            FechaVencimientoSecuencia = "31-12-2025",
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
        public IActionResult comprobanteE31ADEBUG(FacturaDGIIModel1 model)
        {
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";
            string jsonInvoiceFO = JsonConvert.SerializeObject(model);

            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // 1. Ejecución de la DLL
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // 2. Pasamos todo a la vista sin procesar para ver qué contienen
                var debugResponse = new FacturaDGIIResponseModel
                {
                    Token = "DEBUG INVOICE: " + invoice,
                    Mensaje = "DEBUG RESPONSE: " + response
                };

                // Si quieres ver si el JSON es válido o es un error de texto
                ViewBag.MensajeError = $"Invoice: {invoice} | Response: {response}";

                return View("verFactura", debugResponse);
            }
            catch (Exception ex)
            {
                // Si la DLL explota por falta de archivos o permisos en Azure
                ViewBag.MensajeError = "ERROR CRÍTICO: " + ex.Message + " | Inner: " + ex.InnerException?.Message;
                return View("verFactura", new FacturaDGIIResponseModel());
            }
        }


        [HttpPost]
        public IActionResult comprobanteE31A(FacturaDGIIModel1 model)
        {
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";
            string jsonInvoiceFO = JsonConvert.SerializeObject(model);

            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL's
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
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
                    // IdDoc
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    // Emisor
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

                    // Comprador
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

                    // Totales
                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    // Fechas
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

                            FacturaId = registro.Id, // Asignamos el ID de la factura recién creada
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
                    //return RedirectToAction("GenerarPDF", new { id = registro.Id, codigoSeguridad = respuesta.CodigoSeguridad });
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
                            eNCF = "E310000000002",
                            FechaVencimientoSecuencia = "31-12-2025",
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
                        Totales = new TotalesModel2
                        {
                            MontoGravadoTotal = "3230.00",
                            MontoGravadoI1 = "3230.00",
                            ITBIS1 = "18",
                            TotalITBIS = "713.04",
                            TotalITBIS1 = "713.04",
                            MontoImpuestoAdicional = "731.32",

                            ImpuestosAdicionales = new ImpuestosAdicionalesModel2
                            {
                                ImpuestoAdicional = new List<ImpuestoAdicionalTotalesModel2>
                                {
                                    new ImpuestoAdicionalTotalesModel2
                                    {
                                        TipoImpuesto = "006",
                                        TasaImpuestoAdicional = "633.85",
                                        MontoImpuestoSelectivoConsumoEspecifico = "540.04"
                                    },
                                    new ImpuestoAdicionalTotalesModel2
                                    {
                                        TipoImpuesto = "023",
                                        TasaImpuestoAdicional = "10",
                                        MontoImpuestoSelectivoConsumoAdvalorem = "191.28"
                                    }
                                }
                            },
                            MontoTotal = "4674.35"
                        }
                    },
                    DetallesItems = new DetallesItemsModel2
                    {
                        Item = new List<ItemModel2>
                {
                    new ItemModel2
                    {
                        NumeroLinea = "1",
                        IndicadorFacturacion = "1",
                        NombreItem = "PTE. CJ 24/12OZ",
                        IndicadorBienoServicio = "1",
                        CantidadItem = "2.00",
                        UnidadMedida = "6",
                        CantidadReferencia = "24",
                        UnidadReferencia = "5",
                        TablaSubcantidad = new TablaSubcantidadModel2
                        {
                            SubcantidadItem = new List<SubcantidadItemModel2>
                            {
                                new SubcantidadItemModel2
                                {
                                    Subcantidad = "0.355",
                                    CodigoSubcantidad = "24"
                                }
                            }
                        },
                        GradosAlcohol = "5.00",
                        PrecioUnitarioReferencia = "65.00",
                        PrecioUnitarioItem = "1615.00",
                        TablaImpuestoAdicional = new TablaImpuestoAdicionalModel2
                        {
                            ImpuestoAdicional = new List<ImpuestoAdicionalItemModel2>
                            {
                                new ImpuestoAdicionalItemModel2 { TipoImpuesto = "006" },
                                new ImpuestoAdicionalItemModel2 { TipoImpuesto = "023" }
                            }
                        },
                        MontoItem = "3230.00"
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
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";

            //string jsonInvoiceFO = JsonConvert.SerializeObject(model);

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });


            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
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
                    // IdDoc
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    // Emisor
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

                    // Comprador
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

                    // Totales
                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    // Fechas
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
                            FacturaId = registro.Id, // Asignamos el ID de la factura recién creada
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
                    //return RedirectToAction("GenerarPDF", new { id = registro.Id, codigoSeguridad = respuesta.CodigoSeguridad });
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
                            FechaVencimientoSecuencia = "31-12-2025",
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

            return View(model); // Asegúrate de tener una vista correspondiente
        }

        [HttpPost]
        public IActionResult comprobanteE31C(FacturaDGIIModel3 model)
        {
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";

            //string jsonInvoiceFO = JsonConvert.SerializeObject(model);

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });


            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
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
                    // IdDoc
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    // Emisor
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

                    // Comprador
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

                    // Totales
                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    // Fechas
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
                            FacturaId = registro.Id, // Asignamos el ID de la factura recién creada
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
                    //return RedirectToAction("GenerarPDF", new { id = registro.Id, codigoSeguridad = respuesta.CodigoSeguridad });
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
                            FechaVencimientoSecuencia = "31-12-2025",
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

            return View(model); // Asegúrate de tener una vista para mostrarlo correctamente
        }



        [HttpPost]
        public IActionResult comprobanteE31D(FacturaDGIIModel4 model)
        {
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";

            //string jsonInvoiceFO = JsonConvert.SerializeObject(model);

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });


            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
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
                    // IdDoc
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    // Emisor
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

                    // Comprador
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

                    // Totales
                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    // Fechas
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
                            FacturaId = registro.Id, // Asignamos el ID de la factura recién creada
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
                    //return RedirectToAction("GenerarPDF", new { id = registro.Id, codigoSeguridad = respuesta.CodigoSeguridad });
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
                            FechaVencimientoSecuencia = "31-12-2025",
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
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";

            //string jsonInvoiceFO = JsonConvert.SerializeObject(model);

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                // Si TablaCodigosItem no es null
                if (item.TablaSubDescuento?.SubDescuento != null)
                {
                    // Filtrar objetos vacíos
                    item.TablaSubDescuento.SubDescuento = item.TablaSubDescuento.SubDescuento
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubDescuento) && !string.IsNullOrWhiteSpace(ci.MontoSubDescuento))
                        .ToList();

                    // Si después de filtrar está vacío, eliminar la tabla entera
                    if (!item.TablaSubDescuento.SubDescuento.Any())
                    {
                        item.TablaSubDescuento = null;
                    }
                }
            }

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                // Si TablaCodigosItem no es null
                if (item.TablaSubRecargo?.SubRecargo != null)
                {
                    // Filtrar objetos vacíos
                    item.TablaSubRecargo.SubRecargo = item.TablaSubRecargo.SubRecargo
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubRecargo) && !string.IsNullOrWhiteSpace(ci.MontoSubRecargo))
                        .ToList();

                    // Si después de filtrar está vacío, eliminar la tabla entera
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


            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
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
                    // IdDoc
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    // Emisor
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

                    // Comprador
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

                    // Totales
                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    // Fechas
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
                            FacturaId = registro.Id, // Asignamos el ID de la factura recién creada
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
                    //return RedirectToAction("GenerarPDF", new { id = registro.Id, codigoSeguridad = respuesta.CodigoSeguridad });
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
                            // Se omiten los campos que no aparecen en el JSON: FechaVencimientoSecuencia, IndicadorEnvioDiferido, IndicadorMontoGravado
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
                        InformacionesAdicionales = new InformacionesAdicionales6
                        {
                            NumeroContenedor = "8019289",
                            NumeroReferencia = "1447"
                        },
                        Totales = new TotalesModel6
                        {
                            MontoExento = "300000.00",
                            MontoTotal = "300000.00"
                            // No se incluyen campos como ITBIS ni impuestos adicionales porque no están en el JSON
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
                        // No se incluye TablaImpuestoAdicional porque no está en el JSON
                    }
                }
                    }
                }
            };

            return View(model); // Asegúrate que la vista correspondiente maneje correctamente FacturaDGIIModel6
        }

        [HttpPost]
        public IActionResult comprobanteE32A(FacturaDGIIModel6 model)
        {
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";

            //string jsonInvoiceFO = JsonConvert.SerializeObject(model);

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });


            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
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
                    // IdDoc
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    //FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    // Emisor
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

                    // Comprador
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

                    // Totales
                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    // Fechas
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
                            FacturaId = registro.Id, // Asignamos el ID de la factura recién creada
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
                    //return RedirectToAction("GenerarPDF", new { id = registro.Id, codigoSeguridad = respuesta.CodigoSeguridad });
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
                    // Agrega aquí los demás ítems (2 al 15) replicando la misma estructura
                    // Puedes usar un bucle si los datos son repetitivos o lo deseas hacer dinámico
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

                // Solo agregar TablaCodigosItem si i <= 5
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
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";

            //string jsonInvoiceFO = JsonConvert.SerializeObject(model);

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                // Si TablaCodigosItem no es null
                if (item.TablaCodigosItem?.CodigosItem != null)
                {
                    // Filtrar objetos vacíos
                    item.TablaCodigosItem.CodigosItem = item.TablaCodigosItem.CodigosItem
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoCodigo) && !string.IsNullOrWhiteSpace(ci.CodigoItem))
                        .ToList();

                    // Si después de filtrar está vacío, eliminar la tabla entera
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


            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
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
                    // IdDoc
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    //FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    // Emisor
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

                    // Comprador
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

                    // Totales
                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    // Fechas
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
                            FacturaId = registro.Id, // Asignamos el ID de la factura recién creada
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
                    //return RedirectToAction("GenerarPDF", new { id = registro.Id, codigoSeguridad = respuesta.CodigoSeguridad });
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
                            FechaVencimientoSecuencia = "31-12-2025",
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
                        NCFModificado = "E320000000002",
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
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";

            //string jsonInvoiceFO = JsonConvert.SerializeObject(model);

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                // Si TablaCodigosItem no es null
                if (item.TablaSubDescuento?.SubDescuento != null)
                {
                    // Filtrar objetos vacíos
                    item.TablaSubDescuento.SubDescuento = item.TablaSubDescuento.SubDescuento
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubDescuento) && !string.IsNullOrWhiteSpace(ci.MontoSubDescuento))
                        .ToList();

                    // Si después de filtrar está vacío, eliminar la tabla entera
                    if (!item.TablaSubDescuento.SubDescuento.Any())
                    {
                        item.TablaSubDescuento = null;
                    }
                }
            }

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                // Si TablaCodigosItem no es null
                if (item.TablaSubRecargo?.SubRecargo != null)
                {
                    // Filtrar objetos vacíos
                    item.TablaSubRecargo.SubRecargo = item.TablaSubRecargo.SubRecargo
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubRecargo) && !string.IsNullOrWhiteSpace(ci.MontoSubRecargo))
                        .ToList();

                    // Si después de filtrar está vacío, eliminar la tabla entera
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


            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
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
                    // IdDoc
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    // Emisor
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

                    // Comprador
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

                    // Totales
                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    // Fechas
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
                            FacturaId = registro.Id, // Asignamos el ID de la factura recién creada
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
                    //return RedirectToAction("GenerarPDF", new { id = registro.Id, codigoSeguridad = respuesta.CodigoSeguridad });
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
                        RazonModificacion = ""
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult comprobanteE34(FacturaDGIIModel9 model)
        {
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";

            //string jsonInvoiceFO = JsonConvert.SerializeObject(model);

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                // Si TablaCodigosItem no es null
                if (item.TablaSubDescuento?.SubDescuento != null)
                {
                    // Filtrar objetos vacíos
                    item.TablaSubDescuento.SubDescuento = item.TablaSubDescuento.SubDescuento
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubDescuento) && !string.IsNullOrWhiteSpace(ci.MontoSubDescuento))
                        .ToList();

                    // Si después de filtrar está vacío, eliminar la tabla entera
                    if (!item.TablaSubDescuento.SubDescuento.Any())
                    {
                        item.TablaSubDescuento = null;
                    }
                }
            }

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                // Si TablaCodigosItem no es null
                if (item.TablaSubRecargo?.SubRecargo != null)
                {
                    // Filtrar objetos vacíos
                    item.TablaSubRecargo.SubRecargo = item.TablaSubRecargo.SubRecargo
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubRecargo) && !string.IsNullOrWhiteSpace(ci.MontoSubRecargo))
                        .ToList();

                    // Si después de filtrar está vacío, eliminar la tabla entera
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


            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
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
                    // IdDoc
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    // Emisor
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

                    // Comprador
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

                    // Totales
                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    // Fechas
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
                            FacturaId = registro.Id, // Asignamos el ID de la factura recién creada
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
                    //return RedirectToAction("GenerarPDF", new { id = registro.Id, codigoSeguridad = respuesta.CodigoSeguridad });
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
                            FechaVencimientoSecuencia = "31-12-2025",
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
                            RNCComprador = "533445861",
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
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";

            //string jsonInvoiceFO = JsonConvert.SerializeObject(model);

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                // Si TablaCodigosItem no es null
                if (item.TablaSubDescuento?.SubDescuento != null)
                {
                    // Filtrar objetos vacíos
                    item.TablaSubDescuento.SubDescuento = item.TablaSubDescuento.SubDescuento
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubDescuento) && !string.IsNullOrWhiteSpace(ci.MontoSubDescuento))
                        .ToList();

                    // Si después de filtrar está vacío, eliminar la tabla entera
                    if (!item.TablaSubDescuento.SubDescuento.Any())
                    {
                        item.TablaSubDescuento = null;
                    }
                }
            }

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                // Si TablaCodigosItem no es null
                if (item.TablaSubRecargo?.SubRecargo != null)
                {
                    // Filtrar objetos vacíos
                    item.TablaSubRecargo.SubRecargo = item.TablaSubRecargo.SubRecargo
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubRecargo) && !string.IsNullOrWhiteSpace(ci.MontoSubRecargo))
                        .ToList();

                    // Si después de filtrar está vacío, eliminar la tabla entera
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


            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
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
                    // IdDoc
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    // Emisor
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

                    // Comprador
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

                    // Totales
                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    // Fechas
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
                            FacturaId = registro.Id, // Asignamos el ID de la factura recién creada
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
                    //return RedirectToAction("GenerarPDF", new { id = registro.Id, codigoSeguridad = respuesta.CodigoSeguridad });
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
                            FechaVencimientoSecuencia = "31-12-2025"
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
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL's
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
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
                    // IdDoc
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    // Emisor
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

                    // Totales
                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    // Fechas
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
                            FacturaId = registro.Id, // Asignamos el ID de la factura recién creada
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
                    //return RedirectToAction("GenerarPDF", new { id = registro.Id, codigoSeguridad = respuesta.CodigoSeguridad });
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
                            FechaVencimientoSecuencia = "31-12-2025",
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
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";

            //string jsonInvoiceFO = JsonConvert.SerializeObject(model);

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                // Si TablaCodigosItem no es null
                if (item.TablaSubDescuento?.SubDescuento != null)
                {
                    // Filtrar objetos vacíos
                    item.TablaSubDescuento.SubDescuento = item.TablaSubDescuento.SubDescuento
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubDescuento) && !string.IsNullOrWhiteSpace(ci.MontoSubDescuento))
                        .ToList();

                    // Si después de filtrar está vacío, eliminar la tabla entera
                    if (!item.TablaSubDescuento.SubDescuento.Any())
                    {
                        item.TablaSubDescuento = null;
                    }
                }
            }

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                // Si TablaCodigosItem no es null
                if (item.TablaSubRecargo?.SubRecargo != null)
                {
                    // Filtrar objetos vacíos
                    item.TablaSubRecargo.SubRecargo = item.TablaSubRecargo.SubRecargo
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubRecargo) && !string.IsNullOrWhiteSpace(ci.MontoSubRecargo))
                        .ToList();

                    // Si después de filtrar está vacío, eliminar la tabla entera
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


            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
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
                    // IdDoc
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    // Emisor
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

                    // Comprador
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

                    // Totales
                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    // Fechas
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
                            FacturaId = registro.Id, // Asignamos el ID de la factura recién creada
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
                    //return RedirectToAction("GenerarPDF", new { id = registro.Id, codigoSeguridad = respuesta.CodigoSeguridad });
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
                            FechaVencimientoSecuencia = "31-12-2025",
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
                            RazonSocialComprador = "DOCUMENTOS ELECTRONICOS DE 04",
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
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";

            //string jsonInvoiceFO = JsonConvert.SerializeObject(model);

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                // Si TablaCodigosItem no es null
                if (item.TablaSubDescuento?.SubDescuento != null)
                {
                    // Filtrar objetos vacíos
                    item.TablaSubDescuento.SubDescuento = item.TablaSubDescuento.SubDescuento
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubDescuento) && !string.IsNullOrWhiteSpace(ci.MontoSubDescuento))
                        .ToList();

                    // Si después de filtrar está vacío, eliminar la tabla entera
                    if (!item.TablaSubDescuento.SubDescuento.Any())
                    {
                        item.TablaSubDescuento = null;
                    }
                }
            }

            foreach (var item in model.ECF.DetallesItems.Item)
            {
                // Si TablaCodigosItem no es null
                if (item.TablaSubRecargo?.SubRecargo != null)
                {
                    // Filtrar objetos vacíos
                    item.TablaSubRecargo.SubRecargo = item.TablaSubRecargo.SubRecargo
                        .Where(ci => ci != null && !string.IsNullOrWhiteSpace(ci.TipoSubRecargo) && !string.IsNullOrWhiteSpace(ci.MontoSubRecargo))
                        .ToList();

                    // Si después de filtrar está vacío, eliminar la tabla entera
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


            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
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
                    // IdDoc
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    // Emisor
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

                    // Comprador
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

                    // Totales
                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    // Fechas
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
                            FacturaId = registro.Id, // Asignamos el ID de la factura recién creada
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
                    //return RedirectToAction("GenerarPDF", new { id = registro.Id, codigoSeguridad = respuesta.CodigoSeguridad });
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
                            FechaVencimientoSecuencia = "31-12-2025",
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
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";

            //string jsonInvoiceFO = JsonConvert.SerializeObject(model);

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });


            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
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
                    // IdDoc
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    // Emisor
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

                    // Comprador
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

                    // Totales
                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    // Fechas
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
                            FacturaId = registro.Id, // Asignamos el ID de la factura recién creada
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
                    //return RedirectToAction("GenerarPDF", new { id = registro.Id, codigoSeguridad = respuesta.CodigoSeguridad });
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
                            FechaVencimientoSecuencia = "31-12-2025",
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
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";

            //string jsonInvoiceFO = JsonConvert.SerializeObject(model);

            string jsonInvoiceFO = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });


            string urlValidarSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/ValidarSemilla";
            string urlRecepcionFactura = "https://ecf.dgii.gov.do/certecf/recepcion/api/FacturasElectronicas";
            string urlConsultaFactura = "https://ecf.dgii.gov.do/certecf/consultaresultado/api/Consultas/Estado";

            try
            {
                // Llamada al método de la DLL
                string invoice = FacturacionElectronicaDGII.EnviarTokenSincrona(urlSemilla, passCert, jsonInvoiceFO);
                string response = FacturacionElectronicaDGII.EnviarFacturaElectronicaSincrona(urlValidarSemilla, urlRecepcionFactura, urlConsultaFactura);

                // Parsear el JSON 'invoice'
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
                    // IdDoc
                    TipoeCF = model?.ECF?.Encabezado?.IdDoc?.TipoeCF,
                    ENCF = model?.ECF?.Encabezado?.IdDoc?.eNCF,
                    FechaVencimientoSecuencia = model?.ECF?.Encabezado?.IdDoc?.FechaVencimientoSecuencia,
                    TipoPago = model?.ECF?.Encabezado?.IdDoc?.TipoPago,
                    IndicadorEnvioDiferido = model?.ECF?.Encabezado?.IdDoc?.IndicadorEnvioDiferido,
                    IndicadorMontoGravado = model?.ECF?.Encabezado?.IdDoc?.IndicadorMontoGravado,
                    TipoIngresos = model?.ECF?.Encabezado?.IdDoc?.TipoIngresos,

                    // Emisor
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

                    // Comprador
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

                    // Totales
                    MontoGravadoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoTotal ?? "0"),
                    MontoGravadoI1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoGravadoI1 ?? "0"),
                    ITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.ITBIS1 ?? "0"),
                    TotalITBIS = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS ?? "0"),
                    TotalITBIS1 = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.TotalITBIS1 ?? "0"),
                    MontoTotal = Convert.ToDecimal(model?.ECF?.Encabezado?.Totales?.MontoTotal ?? "0"),

                    // Fechas
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
                            FacturaId = registro.Id, // Asignamos el ID de la factura recién creada
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
                    //return RedirectToAction("GenerarPDF", new { id = registro.Id, codigoSeguridad = respuesta.CodigoSeguridad });
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
            string thumbprint = "5F5017E1810EBEAF9DAE0AD482C252F4AC19CA91"; // thumbprint real
            var resultado = FacturacionElectronicaDGII.GetCertificateFromStoreWINDOWS2(thumbprint);

            // Mapear manualmente a tu modelo
            var model = new CertCheckResult
            {
                Existe = resultado.Existe,
                Mensaje = resultado.Mensaje,
                Subject = resultado.Subject,
                Thumbprint = resultado.Thumbprint
            };

            return View(model); // <-- ahora sí pasa el modelo correcto
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

        // POST: Recibir los datos del formulario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarEmisor(EmisorInfo emisorInfo)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // ✅ Aquí el modelo ya está lleno con los datos del formulario
                    Console.WriteLine($"RNC: {emisorInfo.RNCEmisor}");
                    Console.WriteLine($"Razón Social: {emisorInfo.RazonSocialEmisor}");

                    // Guardar en la base de datos
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

            // Si hay errores, mostrar el formulario again con los datos ingresados
            return View(emisorInfo);
        }

    }
}
