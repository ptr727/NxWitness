using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CreateMatrix;

public class MatrixJsonSchema
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
        NullValueHandling = NullValueHandling.Ignore,
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };

    [Required] public List<ImageInfo> Images { get; set; } = new();

    public static void ToFile(string path, MatrixJsonSchema jsonSchema)
    {
        File.WriteAllText(path, ToJson(jsonSchema));
    }

    private static string ToJson(MatrixJsonSchema jsonSchema)
    {
        return JsonConvert.SerializeObject(jsonSchema, Settings);
    }
}