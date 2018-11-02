using System;
using System.AddIn;
using System.Drawing;
using System.Windows.Forms;
using RightNow.AddIns.AddInViews;


namespace PayablesAddProduct
{
    public class Component : IWorkspaceComponent2
    {
        private Payables control;


        public Component(bool inDesignMode, IRecordContext recordContext, IGlobalContext globalContext)
        {

            control = new Payables(inDesignMode, recordContext, globalContext);
            if (!inDesignMode)
            {
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

        public void RuleActionInvoked(string actionName)
        {
            throw new NotImplementedException();
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