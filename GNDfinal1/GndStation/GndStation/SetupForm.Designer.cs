namespace GndStation
{
    partial class SetupForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.LoadParamsBtn = new System.Windows.Forms.Button();
            this.ReadParamsBtn = new System.Windows.Forms.Button();
            this.SaveParamsBtn = new System.Windows.Forms.Button();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.label20 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.Set_param = new System.Windows.Forms.Button();
            this.Param_value = new System.Windows.Forms.TextBox();
            this.Param_name = new System.Windows.Forms.TextBox();
            this.GeoFence_Enable = new System.Windows.Forms.CheckBox();
            this.SafetyMode = new System.Windows.Forms.TrackBar();
            this.SceneFence = new System.Windows.Forms.Button();
            this.Read_escenario = new System.Windows.Forms.Button();
            this.label16 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.GeofenceBox = new System.Windows.Forms.GroupBox();
            this.label18 = new System.Windows.Forms.Label();
            this.CloseBtn = new System.Windows.Forms.Button();
            this.groupBox7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SafetyMode)).BeginInit();
            this.GeofenceBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // LoadParamsBtn
            // 
            this.LoadParamsBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LoadParamsBtn.Location = new System.Drawing.Point(160, 135);
            this.LoadParamsBtn.Name = "LoadParamsBtn";
            this.LoadParamsBtn.Size = new System.Drawing.Size(114, 51);
            this.LoadParamsBtn.TabIndex = 1;
            this.LoadParamsBtn.Text = "Load Parameters";
            this.LoadParamsBtn.UseVisualStyleBackColor = true;
            this.LoadParamsBtn.Click += new System.EventHandler(this.LoadParamsBtn_Click);
            // 
            // ReadParamsBtn
            // 
            this.ReadParamsBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ReadParamsBtn.Location = new System.Drawing.Point(160, 12);
            this.ReadParamsBtn.Name = "ReadParamsBtn";
            this.ReadParamsBtn.Size = new System.Drawing.Size(114, 51);
            this.ReadParamsBtn.TabIndex = 2;
            this.ReadParamsBtn.Text = "Read Parameters";
            this.ReadParamsBtn.UseVisualStyleBackColor = true;
            this.ReadParamsBtn.Click += new System.EventHandler(this.ReadParamsBtn_Click);
            // 
            // SaveParamsBtn
            // 
            this.SaveParamsBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SaveParamsBtn.Location = new System.Drawing.Point(160, 71);
            this.SaveParamsBtn.Name = "SaveParamsBtn";
            this.SaveParamsBtn.Size = new System.Drawing.Size(114, 51);
            this.SaveParamsBtn.TabIndex = 3;
            this.SaveParamsBtn.Text = "Save Parameters";
            this.SaveParamsBtn.UseVisualStyleBackColor = true;
            this.SaveParamsBtn.Click += new System.EventHandler(this.SaveParamsBtn_Click);
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.label20);
            this.groupBox7.Controls.Add(this.label19);
            this.groupBox7.Controls.Add(this.Set_param);
            this.groupBox7.Controls.Add(this.Param_value);
            this.groupBox7.Controls.Add(this.Param_name);
            this.groupBox7.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox7.Location = new System.Drawing.Point(160, 257);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(320, 102);
            this.groupBox7.TabIndex = 49;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "Parameter Set";
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label20.Location = new System.Drawing.Point(115, 66);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(44, 18);
            this.label20.TabIndex = 38;
            this.label20.Text = "Value";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label19.Location = new System.Drawing.Point(22, 66);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(48, 18);
            this.label19.TabIndex = 37;
            this.label19.Text = "Name";
            // 
            // Set_param
            // 
            this.Set_param.Location = new System.Drawing.Point(163, 29);
            this.Set_param.Name = "Set_param";
            this.Set_param.Size = new System.Drawing.Size(66, 27);
            this.Set_param.TabIndex = 28;
            this.Set_param.Text = "Set";
            this.Set_param.UseVisualStyleBackColor = true;
            this.Set_param.Click += new System.EventHandler(this.Set_param_Click);
            // 
            // Param_value
            // 
            this.Param_value.Location = new System.Drawing.Point(117, 29);
            this.Param_value.Name = "Param_value";
            this.Param_value.Size = new System.Drawing.Size(40, 29);
            this.Param_value.TabIndex = 14;
            // 
            // Param_name
            // 
            this.Param_name.Location = new System.Drawing.Point(11, 28);
            this.Param_name.Name = "Param_name";
            this.Param_name.Size = new System.Drawing.Size(100, 29);
            this.Param_name.TabIndex = 13;
            // 
            // GeoFence_Enable
            // 
            this.GeoFence_Enable.AutoSize = true;
            this.GeoFence_Enable.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GeoFence_Enable.Location = new System.Drawing.Point(56, 39);
            this.GeoFence_Enable.Name = "GeoFence_Enable";
            this.GeoFence_Enable.Size = new System.Drawing.Size(65, 19);
            this.GeoFence_Enable.TabIndex = 59;
            this.GeoFence_Enable.Text = "Enable";
            this.GeoFence_Enable.UseVisualStyleBackColor = true;
            this.GeoFence_Enable.CheckedChanged += new System.EventHandler(this.GeoFence_Enable_CheckedChanged);
            // 
            // SafetyMode
            // 
            this.SafetyMode.LargeChange = 1;
            this.SafetyMode.Location = new System.Drawing.Point(36, 62);
            this.SafetyMode.Maximum = 3;
            this.SafetyMode.Minimum = 1;
            this.SafetyMode.Name = "SafetyMode";
            this.SafetyMode.Size = new System.Drawing.Size(104, 45);
            this.SafetyMode.TabIndex = 60;
            this.SafetyMode.Value = 2;
            // 
            // SceneFence
            // 
            this.SceneFence.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SceneFence.Location = new System.Drawing.Point(36, 123);
            this.SceneFence.Name = "SceneFence";
            this.SceneFence.Size = new System.Drawing.Size(100, 38);
            this.SceneFence.TabIndex = 61;
            this.SceneFence.Text = "Escenario";
            this.SceneFence.UseVisualStyleBackColor = true;
            this.SceneFence.Click += new System.EventHandler(this.SceneFence_Click);
            // 
            // Read_escenario
            // 
            this.Read_escenario.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Read_escenario.Location = new System.Drawing.Point(36, 178);
            this.Read_escenario.Name = "Read_escenario";
            this.Read_escenario.Size = new System.Drawing.Size(100, 55);
            this.Read_escenario.TabIndex = 62;
            this.Read_escenario.Text = "Read Existing";
            this.Read_escenario.UseVisualStyleBackColor = true;
            this.Read_escenario.Click += new System.EventHandler(this.Read_SceneFence_Click);
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.Location = new System.Drawing.Point(66, 94);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(36, 16);
            this.label16.TabIndex = 64;
            this.label16.Text = "RTL";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.Location = new System.Drawing.Point(19, 94);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(41, 16);
            this.label17.TabIndex = 65;
            this.label17.Text = "Land";
            // 
            // GeofenceBox
            // 
            this.GeofenceBox.Controls.Add(this.label18);
            this.GeofenceBox.Controls.Add(this.Read_escenario);
            this.GeofenceBox.Controls.Add(this.label17);
            this.GeofenceBox.Controls.Add(this.GeoFence_Enable);
            this.GeofenceBox.Controls.Add(this.label16);
            this.GeofenceBox.Controls.Add(this.SafetyMode);
            this.GeofenceBox.Controls.Add(this.SceneFence);
            this.GeofenceBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GeofenceBox.Location = new System.Drawing.Point(300, 12);
            this.GeofenceBox.Name = "GeofenceBox";
            this.GeofenceBox.Size = new System.Drawing.Size(180, 239);
            this.GeofenceBox.TabIndex = 66;
            this.GeofenceBox.TabStop = false;
            this.GeofenceBox.Text = "Geofence";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.Location = new System.Drawing.Point(108, 94);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(48, 16);
            this.label18.TabIndex = 66;
            this.label18.Text = "Brake";
            // 
            // CloseBtn
            // 
            this.CloseBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CloseBtn.Location = new System.Drawing.Point(160, 200);
            this.CloseBtn.Name = "CloseBtn";
            this.CloseBtn.Size = new System.Drawing.Size(114, 51);
            this.CloseBtn.TabIndex = 67;
            this.CloseBtn.Text = "Close";
            this.CloseBtn.UseVisualStyleBackColor = true;
            this.CloseBtn.Click += new System.EventHandler(this.CloseBtn_Click);
            // 
            // SetupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1105, 582);
            this.Controls.Add(this.CloseBtn);
            this.Controls.Add(this.GeofenceBox);
            this.Controls.Add(this.groupBox7);
            this.Controls.Add(this.SaveParamsBtn);
            this.Controls.Add(this.ReadParamsBtn);
            this.Controls.Add(this.LoadParamsBtn);
            this.Name = "SetupForm";
            this.Text = "SetupForm";
            this.groupBox7.ResumeLayout(false);
            this.groupBox7.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SafetyMode)).EndInit();
            this.GeofenceBox.ResumeLayout(false);
            this.GeofenceBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button LoadParamsBtn;
        private System.Windows.Forms.Button ReadParamsBtn;
        private System.Windows.Forms.Button SaveParamsBtn;
        private System.Windows.Forms.GroupBox groupBox7;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Button Set_param;
        private System.Windows.Forms.TextBox Param_value;
        private System.Windows.Forms.TextBox Param_name;
        private System.Windows.Forms.CheckBox GeoFence_Enable;
        private System.Windows.Forms.TrackBar SafetyMode;
        private System.Windows.Forms.Button SceneFence;
        private System.Windows.Forms.Button Read_escenario;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.GroupBox GeofenceBox;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Button CloseBtn;
    }
}