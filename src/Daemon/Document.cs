using System.Collections.Generic;

namespace Idb.Sec.Convergence.Daemon
{
    public class Document
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Code { get; set; }
        public string Language { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
    }
}