using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Patapon4GameServer.Core.Assets
{
    public struct EntityAssetInfo
    {
        public string Id;

        public Dictionary<int, AssetDependency> Dependencies;

        public static List<EntityAssetInfo> ListAll(string scriptLoader)
        {
            List<EntityAssetInfo> toReturn = new List<EntityAssetInfo>();

            var xml = new XmlDocument();
            xml.LoadXml(scriptLoader);

            foreach (XmlElement node in xml.GetElementsByTagName("Entity"))
            {
                toReturn.Add(new EntityAssetInfo()
                {
                    Id = node.GetAttribute("id"),
                    Dependencies = new Func<Dictionary<int, AssetDependency>>(() =>
                    {
                        var dependencies = new Dictionary<int, AssetDependency>();
                        foreach (XmlElement dependency in node.GetElementsByTagName("Load"))
                        {
                            dependencies.Add(dependencies.Count, new AssetDependency() { type = dependency.GetAttribute("type"), path = dependency.InnerText });
                        }
                        return dependencies;
                    })()
                });
            }

            return toReturn;
        }
    }
}
