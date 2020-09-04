using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OvenWeb.Data
{
    public class OvenSettingsService
    {
        public OvenSettings OvenSettings { get; set; }
        private readonly string _jsonFile;

        public OvenSettingsService(IWebHostEnvironment env)
        {
            _jsonFile = System.IO.Path.Combine(env.ContentRootPath, "ovensettings.json");
            if (System.IO.File.Exists(_jsonFile))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(_jsonFile);
                    if (json.Length > 0)
                    {
                        OvenSettings = System.Text.Json.JsonSerializer.Deserialize<OvenSettings>(json);
                    }
                }
                catch (Exception)
                {
                    OvenSettings = new OvenSettings();
                }
            }
            else
                OvenSettings = new OvenSettings();
        }

        public void Save()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(OvenSettings);
            System.IO.File.WriteAllText(_jsonFile, json);
        }
    }
}
