using ConexionDGII;
using DGIIFacturadorLoginMVCApp.Data;
using DGIIFacturadorLoginMVCApp.Data.Migrations;
using DGIIFacturadorLoginMVCApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // Mostrar formulario vacío (GET)
        [HttpGet]
        public IActionResult registrarfacturavacia()
        {
            // Opcionalmente puedes inicializar un modelo vacío con un item ya cargado
            var model = new FacturaDGIIModel
            {
                ECF = new ECFModel
                {
                    Encabezado = new EncabezadoModel
                    {
                        Emisor = new EmisorModel(),
                        Comprador = new CompradorModel(),
                        Totales = new TotalesModel(),
                        IdDoc = new VersionIdDocModel()
                    },
                    DetallesItems = new DetallesItemsModel
                    {
                        Item = new List<ItemModel> { new ItemModel() }
                    }
                }
            };

            return View(model); // Esto abre el formulario
        }

        [HttpGet]
        public IActionResult registrarfactura()
        {
            var model = new FacturaDGIIModel
            {
                ECF = new ECFModel
                {
                    FechaHoraFirma = "01-03-2025 05:07:00",
                    Encabezado = new EncabezadoModel
                    {
                        Version = "1.0",
                        IdDoc = new VersionIdDocModel
                        {
                            TipoeCF = "31",
                            eNCF = "E310000000030",
                            FechaVencimientoSecuencia = "31-12-2025",
                            IndicadorEnvioDiferido = "1",
                            IndicadorMontoGravado = "0",
                            TipoIngresos = "01",
                            TipoPago = "1"
                        },
                        Emisor = new EmisorModel
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
                        Comprador = new CompradorModel
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
                        Totales = new TotalesModel
                        {
                            MontoGravadoTotal = "6000.00",
                            MontoGravadoI1 = "6000.00",
                            ITBIS1 = "18",
                            TotalITBIS = "1080.00",
                            TotalITBIS1 = "1080.00",
                            MontoTotal = "7080.00"
                        }
                    },
                    DetallesItems = new DetallesItemsModel
                    {
                        Item = new List<ItemModel>
                {
                    new ItemModel
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
        public IActionResult registrarfactura(FacturaDGIIModel model)
        {
            string urlSemilla = "https://ecf.dgii.gov.do/certecf/autenticacion/api/Autenticacion/Semilla";
            string passCert = "LD271167";
            string jsonInvoiceFO = JsonConvert.SerializeObject(model);

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

                return View("verFactura", respuesta);

                return View(null);

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
