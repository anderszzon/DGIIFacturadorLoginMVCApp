﻿using ConexionDGII;
using DGIIFacturadorLoginMVCApp.Data;
using DGIIFacturadorLoginMVCApp.Data.Migrations;
using DGIIFacturadorLoginMVCApp.Models;
using iText.Barcodes;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public IActionResult GenerarPDF(int id)
        {
            // Obtener la factura desde la base de datos
            var factura = _context.FacturasDGII
                //.Include(f => f.)
                //.FirstOrDefault(f => f.Id == 31);
                .FirstOrDefault(f => f.Id == id);

            if (factura == null)
                return NotFound();

            byte[] pdfBytes = CrearFacturaPDFInMemory(factura);

            //return File(pdfBytes, "application/pdf", $"Factura_{factura.ENCF}.pdf");
            //return File(pdfBytes, "application/pdf");
            //return Content("mensaje");
            //return File(pdfBytes, "application/pdf", $"Factura_{factura.ENCF}.pdf");

            //return View("verfacturaPDF");
            return File(pdfBytes, "application/pdf", $"Factura_{factura.ENCF}.pdf");

            //return View("verfacturaPDF", $"Factura_{factura.ENCF}.pdf");
            //return RedirectToAction("VerFacturaPDF", new { id = id });

        }

        private byte[] CrearFacturaPDFInMemory(FacturasDGII factura)
        {
            using (var ms = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(ms); // ← usar memoria, NO disco
                PdfDocument pdf = new PdfDocument(writer);
                Document doc = new Document(pdf);

                doc.Add(new Paragraph("FACTURA").SetFontSize(18));
                doc.Add(new Paragraph($"Tipo eCF: {factura.TipoeCF}"));
                doc.Add(new Paragraph($"eNCF: {factura.ENCF}"));
                doc.Add(new Paragraph($"FechaVencimientoSecuencia: {factura.FechaVencimientoSecuencia}"));
                doc.Add(new Paragraph($"IndicadorEnvioDiferido: {factura.IndicadorEnvioDiferido}"));
                doc.Add(new Paragraph($"IndicadorMontoGravado: {factura.IndicadorMontoGravado}"));

                doc.Add(new Paragraph(" "));

                Table table = new Table(4);
                table.AddHeaderCell("Producto");
                table.AddHeaderCell("Cantidad");
                table.AddHeaderCell("Precio Unitario");
                table.AddHeaderCell("Total");
                // Aquí puedes agregar los productos reales si los tienes

                doc.Add(table);

                string url = $"https://ecf.dgii.gov.do/certecf/ConsultaTimbre?RncEmisor=130322791&RncComprador=131880681&ENCF=E310000000029&FechaEmision=01-04-2020&MontoTotal=7080.00&FechaFirma=01-03-2025%2005:07:00&CodigoSeguridad=p1NsBj"; 
                BarcodeQRCode qrCode = new BarcodeQRCode(url);
                Image qrCodeImage = new Image(qrCode.CreateFormXObject(pdf));
                qrCodeImage.ScaleToFit(100, 100);
                doc.Add(new Paragraph("Código QR:"));
                doc.Add(qrCodeImage);

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
        public IActionResult registrarfacturaE310000000001()
        {
            var model = new FacturaDGIIModel1
            {
                ECF = new ECFModel1
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel1
                    {
                        Version = "1.0",
                        IdDoc = new VersionIdDocModel1
                        {
                            TipoeCF = "31",
                            eNCF = "E310000000030",
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
        public IActionResult registrarfacturaE310000000001(FacturaDGIIModel1 model)
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

                //return View("verFactura", respuesta);
                return RedirectToAction("GenerarPDF", new { id = registro.Id });

                //return View(null);

                //return View("NombreDeLaVista", model);
                //return View("verFactura", model); // o mostrar resultados


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
        public IActionResult registrarfacturaE310000000002()
        {
            var model = new FacturaDGIIModel2
            {
                ECF = new ECFModel2
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel2
                    {
                        Version = "1.0",
                        IdDoc = new VersionIdDocModel2
                        {
                            TipoeCF = "31",
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
        public IActionResult registrarfacturaE310000000002(FacturaDGIIModel2 model)
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

                return RedirectToAction("GenerarPDF", new { id = registro.Id });

                //return View("verFactura", respuesta);
                //return View(null);

                //return View("NombreDeLaVista", model);
                //return View("verFactura", model); // o mostrar resultados


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
        public IActionResult registrarfacturaE310000000003()
        {
            var model = new FacturaDGIIModel3
            {
                ECF = new ECFModel3
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel3
                    {
                        Version = "1.0",
                        IdDoc = new VersionIdDocModel3
                        {
                            TipoeCF = "31",
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
        public IActionResult registrarfacturaE310000000003(FacturaDGIIModel3 model)
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

                return RedirectToAction("GenerarPDF", new { id = registro.Id });

                //return View("verFactura", respuesta);
                //return View(null);

                //return View("NombreDeLaVista", model);
                //return View("verFactura", model); // o mostrar resultados


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
        public IActionResult registrarfacturaE320000000001()
        {
            var model = new FacturaDGIIModel6
            {
                ECF = new ECFModel6
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel6
                    {
                        Version = "1.0",
                        IdDoc = new VersionIdDocModel6
                        {
                            TipoeCF = "32",
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
        public IActionResult registrarfacturaE320000000001(FacturaDGIIModel6 model)
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

                return RedirectToAction("GenerarPDF", new { id = registro.Id });

                //return View("verFactura", respuesta);
                //return View(null);

                //return View("NombreDeLaVista", model);
                //return View("verFactura", model); // o mostrar resultados


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
    }
}
