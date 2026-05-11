using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Practica1;

namespace Practica1
{
    public partial class Form1 : Form
    {
        private class RegistroVariable
        {
            public string Nombre { get; set; }
            public string Tipo { get; set; }
            public string Ambito { get; set; }
            public string ValorInicial { get; set; }
            public int Linea { get; set; }
        }

        private class RegistroFuncion
        {
            public string Nombre { get; set; }
            public string TipoRetorno { get; set; }
            public string Parametros { get; set; }
            public int CantidadParametros { get; set; }
            public int Linea { get; set; }
        }

        private HashSet<int> LineasConExpresionAutonoma = new HashSet<int>();
        private HashSet<int> LineasEcuaciones = new HashSet<int>();
        private enum TipoTokenExpresion
        {
            Identificador,
            Numero,
            Cadena,
            Caracter,
            Booleano,
            Operador,
            ParentesisAbre,
            ParentesisCierra,
            CorcheteAbre,
            CorcheteCierra,
            Coma,
            Fin
        }

        private class TokenExpresion
        {
            public TipoTokenExpresion Tipo { get; set; }
            public string Valor { get; set; }
            public int Posicion { get; set; }
        }

        private class ResultadoAnalisisExpresion
        {
            public bool EsValida { get; set; }
            public string Error { get; set; }
            public bool TieneOperadoresAritmeticos { get; set; }
            public bool TieneOperadoresRelacionales { get; set; }
            public bool TieneOperadoresLogicos { get; set; }
            public bool TieneBooleanoLiteral { get; set; }
        }

        private class AnalizadorExpresiones
        {
            private readonly List<TokenExpresion> tokens;
            private int indice;

            public AnalizadorExpresiones(List<TokenExpresion> tokens)
            {
                this.tokens = tokens;
            }

            public ResultadoAnalisisExpresion Resultado { get; } = new ResultadoAnalisisExpresion();

            public bool Analizar(out string error)
            {
                error = null;

                if (tokens == null || tokens.Count == 0)
                {
                    error = "La expresión está vacía.";
                    return false;
                }

                if (!ParseExpression())
                {
                    error = Resultado.Error ?? "Expresión inválida.";
                    return false;
                }

                if (TokenActual().Tipo != TipoTokenExpresion.Fin)
                {
                    error = $"Token inesperado '{TokenActual().Valor}' en la posición {TokenActual().Posicion + 1}.";
                    return false;
                }

                Resultado.EsValida = true;
                return true;
            }

            private bool ParseExpression()
            {
                return ParseLogicalOr();
            }

            private bool ParseLogicalOr()
            {
                if (!ParseLogicalAnd()) return false;

                while (CoincideOperador("||"))
                {
                    Resultado.TieneOperadoresLogicos = true;
                    Avanzar();

                    if (!ParseLogicalAnd())
                    {
                        return RegistrarError("Se esperaba una expresión después de '||'.");
                    }
                }

                return true;
            }

            private bool ParseLogicalAnd()
            {
                if (!ParseEquality()) return false;

                while (CoincideOperador("&&"))
                {
                    Resultado.TieneOperadoresLogicos = true;
                    Avanzar();

                    if (!ParseEquality())
                    {
                        return RegistrarError("Se esperaba una expresión después de '&&'.");
                    }
                }

                return true;
            }

            private bool ParseEquality()
            {
                if (!ParseRelational()) return false;

                while (CoincideOperador("==") || CoincideOperador("!="))
                {
                    Resultado.TieneOperadoresRelacionales = true;
                    Avanzar();

                    if (!ParseRelational())
                    {
                        return RegistrarError("Se esperaba una expresión de comparación válida.");
                    }
                }

                return true;
            }

            private bool ParseRelational()
            {
                if (!ParseAdditive()) return false;

                while (CoincideOperador("<") || CoincideOperador(">") || CoincideOperador("<=") || CoincideOperador(">="))
                {
                    Resultado.TieneOperadoresRelacionales = true;
                    Avanzar();

                    if (!ParseAdditive())
                    {
                        return RegistrarError("Se esperaba una expresión después del operador relacional.");
                    }
                }

                return true;
            }

            private bool ParseAdditive()
            {
                if (!ParseMultiplicative()) return false;

                while (CoincideOperador("+") || CoincideOperador("-"))
                {
                    Resultado.TieneOperadoresAritmeticos = true;
                    Avanzar();

                    if (!ParseMultiplicative())
                    {
                        return RegistrarError("Se esperaba un término después del operador aritmético.");
                    }
                }

                return true;
            }

            private bool ParseMultiplicative()
            {
                if (!ParseUnary()) return false;

                while (CoincideOperador("*") || CoincideOperador("/") || CoincideOperador("%"))
                {
                    Resultado.TieneOperadoresAritmeticos = true;
                    Avanzar();

                    if (!ParseUnary())
                    {
                        return RegistrarError("Se esperaba un factor después del operador aritmético.");
                    }
                }

                return true;
            }

