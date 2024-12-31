using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramGameTest.Configuration
{
    public interface IConfig : IEnumerable<IConfig>
    {
        public string Value { get; }
        public IConfig this[string key] { get; }
        public IConfig this[int index] { get; }
    }
}
