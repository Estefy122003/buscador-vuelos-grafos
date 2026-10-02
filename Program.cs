/*
 * Guía de Prácticas #04 - Árboles y grafos
 * Problema: encontrar vuelos baratos a partir de una base de datos ficticia.
 *
 * Estructura utilizada: grafo ponderado dirigido (lista de adyacencia).
 * Algoritmo de búsqueda: Dijkstra con cola de prioridad (PriorityQueue).
 * Requiere .NET 6 o superior.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
 
namespace VuelosBaratos
{
    /// <summary>Arista del grafo: un vuelo directo entre dos aeropuertos.</summary>
    public class Vuelo
    {
        public string Origen { get; }
        public string Destino { get; }
        public double Precio { get; }
        public int Duracion { get; }
        public string Aerolinea { get; }
 
        public Vuelo(string origen, string destino, double precio, int duracion, string aerolinea)
        {
            Origen = origen;
            Destino = destino;
            Precio = precio;
            Duracion = duracion;
            Aerolinea = aerolinea;
        }
    }
 
    public enum Criterio { Precio, Duracion }
 
    /// <summary>Grafo dirigido y ponderado representado con lista de adyacencia.</summary>
    public class GrafoVuelos
    {
        // aeropuerto -> lista de vuelos de salida
        public Dictionary<string, List<Vuelo>> Adyacencia { get; } = new Dictionary<string, List<Vuelo>>();
 
        public void AgregarVuelo(Vuelo vuelo)
        {
            if (!Adyacencia.ContainsKey(vuelo.Origen))
                Adyacencia[vuelo.Origen] = new List<Vuelo>();
            if (!Adyacencia.ContainsKey(vuelo.Destino))
                Adyacencia[vuelo.Destino] = new List<Vuelo>(); // asegura el nodo destino
            Adyacencia[vuelo.Origen].Add(vuelo);
        }
 
        public List<string> Aeropuertos =>
            Adyacencia.Keys.OrderBy(a => a, StringComparer.Ordinal).ToList();
 
        public List<Vuelo> Vuelos =>
            Adyacencia.Values.SelectMany(lista => lista).ToList();
 
        // ------------------------------------------------------------------
        // Carga de la base de datos ficticia (archivo de texto)
        // ------------------------------------------------------------------
        public static GrafoVuelos DesdeArchivo(string ruta)
        {
            var grafo = new GrafoVuelos();
            foreach (var texto in File.ReadLines(ruta))
            {
                string linea = texto.Trim();
                if (linea.Length == 0 || linea.StartsWith("#"))
                    continue;
                string[] c = linea.Split(',');
                grafo.AgregarVuelo(new Vuelo(
                    c[0].Trim(),
                    c[1].Trim(),
                    double.Parse(c[2], CultureInfo.InvariantCulture),
                    int.Parse(c[3], CultureInfo.InvariantCulture),
                    c[4].Trim()));
            }
            return grafo;
        }
 
        private static double Peso(Vuelo v, Criterio criterio) =>
            criterio == Criterio.Precio ? v.Precio : v.Duracion;
 
        // ------------------------------------------------------------------
        // Dijkstra: ruta más barata (Criterio.Precio) o más corta (Criterio.Duracion)
        // ------------------------------------------------------------------
        public (double Total, List<Vuelo> Tramos)? RutaOptima(string origen, string destino,
                                                              Criterio criterio = Criterio.Precio)
        {
            if (!Adyacencia.ContainsKey(origen) || !Adyacencia.ContainsKey(destino))
                return null;
 
            var costo = new Dictionary<string, double>();
            foreach (var a in Adyacencia.Keys)
                costo[a] = double.PositiveInfinity;
            var previo = new Dictionary<string, Vuelo>();
 
            costo[origen] = 0;
            var cola = new PriorityQueue<string, double>();
            cola.Enqueue(origen, 0);
 
            while (cola.TryDequeue(out string? nodo, out double actual))
            {
                if (actual > costo[nodo]) continue;   // entrada obsoleta
                if (nodo == destino) break;
 
                foreach (var vuelo in Adyacencia[nodo])
                {
                    double nuevo = actual + Peso(vuelo, criterio);
                    if (nuevo < costo[vuelo.Destino])
                    {
                        costo[vuelo.Destino] = nuevo;
                        previo[vuelo.Destino] = vuelo;
                        cola.Enqueue(vuelo.Destino, nuevo);
                    }
                }
            }
 
            if (double.IsPositiveInfinity(costo[destino]))
                return null;
 
            // Reconstrucción del itinerario desde el destino hacia el origen
            var tramos = new List<Vuelo>();
            string actualNodo = destino;
            while (actualNodo != origen)
            {
                Vuelo v = previo[actualNodo];
                tramos.Add(v);
                actualNodo = v.Origen;
            }
            tramos.Reverse();
            return (costo[destino], tramos);
        }
 
        // ------------------------------------------------------------------
        // Recorrido BFS: aeropuertos alcanzables y número mínimo de vuelos
        // ------------------------------------------------------------------
        public Dictionary<string, int> EscalasMinimas(string origen)
        {
            var niveles = new Dictionary<string, int> { [origen] = 0 };
            var cola = new Queue<string>();
            cola.Enqueue(origen);
            while (cola.Count > 0)
            {
                string nodo = cola.Dequeue();
                foreach (var v in Adyacencia[nodo])
                {
                    if (!niveles.ContainsKey(v.Destino))
                    {
                        niveles[v.Destino] = niveles[nodo] + 1;
                        cola.Enqueue(v.Destino);
                    }
                }
            }
            return niveles;
        }
    }
 
    public static class Program
    {
        // ------------------------------------------------------------------
        // Reportería
        // ------------------------------------------------------------------
        static void ReporteAeropuertos(GrafoVuelos grafo)
        {
            Console.WriteLine("\n=== REPORTE 1: Aeropuertos y vuelos directos de salida ===");
            foreach (var a in grafo.Aeropuertos)
            {
                var destinos = string.Join(", ",
                    grafo.Adyacencia[a].Select(v => $"{v.Destino}(${v.Precio:F0})"));
                Console.WriteLine($"{a}: {(destinos.Length > 0 ? destinos : "-")}");
            }
        }
 
        static void ReporteVuelos(GrafoVuelos grafo)
        {
            Console.WriteLine("\n=== REPORTE 2: Todos los vuelos registrados ===");
            Console.WriteLine($"{"Origen",-7}{"Destino",-9}{"Precio",8}{"Min",6}  Aerolínea");
            var ordenados = grafo.Vuelos
                .OrderBy(v => v.Origen, StringComparer.Ordinal)
                .ThenBy(v => v.Destino, StringComparer.Ordinal);
            foreach (var v in ordenados)
                Console.WriteLine($"{v.Origen,-7}{v.Destino,-9}{v.Precio,8:F2}{v.Duracion,6}  {v.Aerolinea}");
            Console.WriteLine($"Total: {grafo.Vuelos.Count} vuelos, {grafo.Aeropuertos.Count} aeropuertos");
        }
 
        static void ReporteRuta(GrafoVuelos grafo, string origen, string destino,
                                Criterio criterio = Criterio.Precio)
        {
            var sw = Stopwatch.StartNew();
            var resultado = grafo.RutaOptima(origen, destino, criterio);
            sw.Stop();
 
            string etiqueta = criterio == Criterio.Precio ? "MÁS BARATA" : "MÁS RÁPIDA";
            string nombre = criterio == Criterio.Precio ? "precio" : "duracion";
            Console.WriteLine($"\n=== CONSULTA: ruta {etiqueta} {origen} -> {destino} ===");
 
            if (resultado == null)
            {
                Console.WriteLine("No existe ruta entre esos aeropuertos.");
                return;
            }
            var (total, tramos) = resultado.Value;
            foreach (var v in tramos)
                Console.WriteLine($"  {v.Origen} -> {v.Destino}  ${v.Precio:F2}  {v.Duracion} min  ({v.Aerolinea})");
            string unidad = criterio == Criterio.Precio ? "USD" : "min";
            Console.WriteLine($"  Total {nombre}: {total:F2} {unidad} | Escalas: {tramos.Count - 1}");
            Console.WriteLine($"  Tiempo de ejecución: {sw.Elapsed.TotalMilliseconds * 1000:F1} microsegundos");
        }
 
        static void ReporteEscalas(GrafoVuelos grafo, string origen)
        {
            Console.WriteLine($"\n=== REPORTE 3: Escalas mínimas desde {origen} (BFS) ===");
            var ordenado = grafo.EscalasMinimas(origen)
                .OrderBy(par => par.Value)
                .ThenBy(par => par.Key, StringComparer.Ordinal);
            foreach (var par in ordenado)
                Console.WriteLine($"  {par.Key}: {par.Value} vuelo(s)");
        }
 
        // ------------------------------------------------------------------
        // Análisis del tiempo de ejecución con grafos de distinto tamaño
        // ------------------------------------------------------------------
        static GrafoVuelos GrafoAleatorio(int n, int grado = 5, int semilla = 7)
        {
            var rnd = new Random(semilla);
            var g = new GrafoVuelos();
            for (int i = 0; i < n; i++)
            {
                for (int k = 0; k < grado; k++)
                {
                    int j = rnd.Next(n);
                    if (j != i)
                        g.AgregarVuelo(new Vuelo($"A{i}", $"A{j}", rnd.Next(30, 601), rnd.Next(40, 701), "Sim"));
                }
            }
            return g;
        }
 
        static void AnalisisTiempos()
        {
            Console.WriteLine("\n=== ANÁLISIS DE TIEMPO DE EJECUCIÓN (Dijkstra, promedio de 50 pares origen-destino) ===");
            Console.WriteLine($"{"Aeropuertos",12}{"Vuelos",9}{"Tiempo (ms)",14}");
 
            // Calentamiento: evita medir la compilación JIT en la primera llamada
            GrafoAleatorio(100).RutaOptima("A0", "A1");
 
            foreach (int n in new[] { 100, 500, 1000, 2000, 5000 })
            {
                var g = GrafoAleatorio(n);
                int numVuelos = g.Vuelos.Count;
                var rnd = new Random(11);
                double sumaMs = 0;
                for (int i = 0; i < 50; i++)
                {
                    string o = $"A{rnd.Next(n)}";
                    string d = $"A{rnd.Next(n)}";
                    var sw = Stopwatch.StartNew();
                    g.RutaOptima(o, d);
                    sw.Stop();
                    sumaMs += sw.Elapsed.TotalMilliseconds;
                }
                Console.WriteLine($"{n,12}{numVuelos,9}{sumaMs / 50,14:F3}");
            }
        }
 
        public static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
 
            string ruta = args.Length > 0 ? args[0] : "vuelos.txt";
            var sw = Stopwatch.StartNew();
            var grafo = GrafoVuelos.DesdeArchivo(ruta);
            sw.Stop();
            Console.WriteLine($"Base de datos cargada desde '{ruta}' en {sw.Elapsed.TotalMilliseconds:F3} ms");
 
            ReporteAeropuertos(grafo);
            ReporteVuelos(grafo);
            ReporteRuta(grafo, "GPS", "MAD", Criterio.Precio);
            ReporteRuta(grafo, "GPS", "MAD", Criterio.Duracion);
            ReporteRuta(grafo, "GPS", "LIM", Criterio.Precio);
            ReporteRuta(grafo, "CUE", "MIA", Criterio.Precio);
            ReporteEscalas(grafo, "UIO");
            AnalisisTiempos();
        }
    }
}
 
 