            private bool ParseUnary()
            {
                if (CoincideOperador("!") || CoincideOperador("+") || CoincideOperador("-") ||
                    CoincideOperador("++") || CoincideOperador("--"))
                {
                    string operador = TokenActual().Valor;
                    if (operador == "!")
                        Resultado.TieneOperadoresLogicos = true;
                    else
                        Resultado.TieneOperadoresAritmeticos = true;

                    Avanzar();
                    return ParseUnary() || RegistrarError($"Se esperaba una expresión después de '{operador}'.");
                }

                return ParsePostfix();
            }

            private bool ParsePostfix()
            {
                if (!ParsePrimary()) return false;

                while (true)
                {
                    if (Coincide(TipoTokenExpresion.ParentesisAbre))
                    {
                        Avanzar();

                        if (!Coincide(TipoTokenExpresion.ParentesisCierra))
                        {
                            do
                            {
                                if (!ParseExpression())
                                {
                                    return RegistrarError("Argumento inválido en la llamada a función.");
                                }
                            }
                            while (ConsumirSiEs(TipoTokenExpresion.Coma));
                        }

                        if (!ConsumirSiEs(TipoTokenExpresion.ParentesisCierra))
                        {
                            return RegistrarError("Falta ')' en la llamada a función.");
                        }

                        continue;
                    }

                    if (Coincide(TipoTokenExpresion.CorcheteAbre))
                    {
                        Avanzar();

                        if (!ParseExpression())
                        {
                            return RegistrarError("Índice inválido dentro de los corchetes.");
                        }

                        if (!ConsumirSiEs(TipoTokenExpresion.CorcheteCierra))
                        {
                            return RegistrarError("Falta ']' en el acceso al arreglo.");
                        }

                        continue;
                    }

                    if (CoincideOperador("++") || CoincideOperador("--"))
                    {
                        Resultado.TieneOperadoresAritmeticos = true;
                        Avanzar();
                        continue;
                    }

                    break;
                }

                return true;
            }

            private bool ParsePrimary()
            {
                TokenExpresion actual = TokenActual();

                if (actual.Tipo == TipoTokenExpresion.Numero ||
                    actual.Tipo == TipoTokenExpresion.Identificador ||
                    actual.Tipo == TipoTokenExpresion.Cadena ||
                    actual.Tipo == TipoTokenExpresion.Caracter)
                {
                    Avanzar();
                    return true;
                }

                if (actual.Tipo == TipoTokenExpresion.Booleano)
                {
                    Resultado.TieneBooleanoLiteral = true;
                    Avanzar();
                    return true;
                }

                if (ConsumirSiEs(TipoTokenExpresion.ParentesisAbre))
                {
                    if (!ParseExpression())
                    {
                        return RegistrarError("La subexpresión entre paréntesis es inválida.");
                    }

                    if (!ConsumirSiEs(TipoTokenExpresion.ParentesisCierra))
                    {
                        return RegistrarError("Falta ')' para cerrar la subexpresión.");
                    }

                    return true;
                }

                return RegistrarError($"Se encontró '{actual.Valor}' donde se esperaba un operando.");
            }

            private TokenExpresion TokenActual()
            {
                return indice < tokens.Count ? tokens[indice] : tokens[tokens.Count - 1];
            }

            private void Avanzar()
            {
                if (indice < tokens.Count)
                {
                    indice++;
                }
            }

            private bool Coincide(TipoTokenExpresion tipo)
            {
                return TokenActual().Tipo == tipo;
            }

            private bool CoincideOperador(string operador)
            {
                return TokenActual().Tipo == TipoTokenExpresion.Operador && TokenActual().Valor == operador;
            }

            private bool ConsumirSiEs(TipoTokenExpresion tipo)
            {
                if (!Coincide(tipo))
                {
                    return false;
                }

                Avanzar();
                return true;
            }

            private bool RegistrarError(string mensaje)
            {
                if (string.IsNullOrWhiteSpace(Resultado.Error))
                {
                    Resultado.Error = mensaje;
                }

                return false;
            }
        }


        // Tabla de variables globales
        private Dictionary<string, (string tipo, bool esArreglo, int tam)> Variables = new Dictionary<string, (string, bool, int)>();

        private HashSet<string> FuncionesDeclaradas = new HashSet<string>();
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
            LineasEcuaciones.Clear();





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

                if (IntentarAnalizarExpresionAritmeticaIndependiente(lineaProcesable))
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

                bool esEcuacionMatematica = EsEcuacionMatematica(lineaProcesable);

                if (esEcuacionMatematica)
                {
                    LineasEcuaciones.Add(Numero_Linea);
                    ValidarEcuacionMatematica(lineaProcesable);
                }
                else
                {
                    DetectarDeclaraciones(lineaProcesable);
                    DetectarAsignacion(lineaProcesable);
                    DetectarEstructuras(lineaProcesable);
                }

                if (EsExpresionAutonoma(lineaProcesable, nivelBloque))
                {
                    LineasConExpresionAutonoma.Add(Numero_Linea);
                    ValidarExpresionAutonoma(lineaProcesable);
                    Numero_Linea++;
                    continue;
                }

