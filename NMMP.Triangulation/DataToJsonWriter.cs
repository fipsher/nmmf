using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NMMP.Triangulation
{
    public static class DataToJsonWriter
    {
        public static void Write<T>(List<T> data, string path)
        {

            var json = JsonConvert.SerializeObject(data);

            //write string to file
            System.IO.File.WriteAllText(path, json);
        }

        public static void WriteOne<T>(T data, string path)
        {
            var json = JsonConvert.SerializeObject(data);

            //write string to file
            System.IO.File.WriteAllText(path, json);
        }
    }
}
