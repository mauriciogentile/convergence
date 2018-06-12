using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Idb.Sec.Convergence.Daemon.IdbDocs;

namespace Idb.Sec.Convergence.Daemon
{
    public interface IDocumentStorage
    {
        Task<IEnumerable<Document>> SearchByCodeAsync(string code);
    }

    public class DocumentStorage : IDocumentStorage
    {
        private readonly string _dst;

        public DocumentStorage(string dst)
        {
            _dst = dst;
        }

        public async Task<IEnumerable<Document>> SearchByCodeAsync(string code)
        {
            using (var wsidbDocsSoapClient = new WSIDBDocsSoapClient())
            {
                var results =
                    await
                        wsidbDocsSoapClient.SearchAsync(_dst, "SEC_REG_NBR DOCNAME LANGUAGE URL DOCNUM",
                            string.Format("SEC_REG_NBR='{0}'", code), "");
                var docList =
                    results.FirstChild.ChildNodes.Cast<XmlNode>()
                        .Where<XmlNode>(node => node.Attributes != null)
                        .Select(x => x.Attributes)
                        .Select(attr =>
                        {
                            var lang = attr.GetNamedItem("LANGUAGE").Value;
                            return new Document
                            {
                                Id = attr.GetNamedItem("DOCNUM").Value,
                                Code = attr.GetNamedItem("SEC_REG_NBR").Value,
                                Name = attr.GetNamedItem("DOCNAME").Value,
                                Language = lang == "S" ? "SP" : (lang == "E" ? "EN" : (lang == "P" ? "PT" : "FR")),
                                Url = attr.GetNamedItem("URL").Value,
                            };
                        }).ToList();
                return docList;
            }
        }
    }
}