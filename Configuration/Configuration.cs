using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramGameTest.Configuration
{
    public class Configuration : IConfig
    {
        private Dictionary<string, IConfig> _config;

        private List<IConfig> _configList;

        public IConfig this[string key] => _config[key];
        public IConfig this[int index] => _configList[index];

        public string Value { get; private set; }

        public Configuration() { }


        private Configuration(string json, bool isStr)
        {
            Value = json;
            if (!isStr)
            {
                _config = new Dictionary<string, IConfig>();
                _configList = new List<IConfig>();
                if (json[0] == '[')
                {
                    for (int i = 1; i < json.Length; i++)
                    {
                        var value = ReadBetweenBrackets(json, ref i, out isStr);
                        _configList.Add(new Configuration(value, isStr));
                    }
                }
                else
                {
                    for (int i = 1; i < json.Length; i++)
                    {
                        var key = ReadBetweenBrackets(json, ref i, out isStr);
                        i++;
                        var value = ReadBetweenBrackets(json, ref i, out isStr);
                        _config[key.Replace("\"", "")] = new Configuration(value, isStr);
                    }
                    _configList.Add(this);
                }
            }
            else
            {
                Value = json.Replace("\"", "");
            }
        }

        private string ReadBetweenBrackets(string value, ref int index, out bool isString)
        {
            Dictionary<char, char> brackets = new Dictionary<char, char>()
            {
                { '\"', '\"'},
                {'}', '{' },
                {']', '[' }
            };
            Stack<char> stack = new Stack<char>();
            isString = value[index] == '\"';
            stack.Push(value[index]);
            int i = index + 1;
            for (; stack.Count > 0; i++)
            {
                switch (value[i])
                {
                    case ']':
                    case '}':
                        if (stack.Pop() != brackets[value[i]])
                            throw new InvalidOperationException();
                        break;
                    case '[':
                    case '{':
                        stack.Push(value[i]);
                        break;
                    case '\"':
                    case '\'':
                        if (stack.Peek() == brackets[value[i]])
                            stack.Pop();
                        else
                            stack.Push(value[i]);
                        break;
                }
            }
            var res = value.Substring(index, i - index);
            index = i;
            return res;
        }


        public static Configuration ReadFromFile(string path)
        {
            using (var reader = new StreamReader(new FileStream(path, FileMode.Open)))
            {
                return new Configuration(reader.ReadToEnd().Replace("\r\n", "").Replace(" ", ""), false);
            }
        }

        public IEnumerator<IConfig> GetEnumerator()
        {
            return _configList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _configList.GetEnumerator();
        }
    }
}
