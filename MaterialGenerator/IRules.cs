using System.Collections.Generic;
using System.IO;

namespace MaterialGenerator
{
    public interface IRules
    {
        Dictionary<Material, List<string>> Rules();

        Dictionary<string, List<int>> GetFromFile(string data);
    }
}