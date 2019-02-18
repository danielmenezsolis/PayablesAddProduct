using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RightNow.AddIns.AddInViews;
using PayablesAddProduct.SOAPICCS;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Net;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using MoreLinq;
using System.Xml.Serialization;
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json;

namespace PayablesAddProduct
{
    public partial class Payables : UserControl
    {
        private bool inDesignMode;
        public IRecordContext recordContext;
        public IGlobalContext globalContext;
        public IGenericObject SerObject;
        public IGenericObject PayableService;


        public string CPQPass { get; set; }
        public string SRType { get; set; }
        public string AirtportText { get; set; }
        public int IdService { get; set; }
        RightNowSyncPortClient clientORN { get; set; }
        public string ItemClaveSat { get; set; }
        public string ParticipacionCobro { get; set; }
        public string CuentaGasto { get; set; }
        public string Royalty { get; set; }
        public string Pagos { get; set; }
        public string ClasificacionPago { get; set; }
        public string Informativo { get; set; }
        public string Component { get; set; }
        public string Pax { get; set; }
        public string Categories { get; set; }
        public string ItemDesc { get; set; }
        public double Prices { get; set; }
        public int IdIncident { get; set; }
        public string Quantity { get; set; }

        public Payables()
        {
            InitializeComponent();
        }
        public Payables(bool inDesignMode, IRecordContext recordContext, IGlobalContext globalContext) : this()
        {
            this.inDesignMode = inDesignMode;
            this.recordContext = recordContext;
            this.globalContext = globalContext;
        }
        internal void LoadData()
        {
            try
            {
                Init();
                CPQPass = getPassword("CPQ");
                Quantity = "1";
                SerObject = recordContext.GetWorkspaceRecord("CO$Services") as IGenericObject;
                if (SerObject != null)
                {
                    IdIncident = 0;
                    string itemNumber = "";
                    IList<IGenericField> fields = SerObject.GenericFields;
                    foreach (IGenericField field in fields)
                    {
                        if (field.Name == "Incident")
                        {
                            IdIncident = int.Parse(field.DataValue.Value.ToString());
                        }
                        if (field.Name == "ItemNumber")
                        {
                            itemNumber = field.DataValue.Value.ToString();
                        }
                    }
                    SRType = GetSRType();
                    AirtportText = GetAirport(SerObject.Id);
                    if (SRType == "CATERING")
                    {
                        getSuppliers();
                        PayableService = recordContext.GetWorkspaceRecord("CO$Payables") as IGenericObject;
                        IList<IGenericField> fieldsk = PayableService.GenericFields;
                        foreach (IGenericField genField in fieldsk)
                        {
                            if (genField.Name == "ExchangeRate")
                            {
                                genField.DataValue.Value = getExchangeRate(GetDeliveryDate()).ToString();
                            }
                            txtItemNumber.Visible = false;
                            CboProductos.Visible = false;
                            lblProduct.Visible = false;
                            lblHeader.Text = "Add Supplier: ";
                        }
                        /*
                       txtItemNumber.Visible = false;
                       CboProductos.Visible = true;

                       if (!String.IsNullOrEmpty(AirtportText))
                       {
                           GetPData(AirtportText, "");
                       }
                       */
                    }
                    else
                    {
                        CboProductos.Visible = false;
                        txtItemNumber.Visible = true;
                        txtItemNumber.Text = itemNumber;
                    }

                }
            }
            catch (Exception ex)
            {
                globalContext.LogMessage("Load: " + ex.Message + "Det: " + ex.StackTrace);

            }
        }
        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                if (SRType == "CATERING")
                {
                    PayableService = recordContext.GetWorkspaceRecord("CO$Payables") as IGenericObject;
                    IList<IGenericField> fields = PayableService.GenericFields;
                    foreach (IGenericField genField in fields)
                    {
                        if (genField.Name == "Supplier")
                        {
                            genField.DataValue.Value = cboSuppliers.Text;
                        }
                    }
                }
                else
                {
                    GetPData(AirtportText, (SRType == "CATERING" ? CboProductos.SelectedValue.ToString() : txtItemNumber.Text));
                    Prices = GetPrices(AirtportText, (SRType == "CATERING" ? CboProductos.SelectedValue.ToString() : txtItemNumber.Text));
                    LlenarValoresServicio();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("AddClick: " + ex.Message + "Det: " + ex.StackTrace);
            }
        }

