using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Practica1
{
    internal class AnalizadorExpresionesBinario
    {
        internal class NodoExpresion
        {
            public string Valor { get; }
            public NodoExpresion Izquierdo { get; set; }
            public NodoExpresion Derecho { get; set; }

            /// <summary>
            /// Indica si el nodo almacena un operador binario.
            /// </summary>
            public bool EsOperador => Valor == "+" || Valor == "-" || Valor == "*" || Valor == "/";

            public NodoExpresion(string valor)
            {
                Valor = valor;
            }
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

                if (EsNumero(token))
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
                if (EsNumero(token))
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

        private static bool EsNumero(string token)
        {
            return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }
    }
}
    

