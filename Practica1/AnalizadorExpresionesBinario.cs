using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Practica1
{
    /// <summary>
    /// Representa un árbol binario de expresión aritmética.
    /// Su objetivo es documentar claramente el proceso de análisis léxico/sintáctico
    /// que transforma una expresión infija (por ejemplo: "(3+4)*2")
    /// en una estructura de nodos padre-hijo.
    /// </summary>
    public class ArbolExpresionBinaria
    {
        /// <summary>
        /// Nodo base para cualquier elemento del árbol.
        /// Un nodo puede ser operador (+,-,*,/) u operando (número o identificador).
        /// </summary>
        public class NodoExpresion
        {
            public string Valor { get; }
            public NodoExpresion Izquierdo { get; set; }
            public NodoExpresion Derecho { get; set; }

            /// <summary>
            /// Indica si el nodo almacena un operador binario.
            /// </summary>
            public bool EsOperador => EsTokenOperador(Valor);

            public NodoExpresion(string valor)
            {
                Valor = valor;
            }
        }
        public class ResultadoEvaluacion
        {
            public bool SePuedeEvaluar { get; set; }
            public double Valor { get; set; }
            public List<string> Correspondencias { get; } = new List<string>();
            public List<string> VariablesUsadas { get; } = new List<string>();
            public string Error { get; set; }
        }

        private class ValorSubexpresion
        {
            public double Valor { get; set; }
            public string Expresion { get; set; }
        }


        public NodoExpresion Raiz { get; private set; }

        /// <summary>
        /// Crea el árbol binario a partir de una expresión infija.
        /// 1) Convierte infijo a postfijo (Shunting Yard simplificado).
        /// 2) Construye nodos usando una pila.
        /// </summary>
        public void ConstruirDesdeInfija(string expresionInfija)
        {
            if (string.IsNullOrWhiteSpace(expresionInfija))
            {
                throw new ArgumentException("La expresión no puede estar vacía.", nameof(expresionInfija));
            }

            Queue<string> postfija = ConvertirInfijaAPostfija(Tokenizar(expresionInfija));
            Stack<NodoExpresion> pilaNodos = new Stack<NodoExpresion>();

            while (postfija.Count > 0)
            {
                string token = postfija.Dequeue();

                if (EsOperando(token))
                {
                    pilaNodos.Push(new NodoExpresion(token));
                    continue;
                }

                if (pilaNodos.Count < 2)
                {
                    throw new InvalidOperationException("Expresión inválida: faltan operandos para el operador '" + token + "'.");
                }

                NodoExpresion derecho = pilaNodos.Pop();
                NodoExpresion izquierdo = pilaNodos.Pop();
                NodoExpresion operador = new NodoExpresion(token)
                {
                    Izquierdo = izquierdo,
                    Derecho = derecho
                };

                pilaNodos.Push(operador);
            }

            if (pilaNodos.Count != 1)
            {
                throw new InvalidOperationException("Expresión inválida: el árbol no se pudo construir correctamente.");
            }

            Raiz = pilaNodos.Pop();
        }

        /// <summary>
        /// Obtiene la representación inorden totalmente parentizada.
        /// Es útil para evidenciar la jerarquía del árbol en reportes.
        /// </summary>
        public string ObtenerInordenParentizado()
        {
            if (Raiz == null)
            {
                return string.Empty;
            }

            return RecorrerInorden(Raiz);
        }

        public double Evaluar(IReadOnlyDictionary<string, double> valoresVariables)
        {
            if (Raiz == null)
            {
                throw new InvalidOperationException("Primero debe construirse el árbol de expresión.");
            }

            return EvaluarNodo(Raiz, valoresVariables ?? new Dictionary<string, double>());
        }

        public ResultadoEvaluacion Evaluar(Dictionary<string, double> valoresVariables = null)
        {
            ResultadoEvaluacion resultado = new ResultadoEvaluacion();

            if (Raiz == null)
            {
                resultado.Error = "El árbol aún no ha sido construido.";
                return resultado;
            }

            valoresVariables = valoresVariables ?? new Dictionary<string, double>();

            try
            {
                ValorSubexpresion valorRaiz = EvaluarNodo(Raiz, valoresVariables, resultado);
                resultado.Valor = valorRaiz.Valor;
                resultado.SePuedeEvaluar = true;
            }
            catch (InvalidOperationException ex)
            {
                resultado.Error = ex.Message;
                resultado.SePuedeEvaluar = false;
            }

            return resultado;
        }
        private static double EvaluarNodo(NodoExpresion nodo, IReadOnlyDictionary<string, double> valoresVariables)
        {
            if (nodo == null)
            {
                throw new InvalidOperationException("Nodo de expresión inválido.");
            }

            if (!nodo.EsOperador)
            {
                if (double.TryParse(nodo.Valor, NumberStyles.Float, CultureInfo.InvariantCulture, out double numero))
                {
                    return numero;
                }

                if (valoresVariables.TryGetValue(nodo.Valor, out double valorVariable))
                {
                    return valorVariable;
                }

                throw new InvalidOperationException("No se conoce el valor de la variable '" + nodo.Valor + "'.");
            }

            double izquierdo = EvaluarNodo(nodo.Izquierdo, valoresVariables);
            double derecho = EvaluarNodo(nodo.Derecho, valoresVariables);

            switch (nodo.Valor)
            {
                case "+":
                    return izquierdo + derecho;
                case "-":
                    return izquierdo - derecho;
                case "*":
                    return izquierdo * derecho;
                case "/":
                    if (derecho == 0)
                    {
                        throw new DivideByZeroException("División entre cero al evaluar la expresión.");
                    }
                    return izquierdo / derecho;
                default:
                    throw new InvalidOperationException("Operador no soportado: '" + nodo.Valor + "'.");
            }
        }

        private static ValorSubexpresion EvaluarNodo(NodoExpresion nodo, Dictionary<string, double> valoresVariables, ResultadoEvaluacion resultado)
        {
            if (nodo == null)
            {
                throw new InvalidOperationException("No se puede evaluar un nodo vacío.");
            }

            if (!nodo.EsOperador)
            {
                if (double.TryParse(nodo.Valor, NumberStyles.Float, CultureInfo.InvariantCulture, out double numero))
                {
                    return new ValorSubexpresion { Valor = numero, Expresion = nodo.Valor };
                }

                if (valoresVariables.TryGetValue(nodo.Valor, out double valorVariable))
                {
                    resultado.VariablesUsadas.Add(nodo.Valor + " = " + FormatearNumero(valorVariable));
                    return new ValorSubexpresion { Valor = valorVariable, Expresion = nodo.Valor };
                }

                throw new InvalidOperationException("No se conoce un valor numérico para el identificador '" + nodo.Valor + "'.");
            }

            ValorSubexpresion izquierdo = EvaluarNodo(nodo.Izquierdo, valoresVariables, resultado);
            ValorSubexpresion derecho = EvaluarNodo(nodo.Derecho, valoresVariables, resultado);
            double valor;

            switch (nodo.Valor)
            {
                case "+":
                    valor = izquierdo.Valor + derecho.Valor;
                    break;
                case "-":
                    valor = izquierdo.Valor - derecho.Valor;
                    break;
                case "*":
                    valor = izquierdo.Valor * derecho.Valor;
                    break;
                case "/":
                    if (Math.Abs(derecho.Valor) < double.Epsilon)
                    {
                        throw new InvalidOperationException("No se puede dividir entre cero en la subexpresión '" + derecho.Expresion + "'.");
                    }

                    valor = izquierdo.Valor / derecho.Valor;
                    break;
                default:
                    throw new InvalidOperationException("Operador no soportado para evaluación: '" + nodo.Valor + "'.");
            }

            string expresion = "(" + izquierdo.Expresion + " " + nodo.Valor + " " + derecho.Expresion + ")";
            resultado.Correspondencias.Add(expresion + " = " + FormatearNumero(valor));

            return new ValorSubexpresion { Valor = valor, Expresion = expresion };
        }

        private static string FormatearNumero(double valor)
        {
            return valor.ToString("0.######", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Genera una representación textual del árbol para mostrarla en la salida
        /// del analizador sin requerir controles gráficos adicionales.
        /// </summary>
        public string ObtenerArbolComoTexto()
        {
            if (Raiz == null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            ConstruirArbolComoTexto(Raiz, string.Empty, true, "Raíz", sb);
            return sb.ToString();
        }

        private static void ConstruirArbolComoTexto(NodoExpresion nodo, string prefijo, bool esUltimo, string etiqueta, StringBuilder sb)
        {
            if (nodo == null)
            {
                return;
            }

            sb.Append(prefijo)
              .Append(esUltimo ? "└── " : "├── ")
              .Append(etiqueta)
              .Append(": ")
              .AppendLine(nodo.Valor);

            string nuevoPrefijo = prefijo + (esUltimo ? "    " : "│   ");

            if (nodo.Izquierdo != null || nodo.Derecho != null)
            {
                ConstruirArbolComoTexto(nodo.Izquierdo, nuevoPrefijo, false, "Izq", sb);
                ConstruirArbolComoTexto(nodo.Derecho, nuevoPrefijo, true, "Der", sb);
            }
        }

        private static string RecorrerInorden(NodoExpresion nodo)
        {
            if (nodo == null)
            {
                return string.Empty;
            }

            if (!nodo.EsOperador)
            {
                return nodo.Valor;
            }

            return "(" + RecorrerInorden(nodo.Izquierdo) + " " + nodo.Valor + " " + RecorrerInorden(nodo.Derecho) + ")";
        }

        private static IEnumerable<string> Tokenizar(string expresion)
        {
            List<string> tokens = new List<string>();
            int i = 0;

            while (i < expresion.Length)
            {
                char c = expresion[i];

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (char.IsDigit(c) || c == '.')
                {
                    int inicio = i;
                    i++;
                    while (i < expresion.Length && (char.IsDigit(expresion[i]) || expresion[i] == '.'))
                    {
                        i++;
                    }
                    tokens.Add(expresion.Substring(inicio, i - inicio));
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    int inicio = i;
                    i++;
                    while (i < expresion.Length && (char.IsLetterOrDigit(expresion[i]) || expresion[i] == '_'))
                    {
                        i++;
                    }
                    tokens.Add(expresion.Substring(inicio, i - inicio));
                    continue;
                }

                if ("+-*/()".IndexOf(c) >= 0)
                {
                    tokens.Add(c.ToString());
                    i++;
                    continue;
                }

                throw new InvalidOperationException("Símbolo no soportado en la expresión: '" + c + "'.");
            }

            return tokens;
        }

        private static Queue<string> ConvertirInfijaAPostfija(IEnumerable<string> tokens)
        {
            Queue<string> salida = new Queue<string>();
            Stack<string> operadores = new Stack<string>();

            foreach (string token in tokens)
            {
                if (EsOperando(token))
                {
                    salida.Enqueue(token);
                    continue;
                }

                if (token == "(")
                {
                    operadores.Push(token);
                    continue;
                }

                if (token == ")")
                {
                    while (operadores.Count > 0 && operadores.Peek() != "(")
                    {
                        salida.Enqueue(operadores.Pop());
                    }

                    if (operadores.Count == 0 || operadores.Pop() != "(")
                    {
                        throw new InvalidOperationException("Paréntesis desbalanceados en la expresión.");
                    }

                    continue;
                }

                if (!EsTokenOperador(token))
                {
                    throw new InvalidOperationException("Token no reconocido en la expresión: '" + token + "'.");
                }

                while (operadores.Count > 0 && operadores.Peek() != "(" &&
                       Precedencia(operadores.Peek()) >= Precedencia(token))
                {
                    salida.Enqueue(operadores.Pop());
                }

                operadores.Push(token);
            }

            while (operadores.Count > 0)
            {
                string operador = operadores.Pop();
                if (operador == "(")
                {
                    throw new InvalidOperationException("Paréntesis desbalanceados en la expresión.");
                }

                salida.Enqueue(operador);
            }

            return salida;
        }

        private static int Precedencia(string operador)
        {
            return (operador == "*" || operador == "/") ? 2 : 1;
        }

        private static bool EsOperando(string token)
        {
            return EsNumero(token) || EsIdentificador(token);
        }

        private static bool EsNumero(string token)
        {
            return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }

        private static bool EsIdentificador(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            if (!char.IsLetter(token[0]) && token[0] != '_')
            {
                return false;
            }

            for (int i = 1; i < token.Length; i++)
            {
                if (!char.IsLetterOrDigit(token[i]) && token[i] != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool EsTokenOperador(string token)
        {
            return token == "+" || token == "-" || token == "*" || token == "/";
        }
    }
}