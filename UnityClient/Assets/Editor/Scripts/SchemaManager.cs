using UnityEngine;
using UnityEditor;
using System.IO;

public class SchemaManager
{
    [MenuItem("Tools/Update Schemas")]
    public static void UpdateSchemas()
    {
        string aqFolder = "C:\\proj\\VBL\\vbl-aquarium\\models\\csharp\\";
        string unityFolder = "Packages/vbl.urchin/Scripts/JSON/";

        string[] files = { "UrchinModels.cs", "GenericModels.cs", "DockModels.cs", "LoggingModels.cs" };

        foreach (string file in files)
        {
            File.Copy(Path.Combine(aqFolder, file),
                Path.Combine(unityFolder, file), true);
        }

        Debug.Log("Schemas updated successfully!");
        AssetDatabase.Refresh();
    }
}
