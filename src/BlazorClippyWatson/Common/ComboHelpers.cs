using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippyWatson.Common
{
    public static class ComboHelpers
    {
        /// <summary>
        /// https://stackoverflow.com/questions/1952153/what-is-the-best-way-to-find-all-combinations-of-items-in-an-array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> GetPermutationsWithRept<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new T[] { t });
            return GetPermutationsWithRept(list, length - 1)
                .SelectMany(t => list,
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }

        public static IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> list, int length)
        {
            if (length == 1) return list.Select(t => new T[] { t });
            return GetPermutations(list, length - 1)
                .SelectMany(t => list.Where(o => !t.Contains(o)),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }
        public static IEnumerable<IEnumerable<T>> GetKCombsWithRept<T>(IEnumerable<T> list, int length) where T : IComparable
        {
            if (length == 1) return list.Select(t => new T[] { t });
            return GetKCombsWithRept(list, length - 1)
                .SelectMany(t => list.Where(o => o.CompareTo(t.Last()) >= 0),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }
        public static IEnumerable<IEnumerable<T>> GetKCombs<T>(IEnumerable<T> list, int length) where T : IComparable
        {
            if (length == 1) return list.Select(t => new T[] { t });
            return GetKCombs(list, length - 1)
                .SelectMany(t => list.Where(o => o.CompareTo(t.Last()) > 0),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }

        /// <summary>
        /// https://stackoverflow.com/questions/32571057/generate-all-combinations-from-multiple-n-lists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static List<List<T>> GetAllPossibleCombos<T>(List<List<T>> objects)
        {
            IEnumerable<List<T>> combos = new List<List<T>>() { new List<T>() };

            foreach (var inner in objects)
            {
                combos = combos.SelectMany(r => inner
                .Select(x => {
                    var n = r.DeepClone();
                    if (x != null)
                    {
                        n.Add(x);
                    }
                    return n;
                }).ToList());
            }

            // Remove combinations were all items are empty
            return combos.Where(c => c.Count > 0).ToList();
        }

        public static List<List<T>> GetAllPossibleCombosOptimized<T>(List<List<T>> objects)
        {
            IEnumerable<List<T>> combos = new List<List<T>>() { new List<T>() };

            foreach (var inner in objects)
            {
                combos = combos.SelectMany(r => inner
                .Select(x => {
                    var n = r.DeepClone();
                    if (x != null)
                    {
                        n.Add(x);
                    }
                    return n;
                }).ToList());
            }
                        
            // Remove combinations were all items are empty
            var list = new ConcurrentQueue<List<T>>();
            Parallel.ForEach(combos, inner =>
            {
                if (inner.Count > 0)
                    list.Enqueue(inner);
            });

            var result = new List<List<T>>();
            while (list.TryDequeue(out var item))
                result.Add(item);

            return result;
        }

        public static List<List<string>> GetAllPossibleCombosOptimizedNotGeneric(List<List<string>> objects)
        {
            IEnumerable<List<string>> combos = new List<List<string>>() { new List<string>() };

            foreach (var inner in objects)
            {
                combos = combos.SelectMany(r => inner
                .Select(x => {
                    var n = new List<string>(r);
                    if (x != null)
                    {
                        n.Add(x);
                    }
                    return n.ToList();//n.OrderBy(o => o).ToList();
                }).ToList());
            }

            // Remove combinations were all items are empty
            var list = new ConcurrentQueue<List<string>>();
            Parallel.ForEach(combos, inner =>
            {
                if (inner.Count > 0)
                    list.Enqueue(inner);
            });

            var result = new List<List<string>>();
            while (list.TryDequeue(out var item))
                result.Add(item);

            return result.ToList();
        }

        public static T DeepClone<T>(this T source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source), deserializeSettings);

        }
    }
}
