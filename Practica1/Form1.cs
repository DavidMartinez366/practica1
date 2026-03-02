using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Practica1
{
    public partial class Form1 : Form
    {

        // Tabla de variables globales
        private Dictionary<string, (string tipo, bool esArreglo, int tam)> Variables = new Dictionary<string, (string, bool, int)>();

       
        private List<string> TiposValidos = new List<string>
        {
            "int", "float", "double", "char", "bool", "long", "short", "void"
        };

       
        private List<string> Directivas = new List<string> { "include", "define" };

        
        private List<string> P_Reservadas = new List<string>
        {
            "int", "float", "return", "if", "else", "while", "for", "char", "void", "double",
            "include", "main", "break", "case", "const", "continue", "default", "do", "enum",
            "extern", "goto", "long", "register", "short", "signed", "sizeof", "static", "struct",
            "switch", "typedef", "union", "unsigned", "volatile", "auto", "bool", "class",
            "delete", "friend", "inline", "new", "operator", "private", "protected", "public",
            "template", "this", "throw", "try", "typename", "using", "virtual", "namespace",
            "nullptr", "printf", "constexpr", "decltype", "static_assert","<condicion>"
        };

        
        private Dictionary<string, string> Traducciones = new Dictionary<string, string>
        {
            { "int", "entero" },
            { "float", "flotante" },
            { "return", "retornar" },
            { "if", "si" },
            { "else", "sino" },
            { "while", "mientras" },
            { "for", "para" },
            { "char", "caracter" },
            { "void", "vacío" },
            { "double", "doble" },
            { "include", "incluir" },
            { "main", "principal" },
            { "break", "romper" },
            { "case", "caso" },
            { "const", "constante" },
            { "continue", "continuar" },
            { "default", "por_defecto" },
            { "do", "hacer" },
            { "enum", "enumeración" },
            { "extern", "externo" },
            { "goto", "ir_a" },
            { "long", "largo" },
            { "register", "registro" },
            { "short", "corto" },
            { "signed", "con_signo" },
            { "sizeof", "tamaño_de" },
            { "static", "estático" },
            { "struct", "estructura" },
            { "switch", "cambiar" },
            { "typedef", "definir_tipo" },
            { "union", "unión" },
            { "unsigned", "sin_signo" },
            { "volatile", "volátil" },
            { "auto", "automático" },
            { "bool", "booleano" },
            { "class", "clase" },
            { "delete", "eliminar" },
            { "friend", "amigo" },
            { "inline", "en_linea" },
            { "new", "nuevo" },
            { "operator", "operador" },
            { "private", "privado" },
            { "protected", "protegido" },
            { "public", "público" },
            { "template", "plantilla" },
            { "this", "este" },
            { "throw", "lanzar" },
            { "try", "intentar" },
            { "typename", "nombre_tipo" },
            { "using", "usando" },
            { "virtual", "virtual" },
            { "namespace", "espacio_de_nombres" },
            { "nullptr", "nulo" },
            { "printf", "imprimir" },
            { "constexpr", "constante_tiempo_compilación" },
            { "decltype", "tipo_declarado" },
            { "static_assert", "afirmación_estática" }
        };

        public Form1()
        {
            InitializeComponent();
            analizarToolStripMenuItem.Enabled = false;
        }
        private void Form1_Load(object sender, EventArgs e) { }

        private void nuevoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            archivo = null;
        }

        private void guardarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Guardar();
        }
        private void Guardar()
        {
            if (string.IsNullOrEmpty(archivo))
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "C Files|*.c";
                if (sfd.ShowDialog() != DialogResult.OK)
                    return;
                archivo = sfd.FileName;
            }

            using (StreamWriter sw = new StreamWriter(archivo))
            {
                sw.Write(richTextBox1.Text);
            }
        }
        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "C Files|*.c";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                archivo = ofd.FileName;
                using (StreamReader sr = new StreamReader(archivo))
                {
                    richTextBox1.Text = sr.ReadToEnd();
                }
                analizarToolStripMenuItem.Enabled = true;
                this.Text = "Mi compilador - " + archivo;
            }
        }

        private void guardarComoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "C Files|*.c";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                archivo = sfd.FileName;
                using (StreamWriter sw = new StreamWriter(archivo))
                {
                    sw.Write(richTextBox1.Text);
                }
            }
        }

        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void analizarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Guardar();
            if (archivo == null)
            {
                MessageBox.Show("Abra un archivo primero.");
                return;
            }

            N_Error = 0;
            Numero_Linea = 1;
            Rtbx_salida.Clear();

            archivoBack = Path.ChangeExtension(archivo, ".back");
            Leer = new StreamReader(archivo);
            Escribir = new StreamWriter(archivoBack);

            
            string[] lineas = File.ReadAllLines(archivo);
            Variables.Clear();

            
            DentroComentarioBloque = false;
            int nivelBloque = 0;

            foreach (string linea in lineas)
            {
                string original = linea;
                string lineaProcesable = linea;

                
                if (DentroComentarioBloque)
                {
                    int idxCierre = lineaProcesable.IndexOf("*/");
                    if (idxCierre != -1)
                    {
                        lineaProcesable = lineaProcesable.Substring(idxCierre + 2);
                        DentroComentarioBloque = false;
                    }
                    else
                    {
                        Numero_Linea++;
                        continue;
                    }
                }

                
                int idxLineComment = lineaProcesable.IndexOf("//");
                int idxBlockStart = lineaProcesable.IndexOf("/*");

                if (idxLineComment != -1 && (idxBlockStart == -1 || idxLineComment < idxBlockStart))
                {
                    lineaProcesable = lineaProcesable.Substring(0, idxLineComment);
                }

                idxBlockStart = lineaProcesable.IndexOf("/*");
                if (idxBlockStart != -1)
                {
                    int idxBlockEnd = lineaProcesable.IndexOf("*/", idxBlockStart + 2);
                    if (idxBlockEnd != -1)
                    {
                        string antes = lineaProcesable.Substring(0, idxBlockStart);
                        string despues = lineaProcesable.Substring(idxBlockEnd + 2);
                        lineaProcesable = (antes + " " + despues).Trim();
                    }
                    else
                    {
                        lineaProcesable = lineaProcesable.Substring(0, idxBlockStart);
                        DentroComentarioBloque = true;
                    }
                }

                
                lineaProcesable = lineaProcesable.Trim();
                if (string.IsNullOrWhiteSpace(lineaProcesable))
                {
                    Numero_Linea++;
                    continue;
                }

                
                if (lineaProcesable.StartsWith("#") || lineaProcesable.StartsWith("case ") || lineaProcesable.StartsWith("default:") || lineaProcesable == "default")
                {
                    Numero_Linea++;
                    continue;
                }

                int numClose = CountCharOutsideStrings(lineaProcesable, '}');
                if (numClose > 0)
                {
                    nivelBloque -= numClose;
                    if (nivelBloque < 0) nivelBloque = 0;
                }

                DetectarDeclaraciones(lineaProcesable);
                DetectarAsignacion(lineaProcesable);
                DetectarEstructuras(lineaProcesable);

                bool esEstructuraOBloque = lineaProcesable.StartsWith("if") ||
                                           lineaProcesable.StartsWith("while") ||
                                           lineaProcesable.StartsWith("for") ||
                                           lineaProcesable.Contains("{") ||
                                           lineaProcesable.Contains("}");

                if (!esEstructuraOBloque)
                {
                    ValidarSentenciaEnBloque(lineaProcesable, nivelBloque);
                }

                int numOpen = CountCharOutsideStrings(lineaProcesable, '{');
                if (numOpen > 0)
                {
                    nivelBloque += numOpen;
                }

                Numero_Linea++;
            }

            AnalizarLlavesYParentesis(lineas);

            Numero_Linea = 1;
            Leer.DiscardBufferedData();
            Leer.BaseStream.Seek(0, SeekOrigin.Begin);
            i_caracter = Leer.Read();

            // Análisis léxico 
            while (i_caracter != -1)
            {
                switch (Tipo_caracter(i_caracter))
                {
                    case 'l': Identificador(); break;
                    case 'd': Numero(); break;
                    case 's': Simbolo(); break;
                    case 'n': SaltosLinea(); break;
                    case 'e': i_caracter = Leer.Read(); break;
                    case '#': Directiva(); break;
                    case 'c': Caracter(); break;
                    case '"': Cadena(); break;
                    default:
                        Error(i_caracter);
                        i_caracter = Leer.Read();
                        break;
                }
            }

            Escribir.Close();
            Leer.Close();

            if (N_Error == 0)
                Rtbx_salida.AppendText("Análisis completado sin errores.\n");
            else
                Rtbx_salida.AppendText($"\nAnálisis completado con {N_Error} errores.\n");
        }

        private int CountCharOutsideStrings(string line, char target)
        {
            bool inString = false;
            bool escape = false;
            int count = 0;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inString)
                {
                    if (escape)
                    {
                        escape = false;
                        continue;
                    }
                    if (c == '\\')
                    {
                        escape = true;
                        continue;
                    }
                    if (c == '"')
                    {
                        inString = false;
                    }
                    continue;
                }
                else
                {
                    if (c == '"')
                    {
                        inString = true;
                        continue;
                    }
                    if (c == target)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private char Tipo_caracter(int caracter)
        {
            // letras (incluye '_' para identificadores válidos)
            if ((caracter >= 65 && caracter <= 90) || (caracter >= 97 && caracter <= 122) || caracter == 95) return 'l';
            if (caracter >= 48 && caracter <= 57) return 'd'; // números
            switch (caracter)
            {
                case 10: return 'n'; // salto de línea
                case 32: return 'e'; // espacio
                case 9: return 'e'; // tab
                case '"': return '"'; // cadena
                case '\'': return 'c'; // caracter
                case '#': return '#'; // directiva
                default: return 's'; // símbolo u otro
            }
        }
        private void Identificador()
        {
            string token = "";
            // el primer caracter ya es letra o '_'
            do
            {
                token += (char)i_caracter;
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'l' || Tipo_caracter(i_caracter) == 'd'); // permite dígitos dentro

            string resultado = P_Reservadas.Contains(token)
                ? $"palabra reservada {token}"
                : $"identificador {token}";

            Escribir.WriteLine(resultado);

            // Verificar uso de variable: sólo si no es palabra reservada y no es función
            char siguiente = (i_caracter == -1) ? '\0' : (char)i_caracter;
            VerificarUsoVariable(token, siguiente);
        }

        private void Numero()
        {
            string numero = "";
            bool puntoEncontrado = false;
            do
            {
                if ((char)i_caracter == '.')
                {
                    if (puntoEncontrado) break;
                    puntoEncontrado = true;
                    numero += '.';
                    i_caracter = Leer.Read();
                    continue;
                }
                numero += (char)i_caracter;
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'd' || (char)i_caracter == '.');

            Escribir.WriteLine($"número {numero}");
        }
        private void Simbolo()
        {
            char c = (char)i_caracter;

            // Detectar comentarios
            if (c == '/')
            {
                int siguiente = Leer.Read();

                // Comentario de línea //
                if (siguiente == '/')
                {
                    // Consumir hasta salto de línea
                    while (i_caracter != 10 && i_caracter != -1)
                        i_caracter = Leer.Read();

                    Escribir.WriteLine("comentario");
                    return;
                }

                // Comentario de bloque /* */
                else if (siguiente == '*')
                {
                    DetectarComentarioBloque();
                    return;
                }

                Escribir.WriteLine($"símbolo /");
                i_caracter = siguiente;
                return;
            }

            // No es comentario, procesar símbolo normal
            Escribir.WriteLine($"símbolo {c}");
            i_caracter = Leer.Read();
        }

        private void SaltosLinea()
        {
            Escribir.WriteLine("LF");
            Numero_Linea++;
            i_caracter = Leer.Read();
        }
        private void Cadena()
        {
            string token = "\"";
            i_caracter = Leer.Read();
            while (i_caracter != -1 && (char)i_caracter != '"')
            {
                token += (char)i_caracter;
                if ((char)i_caracter == '\n') Numero_Linea++;
                i_caracter = Leer.Read();
            }

            if (i_caracter == '"')
            {
                token += "\"";
                i_caracter = Leer.Read();
            }
            else
            {
                ErrorTexto("Cadena sin cerrar");
                return;
            }

            Escribir.WriteLine($"cadena {token}");
            codigoTraducido += token;
        }
        private void Caracter()
        {
            string c = "'";
            i_caracter = Leer.Read();
            if (i_caracter == -1)
            {
                ErrorTexto("Caracter mal formado");
                return;
            }
            c += (char)i_caracter;
            i_caracter = Leer.Read();
            if (i_caracter != '\'')
            {
                ErrorTexto("Caracter mal formado");
                i_caracter = Leer.Read();
                return;
            }
            c += "'";
            Escribir.WriteLine($"caracter {c}");
            i_caracter = Leer.Read();
        }

        private void Directiva()
        {
            string directiva = "#";
            i_caracter = Leer.Read();

            // Permite espacios entre # y la directiva
            while (i_caracter == 32 || i_caracter == 9) i_caracter = Leer.Read();

            //Lee la palabra de la directiva
            string palabra = "";
            while (i_caracter != -1 && ((i_caracter >= 65 && i_caracter <= 90) || (i_caracter >= 97 && i_caracter <= 122)))
            {
                palabra += (char)i_caracter;
                i_caracter = Leer.Read();
            }

            if (string.IsNullOrEmpty(palabra))
            {
                ErrorTexto("Directiva mal formada después de '#'.");
                while (i_caracter != 10 && i_caracter != -1) i_caracter = Leer.Read();
                return;
            }

            if (!Directivas.Contains(palabra))
            {
                ErrorTexto($"Directiva desconocida #{palabra}. Se esperaba por ejemplo: #include.");
                while (i_caracter != 10 && i_caracter != -1) i_caracter = Leer.Read();
                return;
            }

            Escribir.WriteLine($"directiva #{palabra}");

            while (i_caracter == 32 || i_caracter == 9)
                i_caracter = Leer.Read();

            //Verifica tipo de argumento esperado
            if (i_caracter == '<' || i_caracter == '"')
            {
                char delimitador = (char)i_caracter;
                string argumento = "" + delimitador;
                i_caracter = Leer.Read();

                // Lee hasta cierre correspondiente
                while (i_caracter != -1 && (char)i_caracter != (delimitador == '<' ? '>' : '"'))
                {
                    argumento += (char)i_caracter;
                    i_caracter = Leer.Read();
                }

                // Manejo de errores
                if (i_caracter == -1)
                {
                    ErrorTexto($"Error en #{palabra}: se esperaba {(delimitador == '<' ? "'>'" : "'\"'")} de cierre antes de fin de archivo.");
                    return;
                }

                argumento += (char)i_caracter;
                Escribir.WriteLine($"argumento {argumento}");
                i_caracter = Leer.Read();
            }
            else
            {
                // Si no se encontró ni < ni "
                ErrorTexto($"Error en #{palabra}: se esperaba '<archivo>' o '\"archivo\"' después de #{palabra}.");
                while (i_caracter != 10 && i_caracter != -1) i_caracter = Leer.Read();
            }
        }

        private void DetectarAsignacion(string linea)
        {
            linea = linea.Trim();

            
            foreach (var tipos in P_Reservadas)
            {
                if (linea.StartsWith(tipos + " "))
                    return;
            }

            
            if (!linea.Contains("=") || !linea.EndsWith(";"))
                return;

            string izquierda = linea.Substring(0, linea.IndexOf("=")).Trim();
            string derecha = linea.Substring(linea.IndexOf("=") + 1).Trim().TrimEnd(';').Trim();

                
            if (!Variables.ContainsKey(izquierda))
            {
                ErrorTexto($"Asignación a variable no declarada '{izquierda}' en línea {Numero_Linea}.");
                return;
            }

            var (tipo, esArreglo, tam) = Variables[izquierda];

            if (esArreglo)
            {
                ErrorTexto($"No se puede asignar a un arreglo completo: '{izquierda}' en línea {Numero_Linea}.");
                return;
            }

            // Selección de tipo de análisis
            if (EsConstanteNumerica(derecha))
            {
                ValidarConstanteNumerica(tipo, derecha);
            }
            else if (EsCadena(derecha))
            {
                ValidarCadena(tipo, derecha);
            }
            else if (EsCaracter(derecha))
            {
                ValidarCaracter(tipo, derecha);
            }
            else if (EsExpresionAritmetica(derecha))
            {
                ValidarExpresionAritmetica(tipo, derecha);
            }
            else if (EsExpresionLogica(derecha))
            {
                ValidarExpresionLogica(tipo, derecha);
            }
            else if (Regex.IsMatch(derecha, @"^[A-Za-z_]\w*\s*\(.*\)$"))
            {
                // Es llamada a función, se acepta como válida
            }
            else
            {
                ErrorTexto($"Expresión no reconocida en la asignación de '{izquierda}' en línea {Numero_Linea}.");
            }
        }
        private bool EsConstanteNumerica(string s)
        {
            return float.TryParse(s, out _);
        }

        private bool EsCadena(string s)
        {
            return s.StartsWith("\"") && s.EndsWith("\"");
        }

        private bool EsCaracter(string s)
        {
            return s.Length == 3 && s.StartsWith("'") && s.EndsWith("'");
        }

        private bool EsExpresionAritmetica(string s)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(s, @"^[0-9A-Za-z_\+\-\*\/\(\) ]+$");
        }

        private bool EsExpresionLogica(string s)
        {
            return s.Contains("==") || s.Contains(">") || s.Contains("<") || s.Contains("&&") || s.Contains("||");
        }

        private void ValidarConstanteNumerica(string tipo, string valor)
        {
            if (tipo == "char" || tipo == "void")
            {
                ErrorTexto($"Una constante numérica no puede asignarse a tipo '{tipo}' en línea {Numero_Linea}.");
            }
        }

        private void ValidarCadena(string tipo, string valor)
        {
            if (tipo != "char" && tipo != "char*" && tipo != "string")
            {
                ErrorTexto($"No se puede asignar una cadena a tipo '{tipo}' en línea {Numero_Linea}.");
            }
        }

        private void ValidarCaracter(string tipo, string valor)
        {
            if (tipo != "char")
                ErrorTexto($"Solo variables tipo char pueden recibir caracteres. Tipo actual: '{tipo}'. Línea {Numero_Linea}.");
        }

        private void ValidarExpresionAritmetica(string tipo, string expr)
        {
            if (tipo == "char")
                ErrorTexto($"Expresión aritmética incompatible con tipo char en línea {Numero_Linea}.");
        }

        private void ValidarExpresionLogica(string tipo, string expr)
        {
            if (tipo != "bool" && tipo != "int")
                ErrorTexto($"Expresión lógica incompatible con tipo '{tipo}' en línea {Numero_Linea}.");
        }


        private void Error(int caracter)
        {
            N_Error++;
            string mensaje = $"Error: carácter inválido '{(char)caracter}' (código {caracter}) en línea {Numero_Linea}";
            Escribir.WriteLine(mensaje);
            Rtbx_salida.AppendText(mensaje + Environment.NewLine);
        }

        private void ErrorSintactico(string mensaje)
        {
            N_Error++;
            string texto = $"Error Sintáctico: {mensaje} en línea {Numero_Linea}";
            Escribir.WriteLine(texto);
            Rtbx_salida.AppendText(texto + Environment.NewLine);
        }

        private void ErrorSemantico(string mensaje)
        {
            N_Error++;
            string texto = $"Error Semántico: {mensaje} en línea {Numero_Linea}";
            Escribir.WriteLine(texto);
            Rtbx_salida.AppendText(texto + Environment.NewLine);
        }

        private void ErrorTexto(string mensaje)
        {
            ErrorSemantico(mensaje); 
        }

        private void ValidarEstructurasControl(string linea)
        {
            linea = linea.Trim();
            string palabraReservada = "";

            if (linea.StartsWith("if")) palabraReservada = "if";
            else if (linea.StartsWith("while")) palabraReservada = "while";
            else if (linea.StartsWith("for")) palabraReservada = "for";
            else return;

            // Validación paréntesis
            int idxInicio = linea.IndexOf('(');
            int idxFin = linea.LastIndexOf(')');

            if (idxInicio == -1 || idxFin == -1 || idxFin < idxInicio)
            {
                ErrorSintactico($"La estructura '{palabraReservada}' debe tener paréntesis '(' y ')' bien formados.");
                return;
            }

            // Extrae contenido
            string contenido = linea.Substring(idxInicio + 1, idxFin - idxInicio - 1).Trim();

            // Validación de contenido vacío
            if (string.IsNullOrWhiteSpace(contenido))
            {
                ErrorSintactico($"La condición del '{palabraReservada}' no puede estar vacía.");
                return;
            }

            if (palabraReservada != "for")
            {
                // Si no parece una expresión lógica válida
                if (!EsExpresionLogica(contenido) && !Variables.ContainsKey(contenido) && !bool.TryParse(contenido, out _))
                {
                    bool esVariableBool = false;
                    if (Variables.ContainsKey(contenido))
                    {
                        if (Variables[contenido].tipo == "bool" || Variables[contenido].tipo == "int") esVariableBool = true;
                    }

                    if (!esVariableBool)
                    {
                        ErrorSemantico($"La condición del '{palabraReservada}' espera una expresión lógica o booleana. Encontrado: '{contenido}'.");
                    }
                }
            }
        }

        private void DetectarDeclaraciones(string linea)
        {
            linea = linea.Trim();

            if (String.IsNullOrWhiteSpace(linea) || linea.StartsWith("//") || linea.StartsWith("#") || linea.StartsWith("case ") || linea.StartsWith("default:"))
                return;

            // Detecta Estructuras de Control
            if (linea.StartsWith("if") || linea.StartsWith("while") || linea.StartsWith("for"))
            {
                ValidarEstructurasControl(linea);
                return;
            }

            // Validar ; final
            bool esCabeceraFuncion = linea.Contains("(") && linea.EndsWith(")");
            bool esBloque = linea.EndsWith("{") || linea.EndsWith("}");

            // Si es cabecera de función, validarla aparte
            if (esCabeceraFuncion)
            {
                DetectarDefinicionFuncion(linea);
                return;
            }


            // exige si no es función, ni bloque, ni estructura de control
            if (!esCabeceraFuncion && !esBloque && !linea.EndsWith(";"))
            {
                ErrorSintactico("Falta ';' al final de la sentencia.");
            }

            foreach (var tipo in TiposValidos)
            {
                // Buscar declaraciones
                if (Regex.IsMatch(linea, $@"^{tipo}\s+") || linea.StartsWith(tipo + "[]"))
                {
                    string resto = linea.Substring(tipo.Length).Trim();
                
                    //  Arreglos
                    if (resto.Contains("["))
                    {
                        int ini = resto.IndexOf("[");
                        int fin = resto.IndexOf("]");

                        if (ini == -1 || fin == -1 || fin < ini)
                        {
                            ErrorSintactico("Declaración de arreglo mal formada. Se esperan corchetes '[]'.");
                            return;
                        }

                        string nombre = resto.Substring(0, ini).Trim();
                        string tamStr = resto.Substring(ini + 1, fin - ini - 1).Trim();

                        if (!int.TryParse(tamStr, out int tam))
                        {
                            ErrorSintactico("El tamaño del arreglo debe ser un número entero constante.");
                            return;
                        }

                        // Verificar Inicialización
                        int posIgual = resto.IndexOf("=");
                        if (posIgual != -1)
                        {
                            string inicializacion = resto.Substring(posIgual + 1).Trim().TrimEnd(';');

                            if (!inicializacion.StartsWith("{") || !inicializacion.EndsWith("}"))
                            {
                                ErrorSintactico("La inicialización de arreglos requiere llaves '{...}'.");
                                return;
                            }

                            string contenidoLlaves = inicializacion.Substring(1, inicializacion.Length - 2);

                            // Usa Split con RemoveEmptyEntries para mas valores
                            string[] valores = contenidoLlaves.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                            if (valores.Length > tam)
                            {
                                ErrorSemantico($"Desbordamiento de arreglo '{nombre}'. Tamaño declarado: {tam}, Elementos dados: {valores.Length}.");
                                return;
                            }

                            // Validar que los elementos sean números
                            foreach (var val in valores)
                            {
                                if (!float.TryParse(val.Trim(), out _))
                                {
                                    ErrorSemantico($"El elemento '{val}' en el arreglo no es un número válido.");
                                }
                            }
                        }

                        // Registra variable
                        if (Variables.ContainsKey(nombre))
                        {
                            ErrorSemantico($"La variable '{nombre}' ya ha sido declarada previamente.");
                        }
                        else
                        {
                            Variables.Add(nombre, (tipo, true, tam));
                            Escribir.WriteLine($"Declaración: Arreglo {tipo} {nombre}[{tam}]");
                        }
                    }
                    else
                    {
                        string declaracion = resto.Split('=')[0].Trim().TrimEnd(';');

                        string[] nombres = declaracion.Split(',');

                        foreach (string nom in nombres)
                        {
                            string nombre = nom.Trim();

                            if (Variables.ContainsKey(nombre))
                            {
                                ErrorSemantico($"La variable '{nombre}' ya ha sido declarada previamente.");
                            }
                            else
                            {
                                Variables.Add(nombre, (tipo, false, 0));
                                Escribir.WriteLine($"Declaración: Variable {tipo} {nombre}");
                            }
                        }
                    }
                    return;
                }
            }
        }

        private void VerificarUsoVariable(string token, char siguiente)
        {
            if (!P_Reservadas.Contains(token)) // no verificar palabras reservadas
            {
                // si el siguiente es '(' probablemente sea una llamada o definición de función
                if (siguiente == '(') return;

                // si el token parece una constante numérica o literal, salta
                if (int.TryParse(token, out _) || float.TryParse(token, out _)) return;

                if (!Variables.ContainsKey(token))
                {
                    ErrorTexto($"Variable '{token}' usada sin declarar (línea {Numero_Linea}).");
                }
            }
        }

        private List<string> DetectarEstructuras(string linea)
        {
            List<string> errores = new List<string>();
            string l = linea.Trim();

            // IF simple
            if (Regex.IsMatch(l, @"^if\s*<condicion>\s*\{$"))
            {
                return errores;
            }

            // IF compuesto
            if (Regex.IsMatch(l, @"^if\s*<condicion>\s*\{$") ||
                Regex.IsMatch(l, @"^\}\s*else\s*\{$"))
            {
                return errores;
            }

            // SWITCH
            if (Regex.IsMatch(l, @"^switch\s*\(.+\)\s*\{$"))
            {
                return errores;
            }

            // WHILE
            if (Regex.IsMatch(l, @"^while\s*<condicion>\s*\{$"))
            {
                return errores;
            }

            // FOR
            if (Regex.IsMatch(l, @"^for\s*\(.+\)\s*\{$"))
            {
                return errores;
            }


            // No coincide con ninguna estructura válida:
            if (l.Contains("<condicion>"))
                errores.Add("Error: Uso incorrecto de <condicion>.");

            return errores;
        }


        private void AnalizarLlavesYParentesis(string[] lineas)
        {
            Stack<(char caracter, int linea)> pila = new Stack<(char, int)>();
            int lineaActual = 1;

            foreach (string linea in lineas)
            {
                for (int i = 0; i < linea.Length; i++)
                {
                    char c = linea[i];

                    if (c == '(' || c == '{')
                    {
                        pila.Push((c, lineaActual));
                    }
                    else if (c == ')' || c == '}')
                    {
                        if (pila.Count == 0)
                        {
                            ErrorTexto($"Cierre '{c}' sin apertura correspondiente en línea {lineaActual}.");
                            continue;
                        }

                        var (abierto, lineaApertura) = pila.Pop();
                        if ((abierto == '(' && c != ')') || (abierto == '{' && c != '}'))
                        {
                            ErrorTexto($"Cierre incorrecto '{c}' para apertura '{abierto}' en línea {lineaActual} (abierto en línea {lineaApertura}).");
                        }
                    }
                }
                lineaActual++;
            }

            while (pila.Count > 0)
            {
                var (abierto, lineaApertura) = pila.Pop();
                ErrorTexto($"Apertura '{abierto}' sin cierre correspondiente (abierto en línea {lineaApertura}).");
            }
        }



        private bool DentroComentarioBloque = false;

        private bool EsComentario(string linea)
        {
            string l = linea.Trim();

            // Si ya estamos dentro de un comentario /**/
            if (DentroComentarioBloque)
            {
                if (l.Contains("*/"))
                {
                    DentroComentarioBloque = false;
                }
                return true; // Ignorar línea completa
            }

            // Comentario de línea //
            if (l.StartsWith("//"))
                return true;

            // Inicio de bloque /*
            if (l.StartsWith("/*"))
            {
                DentroComentarioBloque = !l.Contains("*/");
                return true;
            }

            return false;
        }

        private void DetectarComentarioBloque()
        {
            int anterior = 0;
            i_caracter = Leer.Read();

            while (i_caracter != -1)
            {
                if (anterior == '*' && i_caracter == '/')
                {
                    i_caracter = Leer.Read();
                    break;
                }

                anterior = i_caracter;
                i_caracter = Leer.Read();
            }

            Escribir.WriteLine("comentario");
        }


        private void ValidarSentenciaEnBloque(string linea, int nivelBloque)
        {
            if (nivelBloque <= 0) return;

            string l = linea.Trim();

            if (string.IsNullOrWhiteSpace(l)) return;

            if (l.StartsWith("//") || l.StartsWith("/*") || l.StartsWith("*") || l.StartsWith("*/"))
                return;

            if (l == "{" || l == "}") return;

            if (l.StartsWith("case ") && l.EndsWith(":")) return;
            if (l == "default:") return;

            if (l.StartsWith("if") || l.StartsWith("else") || l.StartsWith("for") ||
                l.StartsWith("while") || l.StartsWith("switch") || l == "do")
                return;

            if (l.Contains("printf"))
            {
                if (!Regex.IsMatch(l, @"\bprintf\s*\("))
                {
                    ErrorTexto($"Uso incorrecto de 'printf': '{l}'.");
                    return;
                }
            }
            else
            {
                // Si contiene algo parecido, detectarlo como error:
                if (Regex.IsMatch(l, @"\bprin|prinf|pritnf|print|printff"))
                {
                    ErrorTexto($"Error: '{l}'.");
                    return;
                }
            }

            // VALIDAR COMILLAS
            if (l.Count(c => c == '"') % 2 != 0)
            {
                ErrorTexto($"Comillas desbalanceadas en: '{l}'.");
                return;
            }

            // VALIDAR PARÉNTESIS
            int open = l.Count(c => c == '(');
            int close = l.Count(c => c == ')');

            if (open != close)
            {
                ErrorTexto($"Paréntesis desbalanceados en: '{l}'.");
                return;
            }

            // VALIDAR ';'
            if (!l.EndsWith(";"))
            {
                ErrorTexto($"Falta ';' al final de la instrucción: '{l}'.");
                return;
            }
        }
        private void DetectarDefinicionFuncion(string linea)
        {
            linea = linea.Trim();

            // Debe terminar en ')'
            if (!linea.Contains("(") || !linea.EndsWith(")"))
                return;

            // Expresión regular para validar estructura
            var match = Regex.Match(linea,
                @"^(int|float|double|char|bool|void|string)\s+([A-Za-z_]\w*)\s*\((.*)\)$");

            if (!match.Success)
            {
                ErrorSintactico("Definición de función mal formada.");
                return;
            }

            string tipoRetorno = match.Groups[1].Value;
            string nombreFuncion = match.Groups[2].Value;
            string parametros = match.Groups[3].Value;

            Escribir.WriteLine($"Definición de función detectada: {tipoRetorno} {nombreFuncion}");

            // Validar parámetros
            if (!string.IsNullOrWhiteSpace(parametros))
            {
                string[] listaParametros = parametros.Split(',');

                foreach (string param in listaParametros)
                {
                    string p = param.Trim();

                    var matchParam = Regex.Match(p,
                        @"^(int|float|double|char|bool|string)\s+([A-Za-z_]\w*)$");

                    if (!matchParam.Success)
                    {
                        ErrorSintactico($"Parámetro inválido en función '{nombreFuncion}': '{p}'");
                    }
                }
            }
        }





    }
}
