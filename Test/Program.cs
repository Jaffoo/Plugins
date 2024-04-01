using System.Reflection;
using System.Xml;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var file = File.Exists("E:\\git\\Plugins\\ChatGPT\\bin\\Release\\net8.0\\publish\\ChatGPT.dll");
            var dll = Assembly.LoadFrom("E:\\git\\Plugins\\ChatGPT\\bin\\Release\\net8.0\\publish\\ChatGPT.dll");
            Type[] types = dll.GetTypes();

            foreach (Type t in types)
            {
                Console.WriteLine(t.FullName);
            }
        }
    }
}
