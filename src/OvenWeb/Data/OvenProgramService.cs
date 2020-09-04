using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OvenWeb.Data
{
    public class OvenProgramService
    {
        public List<OvenProgram> Programs { get; set; }

        private readonly string _jsonFile;

        public OvenProgramService(IWebHostEnvironment env)
        {
            _jsonFile = System.IO.Path.Combine(env.ContentRootPath, "ovenprogram.json");
            if (System.IO.File.Exists(_jsonFile))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(_jsonFile);
                    if (json.Length > 0)
                    {
                        Programs = System.Text.Json.JsonSerializer.Deserialize<List<OvenProgram>>(json);
                    }
                }
                catch (Exception)
                {
                    Programs = new List<OvenProgram>();
                }
            }
            else
                Programs = new List<OvenProgram>();
        }

        public void Save()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(Programs);
            System.IO.File.WriteAllText(_jsonFile, json);
        }
    }
}