        public bool Init()
        {
            try
            {
                bool result = false;
                EndpointAddress endPointAddr = new EndpointAddress(globalContext.GetInterfaceServiceUrl(ConnectServiceType.Soap));
                // Minimum required
                BasicHttpBinding binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential);
                binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
                binding.ReceiveTimeout = new TimeSpan(0, 10, 0);
                binding.MaxReceivedMessageSize = 1048576; //1MB
                binding.SendTimeout = new TimeSpan(0, 10, 0);
                // Create client proxy class
                clientORN = new RightNowSyncPortClient(binding, endPointAddr);
                // Ask the client to not send the timestamp
                BindingElementCollection elements = clientORN.Endpoint.Binding.CreateBindingElements();
                elements.Find<SecurityBindingElement>().IncludeTimestamp = false;
                clientORN.Endpoint.Binding = new CustomBinding(elements);
                // Ask the Add-In framework the handle the session logic
                globalContext.PrepareConnectSession(clientORN.ChannelFactory);
                if (clientORN != null)
                {
                    result = true;
                }

                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;

            }
        }
        public string GetAirport(int Service)
        {
            string air = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT Airport FROM CO.Services WHERE ID =" + Service + "";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {

                    air = data;
                }

            }
            return air;
        }
        public string GetSRType()
        {
            try
            {
                string SRTYPE = "";
                if (IdIncident != 0)
                {
                    ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                    APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                    clientInfoHeader.AppID = "Query Example";
                    String queryString = "SELECT I.Customfields.c.sr_type.LookupName FROM Incident I WHERE id=" + IdIncident;
                    clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                    foreach (CSVTable table in queryCSV.CSVTables)
                    {
                        String[] rowData = table.Rows;
                        foreach (String data in rowData)
                        {
                            SRTYPE = data;
                        }
                    }
                }
                switch (SRTYPE)
                {
                    case "Catering":
                        SRTYPE = "CATERING";
                        break;
                    case "FCC":
                        SRTYPE = "FCC";
                        break;
                    case "FBO":
                        SRTYPE = "FBO";
                        break;
                    case "Fuel":
                        SRTYPE = "FUEL";
                        break;
                    case "Hangar Space":
                        SRTYPE = "GYCUSTODIA";
                        break;
                    case "SENEAM Fee":
                        SRTYPE = "SENEAM";
                        break;
                    case "Permits":
                        SRTYPE = "PERMISOS";
                        break;
                }
                return SRTYPE;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetType: " + ex.Message + "Detail: " + ex.StackTrace);
                return "";
            }
        }
        public DateTime GetDeliveryDate()
        {
            try
            {
                DateTime DDate = DateTime.Now;
                if (IdIncident != 0)
                {
                    ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                    APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                    clientInfoHeader.AppID = "Query Example";
                    String queryString = "SELECT Customfields.C.delivery_datetime As DDate FROM Incident WHERE ID =" + IdIncident;
                    clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                    foreach (CSVTable table in queryCSV.CSVTables)
                    {
                        String[] rowData = table.Rows;
                        foreach (String data in rowData)
                        {
                            DDate = DateTime.Parse(data);
                        }
                    }
                }

                return DDate;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetType: " + ex.Message + "Detail: " + ex.StackTrace);
                return DateTime.Now;
            }
        }



        public void GetPData(string AirportText, string ItemN)
        {
            try
            {
                string envelope = "<soapenv:Envelope" +
                 "   xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"" +
                 "   xmlns:typ=\"http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/types/\"" +
                 "   xmlns:typ1=\"http://xmlns.oracle.com/adf/svc/types/\">" +
                 "<soapenv:Header/>" +
                 "<soapenv:Body>" +
                 "<typ:findItem>" +
                 "<typ:findCriteria>" +
                 "<typ1:fetchStart>0</typ1:fetchStart>" +
                 "<typ1:fetchSize>-1</typ1:fetchSize>" +
                 "<typ1:filter>" +
                 "<typ1:group>" +
                 "<typ1:item>" +
                 "<typ1:conjunction>And</typ1:conjunction>" +
                 "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                 "<typ1:attribute>ItemNumber</typ1:attribute>";
                if (!string.IsNullOrEmpty(ItemN))
                {
                    envelope += "<typ1:operator>=</typ1:operator>" +
                     "<typ1:value>" + ItemN + "</typ1:value>";
                }
                else
                {
                    envelope += "<typ1:operator>CONTAINS</typ1:operator>" +
                    "<typ1:value>CATERINGNJ</typ1:value>";
                }
                envelope += "</typ1:item>" +
                  "<typ1:item>" +
                 "<typ1:conjunction>And</typ1:conjunction>" +
                 "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                 "<typ1:attribute>OrganizationCode</typ1:attribute>" +
                 "<typ1:operator>=</typ1:operator>" +
                 "<typ1:value>IO_AEREO_" + AirportText + "</typ1:value>" +
                 "</typ1:item>" +
                "</typ1:group>" +
                "</typ1:filter>" +
                "<typ1:findAttribute>ItemNumber</typ1:findAttribute>" +
                "<typ1:findAttribute>ItemDescription</typ1:findAttribute>";
                if (!string.IsNullOrEmpty(ItemN))
                {
                    envelope += "<typ1:findAttribute>ItemDFF</typ1:findAttribute>";
                }
                envelope += "</typ:findCriteria>" +
                 "<typ:findControl>" +
                 "<typ1:retrieveAllTranslations>true</typ1:retrieveAllTranslations>" +
                 "</typ:findControl>" +
                 "</typ:findItem>" +
                 "</soapenv:Body>" +
                 "</soapenv:Envelope>";
                globalContext.LogMessage(envelope);
                byte[] byteArray = Encoding.UTF8.GetBytes(envelope);
                // Construct the base 64 encoded string used as credentials for the service call
                byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes("itotal" + ":" + "Oracle123");
                string credentials = System.Convert.ToBase64String(toEncodeAsBytes);
                // Create HttpWebRequest connection to the service
                HttpWebRequest request =
                 (HttpWebRequest)WebRequest.Create("https://egqy-test.fa.us6.oraclecloud.com:443/fscmService/ItemServiceV2");
                // Configure the request content type to be xml, HTTP method to be POST, and set the content length
                request.Method = "POST";
                request.ContentType = "text/xml;charset=UTF-8";
                request.ContentLength = byteArray.Length;
                // Configure the request to use basic authentication, with base64 encoded user name and password, to invoke the service.
                request.Headers.Add("Authorization", "Basic " + credentials);
                // Set the SOAP action to be invoked; while the call works without this, the value is expected to be set based as per standards
                request.Headers.Add("SOAPAction", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/findItem");
                // Write the xml payload to the request
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                // Write the xml payload to the request
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result = "";
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        doc = XDocument.Load(stream);
                        result = doc.ToString();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);
                        XmlNamespaceManager nms = new XmlNamespaceManager(xmlDoc.NameTable);
                        nms.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                        nms.AddNamespace("wsa", "http://www.w3.org/2005/08/addressing");
                        nms.AddNamespace("typ", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/types/");
                        nms.AddNamespace("ns0", "http://xmlns.oracle.com/adf/svc/types/");
                        nms.AddNamespace("ns1", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/");
                        if (!String.IsNullOrEmpty(ItemN))
                        {
                            XmlNodeList nodeList = xmlDoc.SelectNodes("//ns0:Value", nms);
                            foreach (XmlNode node in nodeList)
                            {
                                if (node.HasChildNodes)
                                {
                                    if (node.LocalName == "Value")
                                    {
                                        XmlNodeList nodeListvalue = node.ChildNodes;
                                        foreach (XmlNode nodeValue in nodeListvalue)
                                        {
                                            if (nodeValue.LocalName == "ItemDescription")
                                            {
                                                ItemDesc = nodeValue.InnerText.ToUpper();
                                            }
                                        }
                                    }
                                }
                            }

                            XmlNode desiredNode = xmlDoc.SelectSingleNode("//ns1:ItemDFF", nms);
                            if (desiredNode != null)
                            {
                                if (desiredNode.HasChildNodes)
                                {
                                    for (int i = 0; i < desiredNode.ChildNodes.Count; i++)
                                    {

                                        if (desiredNode.ChildNodes[i].LocalName == "xxParticipacionCobro")
                                        {
                                            ParticipacionCobro = desiredNode.ChildNodes[i].InnerText == "SI" ? "1" : "0";
                                        }
                                        if (desiredNode.ChildNodes[i].LocalName == "xxCategoriaRoyalty")
                                        {
                                            Royalty = desiredNode.ChildNodes[i].InnerText == "SI" ? "1" : "0";
                                        }
                                        if (desiredNode.ChildNodes[i].LocalName == "xxPagos")
                                        {
                                            Pagos = desiredNode.ChildNodes[i].InnerText;
                                        }
                                        if (desiredNode.ChildNodes[i].LocalName == "xxClasificacionPago")
                                        {
                                            ClasificacionPago = desiredNode.ChildNodes[i].InnerText;
                                        }
                                        if (desiredNode.ChildNodes[i].LocalName == "cuentaGastoCx")
                                        {
                                            CuentaGasto = desiredNode.ChildNodes[i].InnerText;
                                        }
                                        if (desiredNode.ChildNodes[i].LocalName == "xxInformativo")
                                        {
                                            Informativo = desiredNode.ChildNodes[i].InnerText == "SI" ? "1" : "0";
                                        }
                                        if (desiredNode.ChildNodes[i].LocalName == "xxPaqueteInv")
                                        {
                                            Pax = desiredNode.ChildNodes[i].InnerText == "SI" ? "1" : "0";
                                        }
                                    }
                                }
                                Categories = GetCategories(ItemN, AirportText);
                            }
                        }
                        else
                        {
                            Dictionary<string, string> test = new Dictionary<string, string>();
                            AutoCompleteStringCollection dataCollection = new AutoCompleteStringCollection();
                            XmlNodeList nodeList = xmlDoc.SelectNodes("//ns0:Value", nms);
                            foreach (XmlNode node in nodeList)
                            {
                                string INum = "";
                                string IDesc = "";
                                if (node.HasChildNodes)
                                {
                                    if (node.LocalName == "Value")
                                    {
                                        XmlNodeList nodeListvalue = node.ChildNodes;
                                        foreach (XmlNode nodeValue in nodeListvalue)
                                        {
                                            if (nodeValue.LocalName == "ItemNumber")
                                            {
                                                INum = nodeValue.InnerText;
                                            }
                                            if (nodeValue.LocalName == "ItemDescription")
                                            {
                                                IDesc = nodeValue.InnerText.ToUpper();
                                                dataCollection.Add(IDesc);
                                            }
                                        }
                                    }
                                    test.Add(INum, IDesc);

                                }
                            }
                            if (test.Count > 0)
                            {
                                CboProductos.DataSource = new BindingSource(test.OrderBy(item => item.Value), null);
                                CboProductos.DisplayMember = "Value";
                                CboProductos.ValueMember = "Key";
                                string value = ((KeyValuePair<string, string>)CboProductos.SelectedItem).Value;
                                CboProductos.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                                CboProductos.AutoCompleteSource = AutoCompleteSource.ListItems;
                                AutoCompleteStringCollection combData = dataCollection;
                                CboProductos.AutoCompleteCustomSource = combData;
                                getSuppliers();


                            }
                            else
                            {
                                CboProductos.Enabled = false;
                                btnAdd.Enabled = false;
                            }
                        }
                    }
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetPData" + ex.Message + "DEtalle: " + ex.StackTrace);
            }
        }
        public string GetCategories(string ItemN, string Airport)
        {
            try
            {
                string cats = "";
                string envelope = "<soapenv:Envelope" +
                "   xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\"" +
                "   xmlns:typ=\"http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/types/\"" +
                "   xmlns:typ1=\"http://xmlns.oracle.com/adf/svc/types/\">" +
                "<soapenv:Header/>" +
                "<soapenv:Body>" +
       "<typ:findItem>" +
           "<typ:findCriteria>" +
               "<typ1:fetchStart>0</typ1:fetchStart>" +
               "<typ1:fetchSize>-1</typ1:fetchSize>" +
               "<typ1:filter>" +
                   "<typ1:group>" +
                       "<typ1:item>" +
                           "<typ1:conjunction>And</typ1:conjunction>" +
                           "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                           "<typ1:attribute>ItemNumber</typ1:attribute>" +
                           "<typ1:operator>=</typ1:operator>" +
                           "<typ1:value>" + ItemN + "</typ1:value>" +
                       "</typ1:item>" +
                       "<typ1:item>" +
                           "<typ1:conjunction>And</typ1:conjunction>" +
                           "<typ1:upperCaseCompare>true</typ1:upperCaseCompare>" +
                           "<typ1:attribute>OrganizationCode</typ1:attribute>" +
                           "<typ1:operator>=</typ1:operator>" +
                           "<typ1:value>IO_AEREO_" + Airport + "</typ1:value>" +
                       "</typ1:item>" +
                   "</typ1:group>" +
               "</typ1:filter>" +
               "<typ1:findAttribute>ItemCategory</typ1:findAttribute>" +
           "</typ:findCriteria>" +
           "<typ:findControl>" +
               "<typ1:retrieveAllTranslations>true</typ1:retrieveAllTranslations>" +
           "</typ:findControl>" +
       "</typ:findItem>" +
   "</soapenv:Body>" +
"</soapenv:Envelope>";
                byte[] byteArray = Encoding.UTF8.GetBytes(envelope);
                byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes("itotal" + ":" + "Oracle123");
                string credentials = System.Convert.ToBase64String(toEncodeAsBytes);
                HttpWebRequest request =
                 (HttpWebRequest)WebRequest.Create("https://egqy-test.fa.us6.oraclecloud.com:443/fscmService/ItemServiceV2");
                request.Method = "POST";
                request.ContentType = "text/xml;charset=UTF-8";
                request.ContentLength = byteArray.Length;
                request.Headers.Add("Authorization", "Basic " + credentials);
                request.Headers.Add("SOAPAction", "http://xmlns.oracle.com/apps/scm/productModel/items/fscmService/ItemServiceV2");
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result = "";
                using (WebResponse responseComponent = request.GetResponse())
                {
                    using (Stream stream = responseComponent.GetResponseStream())
                    {
                        doc = XDocument.Load(stream);
                        result = doc.ToString();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);
                        XmlNamespaceManager nms = new XmlNamespaceManager(xmlDoc.NameTable);
                        nms.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                        nms.AddNamespace("wsa", "http://www.w3.org/2005/08/addressing");
                        nms.AddNamespace("typ", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/types/");
                        nms.AddNamespace("ns1", "http://xmlns.oracle.com/apps/scm/productModel/items/itemServiceV2/");
                        XmlNodeList nodeList = xmlDoc.SelectNodes("//ns1:ItemCategory", nms);
                        foreach (XmlNode node in nodeList)
                        {
                            ComponentChild component = new ComponentChild();
                            if (node.HasChildNodes)
                            {
                                if (node.LocalName == "ItemCategory")
                                {
                                    XmlNodeList nodeListvalue = node.ChildNodes;
                                    foreach (XmlNode nodeValue in nodeListvalue)
                                    {
                                        if (nodeValue.LocalName == "CategoryName")
                                        {
                                            cats += nodeValue.InnerText + "+";
                                        }
                                    }
                                }
                            }

                        }
                        responseComponent.Close();
                    }
                }

                return cats;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.InnerException.ToString());
                return "";
            }
        }
        public void LlenarValoresServicio()
        {
            try
            {
                PayableService = recordContext.GetWorkspaceRecord("CO$Payables") as IGenericObject;
                IList<IGenericField> fields = PayableService.GenericFields;
                foreach (IGenericField genField in fields)
                {

                    if (genField.Name == "CuentaGasto")
                    {
                        genField.DataValue.Value = CuentaGasto;
                    }
                    if (genField.Name == "CategoriaRoyalty")
                    {
                        genField.DataValue.Value = ParticipacionCobro;
                    }
                    if (genField.Name == "ItemDescription")
                    {
                        if (SRType == "CATERING")
                        {
                            genField.DataValue.Value = CboProductos.Text.Trim();
                        }
                        else
                        {
                            genField.DataValue.Value = ItemDesc.Trim().TrimStart().TrimEnd();
                        }
                    }
                    if (genField.Name == "ItemNumber")
                    {
                        if (SRType == "CATERING")
                        {
                            genField.DataValue.Value = CboProductos.SelectedValue.ToString().Trim();
                        }
                        else
                        {
                            genField.DataValue.Value = txtItemNumber.Text.Trim().TrimStart().TrimEnd();
                        }
                    }
                    if (genField.Name == "Pagos")
                    {
                        genField.DataValue.Value = Pagos;
                    }
                    if (genField.Name == "ParticipacionCobro")
                    {
                        genField.DataValue.Value = ParticipacionCobro;
                    }
                    if (genField.Name == "ClasificacionPagos")
                    {
                        genField.DataValue.Value = ClasificacionPago;
                    }
                    if (genField.Name == "Airport")
                    {
                        genField.DataValue.Value = AirtportText;
                    }
                    if (genField.Name == "Paquete")
                    {
                        genField.DataValue.Value = Pax;
                    }
                    if (genField.Name == "Informativo")
                    {

                        genField.DataValue.Value = Informativo;
                    }
                    if (genField.Name == "Categories")
                    {
                        genField.DataValue.Value = Categories;
                    }
                    if (genField.Name == "Supplier")
                    {
                        genField.DataValue.Value = cboSuppliers.Text.Trim();
                    }
                    if (genField.Name == "TicketAmount")
                    {
                        genField.DataValue.Value = Prices;
                    }
                    if (genField.Name == "UnitCost")
                    {
                        genField.DataValue.Value = Prices;
                    }
                    if (genField.Name == "Quantity")
                    {
                        genField.DataValue.Value = "1";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("LlenarValores: " + ex.Message + "Det: " + ex.StackTrace);
            }

        }
        private void getSuppliers()
        {
            cboSuppliers.DataSource = null;
            cboSuppliers.Enabled = false;
            try
            {

                string envelope = "<soap:Envelope " +
               "	xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\"" +
    "	xmlns:pub=\"http://xmlns.oracle.com/oxp/service/PublicReportService\">" +
     "<soap:Header/>" +
    "	<soap:Body>" +
    "		<pub:runReport>" +
    "			<pub:reportRequest>" +
    "			<pub:attributeFormat>xml</pub:attributeFormat>" +
    "				<pub:attributeLocale></pub:attributeLocale>" +
    "				<pub:attributeTemplate></pub:attributeTemplate>" +
    "				<pub:reportAbsolutePath>Custom/Integracion/XX_ITEM_SUPPLIER_ORG_REP.xdo</pub:reportAbsolutePath>" +
    "				<pub:sizeOfDataChunkDownload>-1</pub:sizeOfDataChunkDownload>" +
              " <pub:parameterNameValues>" +
                                "<pub:item>" +
                                    "<pub:name>pAereo</pub:name> " +
                                    "<pub:values> " +
                                        "<pub:item>IO_AEREO_" + AirtportText + "</pub:item> " +
                                    "</pub:values> " +
                                "</pub:item> " +
                                "<pub:item> " +
                                   "<pub:name>pItem</pub:name>" +
                                    "<pub:values>" +
                                        "<pub:item>" + (SRType == "CATERING" ? "CATEIOT0081" : txtItemNumber.Text) + "</pub:item>" +
                                    "</pub:values>" +
                                "</pub:item>" +
                            "</pub:parameterNameValues>" +
                "</pub:reportRequest>" +
                "</pub:runReport>" +
                "</soap:Body>" +
    "</soap:Envelope>";
                globalContext.LogMessage(envelope);
                byte[] byteArray = Encoding.UTF8.GetBytes(envelope);
                // Construct the base 64 encoded string used as credentials for the service call
                byte[] toEncodeAsBytes = ASCIIEncoding.ASCII.GetBytes("itotal" + ":" + "Oracle123");
                string credentials = Convert.ToBase64String(toEncodeAsBytes);
                // Create HttpWebRequest connection to the service
                HttpWebRequest request =
                 (HttpWebRequest)WebRequest.Create("https://egqy-test.fa.us6.oraclecloud.com:443/xmlpserver/services/ExternalReportWSSService");
                // Configure the request content type to be xml, HTTP method to be POST, and set the content length
                request.Method = "POST";

                request.ContentType = "application/soap+xml; charset=UTF-8;action=\"\"";
                request.ContentLength = byteArray.Length;
                // Configure the request to use basic authentication, with base64 encoded user name and password, to invoke the service.
                request.Headers.Add("Authorization", "Basic " + credentials);

                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                // Write the xml payload to the request
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result;
                Dictionary<string, string> test = new Dictionary<string, string>();
                List<Sup> sups = new List<Sup>();
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        doc = XDocument.Load(stream);
                        result = doc.ToString();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);

                        XmlNamespaceManager nms = new XmlNamespaceManager(xmlDoc.NameTable);
                        nms.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                        nms.AddNamespace("ns2", "http://xmlns.oracle.com/oxp/service/PublicReportService");

                        XmlNode desiredNode = xmlDoc.SelectSingleNode("//ns2:runReportReturn", nms);
                        if (desiredNode != null)
                        {
                            if (desiredNode.HasChildNodes)
                            {
                                for (int i = 0; i < desiredNode.ChildNodes.Count; i++)
                                {
                                    if (desiredNode.ChildNodes[i].LocalName == "reportBytes")
                                    {
                                        byte[] data = Convert.FromBase64String(desiredNode.ChildNodes[i].InnerText);
                                        string decodedString = Encoding.UTF8.GetString(data);
                                        globalContext.LogMessage(decodedString);
                                        XmlTextReader reader = new XmlTextReader(new System.IO.StringReader(decodedString));
                                        reader.Read();
                                        XmlSerializer serializer = new XmlSerializer(typeof(DATA_DS_ITEMSUP));
                                        DATA_DS_ITEMSUP res = (DATA_DS_ITEMSUP)serializer.Deserialize(reader);
                                        var lista = res.G_N_ITEMSUP.Find(x => (x.ORGANIZATION_CODE.Trim() == "IO_AEREO_" + AirtportText));
                                        if (lista != null)
                                        {
                                            foreach (G_1_ITEMSUP item in lista.G_1_ITEMSUP)
                                            {
                                                if (item.ITEM_NUMBER == (SRType == "CATERING" ? "CATEIOT0081" : txtItemNumber.Text))
                                                {
                                                    Sup sup = new Sup();
                                                    sup.Id = item.VENDOR_ID;
                                                    sup.Name = item.PARTY_NAME;
                                                    sups.Add(sup);
                                                }

                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                test = sups.DistinctBy(y => y.Id).ToDictionary(k => k.Id, k => k.Name);
                test.Add("0", "NO SUPPLIER");
                cboSuppliers.DataSource = test.ToArray();
                cboSuppliers.DisplayMember = "Value";
                cboSuppliers.ValueMember = "Key";
                cboSuppliers.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetSupliers" + ex.Message + "DEtalle: " + ex.StackTrace);
            }
        }

        private void CboProductos_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                GetPData(AirtportText, CboProductos.SelectedValue.ToString());
                getSuppliers();
            }
            catch (Exception ex)
            {
                MessageBox.Show("CboProductos_SelectedIndexChanged" + ex.Message + "DEtalle: " + ex.StackTrace);
            }
        }

        private void txtItemNumber_TextChanged(object sender, EventArgs e)
        {
            GetPData(AirtportText, txtItemNumber.Text);
            getSuppliers();
        }

        public string getPassword(string application)
        {
            string password = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT Password FROM CO.Password WHERE Aplicacion='" + application + "'";
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    password = String.IsNullOrEmpty(data) ? "" : data;
                }
            }
            return password;
        }


        private double GetPrices(string airport, string itemn)
        {
            double price = 0;
            try
            {
                airport = "IO_AEREO_" + airport;
                var client = new RestClient("https://iccs.bigmachines.com/");
                string User = Encoding.UTF8.GetString(Convert.FromBase64String("aW1wbGVtZW50YWRvcg=="));
                string Pass = Encoding.UTF8.GetString(Convert.FromBase64String("U2luZXJneTIwMTgu"));
                client.Authenticator = new HttpBasicAuthenticator("servicios", CPQPass);
                string definicion = "?totalResults=true&q={str_item_number:'" + itemn + "',str_icao_iata_code: '" + airport + "'}";
                globalContext.LogMessage(definicion);
                var request = new RestRequest("rest/v6/customPrecios/" + definicion, Method.GET);
                IRestResponse response = client.Execute(request);
                ClaseParaPrecios.RootObject rootObjectPrices = JsonConvert.DeserializeObject<ClaseParaPrecios.RootObject>(response.Content);
                if (rootObjectPrices != null && rootObjectPrices.items.Count > 0)
                {
                    foreach (ClaseParaPrecios.Item item in rootObjectPrices.items)
                    {
                        price = item.flo_amount;
                    }
                }
                else
                {
                    price = 0;
                }
                return price;
            }
            catch (Exception ex)
            {
                globalContext.LogMessage("GetPrices: " + ex.Message + "Detalle: " + ex.StackTrace);

                return 0;
            }
        }
        private void cboSuppliers_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private double getExchangeRate(DateTime date)
        {
            try
            {
                double rate = 1;
                string envelope = "<soap:Envelope " +
                "	xmlns:soap=\"http://www.w3.org/2003/05/soap-envelope\"" +
     "	xmlns:pub=\"http://xmlns.oracle.com/oxp/service/PublicReportService\">" +
       "<soap:Header/>" +
     "	<soap:Body>" +
     "		<pub:runReport>" +
     "			<pub:reportRequest>" +
     "			<pub:attributeFormat>xml</pub:attributeFormat>" +
     "				<pub:attributeLocale>en</pub:attributeLocale>" +
     "				<pub:attributeTemplate>default</pub:attributeTemplate>" +

                 "<pub:parameterNameValues>" +
                      "<pub:item>" +
                   "<pub:name>P_EXCHANGE_DATE</pub:name>" +
                   "<pub:values>" +
                      "<pub:item>" + date.ToString("yyyy-MM-dd") + "</pub:item>" +
                   "</pub:values>" +
                "</pub:item>" +
                 "</pub:parameterNameValues>" +

     "				<pub:reportAbsolutePath>Custom/Integracion/XX_DAILY_RATES_REP.xdo</pub:reportAbsolutePath>" +
     "				<pub:sizeOfDataChunkDownload>-1</pub:sizeOfDataChunkDownload>" +
     "			</pub:reportRequest>" +
     "		</pub:runReport>" +
     "	</soap:Body>" +
     "</soap:Envelope>";
                globalContext.LogMessage("Payables Get Exchange Date Rate:" + envelope);
                byte[] byteArray = Encoding.UTF8.GetBytes(envelope);
                // Construct the base 64 encoded string used as credentials for the service call
                byte[] toEncodeAsBytes = ASCIIEncoding.ASCII.GetBytes("itotal" + ":" + "Oracle123");
                string credentials = Convert.ToBase64String(toEncodeAsBytes);
                // Create HttpWebRequest connection to the service
                HttpWebRequest request =
                 (HttpWebRequest)WebRequest.Create("https://egqy-test.fa.us6.oraclecloud.com:443/xmlpserver/services/ExternalReportWSSService");
                // Configure the request content type to be xml, HTTP method to be POST, and set the content length
                request.Method = "POST";

                request.ContentType = "application/soap+xml; charset=UTF-8;action=\"\"";
                request.ContentLength = byteArray.Length;
                // Configure the request to use basic authentication, with base64 encoded user name and password, to invoke the service.
                request.Headers.Add("Authorization", "Basic " + credentials);
                // Set the SOAP action to be invoked; while the call works without this, the value is expected to be set based as per standards
                //request.Headers.Add("SOAPAction", "http://xmlns.oracle.com/apps/cdm/foundation/parties/organizationService/applicationModule/findOrganizationProfile");
                // Write the xml payload to the request
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                // Write the xml payload to the request
                XDocument doc;
                XmlDocument docu = new XmlDocument();
                string result;
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        doc = XDocument.Load(stream);
                        result = doc.ToString();
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(result);
                        XmlNamespaceManager nms = new XmlNamespaceManager(xmlDoc.NameTable);
                        nms.AddNamespace("env", "http://schemas.xmlsoap.org/soap/envelope/");
                        nms.AddNamespace("ns2", "http://xmlns.oracle.com/oxp/service/PublicReportService");

                        XmlNode desiredNode = xmlDoc.SelectSingleNode("//ns2:runReportReturn", nms);
                        if (desiredNode.HasChildNodes)
                        {
                            for (int i = 0; i < desiredNode.ChildNodes.Count; i++)
                            {
                                if (desiredNode.ChildNodes[i].LocalName == "reportBytes")
                                {
                                    byte[] data = Convert.FromBase64String(desiredNode.ChildNodes[i].InnerText);
                                    string decodedString = Encoding.UTF8.GetString(data);
                                    XmlTextReader reader = new XmlTextReader(new System.IO.StringReader(decodedString));
                                    reader.Read();
                                    XmlSerializer serializer = new XmlSerializer(typeof(DATA_DS_RATES));
                                    DATA_DS_RATES res = (DATA_DS_RATES)serializer.Deserialize(reader);
                                    if (res.G_N_RATES != null)
                                    {
                                        rate = Convert.ToDouble(res.G_N_RATES.G_1_RATES.CONVERSION_RATE);
                                    }
                                }
                            }
                        }
                    }
                }

                return rate;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                return 1;
            }
        }



    }
    public class ComponentChild
    {
        public string Airport
        { get; set; }
        public string CategoriaRoyalty
        { get; set; }
        public string ClasificacionPagos
        { get; set; }
        public string Componente
        { get; set; }
        public string Costo
        { get; set; }
        public string CuentaGasto
        { get; set; }
        public int Incident
        { get; set; }
        public string Informativo
        { get; set; }
        public string ItemDescription
        { get; set; }
        public string ItemNumber
        { get; set; }
        public int Itinerary
        { get; set; }
        public string Pagos
        { get; set; }
        public string Paquete
        { get; set; }
        public string ParticipacionCobro
        { get; set; }
        public string Precio
        { get; set; }
    }
    public class Sup
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "G_1_ITEMSUP")]
    public class G_1_ITEMSUP
    {
        [XmlElement(ElementName = "ASL_ID")]
        public string ASL_ID { get; set; }
        [XmlElement(ElementName = "VENDOR_ID")]
        public string VENDOR_ID { get; set; }
        [XmlElement(ElementName = "PARTY_ID")]
        public string PARTY_ID { get; set; }
        [XmlElement(ElementName = "PARTY_NUMBER")]
        public string PARTY_NUMBER { get; set; }
        [XmlElement(ElementName = "PARTY_NAME")]
        public string PARTY_NAME { get; set; }
        [XmlElement(ElementName = "INVENTORY_ITEM_ID")]
        public string INVENTORY_ITEM_ID { get; set; }
        [XmlElement(ElementName = "ITEM_NUMBER")]
        public string ITEM_NUMBER { get; set; }
        [XmlElement(ElementName = "DESCRIPTION")]
        public string DESCRIPTION { get; set; }
        [XmlElement(ElementName = "PRIMARY_UOM_CODE")]
        public string PRIMARY_UOM_CODE { get; set; }
    }

    [XmlRoot(ElementName = "G_N_ITEMSUP")]
    public class G_N_ITEMSUP
    {
        [XmlElement(ElementName = "ORGANIZATION_CODE")]
        public string ORGANIZATION_CODE { get; set; }
        [XmlElement(ElementName = "G_1_ITEMSUP")]
        public List<G_1_ITEMSUP> G_1_ITEMSUP { get; set; }
    }

    [XmlRoot(ElementName = "DATA_DS_ITEMSUP")]
    public class DATA_DS_ITEMSUP
    {
        [XmlElement(ElementName = "G_N_ITEMSUP")]
        public List<G_N_ITEMSUP> G_N_ITEMSUP { get; set; }
    }

    //RATES
    [XmlRoot(ElementName = "G_1_RATES")]
    public class G_1_RATES
    {
        [XmlElement(ElementName = "CONVERSION_RATE")]
        public string CONVERSION_RATE { get; set; }
        [XmlElement(ElementName = "CONVERSION_DATE")]
        public string CONVERSION_DATE { get; set; }
    }

    [XmlRoot(ElementName = "G_N_RATES")]
    public class G_N_RATES
    {
        [XmlElement(ElementName = "USER_CONVERSION_TYPE")]
        public string USER_CONVERSION_TYPE { get; set; }
        [XmlElement(ElementName = "G_1_RATES")]
        public G_1_RATES G_1_RATES { get; set; }
    }

    [XmlRoot(ElementName = "DATA_DS_RATES")]
    public class DATA_DS_RATES
    {
        [XmlElement(ElementName = "P_EXCHANGE_DATE")]
        public string P_EXCHANGE_DATE { get; set; }
        [XmlElement(ElementName = "G_N_RATES")]
        public G_N_RATES G_N_RATES { get; set; }
    }


}
