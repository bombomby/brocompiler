using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace BroCollector
{
    public class Serializer
    {
        public static void Save<T>(Stream stream, T obj)
        {
            DataContractSerializer formatter = new DataContractSerializer(typeof(T));
            using (var writer = XmlDictionaryWriter.CreateBinaryWriter(stream))
            {
                formatter.WriteObject(writer, obj);
            }
        }

        public static T Load<T>(Stream stream)  where T : class
        {
            DataContractSerializer formatter = new DataContractSerializer(typeof(T));
            using (var reader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
            {
                return formatter.ReadObject(reader) as T;
            }
            return null;
        }
    }
}