                DetectarDeclaraciones(lineaProcesable);
                DetectarAsignacion(lineaProcesable);
                DetectarEstructuras(lineaProcesable);

                bool esEstructuraOBloque = lineaProcesable.StartsWith("if") ||
                                           lineaProcesable.StartsWith("while") ||
                                           lineaProcesable.StartsWith("for") ||
                                           lineaProcesable.Contains("{") ||
                                           lineaProcesable.Contains("}");

                if (!esEstructuraOBloque && !esEcuacionMatematica)
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
            {
                Rtbx_salida.AppendText("Análisis completado sin errores.\n");
            }
            else
            {
                Rtbx_salida.AppendText($"\nAnálisis completado con {N_Error} errores.\n");
                
               
            }
            GenerarTablasDeElementos();
            Rtbx_salida.SelectionStart = Rtbx_salida.TextLength;
            Rtbx_salida.ScrollToCaret();
        }

        private void GenerarTablasDeElementos()
        {
            var variables = new List<RegistroVariable>();
            var funciones = new List<RegistroFuncion>();
            var ambitos = new Stack<string>();
            ambitos.Push("global");

            string[] lineas = richTextBox1.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lineas.Length; i++)
            {
                string lineaOriginal = lineas[i];
                string linea = LimpiarLineaParaTabla(lineaOriginal);
                if (string.IsNullOrWhiteSpace(linea)) continue;

                var funcionMatch = Regex.Match(linea,
                   @"^(?<tipo>int|float|double|char|bool|void|long|short)\s+(?<nombre>[A-Za-z_]\w*)\s*\((?<params>[^\)]*)\)\s*(\{)?$");

                if (funcionMatch.Success)
                {
                    string nombreFuncion = funcionMatch.Groups["nombre"].Value;
                    string parametros = funcionMatch.Groups["params"].Value.Trim();
                    string[] listaParametros = string.IsNullOrWhiteSpace(parametros)
                        ? new string[0]
                        : parametros.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();

                    funciones.Add(new RegistroFuncion
                    {
                        Nombre = nombreFuncion,
                        TipoRetorno = funcionMatch.Groups["tipo"].Value,
                        Parametros = string.IsNullOrWhiteSpace(parametros) ? "sin parámetros" : parametros,
                        CantidadParametros = listaParametros.Length,
                        Linea = i + 1
                    });

                    foreach (string param in listaParametros)
                    {
                        var paramMatch = Regex.Match(param,
                            @"^(?<tipo>int|float|double|char|bool|long|short)\s+(\*\s*)?(?<nombre>[A-Za-z_]\w*)(\[\])?$");

                        if (paramMatch.Success)
                        {
                            variables.Add(new RegistroVariable
                            {
                                Nombre = paramMatch.Groups["nombre"].Value,
                                Tipo = paramMatch.Groups["tipo"].Value,
                                Ambito = nombreFuncion,
                                ValorInicial = "parámetro",
                                Linea = i + 1
                            });
                        }
                    }

                    ambitos.Push(nombreFuncion);
                    continue;
                }

                var declaracionMatch = Regex.Match(linea,
                    @"^(?<tipo>int|float|double|char|bool|long|short)\s+(?<resto>[^;]+);$");

                if (declaracionMatch.Success)
                {
                    string tipo = declaracionMatch.Groups["tipo"].Value;
                    string[] declaraciones = declaracionMatch.Groups["resto"].Value.Split(',');

                    foreach (string declaracion in declaraciones)
                    {
                        string entrada = declaracion.Trim();
                        if (string.IsNullOrWhiteSpace(entrada)) continue;

                        var nombreMatch = Regex.Match(entrada, @"^(?<nombre>[A-Za-z_]\w*)(\s*=\s*(?<valor>.+))?$");
                        if (!nombreMatch.Success) continue;

                        variables.Add(new RegistroVariable
                        {
                            Nombre = nombreMatch.Groups["nombre"].Value,
                            Tipo = tipo,
                            Ambito = ambitos.Peek(),
                            ValorInicial = nombreMatch.Groups["valor"].Success
                                ? nombreMatch.Groups["valor"].Value.Trim()
                                : "-",
                            Linea = i + 1
                        });
                    }
                }

                int cierres = linea.Count(c => c == '}');
                while (cierres > 0 && ambitos.Count > 1)
                {
                    ambitos.Pop();
                    cierres--;
                }
            }

            Rtbx_salida.AppendText("\n===== TABLA DE VARIABLES =====\n");
            Rtbx_salida.AppendText("| Nombre | Tipo | Ámbito | Valor inicial | Línea |\n");
            Rtbx_salida.AppendText("|---|---|---|---|---|\n");
            if (variables.Count == 0)
            {
                Rtbx_salida.AppendText("| (sin variables detectadas) | - | - | - | - |\n");
            }
            else
            {
                foreach (var v in variables)
                {
                    Rtbx_salida.AppendText($"| {v.Nombre} | {v.Tipo} | {v.Ambito} | {v.ValorInicial} | {v.Linea} |\n");
                }
            }

