namespace PayablesAddProduct
{
    partial class Payables
    {
        /// <summary> 
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de componentes

        /// <summary> 
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.CboProductos = new System.Windows.Forms.ComboBox();
            this.cboSuppliers = new System.Windows.Forms.ComboBox();
            this.lblProduct = new System.Windows.Forms.Label();
            this.lblSupplier = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btnAdd = new System.Windows.Forms.Button();
            this.txtItemNumber = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // CboProductos
            // 
            this.CboProductos.FormattingEnabled = true;
            this.CboProductos.Location = new System.Drawing.Point(81, 31);
            this.CboProductos.Name = "CboProductos";
            this.CboProductos.Size = new System.Drawing.Size(350, 21);
            this.CboProductos.TabIndex = 0;
            this.CboProductos.SelectedIndexChanged += new System.EventHandler(this.CboProductos_SelectedIndexChanged);
            // 
            // cboSuppliers
            // 
            this.cboSuppliers.FormattingEnabled = true;
            this.cboSuppliers.Location = new System.Drawing.Point(81, 58);
            this.cboSuppliers.Name = "cboSuppliers";
            this.cboSuppliers.Size = new System.Drawing.Size(350, 21);
            this.cboSuppliers.TabIndex = 1;
            this.cboSuppliers.SelectedIndexChanged += new System.EventHandler(this.cboSuppliers_SelectedIndexChanged);
            // 
            // lblProduct
            // 
            this.lblProduct.AutoSize = true;
            this.lblProduct.Location = new System.Drawing.Point(17, 39);
            this.lblProduct.Name = "lblProduct";
            this.lblProduct.Size = new System.Drawing.Size(44, 13);
            this.lblProduct.TabIndex = 2;
            this.lblProduct.Text = "Product";
            // 
            // lblSupplier
            // 
            this.lblSupplier.AutoSize = true;
            this.lblSupplier.Location = new System.Drawing.Point(17, 66);
            this.lblSupplier.Name = "lblSupplier";
            this.lblSupplier.Size = new System.Drawing.Size(45, 13);
            this.lblSupplier.TabIndex = 3;
            this.lblSupplier.Text = "Supplier";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(78, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Add Payable";
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(81, 85);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 5;
            this.btnAdd.Text = "Add";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // txtItemNumber
            // 
            this.txtItemNumber.Enabled = false;
            this.txtItemNumber.Location = new System.Drawing.Point(81, 32);
            this.txtItemNumber.Name = "txtItemNumber";
            this.txtItemNumber.Size = new System.Drawing.Size(350, 20);
            this.txtItemNumber.TabIndex = 6;
            this.txtItemNumber.TextChanged += new System.EventHandler(this.txtItemNumber_TextChanged);
            // 
            // Payables
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txtItemNumber);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblSupplier);
            this.Controls.Add(this.lblProduct);
            this.Controls.Add(this.cboSuppliers);
            this.Controls.Add(this.CboProductos);
            this.Name = "Payables";
            this.Size = new System.Drawing.Size(453, 124);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox CboProductos;
        private System.Windows.Forms.ComboBox cboSuppliers;
        private System.Windows.Forms.Label lblProduct;
        private System.Windows.Forms.Label lblSupplier;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.TextBox txtItemNumber;
    }
}
