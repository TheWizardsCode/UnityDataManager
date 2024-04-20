using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using WizardsCode.Test;

namespace RogueWave.csv
{
    public class CsvWindow : EditorWindow
    {
        private string relativeDataDirectoryPath = "Resources/CSV/Data";
        private Type dataType = typeof(TestScriptableObject);

        MonoScript script = null;
        UnityEngine.Object directoryObject = null;

        [MenuItem("Tools/Wizards Code/Data/CSV Import and Export")]
        public static void ShowWindow()
        {
            GetWindow<CsvWindow>("CSV Import/Export");
        }

        private void OnEnable()
        {
            relativeDataDirectoryPath = EditorPrefs.GetString("CsvWindow.relativeDataDirectoryPath");
            directoryObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>($"Assets/{relativeDataDirectoryPath}");

            string className = EditorPrefs.GetString("CsvWindow.dataType");
            Type type = null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(className);
                if (type != null)
                {
                    break;
                }
            }

            if (type != null)
            {
                string[] guids = AssetDatabase.FindAssets("t:MonoScript");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    MonoScript monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (monoScript.GetClass() == type)
                    {
                        script = monoScript;
                        break;
                    }
                }
            }
        }

        private void OnDisable()
        {
            EditorPrefs.SetString("CsvWindow.relativeDataDirectoryPath", relativeDataDirectoryPath);
            EditorPrefs.SetString("CsvWindow.dataType", dataType.FullName);
        }

        private void OnGUI()
        {
            GUILayout.Label("CSV Import and Export", EditorStyles.boldLabel);

            script = EditorGUILayout.ObjectField(new GUIContent("ScriptableObject class", "Select a script that defines a ScriptableObject subclass that will be serialized to the Excel sheet."), script, typeof(MonoScript), false) as MonoScript;

            if (script != null)
            {
                dataType = script.GetClass();
            }

            directoryObject = EditorGUILayout.ObjectField(new GUIContent("Data Folder", "Select a folder where you want to save the spreadsheet."), directoryObject, typeof(UnityEngine.Object), false);
            if (directoryObject != null)
            {
                string fullPath = AssetDatabase.GetAssetPath(directoryObject);
                relativeDataDirectoryPath = fullPath.Substring(fullPath.IndexOf("/Assets") + "/Assets".Length);
            }

            if (GUILayout.Button("Export Data to CSV"))
            {
                ExportDataToCSV();
            }

            if (GUILayout.Button("Import Data from CSV"))
            {
                ImportRecipeCSV();
            }
        }

        private string GetFilePath(string type, string fileName)
        {
            return $"Assets/{relativeDataDirectoryPath}/{type}/{fileName}.csv";
        }

        [MenuItem("Tools/Wizards Code/Data/Open CSV")]
        static void OpenCSV()
        {
            string path = EditorUtility.OpenFilePanel("Open CSV", Application.dataPath, "csv");
            if (path.Length != 0)
            {
                System.Diagnostics.Process.Start(path);
            }
        }

        [MenuItem("Tools/Wizards Code/Data/Export All Data to CSV", priority = 100)]
        void ExportDataToCSV()
        {
            List<ScriptableObject> dataObjects = Resources.LoadAll<ScriptableObject>("").ToList();
            List<Type> types = dataObjects.Select(r => r.GetType()).Distinct().ToList();

            foreach (Type type in types)
            {
                ScriptableObject[] objectsOfType = dataObjects.Where(r => r.GetType() == type).ToArray();

                if (dataObjects.Count > 0)
                {
                    WriteCSV(objectsOfType, dataType.Name, type.Name);
                }
            }
        }

        [MenuItem("Tools/Wizards Code/Data/Destructive/Import All Recipes from CSV", priority = 200)]
        void ImportRecipeCSV()
        {
            string[] csvFiles = Directory.GetFiles(Path.GetFullPath($"{Application.dataPath}/{relativeDataDirectoryPath}/{dataType.Name}"), "*.csv");

            foreach (string file in csvFiles)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                Assembly assembly = dataType.Assembly;
                MethodInfo method = typeof(CsvWindow).GetMethod("ImportFromCSV", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo generic = method.MakeGenericMethod(dataType);
                generic.Invoke(null, new object[] { dataType.Name, fileNameWithoutExtension });
            }
        }

        void ImportFromCSV<T>(string type, string fileName) where T : ScriptableObject
        {
            int count = 0;
            string path = GetFilePath(type, fileName);

            Debug.Log($"Starting import of {fileName} from {path}");

            string[] lines = File.ReadAllLines(path);

            bool isHeader = true;
            foreach (string line in lines)
            {
                string[] values = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

                if (isHeader)
                {
                    isHeader = false;
                }
                else
                {
                    count++;

                    bool isUpdating = true;
                    T recipe = AssetDatabase.LoadAssetAtPath<T>(values[2]);
                    if (recipe == null)
                    {
                        isUpdating = false;
                        recipe = ScriptableObject.CreateInstance<T>();
                    }

                    EditorUtility.SetDirty(recipe);

                    List<FieldInfo> fields = GetSerializeFields(recipe);
                    for (int i = 0; i < fields.Count; i++)
                    {
                        FieldInfo field = fields[i];
                        string value = values[i + 3];
                        if (field.FieldType == typeof(string))
                        {
                            field.SetValue(recipe, value.Trim('"'));
                        }
                        else
                        {
                            //Debug.Log("Attempting to set " + field.Name + " to " + value + " of type " + field.FieldType);
                            field.SetValue(recipe, Convert.ChangeType(value, field.FieldType));
                        }
                    }

                    if (isUpdating == false)
                    {
                        Debug.LogError($"Not yet saving newly created recipes: {recipe.name}");
                        count--;
                        // AssetDatabase.CreateAsset(recipe, $"Assets/_Dev/Resources/Recipes/{recipe.GetType().Name}.asset");
                    }
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Completed import of {count} {fileName} from {path}");
        }

        void WriteCSV(ScriptableObject[] dataObjects, string type, string fileName)
        {
            int count = 0;
            string path = GetFilePath(type, fileName);
            Debug.Log($"Starting export of {fileName} to {path}");

            string directoryPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            StringBuilder csvContent = new StringBuilder();

            csvContent.Append("Class,InstanceID, Path,");

            List<FieldInfo> fields = GetSerializeFields(dataObjects[0]);

            foreach (FieldInfo field in fields)
            {
                csvContent.Append($"{field.Name} - {field.FieldType}");
                csvContent.Append(",");
            }
            csvContent.AppendLine();

            foreach (ScriptableObject dataObject in dataObjects)
            {
                count++;
                csvContent.Append($"{dataObject.GetType()},{((ScriptableObject)dataObject).GetInstanceID()},{AssetDatabase.GetAssetPath((ScriptableObject)dataObject)},");
                foreach (FieldInfo field in fields)
                {
                    if (field.FieldType == typeof(string))
                    {
                        csvContent.Append($"\"{field.GetValue(dataObject)}\"");
                    }
                    else
                    {
                        csvContent.Append(field.GetValue(dataObject));
                    }
                    csvContent.Append(",");
                }
                csvContent.AppendLine();
            }

            File.WriteAllText(path, csvContent.ToString());

            AssetDatabase.Refresh();

            Debug.Log($"Completed export of {count} {fileName} to {path}");
        }

        List<FieldInfo> GetSerializeFields(ScriptableObject dataObject)
        {
            List<FieldInfo> serializedFields = new List<FieldInfo>();
            Type type = dataObject.GetType();

            while (type != null)
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                foreach (FieldInfo field in fields)
                {
                    if (Attribute.IsDefined(field, typeof(SerializeField)) && (field.FieldType.IsPrimitive || field.FieldType == typeof(string)))
                    {
                        serializedFields.Add(field);
                    }
                }

                if (type == typeof(ScriptableObject))
                {
                    break;
                }

                type = type.BaseType;
            }

            return serializedFields;
        }
    }
}