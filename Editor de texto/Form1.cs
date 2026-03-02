using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Editor_de_texto
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            compilarSolucionToolStripMenuItem1.Enabled = false;
        }
        private void nuevoAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CajaTexto1.Clear();
            archivo = null;
            Form1.ActiveForm.Text = "Mi compildor";
        }
        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog VentanaAbrir = new OpenFileDialog();
            VentanaAbrir.Filter = "Texto|*.c";
            if(VentanaAbrir.ShowDialog() == DialogResult.OK)
            {
                archivo = VentanaAbrir.FileName;
                using (StreamReader Leer = new StreamReader(archivo))
                {
                    CajaTexto1.Text = Leer.ReadToEnd();
                }
            }
            Form1.ActiveForm.Text = "Mi compilador -" + archivo;
            compilarSolucionToolStripMenuItem1.Enabled = true;
        }
        private void guardarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog VentanaAbrir = new OpenFileDialog();
            VentanaAbrir.Filter = "Texto|*.c";
            if (VentanaAbrir.ShowDialog() == DialogResult.OK) 
            {
                archivo = VentanaAbrir.FileName; 
                using (StreamReader Leer = new StreamReader(archivo)) 
                {
                    CajaTexto1.Text = Leer.ReadToEnd(); 
                }
                Form1.ActiveForm.Text = "Mi Compilador - " + archivo; 
                compilarSolucionToolStripMenuItem1.Enabled = true; 
            }
        }
        private void guardar() 
        {
            OpenFileDialog VentanaAbrir = new OpenFileDialog(); 
            VentanaAbrir.Filter = "Texto|*.c"; 
            if (archivo != null) 
            {
                using (StreamWriter Escribir = new StreamWriter(archivo)) 
                {
                    Escribir.Write(CajaTexto1.Text); 
                }
            }
            else 
            {
                if (VentanaAbrir.ShowDialog() == DialogResult.OK) 
                {
                    archivo = VentanaAbrir.FileName; 
                    using (StreamWriter Escribir = new StreamWriter(archivo)) 
                    {
                        Escribir.Write(CajaTexto1.Text); 
                    }
                }
            }
            Form1.ActiveForm.Text = "Mi Compilador - " + archivo; 
        }
        private void guardarComoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog guardar = new SaveFileDialog();

            guardar.Title = "Guardar código como...";
            guardar.Filter =
                "Archivo C (*.c)|*.c|" +
                "Todos los archivos (*.*)|*.*";

            guardar.FileName = "programa";

            if (guardar.ShowDialog() == DialogResult.OK)
            {
                string codigo = CajaTexto1.Text;

                File.WriteAllText(guardar.FileName, codigo);

                MessageBox.Show("Archivo guardado correctamente:\n" + guardar.FileName);
            }
        }
        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
           Application.Exit();
        }
        private void compilarSolucionToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            N_error = 0;
            Numero_linea = 1;
            if (string.IsNullOrEmpty(archivo))
            {
                MessageBox.Show("Debe abrir un archivo primero.");
                return;
            }

            CajaTexto2.Text = "";
            guardar();

            archivoback = Path.ChangeExtension(archivo, ".back");

            // ---- ANÁLISIS LÉXICO ----
            using (Leer = new StreamReader(archivo))
            using (Escribir = new StreamWriter(archivoback, false, Encoding.UTF8))
            {
                int numero_linea_local = 0;
                string linea;

                while ((linea = Leer.ReadLine()) != null)
                {
                    numero_linea_local++;
                    Numero_linea = numero_linea_local;  
                    Analisis_Lexico(linea);             
                }
            }

            CajaTexto2.AppendText($"\nErrores léxicos: {N_error}\n");

            // ---- ANÁLISIS SINTÁCTICO ----
            Analisis_Sintactico();
        }


        private void Cabecera()
        {
            string token = Leer.ReadLine();
            while (token != null)
            {
                if (token == "#")
                {
                    Directiva_Proc();
                }
                else if (token == "Palabra Reservada" || token == "identificador")
                {
                    break;
                }
                else
                {
                    ErrorSintactico("Se esperaba una directiva de preprocesador", token);
                }

                token = Leer.ReadLine();
            }
        }
        private void Directiva_Proc()
        {
            string token = Leer.ReadLine();

            if (token == "Palabra Reservada")
                token = Leer.ReadLine();

            switch (token)
            {
                case "include":
                    token = Leer.ReadLine();
                    if (token == "Libreria" || token == "cadena")
                    {
                        CajaTexto2.AppendText("Directiva include válida\n");
                    }
                    else
                    {
                        ErrorSintactico("Se esperaba una libreria o cadena despues de include", token);
                    }
                    break;

                case "define":
                    CajaTexto2.AppendText("Directiva define valida\n");
                    break;

                default:
                    ErrorSintactico("Se esperaba 'include' o 'define' despues de '#'", token);
                    break;
            }
        }
        private void ErrorSintactico(string mensaje, string token)
        {
            N_error++;
            Numero_linea++;
            CajaTexto2.AppendText($"Error sintactico en linea {Numero_linea}: {mensaje} (Token: {token})\n");
        }
        private void traducirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(archivo) || !File.Exists(archivo))
            {
                MessageBox.Show("Primero agrega algo");
                return;
            }

            string codigoOriginal = File.ReadAllText(archivo);
            string codigoTraducido = codigoOriginal;

            string[] reservadas = P_Reservadas.Split(',');
            string[] traducciones = P_trad.Split(',');

            for (int i = 0; i < reservadas.Length; i++)
            {
                string original = reservadas[i];
                string traduccion = traducciones[i];

                string patron = $@"(?<![A-Za-z0-9_]){Regex.Escape(original)}(?![A-Za-z0-9_])";

                codigoTraducido = Regex.Replace(
                    codigoTraducido,
                    patron,
                    traduccion
                );
            }

            archivotrad = Path.ChangeExtension(archivo, ".trad");
            File.WriteAllText(archivotrad, codigoTraducido);

            CajaTexto2.Text = codigoTraducido;
        }
        private char Tipo_caracter(int caracter) 
        {
            if(caracter >= 65 && caracter <= 90 || caracter >= 97 && caracter <= 122)
            {
                return 'l'; 
            }
            else
            {
                if(caracter >= 48 && caracter <= 57)
                {
                    return 'd';
                }
                else
                {
                    switch (caracter)
                    {
                        case 10: 
                            return 'n';
                        case 34: 
                            return '"'; 
                        case 39:
                            return 'c'; 
                        case 32:
                            return 'e';
                        case 47:
                            return '/';
                        default:
                            return 's';  
                    }
                }
            }
        }
        private bool Palabra_Reservada(string palabra)
        {
            return P_Reservadas.Contains(palabra);
        }
        private void Error(int i_caracter) 
        {
            CajaTexto2.AppendText("Error lexico " + (char)i_caracter + ", linea" + Numero_linea + "\n"); 
            CajaTexto2.SelectionStart = CajaTexto2.Text.Length; 
            CajaTexto2.ScrollToCaret(); 
        }
        private void Cadena()
        {
            do
            {
                i_caracter = Leer.Read(); 
                if (i_caracter == 10) 
                { 
                   Numero_linea++; 
                } 
            } while (i_caracter != 34 && i_caracter != -1); 
            if (i_caracter == -1) 
            {
                Error(-1);
            } 
        }
        private void Caracter() 
        {
            i_caracter = Leer.Read(); 
            i_caracter = Leer.Read();
            if (i_caracter != 39) 
            {
                Error(39);
            }
            
        }
        private void Simbolo()
        {
            elemento = "Simbolo: " + (char)i_caracter + "\n";
        }        
        private void Archivo_Libreria()
        {
            i_caracter = Leer.Read();
            if ((char)i_caracter == 'h') 
            { 
                elemento = "Archivo Libreria\n"; i_caracter = Leer.Read(); 
            }
            else 
            { 
                Error(i_caracter); 
            }
        }
        private void Identificador()
        {
            do
            {
                elemento = elemento + (char)i_caracter;
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'l' || Tipo_caracter(i_caracter) == 'd');

            if ((char)i_caracter == '.') { Archivo_Libreria(); }
            else
            {
                if (Palabra_Reservada(elemento)) elemento = "Palabra Reservada: " + elemento + "\n";
                else elemento = "Identificador: " + elemento + "\n";
            }
        }
        private void Numero_Real()
        {
            do
            {
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'd');
            elemento = "numero_real\n";
        }
        private void Numero()
        {
            do
            {
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'd');
            if ((char)i_caracter == '.') { Numero_Real(); }
            else
            {
                elemento = "numero_entero\n";
            }
        }
        private void Comentario()
        {
            int siguiente = Leer.Read(); 

           
            if (siguiente == '/')
            {
                int c;
                while ((c = Leer.Read()) != -1 && c != '\n')
                {
                }
                elemento = "Comentario de línea\n";
                i_caracter = c; 
                if (i_caracter == '\n') Numero_linea++;
            }
            else if (siguiente == '*')
            {
                int previo = 0;
                int c;
                do
                {
                    previo = i_caracter;
                    c = Leer.Read();
                    i_caracter = c;
                    if (c == '\n') Numero_linea++;
                }
                while (!(previo == '*' && c == '/') && c != -1);

                elemento = "Comentario de bloque\n";

                if (i_caracter != -1)
                    i_caracter = Leer.Read();
            }
            else
            {
                elemento = "Operador division\n";
                i_caracter = siguiente; 
            }
        }
        private void traductorToolStripMenuItem_Click(object sender, EventArgs e) 
        {
            if (string.IsNullOrWhiteSpace(CajaTexto1.Text))
            {
                MessageBox.Show("No hay código para traducir.",
                                "Advertencia",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return; 
            }
            Traductor traductor = new Traductor(); 
            string CodigoOriginal = CajaTexto1.Text; 
            string CodigoTraducido = traductor.TraducirCodigo(CodigoOriginal); 
            SaveFileDialog GuardarDialogo = new SaveFileDialog(); 
            GuardarDialogo.Filter = "Código traducido |*.trad";
            GuardarDialogo.Title = "Guardar codigo traducido en español";
            if(GuardarDialogo.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(GuardarDialogo.FileName))
                {
                    sw.Write(CodigoTraducido);
                }
                MessageBox.Show("Código traducido, guardado en:" + GuardarDialogo.FileName,
                    "Traducción completa", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void analizarToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        public void Analisis_Lexico(string linea)
        {
            if (linea.TrimStart().StartsWith("//"))
                return;

            var regex = new Regex(
                @"(?<string>""[^""]*"")|(?<lib><[^>]+>)|(?<id>[A-Za-z_][A-Za-z0-9_]*)|(?<num>\d+)|(?<sym>[#\{\}\(\);\[\]<>=!+\-*/%,.&|])|(?<invalid>.)",
                RegexOptions.Compiled);

            var coincidencias = regex.Matches(linea);

            foreach (Match match in coincidencias)
            {
                string texto = match.Value;
                if (string.IsNullOrWhiteSpace(texto))
                    continue;

                string tipo;
                if (match.Groups["string"].Success) tipo = "string";
                else if (match.Groups["lib"].Success) tipo = "lib";
                else if (match.Groups["id"].Success) tipo = "id";
                else if (match.Groups["num"].Success) tipo = "num";
                else if (match.Groups["sym"].Success) tipo = "sym";
                else tipo = "invalid";

                // ============================================================
                //          CONTROL DE CONDICIÓN ENTRE PARÉNTESIS
                // ============================================================

                // Iniciar condición al encontrar "(" después de if/while/for
                if (texto == "(" &&
                    (ultimoToken == "if" || ultimoToken == "while" || ultimoToken == "for"))
                {
                    leyendoCondicion = true;

                    Escribir.WriteLine("(");
                    Escribir.WriteLine("condicion");

                    ultimoToken = texto;
                    continue;
                }

                // Cerrar condición al encontrar ")"
                if (texto == ")" && leyendoCondicion)
                {
                    leyendoCondicion = false;

                    Escribir.WriteLine(")");

                    ultimoToken = texto;
                    continue;
                }

                // Ignorar todo lo que está dentro de ( ... ) de una condición
                if (leyendoCondicion)
                {
                    ultimoToken = texto;
                    continue;
                }

                // ============================================================
                //                     PROCESAMIENTO NORMAL
                // ============================================================

                switch (tipo)
                {
                    case "string":
                        Escribir.WriteLine("cadena");
                        break;

                    case "lib":
                        Escribir.WriteLine("<");
                        Escribir.WriteLine("libreria");
                        Escribir.WriteLine(">");
                        break;

                    case "id":
                        Escribir.WriteLine(texto);
                        break;

                    case "num":
                        Escribir.WriteLine(texto);
                        break;

                    case "sym":
                        Escribir.WriteLine(texto);
                        break;

                    case "invalid":
                        N_error++;
                        CajaTexto2.AppendText($"Error en línea {Numero_linea}: token no reconocido = {texto}\n");
                        break;
                }

                ultimoToken = texto;
            }

            Escribir.WriteLine("LF");
            Escribir.Flush();
        }
        public void Analisis_Sintactico()
        {
            CajaTexto2.AppendText("\n--- Análisis sintáctico ---\n");

            if (!File.Exists(archivoback))
            {
                CajaTexto2.AppendText("No se encontró el archivo .back generado.\n");
                return;
            }

            Leer = new StreamReader(archivoback);
            string token;
            bool includeValido = false;
            bool mainValido = false;

            while ((token = GetToken()) != null)
            {
                if (token == "#")
                {
                    string siguiente = GetToken();
                    if (siguiente == "include")
                    {
                        string lib = GetToken();
                        if (lib == "<")
                        {
                            string nombreLib = GetToken(); 
                            string cierre = GetToken();    
                            if (cierre == ">")
                            {
                                includeValido = true;
                                CajaTexto2.AppendText($"Directiva include válida: <{nombreLib}>\n");
                            }
                            else
                            {
                                N_error++;
                                CajaTexto2.AppendText("Error: falta '>' en la directiva include\n");
                            }
                        }
                        else if (lib == "cadena")
                        {
                            includeValido = true;
                            CajaTexto2.AppendText("Directiva include válida con cadena\n");
                        }
                        else
                        {
                            N_error++;
                            CajaTexto2.AppendText("Error: se esperaba una librería o cadena tras include\n");
                        }
                    }
                    else
                    {
                        N_error++;
                        CajaTexto2.AppendText("Error: se esperaba include\n");
                    }
                }

                // ---- VARIABLES GLOBALES Y MAIN ----
                else if (token == "int" || token == "float" || token == "char")
                {
                    string siguiente = GetToken();

                    if (siguiente == "main")
                    {
                        GetToken(); 
                        GetToken(); 
                        GetToken(); 
                        CajaTexto2.AppendText("Función main detectada correctamente\n");
                        mainValido = true;
                    }
                    else
                    {
                        CajaTexto2.AppendText($"Declaración global detectada del tipo '{token}'\n");
                        DeclaracionGlobal(token, siguiente);
                    }
                }

                else if (token == "cadena")
                {
                    CajaTexto2.AppendText("Cadena detectada correctamente\n");
                }

                else if (token == "if")
                {
                    AnalizarIf();
                }               
                else if (token == "while")
                {
                    AnalizarWhile();
                }
                else if (token == "do")
                {
                    AnalizarDoWhile();
                }
                else if (token == "for")
                {
                    AnalizarFor();
                }




                else if (EsIdentificador(token) && mainValido)
                {
                    if (token == "printf")
                    {
                        AnalizarLlamadaPrintf();
                    }
                }
                else if (token == "}")
                {
                }

            }

            Leer.Close();

            if (includeValido && mainValido && N_error == 0)
                CajaTexto2.AppendText("\n Errores sintacticos: 0\n");
            else
                CajaTexto2.AppendText($"\n Errores sintacticos detectados: {N_error}\n");
        }

        private void AnalizarLlamadaPrintf()
        {
            string token = GetToken();
            if (token == "(")
            {
                token = GetToken();
                if (token == "cadena")
                {
                    CajaTexto2.AppendText("Cadena de printf detectada correctamente\n");

                    token = GetToken();
                    if (token == ")")
                    {
                        token = GetToken();
                        if (token == ";")
                        {
                            CajaTexto2.AppendText("Llamada a printf analizada.\n");
                        }
                        else
                        {
                            ErrorS(token, ";"); 
                        }
                    }
                    else
                    {
                        ErrorS(token, ")"); 
                    }
                }
                else
                {
                    ErrorS(token, "una cadena literal");
                }
            }
            else
            {
                ErrorS(token, "("); 
            }
        }


        private void DeclaracionGlobal(string tipo, string primerIdentificador)
        {
            if (!EsIdentificador(primerIdentificador))
            {
                N_error++;
                CajaTexto2.AppendText($"Error: identificador no válido después de '{tipo}' en declaración global\n");
                return;
            }

            bool continuar = true;
            ProcesarUnIdentificador(tipo, primerIdentificador, ref continuar);

            while (continuar)
            {
                string token = GetToken();
                if (token == null)
                {
                    N_error++;
                    CajaTexto2.AppendText($"Error: fin de archivo inesperado en declaración global de '{tipo}'\n");
                    return;
                }

                if (token == ",")
                {
                    string siguienteId = GetToken();
                    if (siguienteId == null || !EsIdentificador(siguienteId))
                    {
                        N_error++;
                        CajaTexto2.AppendText($"Error: identificador esperado después de ',' en declaración global de '{tipo}'\n");
                        return;
                    }
                    ProcesarUnIdentificador(tipo, siguienteId, ref continuar);
                }
                else if (token == ";")
                {
                    CajaTexto2.AppendText($"Declaración global completada: {tipo}\n");
                    return;
                }
                else
                {
                    N_error++;
                    CajaTexto2.AppendText($"Error: token inesperado '{token}' en declaración global de '{tipo}'\n");
                    return;
                }
            }
        }

        private void ProcesarUnIdentificador(string tipo, string identificador, ref bool continuar)
        {
            string token = GetToken();

            if (token == null)
            {
                N_error++;
                CajaTexto2.AppendText($"Error: fin de archivo inesperado tras identificador '{identificador}'\n");
                continuar = false;
                return;
            }

            if (token == ";")
            {
                CajaTexto2.AppendText($"Variable declarada correctamente: {tipo} {identificador}\n");
                continuar = false;
            }
            else if (token == ",")
            {
                CajaTexto2.AppendText($"Variable declarada correctamente: {tipo} {identificador}\n");
                continuar = true;
            }
            else if (token == "[")
            {
                Declaracion_Arreglo(tipo, identificador, ref continuar);
            }
            else if (token == "=")
            {
                CajaTexto2.AppendText($"Inicializando variable: {identificador}\n");

                if (ConstanteSimpleOriginal() == 1) 
                {
                    string siguiente = GetToken();
                    if (siguiente == ";")
                    {
                        CajaTexto2.AppendText($"Variable inicializada: {tipo} {identificador}\n");
                        continuar = false; 
                    }
                    else if (siguiente == ",")
                    {
                        CajaTexto2.AppendText($"Variable inicializada: {tipo} {identificador}\n");
                        continuar = true; 
                    }
                    else
                    {
                        ErrorS(siguiente, "; o ,");
                        continuar = false;
                    }
                }
                else
                {
                    continuar = false;
                }
            }
            else
            {
                N_error++;
                CajaTexto2.AppendText($"Error: se esperaba ';', ',', '[' o '=' después del identificador '{identificador}' (se recibió '{token}')\n");
                continuar = false;
            }
        }

        private int ConstanteSimpleOriginal()
        {
            string token = GetToken();
            switch (token)
            {
                case "-": 
                    token = GetToken();
                    if (int.TryParse(token, out _) || float.TryParse(token, out _) || EsIdentificador(token))
                        return 1;
                    else
                    {
                        ErrorS(token, "numero_entero, numero_real o identificador");
                        return 0;
                    }
                case "numero_real": 
                case "numero_entero":
                case "caracter":
                case "identificador":
                    return 1;
                default:
                    if (int.TryParse(token, out _) || float.TryParse(token, out _) || EsIdentificador(token))
                        return 1;

                    ErrorS(token, "una constante (numero, caracter o identificador)");
                    return 0;
            }
        }

        private int ConstanteSimple(string token)
        {
            if (token == "numero_real" || token == "numero_entero" || token == "caracter" || EsIdentificador(token))
            {
                return 1;
            }
            if (int.TryParse(token, out _) || float.TryParse(token, out _))
            {
                return 1;
            }
            return 0;
        }

        // ---- DECLARACION DE ARREGLO  ----
        private void Declaracion_Arreglo(string tipo, string identificador, ref bool continuar)
        {
            int dimensiones = 0;
            string token = "["; 

            while (token == "[")
            {
                string constante = GetToken();

                if (!Constante(constante) && !EsIdentificador(constante))
                {
                    N_error++;
                    CajaTexto2.AppendText($"Error: se esperaba una constante (int) o identificador dentro de los corchetes de '{identificador}'\n");
                    continuar = false;
                    return;
                }

                string cierre = GetToken();
                if (cierre != "]")
                {
                    N_error++;
                    CajaTexto2.AppendText($"Error: falta ']' en la declaración del arreglo '{identificador}'\n");
                    continuar = false;
                    return;
                }

                dimensiones++;
                token = GetToken(); 
            }

            switch (token)
            {
                case ";":
                    CajaTexto2.AppendText($"Arreglo declarado ({dimensiones}D): {tipo} {identificador}\n");
                    continuar = false;
                    break;
                case ",":
                    CajaTexto2.AppendText($"Arreglo declarado ({dimensiones}D): {tipo} {identificador}\n");
                    continuar = true; 
                    break;
                case "=": 
                    token = GetToken();
                    if (token == "{")
                    {
                        if (BLOQUE_INICIALIZACION())
                        {
                            token = GetToken(); 

                            if (token == "}")
                            {
                                token = GetToken();

                                if (token == ";")
                                {
                                    CajaTexto2.AppendText($"Arreglo inicializado: {tipo} {identificador}\n");
                                    continuar = false;
                                }
                                else if (token == ",")
                                {
                                    CajaTexto2.AppendText($"Arreglo inicializado: {tipo} {identificador}\n");
                                    continuar = true;
                                }
                                else
                                {
                                    ErrorS(token, "; o ,");
                                    continuar = false;
                                }
                            }
                            else
                            {
                                ErrorS(token, "}"); 
                                continuar = false;
                            }
                        }
                        else
                        {
                            continuar = false;
                        }
                    }
                    else
                    {
                        ErrorS(token, "{");
                        continuar = false;
                    }
                    break;
                default:
                    ErrorS(token, "declaracion valida para arreglos (;, , o =)");
                    continuar = false;
                    break;
            }
        }

        private bool BLOQUE_INICIALIZACION()
        {
            CajaTexto2.AppendText("Analizando bloque de inicialización {}...\n");
            string token;

            token = GetToken();

            if (token == "}")
            {
                UnGetToken("}"); 
                return true;
            }

            if (token == "{") 
            {
                if (!BLOQUE_INICIALIZACION())
                {
                    return false; 
                }
                token = GetToken(); 
            }
            else if (ConstanteSimple(token) == 0) 
            {
                ErrorS(token, "una constante, un identificador o '{' para sub-bloque");
                return false;
            }

            CajaTexto2.AppendText("Bloque de inicialización: Elemento inicial detectado\n");

            while (true)
            {
                token = GetToken();
                if (token == null)
                {
                    ErrorS("fin de archivo", ", o }");
                    return false;
                }

                if (token == "}")
                {
                    UnGetToken("}");
                    return true;
                }

                if (token != ",")
                {
                    ErrorS(token, ", o }");
                    return false;
                }

                token = GetToken();

                if (token == null)
                {
                    ErrorS("fin de archivo", "un valor después de la coma");
                    return false;
                }

                if (token == "{") 
                {
                    if (!BLOQUE_INICIALIZACION())
                    {
                        return false;
                    }
                    token = GetToken(); 
                }
                else if (ConstanteSimple(token) == 0) 
                {
                    ErrorS(token, "una constante, un identificador o '{' después de la coma");
                    return false;
                }

                CajaTexto2.AppendText("Bloque de inicialización: Elemento subsiguiente detectado\n");
            }
        }

        // ---- ErrorS ----
        private void ErrorS(string tokenRecibido, string seEsperaba)
        {
            N_error++;
            if (tokenRecibido == null) tokenRecibido = "fin de archivo";
            CajaTexto2.AppendText($"Error Sintáctico: Se recibió '{tokenRecibido}', se esperaba '{seEsperaba}'\n");
        }

        // ---- CONSTANTE() ----
        private bool Constante(string token)
        {
            return int.TryParse(token, out _);
        }

        // ---- IDENTIFICADOR ----
        private bool EsIdentificador(string token)
        {
            if (token == "int" || token == "float" || token == "char" || token == "main" || token == "printf" || token == "return")
                return false;
            return Regex.IsMatch(token ?? "", @"^[A-Za-z_][A-Za-z0-9_]*$");
        }

        // ---- UNGETTOKEN ----
        private void UnGetToken(string token)
        {
            Queue<string> newBuffer = new Queue<string>();
            newBuffer.Enqueue(token);
            while (tokenBuffer.Count > 0)
            {
                newBuffer.Enqueue(tokenBuffer.Dequeue());
            }
            tokenBuffer = newBuffer;
        }

        // ---- LECTURA DE TOKENS ----
        private string GetToken()
        {
            string token;

            while (tokenBuffer.Count > 0)
            {
                token = tokenBuffer.Dequeue();

                if (token != "LF" && token != "CR" && token != " ")
                {
                    return token;
                }
            }

            string linea;
            while ((linea = Leer.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(linea))
                    continue;

                string simbolos = @"\#\{\}\(\);\[\]<>=!+\-*/%,&\|";
                string pattern = $"(==|!=|<=|>=|&&|\\|\\|)|([{simbolos}])";

                var partes = Regex.Split(linea.Trim(), pattern);
                foreach (var p in partes)
                {
                    var t = p.Trim();
                    if (string.IsNullOrEmpty(t))
                        continue;

                    tokenBuffer.Enqueue(t);
                }

                while (tokenBuffer.Count > 0)
                {
                    token = tokenBuffer.Dequeue();

                    if (token != "LF" && token != "CR" && token != " ")
                    {
                        return token;
                    }
                }
            }
            return null;
        }

        private void AnalizarIf()
        {
            string token = GetToken();
            if (token != "(")
            {
                ErrorS(token, "(");
                return;
            }

            token = GetToken();
            if (token != "condicion")
            {
                ErrorS(token, "condicion");
                return;
            }

            token = GetToken();
            if (token != ")")
            {
                ErrorS(token, ")");
                return;
            }

            token = GetToken();
            if (token != "{")
            {
                ErrorS(token, "{");
                return;
            }

            AnalizarBloque();

            token = GetToken();
            if (token == "else")
            {
                token = GetToken();
                if (token != "{")
                {
                    ErrorS(token, "{");
                    return;
                }

                AnalizarBloque();
            }
            else
            {
                UnGetToken(token);
            }
        }

        private void AnalizarBloque()
        {
            string token;

            while ((token = GetToken()) != null)
            {
                if (token == "}")
                {
                    return;
                }

                // ----- MANEJO DE SENTENCIAS -----

                if (token == "if")
                {
                    AnalizarIf();
                }
                else if (EsIdentificador(token))
                {
                    AnalizarPosibleAsignacion(token);
                }
                else if (token == "printf")
                {
                    AnalizarLlamadaPrintf();
                }
                else
                {
                }
            }

            ErrorS("fin de archivo", "}");
        }

        private void AnalizarPosibleAsignacion(string identificador)
        {
            string token = GetToken();

            if (token != "=")
            {
                ErrorS(token, "=");
                return;
            }

            string valor = GetToken();
            if (!EsIdentificador(valor) && !int.TryParse(valor, out _))
            {
                ErrorS(valor, "identificador o número");
                return;
            }

            token = GetToken();
            if (token != ";")
            {
                ErrorS(token, ";");
            }
        }
        private void AnalizarWhile()
        {
            string token = GetToken();
            if (token != "(")
            {
                ErrorS(token, "(");
                return;
            }

            token = GetToken();
            if (token != "condicion")
            {
                ErrorS(token, "condicion");
                return;
            }

            token = GetToken();
            if (token != ")")
            {
                ErrorS(token, ")");
                return;
            }

            token = GetToken();
            if (token != "{")
            {
                ErrorS(token, "{");
                return;
            }

            AnalizarBloque();
        }


        private void AnalizarDoWhile()
        {
            string token = GetToken();
            if (token != "{")
            {
                ErrorS(token, "{");
                return;
            }

            AnalizarBloque();

            token = GetToken();
            if (token != "while")
            {
                ErrorS(token, "while");
                return;
            }

            token = GetToken();
            if (token != "(")
            {
                ErrorS(token, "(");
                return;
            }

            token = GetToken();
            if (token != "condicion")
            {
                ErrorS(token, "condicion");
                return;
            }

            token = GetToken();
            if (token != ")")
            {
                ErrorS(token, ")");
                return;
            }

            token = GetToken();
            if (token != ";")
            {
                ErrorS(token, ";");
                return;
            }
        }

        private void AnalizarFor()
        {
            string token = GetToken();
            if (token != "(")
            {
                ErrorS(token, "(");
                return;
            }

            token = GetToken();
            if (token != "condicion")
            {
                ErrorS(token, "condicion");
                return;
            }

            token = GetToken();
            if (token != ")")
            {
                ErrorS(token, ")");
                return;
            }

            token = GetToken();
            if (token != "{")
            {
                ErrorS(token, "{");
                return;
            }

            AnalizarBloque();
        }






    }
}