            Rtbx_salida.AppendText("\n===== TABLA DE FUNCIONES =====\n");
            Rtbx_salida.AppendText("| Nombre | Tipo de retorno | Parámetros | # parámetros | Línea |\n");
            Rtbx_salida.AppendText("|---|---|---|---|---|\n");
            if (funciones.Count == 0)
            {
                Rtbx_salida.AppendText("| (sin funciones detectadas) | - | - | - | - |\n");
            }
            else
            {
                foreach (var f in funciones)
                {
                    Rtbx_salida.AppendText($"| {f.Nombre} | {f.TipoRetorno} | {f.Parametros} | {f.CantidadParametros} | {f.Linea} |\n");
                }
            }
        }

        private string LimpiarLineaParaTabla(string linea)
        {
            string sinComentarioLinea = Regex.Replace(linea, @"//.*$", "");
            string sinComentariosBloque = Regex.Replace(sinComentarioLinea, @"/\*.*?\*/", "");
            return sinComentariosBloque.Trim();
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

        private bool IntentarAnalizarExpresionAritmeticaIndependiente(string linea)
        {
            string expresion = linea.Trim();

            if (expresion.EndsWith(";"))
            {
                expresion = expresion.Substring(0, expresion.Length - 1).Trim();
            }

            if (!PareceExpresionAritmeticaIndependiente(expresion))
            {
                return false;
            }

            try
            {
                Practica1.ArbolExpresionBinaria arbol = new Practica1.ArbolExpresionBinaria();
                arbol.ConstruirDesdeInfija(expresion);

                Rtbx_salida.AppendText("===== ÁRBOL BINARIO DE EXPRESIÓN =====" + Environment.NewLine);
                Rtbx_salida.AppendText("Línea: " + Numero_Linea + Environment.NewLine);
                Rtbx_salida.AppendText("Expresión infija: " + expresion + Environment.NewLine);
                Rtbx_salida.AppendText("Inorden parentizado: " + arbol.ObtenerInordenParentizado() + Environment.NewLine);
                Rtbx_salida.AppendText(arbol.ObtenerArbolComoTexto());
                Rtbx_salida.AppendText(Environment.NewLine);
            }
            catch (Exception ex)
            {
                ErrorSintactico("Expresión aritmética inválida: " + ex.Message);
            }

            return true;
        }

        private bool PareceExpresionAritmeticaIndependiente(string expresion)
        {
            if (string.IsNullOrWhiteSpace(expresion))
            {
                return false;
            }

            if (!Regex.IsMatch(expresion, @"^[0-9A-Za-z_+\-*/().\s]+$"))
            {
                return false;
            }

            return Regex.IsMatch(expresion, @"[+\-*/]") &&
                   Regex.IsMatch(expresion, @"[0-9A-Za-z_]");
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
            bool expresionValida = ValidarSintaxisExpresionAsignacion(derecha, izquierda);



            ResultadoAnalisisExpresion analisisExpresion = null;
            if (!EsCadena(derecha) && !EsCaracter(derecha))
            {
                analisisExpresion = AnalizarExpresion(derecha);
                if (!analisisExpresion.EsValida && !EsConstanteNumerica(derecha))
                {
                    ErrorSintactico($"Expresión inválida en la asignación de '{izquierda}': {analisisExpresion.Error}");
                    return;
                }
            }


            if (!Variables.ContainsKey(izquierda))
            {
                ErrorTexto($"Asignación a variable no declarada '{izquierda}'.");
                return;
            }

            var (tipo, esArreglo, tam) = Variables[izquierda];

            if (esArreglo)
            {
                ErrorTexto($"No se puede asignar a un arreglo completo: '{izquierda}'.");
                return;
            }

            if (!expresionValida)
                return;
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
            else if ((analisisExpresion != null && analisisExpresion.EsValida) || Regex.IsMatch(derecha, @"^[A-Za-z_]\w*\s*\(.*\)$"))
            {
                // La expresión ya fue validada sintácticamente o corresponde a una llamada a función.
            }
            else
            {
                ErrorTexto($"Expresión no reconocida en la asignación de '{izquierda}'.");
            }

        }

        private bool ValidarSintaxisExpresionAsignacion(string expresion, string variableDestino)
        {
            if (string.IsNullOrWhiteSpace(expresion))
            {
                ErrorSintactico($"La asignación a '{variableDestino}' requiere una expresión a la derecha del '='.");
                return false;
            }

            if (!ParentesisBalanceados(expresion))
            {
                ErrorSintactico($"La expresión asignada a '{variableDestino}' tiene paréntesis desbalanceados.");
                return false;
            }

            List<string> tokens = TokenizarExpresion(expresion);
            if (tokens.Count == 0)
            {
                ErrorSintactico($"La asignación a '{variableDestino}' requiere una expresión válida.");
                return false;
            }

            bool esperaOperando = true;
            int profundidad = 0;

            foreach (string token in tokens)
            {
                if (token == "(")
                {
                    if (!esperaOperando)
                    {
                        ErrorSintactico($"Falta un operador antes de '(' en la asignación a '{variableDestino}'.");
                        return false;
                    }

                    profundidad++;
                    continue;
                }

                if (token == ")")
                {
                    if (esperaOperando)
                    {
                        ErrorSintactico($"Hay un operador sin operando antes de ')' en la asignación a '{variableDestino}'.");
                        return false;
                    }

                    profundidad--;
                    if (profundidad < 0)
                    {
                        ErrorSintactico($"La expresión asignada a '{variableDestino}' tiene un ')' sin apertura correspondiente.");
                        return false;
                    }

                    esperaOperando = false;
                    continue;
                }

                if (EsOperadorAritmetico(token))
                {
                    if (esperaOperando)
                    {
                        ErrorSintactico($"Operador '{token}' sin operando válido en la asignación a '{variableDestino}'.");
                        return false;
                    }

                    esperaOperando = true;
                    continue;
                }

                if (esperaOperando)
                {
                    esperaOperando = false;
                }
                else
                {
                    ErrorSintactico($"Falta un operador entre operandos en la asignación a '{variableDestino}'.");
                    return false;
                }
            }

            if (esperaOperando)
            {
                ErrorSintactico($"La expresión asignada a '{variableDestino}' termina con un operador incompleto.");
                return false;
            }

            return true;
        }

        private bool ParentesisBalanceados(string expresion)
        {
            int balance = 0;

            foreach (char c in expresion)
            {
                if (c == '(')
                    balance++;
                else if (c == ')')
                {
                    balance--;
                    if (balance < 0)
                        return false;
                }
            }

            return balance == 0;
        }

        private List<string> TokenizarExpresion(string expresion)
        {
            return Regex.Matches(expresion, @"[A-Za-z_]\w*|\d+(?:\.\d+)?|[()+\-*/]")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .ToList();
        }

        private bool EsOperadorAritmetico(string token)
        {
            return token == "+" || token == "-" || token == "*" || token == "/";
        }




        private bool EsEcuacionMatematica(string linea)
        {
            if (string.IsNullOrWhiteSpace(linea))
                return false;

            if (linea.EndsWith(";"))
                return false;

            if (linea.Count(c => c == '=') != 1 || linea.Contains("==") || linea.Contains(">=") || linea.Contains("<=") || linea.Contains("!="))
                return false;

            int indiceIgual = linea.IndexOf('=');
            if (indiceIgual <= 0 || indiceIgual >= linea.Length - 1)
                return false;

            string izquierda = linea.Substring(0, indiceIgual).Trim();
            string derecha = linea.Substring(indiceIgual + 1).Trim();

            return EsLadoEcuacionValido(izquierda) && EsLadoEcuacionValido(derecha);
        }

        private bool EsLadoEcuacionValido(string lado)
        {
            if (string.IsNullOrWhiteSpace(lado))
                return false;

            if (!Regex.IsMatch(lado, @"^[A-Za-z0-9_\+\-\*\/\(\)\.\s]+$"))
                return false;

            int balance = 0;
            foreach (char c in lado)
            {
                if (c == '(') balance++;
                if (c == ')') balance--;
                if (balance < 0) return false;
            }

            return balance == 0;
        }

        private void ValidarEcuacionMatematica(string linea)
        {
            int indiceIgual = linea.IndexOf('=');
            string izquierda = linea.Substring(0, indiceIgual).Trim();
            string derecha = linea.Substring(indiceIgual + 1).Trim();

            if (string.IsNullOrWhiteSpace(izquierda) || string.IsNullOrWhiteSpace(derecha))
            {
                ErrorSintactico("La ecuación matemática debe tener una expresión válida a ambos lados de '='.");
                return;
            }

            if (!EsLadoEcuacionValido(izquierda) || !EsLadoEcuacionValido(derecha))
            {
                ErrorSintactico($"La ecuación matemática '{linea}' está mal formada.");
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
        // Analiza la sintaxis completa de una expresión y devuelve el resultado detallado de su validación.
        private ResultadoAnalisisExpresion AnalizarExpresion(string expresion)
        {
            var resultado = new ResultadoAnalisisExpresion();

            if (string.IsNullOrWhiteSpace(expresion))
            {
                resultado.Error = "La expresión está vacía.";
                return resultado;
            }

            List<TokenExpresion> tokens = TokenizarExpresion(expresion, out string errorTokenizacion);
            if (!string.IsNullOrWhiteSpace(errorTokenizacion))
            {
                resultado.Error = errorTokenizacion;
                return resultado;
            }

            var parser = new AnalizadorExpresiones(tokens);
            if (!parser.Analizar(out string errorAnalisis))
            {
                resultado.Error = errorAnalisis;
                return resultado;
            }

            return parser.Resultado;
        }

        // Convierte una expresión de texto en tokens para que el analizador sintáctico pueda recorrerla.
        private List<TokenExpresion> TokenizarExpresion(string expresion, out string error)
        {
            var tokens = new List<TokenExpresion>();
            error = null;

            for (int i = 0; i < expresion.Length; i++)
            {
                char actual = expresion[i];

                if (char.IsWhiteSpace(actual))
                {
                    continue;
                }

                if (char.IsLetter(actual) || actual == '_')
                {
                    int inicio = i;
                    while (i + 1 < expresion.Length && (char.IsLetterOrDigit(expresion[i + 1]) || expresion[i + 1] == '_'))
                    {
                        i++;
                    }

                    string valor = expresion.Substring(inicio, i - inicio + 1);
                    tokens.Add(new TokenExpresion
                    {
                        Tipo = (valor == "true" || valor == "false") ? TipoTokenExpresion.Booleano : TipoTokenExpresion.Identificador,
                        Valor = valor,
                        Posicion = inicio
                    });
                    continue;
                }

                if (char.IsDigit(actual))
                {
                    int inicio = i;
                    bool tienePunto = false;

                    while (i + 1 < expresion.Length)
                    {
                        char siguiente = expresion[i + 1];
                        if (char.IsDigit(siguiente))
                        {
                            i++;
                            continue;
                        }

                        if (siguiente == '.' && !tienePunto)
                        {
                            tienePunto = true;
                            i++;
                            continue;
                        }

                        break;
                    }

                    tokens.Add(new TokenExpresion
                    {
                        Tipo = TipoTokenExpresion.Numero,
                        Valor = expresion.Substring(inicio, i - inicio + 1),
                        Posicion = inicio
                    });
                    continue;
                }

                if (actual == '"')
                {
                    int inicio = i;
                    bool escape = false;
                    i++;

                    while (i < expresion.Length)
                    {
                        if (!escape && expresion[i] == '"')
                        {
                            break;
                        }

                        escape = !escape && expresion[i] == '\\';
                        i++;
                    }

                    if (i >= expresion.Length || expresion[i] != '"')
                    {
                        error = $"Cadena sin cerrar en la posición {inicio + 1}.";
                        return tokens;
                    }

                    tokens.Add(new TokenExpresion
                    {
                        Tipo = TipoTokenExpresion.Cadena,
                        Valor = expresion.Substring(inicio, i - inicio + 1),
                        Posicion = inicio
                    });
                    continue;
                }

                if (actual == '\'')
                {
                    int inicio = i;
                    bool escape = false;
                    i++;

                    while (i < expresion.Length)
                    {
                        if (!escape && expresion[i] == '\'')
                        {
                            break;
                        }

                        escape = !escape && expresion[i] == '\\';
                        i++;
                    }

                    if (i >= expresion.Length || expresion[i] != '\'')
                    {
                        error = $"Caracter sin cerrar en la posición {inicio + 1}.";
                        return tokens;
                    }

                    tokens.Add(new TokenExpresion
                    {
                        Tipo = TipoTokenExpresion.Caracter,
                        Valor = expresion.Substring(inicio, i - inicio + 1),
                        Posicion = inicio
                    });
                    continue;
                }

                string operadorDoble = i + 1 < expresion.Length ? expresion.Substring(i, 2) : string.Empty;
                if (new[] { "&&", "||", "==", "!=", "<=", ">=", "++", "--" }.Contains(operadorDoble))
                {
                    tokens.Add(new TokenExpresion
                    {
                        Tipo = TipoTokenExpresion.Operador,
                        Valor = operadorDoble,
                        Posicion = i
                    });
                    i++;
                    continue;
                }

                if ("+-*/%!<>".Contains(actual))
                {
                    tokens.Add(new TokenExpresion
                    {
                        Tipo = TipoTokenExpresion.Operador,
                        Valor = actual.ToString(),
                        Posicion = i
                    });
                    continue;
                }

                if (actual == '(' || actual == ')' || actual == '[' || actual == ']' || actual == ',')
                {
                    tokens.Add(new TokenExpresion
                    {
                        Tipo = actual == '(' ? TipoTokenExpresion.ParentesisAbre :
                               actual == ')' ? TipoTokenExpresion.ParentesisCierra :
                               actual == '[' ? TipoTokenExpresion.CorcheteAbre :
                               actual == ']' ? TipoTokenExpresion.CorcheteCierra :
                               TipoTokenExpresion.Coma,
                        Valor = actual.ToString(),
                        Posicion = i
                    });
                    continue;
                }

                error = $"Símbolo no válido '{actual}' en la posición {i + 1}.";
                return tokens;
            }

            tokens.Add(new TokenExpresion
            {
                Tipo = TipoTokenExpresion.Fin,
                Valor = "<fin>",
                Posicion = expresion.Length
            });

            return tokens;
        }

        // Determina si una expresión tiene sintaxis correcta y si pertenece al dominio aritmético.

        private bool EsExpresionAritmetica(string s)
        {
            ResultadoAnalisisExpresion resultado = AnalizarExpresion(s);
            return resultado.EsValida && !resultado.TieneOperadoresLogicos && !resultado.TieneOperadoresRelacionales && !resultado.TieneBooleanoLiteral;
        }

        // Determina si una expresión tiene sintaxis correcta y contiene operadores o literales lógicos.
        private bool EsExpresionLogica(string s)
        {
            ResultadoAnalisisExpresion resultado = AnalizarExpresion(s);
            return resultado.EsValida && (resultado.TieneOperadoresLogicos || resultado.TieneOperadoresRelacionales || resultado.TieneBooleanoLiteral);
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
        private void ValidarAsignacionDesdeVariable(string tipoDestino, string nombreOrigen)
        {
            if (!Variables.ContainsKey(nombreOrigen))
            {
                ErrorTexto($"La variable '{nombreOrigen}' no está declarada (línea {Numero_Linea}).");
                return;
            }

            var (tipoOrigen, _, _) = Variables[nombreOrigen];
            if (tipoDestino != tipoOrigen)
            {
                ErrorTexto($"Tipos incompatibles en asignación: no se puede asignar '{tipoOrigen}' a '{tipoDestino}' (línea {Numero_Linea}).");
            }
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

        // Valida la estructura de las tres secciones de un ciclo for separando inicio, condición e incremento.
        private void ValidarExpresionFor(string contenido)
        {
            List<string> segmentos = SepararPorDelimitadorSuperior(contenido, ';');
            if (segmentos.Count != 3)
            {
                ErrorSintactico("La estructura 'for' debe contener exactamente inicio; condición; incremento.");
                return;
            }

            if (!ComponenteForValido(segmentos[0], true, "inicialización")) return;

            string condicion = segmentos[1].Trim();
            if (!string.IsNullOrWhiteSpace(condicion))
            {
                ResultadoAnalisisExpresion analisisCondicion = AnalizarExpresion(condicion);
                if (!analisisCondicion.EsValida)
                {
                    ErrorSintactico($"La condición del 'for' es inválida: {analisisCondicion.Error}");
                    return;
                }
            }

            if (!ComponenteForValido(segmentos[2], false, "incremento")) return;
        }

        // Verifica si una sección individual del for es una declaración, asignación o expresión sintácticamente correcta.
        private bool ComponenteForValido(string componente, bool permitirDeclaracion, string nombreComponente)
        {
            string texto = componente.Trim();
            if (string.IsNullOrWhiteSpace(texto))
            {
                return true;
            }

            if (permitirDeclaracion && TiposValidos.Any(tipo => Regex.IsMatch(texto, $@"^{tipo}\s+[A-Za-z_]\w*(\s*=\s*.+)?$")))
            {
                return true;
            }

            int indiceAsignacion = EncontrarAsignacionSimple(texto);
            if (indiceAsignacion >= 0)
            {
                string izquierda = texto.Substring(0, indiceAsignacion).Trim();
                string derecha = texto.Substring(indiceAsignacion + 1).Trim();

                if (!Regex.IsMatch(izquierda, @"^[A-Za-z_]\w*(\s*\[[^\]]+\])?$"))
                {
                    ErrorSintactico($"La {nombreComponente} del 'for' tiene un lado izquierdo inválido.");
                    return false;
                }

                ResultadoAnalisisExpresion analisisAsignacion = AnalizarExpresion(derecha);
                if (!analisisAsignacion.EsValida)
                {
                    ErrorSintactico($"La {nombreComponente} del 'for' es inválida: {analisisAsignacion.Error}");
                    return false;
                }

                return true;
            }

            ResultadoAnalisisExpresion analisis = AnalizarExpresion(texto);
            if (!analisis.EsValida)
            {
                ErrorSintactico($"La {nombreComponente} del 'for' es inválida: {analisis.Error}");
                return false;
            }

            return true;
        }

        // Divide una cadena por un delimitador sólo cuando éste aparece fuera de paréntesis, corchetes y literales.
        private List<string> SepararPorDelimitadorSuperior(string texto, char delimitador)
        {
            var partes = new List<string>();
            int inicio = 0;
            int nivelParentesis = 0;
            int nivelCorchetes = 0;
            bool enCadena = false;
            bool enCaracter = false;
            bool escape = false;

            for (int i = 0; i < texto.Length; i++)
            {
                char actual = texto[i];

                if ((enCadena || enCaracter) && !escape && actual == '\\')
                {
                    escape = true;
                    continue;
                }

                if (enCadena && !escape && actual == '"')
                {
                    enCadena = false;
                }
                else if (enCaracter && !escape && actual == '\'')
                {
                    enCaracter = false;
                }
                else if (!enCadena && !enCaracter)
                {
                    if (actual == '"') enCadena = true;
                    else if (actual == '\'') enCaracter = true;
                    else if (actual == '(') nivelParentesis++;
                    else if (actual == ')') nivelParentesis--;
                    else if (actual == '[') nivelCorchetes++;
                    else if (actual == ']') nivelCorchetes--;
                    else if (actual == delimitador && nivelParentesis == 0 && nivelCorchetes == 0)
                    {
                        partes.Add(texto.Substring(inicio, i - inicio));
                        inicio = i + 1;
                    }
                }

                if (escape)
                {
                    escape = false;
                }
            }

            partes.Add(texto.Substring(inicio));
            return partes;
        }

        // Encuentra el operador '=' de asignación evitando confundirlo con '==', '<=', '>=' o '!='.
        private int EncontrarAsignacionSimple(string texto)
        {
            for (int i = 0; i < texto.Length; i++)
            {
                if (texto[i] != '=') continue;

                char anterior = i > 0 ? texto[i - 1] : '\0';
                char siguiente = i + 1 < texto.Length ? texto[i + 1] : '\0';

                if (anterior == '=' || anterior == '!' || anterior == '<' || anterior == '>' || siguiente == '=')
                {
                    continue;
                }

                return i;
            }

            return -1;
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

            if (palabraReservada == "for")
            {
                ValidarExpresionFor(contenido);
                return;
            }

            ResultadoAnalisisExpresion analisis = AnalizarExpresion(contenido);
            if (!analisis.EsValida)
            {
                ErrorSintactico($"La condición del '{palabraReservada}' es inválida: {analisis.Error}");
                return;
            }

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

            if (IntentarAnalizarExpresionAritmeticaIndependiente(linea))
                return;

            // Detectar cabecera/definición de función y registrar parámetros para el análisis semántico.
            if (linea.Contains("(") && (linea.EndsWith(")") || linea.EndsWith("{")))
            {
                if (DetectarDefinicionFuncion(linea))
                {
                    return;
                }
            }
            // Validar ; final
            bool esBloque = linea.EndsWith("{") || linea.EndsWith("}");


            // exige si no es función, ni bloque, ni estructura de control
            if (!esBloque && !linea.EndsWith(";"))
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
            if (LineasConExpresionAutonoma.Contains(Numero_Linea))
            {
                return;
            }

            if (!P_Reservadas.Contains(token)) // no verificar palabras reservadas
            {
                if (LineasEcuaciones.Contains(Numero_Linea))
                    return;

                // si el siguiente es '(' probablemente sea una llamada o definición de función
                if (siguiente == '(') return;

                // si el identificador ya fue registrado como función, no se valida como variable
                if (FuncionesDeclaradas.Contains(token)) return;

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

        // Determina si una línea debe tratarse como una expresión aislada y no como una sentencia de C que requiera ';'.
        private bool EsExpresionAutonoma(string linea, int nivelBloque)
        {
            string contenido = linea.Trim();

            if (nivelBloque != 0 || string.IsNullOrWhiteSpace(contenido))
            {
                return false;
            }

            if (contenido.EndsWith(";") || contenido.EndsWith("{") || contenido.EndsWith("}"))
            {
                return false;
            }

            if (contenido.StartsWith("#") || contenido.StartsWith("if") || contenido.StartsWith("while") ||
                contenido.StartsWith("for") || contenido.StartsWith("switch") || contenido.StartsWith("case ") ||
                contenido.StartsWith("default:") || contenido == "default")
            {
                return false;
            }

            if (TiposValidos.Any(tipo => Regex.IsMatch(contenido, $@"^{tipo}\s+")))
            {
                return false;
            }

            if (EncontrarAsignacionSimple(contenido) >= 0)
            {
                return false;
            }

            ResultadoAnalisisExpresion analisis = AnalizarExpresion(contenido);
            return analisis.EsValida;
        }

        // Ejecuta la validación sintáctica de una expresión suelta y reporta sólo errores propios del parser de expresiones.
        private void ValidarExpresionAutonoma(string linea)
        {
            ResultadoAnalisisExpresion analisis = AnalizarExpresion(linea);
            if (!analisis.EsValida)
            {
                ErrorSintactico($"La expresión es inválida: {analisis.Error}");
            }
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
        private bool DetectarDefinicionFuncion(string linea)
        {
            linea = linea.Trim();

            if (!linea.Contains("("))
                return false;


            var match = Regex.Match(linea,
                @"^(int|float|double|char|bool|void|string|long|short)\s+([A-Za-z_]\w*)\s*\((.*)\)\s*(\{)?$");

            if (!match.Success)
            {
                return false;
            }

            string tipoRetorno = match.Groups[1].Value;
            string nombreFuncion = match.Groups[2].Value;
            string parametros = match.Groups[3].Value;

            FuncionesDeclaradas.Add(nombreFuncion);
            Escribir.WriteLine($"Definición de función detectada: {tipoRetorno} {nombreFuncion}");

            if (string.IsNullOrWhiteSpace(parametros) || parametros.Trim() == "void")
            {
                return true;
            }

            string[] listaParametros = parametros.Split(',');

            foreach (string param in listaParametros)
            {
                string p = param.Trim();

                {
                    var matchParam = Regex.Match(p,
                        @"^(int|float|double|char|bool|string|long|short)\s+([A-Za-z_]\w*)(\[\])?$");

                    if (!matchParam.Success)
                    {
                        ErrorSintactico($"Parámetro inválido en función '{nombreFuncion}': '{p}'");
                        continue;
                    }
                    string tipoParametro = matchParam.Groups[1].Value;
                    string nombreParametro = matchParam.Groups[2].Value;
                    if (Variables.ContainsKey(nombreParametro))
                    {
                        continue;
                    }

                    Variables.Add(nombreParametro, (tipoParametro, false, 0));
                }

              
            }

            return true;
        }

    }
}
        





    

