using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippyWatson.Common
{
    public static class ComboHelpers
    {
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
