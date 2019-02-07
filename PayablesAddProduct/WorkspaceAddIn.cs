using System;
using System.AddIn;
using System.Collections.Generic;
using System.Drawing;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Windows.Forms;
using PayablesAddProduct.SOAPICCS;
using RightNow.AddIns.AddInViews;
using RightNow.AddIns.Common;

namespace PayablesAddProduct
{
    public class Component : IWorkspaceComponent2
    {
        public RightNowSyncPortClient clientORN { get; private set; }
        private Payables control;
        private IRecordContext _recordContext;
        private IGenericObject Payable;
        private IGlobalContext gcontext;
        public IIncident Incident { get; set; }
        public int IncidentID { get; set; }


        public Component(bool inDesignMode, IRecordContext recordContext, IGlobalContext globalContext)
        {

            control = new Payables(inDesignMode, recordContext, globalContext);
            if (!inDesignMode)
            {
                _recordContext = recordContext;
                gcontext = globalContext;
                recordContext.DataLoaded += (o, e) =>
                {
                    control.LoadData();
                };
            }
        }

        public bool ReadOnly
        {
            get;
            set;
        }

        public void RuleActionInvoked(string ActionName)
        {
            try
            {
                Init();
                if (ActionName == "CalculateTotal")
                {
                    string Quantity = "1";
                    string UnitCost = "0";
                    double Ticket = 0;
                    Payable = _recordContext.GetWorkspaceRecord("CO$Payables") as IGenericObject;
                    IList<IGenericField> genericFields;
                    genericFields = Payable.GenericFields;
                    foreach (IGenericField gen in genericFields)
                    {
                        if (gen.Name == "Quantity")
                        {
                            Quantity = gen.DataValue.Value.ToString();
                        }
                        if (gen.Name == "UnitCost")
                        {
                            UnitCost = gen.DataValue.Value.ToString();
                        }
                    }
                    Ticket = Math.Round(Convert.ToDouble(Quantity) * Convert.ToDouble(UnitCost), 2);
                    foreach (IGenericField gen in genericFields)
                    {
                        if (gen.Name == "TicketAmount")
                        {
                            gen.DataValue.Value = Ticket;
                        }

                    }
                }

                if (ActionName == "ChangeOUM")
                {
                    string OUM = "";
                    string Quantity = "1";
                    string UnitCost = "0";
                    double Ticket = 0;
                    string services = "0";
                    //Incident = (IIncident)_recordContext.GetWorkspaceRecord(WorkspaceRecordType.Incident);
                    //IncidentID = Incident.ID;
                    Payable = _recordContext.GetWorkspaceRecord("CO$Payables") as IGenericObject;
                    IList<IGenericField> genericFields;
                    genericFields = Payable.GenericFields;
                    foreach (IGenericField gen in genericFields)
                    {

                        if (gen.Name == "UOM_Menu")
                        {
                            OUM = gen.DataValue.Value.ToString();
                        }
                        if (gen.Name == "Services")
                        {
                            services = gen.DataValue.Value.ToString();
                        }
                    }
                  
                    IncidentID = GetIncidentId(services);
                    switch (OUM)
                    {
                        //TW
                        case "2":
                            string ICAOId = getICAODesi(IncidentID);
                            Quantity = GetMTOW(ICAOId);
                            break;
                        //HR
                        case "3":

                            break;
                        //HHR
                        case "4":

                            break;
                        //MIN
                        case "5":

                            break;

                    }
                    foreach (IGenericField gen in genericFields)
                    {
                        if (gen.Name == "Quantity")
                        {
                            gen.DataValue.Value = Quantity;
                        }

                    }
                }

            }
            catch (Exception e)
            {
                gcontext.LogMessage(e.Message + "Det" + e.StackTrace);
            }
        }
        public bool Init()
        {
            try
            {
                bool result = false;
                EndpointAddress endPointAddr = new EndpointAddress(gcontext.GetInterfaceServiceUrl(ConnectServiceType.Soap));
                BasicHttpBinding binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential);
                binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
                binding.ReceiveTimeout = new TimeSpan(0, 10, 0);
                binding.MaxReceivedMessageSize = 1048576; //1MB
                binding.SendTimeout = new TimeSpan(0, 10, 0);
                clientORN = new RightNowSyncPortClient(binding, endPointAddr);
                BindingElementCollection elements = clientORN.Endpoint.Binding.CreateBindingElements();
                elements.Find<SecurityBindingElement>().IncludeTimestamp = false;
                clientORN.Endpoint.Binding = new CustomBinding(elements);
                gcontext.PrepareConnectSession(clientORN.ChannelFactory);
                if (clientORN != null)
                {
                    result = true;
                }
                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en INIT: " + ex.Message);
                return false;
            }
        }
        private string GetMTOW(string idICAO)
        {
            try
            {
                string weight = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT Weight FROM CO.AircraftType WHERE ICAODesignator= '" + idICAO + "'";
                clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        weight = data;
                    }
                }
                return String.IsNullOrEmpty(weight) ? "" : weight;
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetMTOW" + ex.Message + "Det:" + ex.StackTrace);
                return "";
            }
        }
        public string getICAODesi(int Incident)
        {
            string Icao = "";
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT CustomFields.co.Aircraft.AircraftType1.ICAODesignator  FROM Incident WHERE ID =" + Incident;
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    Icao = data;
                }
            }
            return Icao;
        }
        public int GetIncidentId(string Service)
        {
            int i = 0;
            ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
            APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
            clientInfoHeader.AppID = "Query Example";
            String queryString = "SELECT Incident.Id FROM CO.Services WHERE ID  =" + Service;
            clientORN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 1, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
            foreach (CSVTable table in queryCSV.CSVTables)
            {
                String[] rowData = table.Rows;
                foreach (String data in rowData)
                {
                    i = string.IsNullOrEmpty(data) ? 0 : Convert.ToInt32(data);
                }
            }
            return i;
        }


        public string RuleConditionInvoked(string conditionName)
        {
            throw new NotImplementedException();
        }

        public Control GetControl()
        {
            return control;

        }
    }


    [AddIn("Workspace Factory AddIn", Version = "1.0.0.0")]
    public class WorkspaceAddInFactory : IWorkspaceComponentFactory2
    {
        IGlobalContext globalContext { get; set; }
        public IWorkspaceComponent2 CreateControl(bool inDesignMode, IRecordContext RecordContext)
        {
            return new Component(inDesignMode, RecordContext, globalContext);
        }
        public Image Image16
        {
            get { return Properties.Resources.AddIn16; }
        }
        public string Text
        {
            get { return "Payables"; }
        }
        public string Tooltip
        {
            get { return "Payables"; }
        }
        public bool Initialize(IGlobalContext GlobalContext)
        {
            this.globalContext = GlobalContext;
            return true;
        }
    }
}