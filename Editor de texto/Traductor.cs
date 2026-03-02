using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Editor_de_texto
{
    internal class Traductor
    {
        private Dictionary<string, string> traducciones;
        //Constructor donde se inicializa el diccionario
        public Traductor()
        {
            //Se crea el diccionario
            traducciones = new Dictionary<string, string>()
            {
                { "auto", "automatico" },
                { "break", "romper" },
                { "case", "caso" },
                { "char", "caracter" },
                { "const", "constante" },
                { "continue", "continuar" },
                { "default", "defecto" },
                { "do", "hacer" },
                { "double", "doble" },
                { "else", "sino" },
                { "enum", "enumeracion" },
                { "extern", "externo" },
                { "float", "decimal" },
                { "for", "para" },
                { "goto", "irA" },
                { "if", "si" },
                { "inline", "enLinea" },
                { "int", "entero" },
                { "long", "largo" },
                { "register", "registro" },
                { "restrict", "restringido" },
                { "return", "retornar" },
                { "short", "corto" },
                { "signed", "conSigno" },
                { "sizeof", "tamano" },
                { "static", "estatico" },
                { "struct", "estructura" },
                { "switch", "seleccion" },
                { "typedef", "definirTipo" },
                { "union", "union" },
                { "unsigned", "sinSigno" },
                { "void", "vacio" },
                { "volatile", "volatil" },
                { "while", "mientras" },
                { "_Bool", "booleano" },
                { "_Complex", "complejo" },
                { "_Imaginary", "imaginario" },
                { "_Alignas", "alinearComo" },
                { "_Alignof", "alineacionDe" },
                { "_Atomic", "atomico" },
                { "_Generic", "generico" },
                { "_Noreturn", "sinRetorno" },
                { "_Static_assert", "aseveracionEstatica" },
                { "_Thread_local", "localDeHilo" },
                {"include", "incluye" }
            };
        }
        //Función que traduce el código 
        public string TraducirCodigo(string CodigoOriginal)
        {
            string CodigoTraducido = CodigoOriginal; //Se copia el código original
            foreach(var kvp in traducciones) //Recorre cada par clave-valor en el diccionario
            {
                //Usamos Regex para buscar la palabra exacta y remplazarla
                CodigoTraducido = Regex.Replace(
                    CodigoTraducido, $@"\b{kvp.Key}\b", //\b se asegura qu sea una palabra completa
                    kvp.Value); //Se remplaza por el significado en español
            }
            return CodigoTraducido; //Se devuelve el código traducido
        }
    }
}
