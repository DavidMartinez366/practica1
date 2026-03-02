using System.Collections.Generic;
using System.IO;
namespace Editor_de_texto
{
    partial class Form1
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        StreamWriter Escribir;
        StreamReader Leer;
        int i_caracter, N_error;
        char c_caracter;
        string archivo, archivoback, archivotrad;
        int Numero_linea;
        bool leyendoCondicion = false;
        string ultimoToken = "";
        string elemento;
        private Queue<string> tokenBuffer = new Queue<string>();
        string P_trad = "incluye,principal,auto,sino,largo,cambiar,romper,enumerar,registrar,definir_tipo,caso,externo,retornar,union,caracter,flotante,corto,sin_signo,constante,para,con_signo,vacío,continuar,ir_a,tamano_de,volátil,defecto,si,estático,mientras,hacer,entero,estructura,empaquetado,doble";
        string P_Reservadas = "include,main,auto,else,LONG,switch,break,enum,register,typedef,case,extern,return,union,char,float,SHORT,unsigned,const,for,signed,void,continue,goto,sizeof,volatile,default,if,static,while,do,int,struc,_Packed,double";
        /// palabras reservadas(100)
        /// </summary>
     /* string[] P_Reservadas = new string[] 
        {
             "int", "float", "double", "char", "return", "if", "else", "while", "for", "switch",
             "case", "break", "continue", "default", "do", "void", "struct", "typedef", "union",
             "enum", "goto", "const", "static", "extern", "register", "signed", "unsigned", "short",
             "long", "sizeof", "volatile", "auto", "inline", "restrict", "class", "public", "private",
             "protected", "virtual", "template", "typename", "namespace", "using", "try", "catch",
             "throw", "operator", "new", "delete", "this", "friend", "mutable", "explicit", "export",
             "import", "true", "false", "bool", "wchar_t", "char16_t", "char32_t", "nullptr",
             "switch", "case", "default", "break", "continue", "return", "main", "static_cast",
             "dynamic_cast", "reinterpret_cast", "const_cast", "typeid", "asm", "module", "concept",
             "requires", "co_await", "co_yield", "co_return", "final", "override", "sealed",
             "event", "delegate", "interface", "abstract", "record", "partial", "out", "ref",
             "in", "yield", "lock", "unchecked", "checked", "fixed", "stackalloc", "unsafe"
        }; */

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

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.archivoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nuevoAToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.abrirToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.guardarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.guardarComoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.salirToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.compilarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.compilarSolucionToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.traductorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CajaTexto1 = new System.Windows.Forms.RichTextBox();
            this.CajaTexto2 = new System.Windows.Forms.RichTextBox();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.archivoToolStripMenuItem,
            this.compilarToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1067, 30);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // archivoToolStripMenuItem
            // 
            this.archivoToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.nuevoAToolStripMenuItem,
            this.abrirToolStripMenuItem,
            this.guardarToolStripMenuItem,
            this.guardarComoToolStripMenuItem,
            this.toolStripSeparator1,
            this.salirToolStripMenuItem});
            this.archivoToolStripMenuItem.Name = "archivoToolStripMenuItem";
            this.archivoToolStripMenuItem.Size = new System.Drawing.Size(73, 26);
            this.archivoToolStripMenuItem.Text = "Archivo";
            // 
            // nuevoAToolStripMenuItem
            // 
            this.nuevoAToolStripMenuItem.Name = "nuevoAToolStripMenuItem";
            this.nuevoAToolStripMenuItem.Size = new System.Drawing.Size(187, 26);
            this.nuevoAToolStripMenuItem.Text = "Nuevo ";
            this.nuevoAToolStripMenuItem.Click += new System.EventHandler(this.nuevoAToolStripMenuItem_Click);
            // 
            // abrirToolStripMenuItem
            // 
            this.abrirToolStripMenuItem.Name = "abrirToolStripMenuItem";
            this.abrirToolStripMenuItem.Size = new System.Drawing.Size(187, 26);
            this.abrirToolStripMenuItem.Text = "Abrir";
            this.abrirToolStripMenuItem.Click += new System.EventHandler(this.abrirToolStripMenuItem_Click);
            // 
            // guardarToolStripMenuItem
            // 
            this.guardarToolStripMenuItem.Name = "guardarToolStripMenuItem";
            this.guardarToolStripMenuItem.Size = new System.Drawing.Size(187, 26);
            this.guardarToolStripMenuItem.Text = "Guardar";
            this.guardarToolStripMenuItem.Click += new System.EventHandler(this.guardarToolStripMenuItem_Click);
            // 
            // guardarComoToolStripMenuItem
            // 
            this.guardarComoToolStripMenuItem.Name = "guardarComoToolStripMenuItem";
            this.guardarComoToolStripMenuItem.Size = new System.Drawing.Size(187, 26);
            this.guardarComoToolStripMenuItem.Text = "Guardar como";
            this.guardarComoToolStripMenuItem.Click += new System.EventHandler(this.guardarComoToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(184, 6);
            // 
            // salirToolStripMenuItem
            // 
            this.salirToolStripMenuItem.Name = "salirToolStripMenuItem";
            this.salirToolStripMenuItem.Size = new System.Drawing.Size(187, 26);
            this.salirToolStripMenuItem.Text = "Salir";
            this.salirToolStripMenuItem.Click += new System.EventHandler(this.salirToolStripMenuItem_Click);
            // 
            // compilarToolStripMenuItem
            // 
            this.compilarToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.compilarSolucionToolStripMenuItem1,
            this.traductorToolStripMenuItem});
            this.compilarToolStripMenuItem.Name = "compilarToolStripMenuItem";
            this.compilarToolStripMenuItem.Size = new System.Drawing.Size(84, 26);
            this.compilarToolStripMenuItem.Text = "Compilar";
            // 
            // compilarSolucionToolStripMenuItem1
            // 
            this.compilarSolucionToolStripMenuItem1.Name = "compilarSolucionToolStripMenuItem1";
            this.compilarSolucionToolStripMenuItem1.Size = new System.Drawing.Size(155, 26);
            this.compilarSolucionToolStripMenuItem1.Text = "Compilar";
            this.compilarSolucionToolStripMenuItem1.Click += new System.EventHandler(this.compilarSolucionToolStripMenuItem1_Click);
            // 
            // traductorToolStripMenuItem
            // 
            this.traductorToolStripMenuItem.Name = "traductorToolStripMenuItem";
            this.traductorToolStripMenuItem.Size = new System.Drawing.Size(155, 26);
            this.traductorToolStripMenuItem.Text = "Traductor";
            this.traductorToolStripMenuItem.Click += new System.EventHandler(this.traductorToolStripMenuItem_Click);
            // 
            // CajaTexto1
            // 
            this.CajaTexto1.Dock = System.Windows.Forms.DockStyle.Top;
            this.CajaTexto1.Location = new System.Drawing.Point(0, 30);
            this.CajaTexto1.Margin = new System.Windows.Forms.Padding(4);
            this.CajaTexto1.Name = "CajaTexto1";
            this.CajaTexto1.Size = new System.Drawing.Size(1067, 389);
            this.CajaTexto1.TabIndex = 1;
            this.CajaTexto1.Text = "";
            // 
            // CajaTexto2
            // 
            this.CajaTexto2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.CajaTexto2.Location = new System.Drawing.Point(0, 418);
            this.CajaTexto2.Margin = new System.Windows.Forms.Padding(4);
            this.CajaTexto2.Name = "CajaTexto2";
            this.CajaTexto2.ReadOnly = true;
            this.CajaTexto2.Size = new System.Drawing.Size(1067, 136);
            this.CajaTexto2.TabIndex = 2;
            this.CajaTexto2.Text = "";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1067, 554);
            this.Controls.Add(this.CajaTexto2);
            this.Controls.Add(this.CajaTexto1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.Text = "Form 1";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem archivoToolStripMenuItem;
        private System.Windows.Forms.RichTextBox CajaTexto1;
        private System.Windows.Forms.RichTextBox CajaTexto2;
        private System.Windows.Forms.ToolStripMenuItem nuevoAToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem guardarToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem guardarComoToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem salirToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem abrirToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem compilarToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem compilarSolucionToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem traductorToolStripMenuItem;
    }
}

