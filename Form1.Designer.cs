namespace Serum_dynamizer
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tNativeSerum = new TextBox();
            label1 = new Label();
            bNativeSerum = new Button();
            bHomeoSerum = new Button();
            label2 = new Label();
            tHomeoSerum = new TextBox();
            bDynamize = new Button();
            textBox3 = new TextBox();
            SuspendLayout();
            // 
            // tNativeSerum
            // 
            tNativeSerum.Location = new Point(120, 34);
            tNativeSerum.Name = "tNativeSerum";
            tNativeSerum.ReadOnly = true;
            tNativeSerum.Size = new Size(350, 23);
            tNativeSerum.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 16);
            label1.Name = "label1";
            label1.Size = new Size(199, 15);
            label1.TabIndex = 1;
            label1.Text = "Choose the source native Serum file:";
            // 
            // bNativeSerum
            // 
            bNativeSerum.Location = new Point(12, 32);
            bNativeSerum.Name = "bNativeSerum";
            bNativeSerum.Size = new Size(102, 25);
            bNativeSerum.TabIndex = 2;
            bNativeSerum.Text = "Browse";
            bNativeSerum.UseVisualStyleBackColor = true;
            bNativeSerum.Click += bNativeSerum_Click;
            // 
            // bHomeoSerum
            // 
            bHomeoSerum.Location = new Point(12, 94);
            bHomeoSerum.Name = "bHomeoSerum";
            bHomeoSerum.Size = new Size(102, 25);
            bHomeoSerum.TabIndex = 5;
            bHomeoSerum.Text = "Browse";
            bHomeoSerum.UseVisualStyleBackColor = true;
            bHomeoSerum.Click += bHomeoSerum_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 78);
            label2.Name = "label2";
            label2.Size = new Size(303, 15);
            label2.TabIndex = 4;
            label2.Text = "Choose the destination \"homeopathic Serum\" directory:";
            // 
            // tHomeoSerum
            // 
            tHomeoSerum.Location = new Point(120, 96);
            tHomeoSerum.Name = "tHomeoSerum";
            tHomeoSerum.ReadOnly = true;
            tHomeoSerum.Size = new Size(350, 23);
            tHomeoSerum.TabIndex = 3;
            // 
            // bDynamize
            // 
            bDynamize.Font = new Font("Segoe UI", 27.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            bDynamize.Location = new Point(130, 145);
            bDynamize.Name = "bDynamize";
            bDynamize.Size = new Size(213, 86);
            bDynamize.TabIndex = 6;
            bDynamize.Text = "Dynamize!";
            bDynamize.UseVisualStyleBackColor = true;
            bDynamize.Click += bDynamize_Click;
            // 
            // textBox3
            // 
            textBox3.Location = new Point(494, 23);
            textBox3.Multiline = true;
            textBox3.Name = "textBox3";
            textBox3.ReadOnly = true;
            textBox3.Size = new Size(288, 214);
            textBox3.TabIndex = 7;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 258);
            Controls.Add(textBox3);
            Controls.Add(bDynamize);
            Controls.Add(bHomeoSerum);
            Controls.Add(label2);
            Controls.Add(tHomeoSerum);
            Controls.Add(bNativeSerum);
            Controls.Add(label1);
            Controls.Add(tNativeSerum);
            Name = "Form1";
            Text = "Serum Dynamizer v1.0";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox tNativeSerum;
        private Label label1;
        private Button bNativeSerum;
        private Button bHomeoSerum;
        private Label label2;
        private TextBox tHomeoSerum;
        private Button bDynamize;
        private TextBox textBox3;
    }
}
