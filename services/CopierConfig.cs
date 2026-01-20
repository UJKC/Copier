using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class CopierConfig
{
    public string Password { get; set; } = "";
}

public static class CopierConfigService
{
    public static CopierConfig Load(string path)
    {
        var yaml = File.ReadAllText(path);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<CopierConfig>(yaml);
    }

    public static void Save(string path, CopierConfig config)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var yaml = serializer.Serialize(config);
        File.WriteAllText(path, yaml);
    }
}
