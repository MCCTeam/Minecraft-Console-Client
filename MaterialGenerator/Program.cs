using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DotNet.Globbing;

namespace MaterialGenerator
{
    internal class Program
    {
        public static void Main(string[] args)
        {            
            var data = File.ReadAllText("blocks.json");
            foreach (var t in Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.GetInterfaces().Contains(typeof(IRules)) && !x.IsAbstract))
            {
                var res = new Dictionary<int, Material>();


                var r = Activator.CreateInstance(t) as IRules;

                var globs = new Dictionary<Material, List<Glob>>();
                foreach (var rule in r.Rules())
                {
                    globs.Add(rule.Key, new List<Glob>());
                    foreach (var g in rule.Value)
                    {
                        globs[rule.Key].Add(Glob.Parse(g));
                    }
                }


                var rawData = r.GetFromFile(data);
                foreach (var d in rawData)
                {
                    var found = false;

                    foreach (var glob in globs)
                    {
                        if (found) break;

                        foreach (var g in glob.Value)
                        {
                            if (g.IsMatch(d.Key))
                            {
                                found = true;
                                foreach (var id in d.Value)
                                {
                                    res.Add(id, glob.Key);
                                }

                                break;
                            }
                        }
                    }

                    if (!found)
                    {
                        foreach (var id in d.Value)
                        {
                            res.Add(id, Material.Unknown);
                        }
                    }
                }

                var fName = r.GetType().Name + "_map.txt";
                if (File.Exists(fName))
                {
                    File.Delete(fName);
                }

                using (var fs = File.CreateText(fName))
                {
                    foreach (var map in res)
                    {
                        fs.WriteLine($"{{ {map.Key}, Material.{map.Value.ToString()} }},");
                    }
                }

                Console.WriteLine($"found {res.Count} for {r.GetType().Name}");
            }
        }
    }
}