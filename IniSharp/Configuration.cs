﻿using System;
using System.Collections.Generic;

namespace IniSharp
{
    public class Configuration : IConfiguration
    {
        private readonly IParser _parser;

        /// <summary>
        /// Create's an Configuration instance of an ini file.
        /// </summary>
        /// <param name="iniPath">Path to .ini file</param>
        /// <param name="useInMemory">Default: ON, only set false if your file is large and having trouble loading it.</param>
        public Configuration(string iniPath, bool useInMemory = true)
        {
            _parser = new Parser(iniPath, useInMemory);
        }

        public string GetValue(string key)
        {
            var parts = key.Split(':');
            if (parts.Length != 2) return null;

            return _parser.GetValue(parts[0], parts[1]);
        }

        public string this[string key]
        {
            get
            {
                var segments = key.Split(':');
                if (segments.Length != 2)
                {
                    throw new ArgumentException("Key must be in the format 'Section:Key'.", nameof(key));
                }
                var section = segments[0];
                var keyValue = segments[1];

                return _parser.GetValue(section, keyValue);
            }
            set
            {
                SetValue(key, value);
            }
        }

        public void SaveChanges()
        {
            _parser.SaveAllChanges();
        }

        public void SetValue(string key, string value)
        {
            var segments = key.Split(':');
            if (segments.Length != 2)
            {
                throw new ArgumentException("Key must be in the format 'Section:Key'.", nameof(key));
            }

            var section = segments[0];
            var keyValue = segments[1];

            _parser.SetValue(section, keyValue, value);
        }

        public IDictionary<string, string> GetSection(string sectionName)
        {
            return _parser.GetSection(sectionName);
        }

        public T GetSection<T>(string sectionName) where T : new()
        {
            var sectionData = GetSection(sectionName);
            var result = new T();

            foreach (var property in typeof(T).GetProperties())
            {
                if (sectionData.ContainsKey(property.Name))
                {
                    property.SetValue(result, Convert.ChangeType(sectionData[property.Name], property.PropertyType));
                }
            }

            return result;
        }
    }
}
