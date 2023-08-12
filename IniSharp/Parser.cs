﻿using System.Collections.Generic;
using System.IO;

namespace IniSharp
{
    internal class Parser : IParser
    {
        private IDictionary<string, Dictionary<string, string>> _sections;
        private readonly string _configFilePath;
        private bool _changesPending;

        public Parser(string configFile, bool useInMemory)
        {
            _configFilePath = configFile;
            _sections = new Dictionary<string, Dictionary<string, string>>();

            if (useInMemory)
            {
                ParseInMemory(configFile);
            }
            else
            {
                ParseFile(configFile);
            }
        }

        private void ParseInMemory(string configFile)
        {
            var lines = File.ReadAllLines(configFile);
            Dictionary<string, string> currentSection = null;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                if (IsCommentOrEmpty(line)) continue;

                if (IsSectionHeader(line))
                {
                    currentSection = HandleOutsideSectionState(line, i + 1);
                }
                else if (currentSection != null)
                {
                    HandleInsideSectionState(line, currentSection, i + 1);
                }
                else
                {
                    throw new MissingSectionHeaderException(i + 1);
                }
            }
        }

        private void ParseFile(string filePath)
        {
            using (var stream = new StreamReader(filePath))
            {
                int lineNumber = 0;
                ParserState state = ParserState.OutsideSection;
                Dictionary<string, string> currentSection = null;

                while (!stream.EndOfStream)
                {
                    var line = stream.ReadLine().Trim();
                    lineNumber++;

                    if (IsCommentOrEmpty(line)) continue;

                    switch (state)
                    {
                        case ParserState.OutsideSection:
                            currentSection = HandleOutsideSectionState(line, lineNumber);
                            if (currentSection != null) state = ParserState.InsideSection;
                            break;

                        case ParserState.InsideSection:
                            if (IsSectionHeader(line))
                            {
                                currentSection = HandleOutsideSectionState(line, lineNumber);
                            }
                            else
                            {
                                HandleInsideSectionState(line, currentSection, lineNumber);
                            }
                            break;
                    }
                }
            }
        }

        private Dictionary<string, string> HandleOutsideSectionState(string line, int lineNumber)
        {
            if (IsSectionHeader(line))
            {
                var sectionName = line.Substring(1, line.Length - 2).Trim();
                var newSection = new Dictionary<string, string>();
                _sections[sectionName] = newSection;
                return newSection;
            }
            else
            {
                throw new IniParseException(lineNumber, "Expected a section header.");
            }
        }

        public void Save()
        {
            using (StreamWriter writer = new StreamWriter(_configFilePath))
            {
                foreach (var section in _sections)
                {
                    writer.WriteLine($"[{section.Key}]");
                    foreach (var kvp in section.Value)
                    {
                        writer.WriteLine($"{kvp.Key}={kvp.Value}");
                    }
                    writer.WriteLine();  // Add an empty line for better readability
                }
            }
            _changesPending = false;
        }

        private void HandleInsideSectionState(string line, Dictionary<string, string> currentSection, int lineNumber)
        {
            var equalIndex = line.IndexOf('=');
            if (equalIndex <= 0 || equalIndex == line.Length - 1)
            {
                throw new IniParseException(lineNumber, "Invalid key-value pair format.");
            }

            var key = line.Substring(0, equalIndex).Trim();
            var value = line.Substring(equalIndex + 1).Trim();
            currentSection[key] = value;
        }

        public string GetValue(string section, string key)
        {
            if (_sections.TryGetValue(section, out var sectionDict) && sectionDict.TryGetValue(key, out var value))
                return value;

            return null;
        }

        public IDictionary<string, string> GetSection(string section)
        {
            return _sections.TryGetValue(section, out var sectionDict)
                ? new Dictionary<string, string>(sectionDict)
                : null;
        }

        public void SaveAllChanges()
        {
            if (_changesPending)
            {
                Save();
                _changesPending = false;
            }
        }

        public void SetValue(string section, string key, string value)
        {
            if (!_sections.TryGetValue(section, out var sectionDict))
            {
                sectionDict = new Dictionary<string, string>();
                _sections[section] = sectionDict;
            }

            sectionDict[key] = value;
            _changesPending = true;
        }

        private bool IsCommentOrEmpty(string line) => string.IsNullOrEmpty(line) || line.StartsWith(";");
        private bool IsSectionHeader(string line) => line.StartsWith("[") && line.EndsWith("]");
    }
}
