using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ImporterConfig))]
public class ImporterConfigEditor : Editor
{
    protected ImporterConfig m_Config;

    protected System.Reflection.PropertyInfo[] m_PropertyInfos;

    Editor[] m_Editors = null;
    AssetImporter[] m_Importers = null;
    bool[] m_EditorsDisplayed = null;

    System.Type m_TextureImporterInspectorType;
    System.Type m_ModelImporterInspectorType;

    string[] m_Tabs = new string[] { "TEXTURES", "MODELS" };
    int m_CurrentTabs = 0;

    private void OnEnable()
    {
        m_Config = target as ImporterConfig;

        m_TextureImporterInspectorType = System.Type.GetType("UnityEditor.TextureImporterInspector, UnityEditor");
        m_ModelImporterInspectorType = System.Type.GetType("UnityEditor.ModelImporterEditor, UnityEditor");

        m_PropertyInfos = typeof(TextureImporterSettings).GetProperties();
    }

    public override void OnInspectorGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        ImporterRule.RuleType type = (ImporterRule.RuleType)EditorGUILayout.EnumPopup("Add rule : ", ImporterRule.RuleType.NONE);
        if(type != ImporterRule.RuleType.NONE)
        {
            GetNewRulesFromType(type, m_Config);
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        m_CurrentTabs = GUILayout.Toolbar(m_CurrentTabs, m_Tabs);
        if (EditorGUI.EndChangeCheck())
            m_Editors = null;

        switch (m_CurrentTabs)
        {
            case 0:
                DisplayTextureImporterRule();
                break;
            case 1:
                DisplayModelImporter();
                break;
            default:
                break;
        }
    }

    bool DoHeader(string type)
    {
        bool delete = false;

        GUILayout.BeginHorizontal("box");
        GUILayout.Label(type);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Delete"))
        {
            delete = true;
        }
        GUILayout.EndHorizontal();

        return delete;
    }

    //======== DIFFERENT IMPORTERS

    void DisplayTextureImporterRule()
    {
        if (m_Editors == null || m_Editors.Length != m_Config.textureRules.Length)
        {
            m_Editors = new Editor[m_Config.textureRules.Length];
            m_Importers = new TextureImporter[m_Editors.Length];
            m_EditorsDisplayed = new bool[m_Editors.Length];

            for (int i = 0; i < m_Config.textureRules.Length; ++i)
            {
                m_Importers[i] = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(m_Config.textureRules[i].texture));
            }
        }

        for (int i = 0; i < m_Config.textureRules.Length; ++i)
        {
            GUILayout.BeginVertical("box");
            bool delete = DoHeader("TEXTURE");
            ImporterTextureRule rule = m_Config.textureRules[i];
       
            {
                EditorGUI.BeginChangeCheck();
                string newRule = EditorGUILayout.DelayedTextField("File filter", rule.nameFilter);
                if (EditorGUI.EndChangeCheck())
                {
                    rule.nameFilter = newRule;
                    if (newRule == "")
                    {
                        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(rule.texture), "DefaultRule.jpg");
                    }
                    else
                    {
                        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(rule.texture), rule.nameFilter + ".jpg");
                    }
                }

                m_EditorsDisplayed[i] = EditorGUILayout.Foldout(m_EditorsDisplayed[i], "Settings");
                if (m_EditorsDisplayed[i] && m_Importers[i] != null)
                {
                    Editor.CreateCachedEditor(m_Importers[i], m_TextureImporterInspectorType, ref m_Editors[i]);
                    Selection.activeObject = target;
                    EditorGUI.BeginChangeCheck();
                    m_Editors[i].OnInspectorGUI();
                    if (EditorGUI.EndChangeCheck())
                    {
                        //m_TextureImporterInspectorType.GetMethod("Apply", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(m_Editors[i], new object[] { });
                        //EditorUtility.SetDirty(rule.texture);
                    }
                }
            }

            GUILayout.EndVertical();

            if (delete)
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_Config.textureRules[i].texture));
                m_Config.textureRules[i] = null;
                ArrayUtility.RemoveAt(ref m_Config.textureRules, i);
                i--;
            }
        }

    }

    void DisplayModelImporter()
    {
        if (m_Editors == null || m_Editors.Length != m_Config.modelRules.Length)
        {
            m_Editors = new Editor[m_Config.modelRules.Length];
            m_Importers = new ModelImporter[m_Editors.Length];
            m_EditorsDisplayed = new bool[m_Editors.Length];

            for (int i = 0; i < m_Config.modelRules.Length; ++i)
            {
                m_Importers[i] = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(m_Config.modelRules[i].mesh));
            }
        }

        for (int i = 0; i < m_Config.modelRules.Length; ++i)
        {
            GUILayout.BeginVertical("box");
            bool delete = DoHeader("MODEL");
            ImporterModelRule rule = m_Config.modelRules[i];

            {
                EditorGUI.BeginChangeCheck();
                string newRule = EditorGUILayout.DelayedTextField("File filter", rule.nameFilter);
                if(EditorGUI.EndChangeCheck())
                {
                    rule.nameFilter = newRule;
                    if (newRule == "")
                    {
                        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(rule.mesh), "DefaultRule.asset");
                    }
                    else
                    {
                        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(rule.mesh), rule.nameFilter+".asset");
                    }
                }


                m_EditorsDisplayed[i] = EditorGUILayout.Foldout(m_EditorsDisplayed[i], "Settings");
                if (m_EditorsDisplayed[i] && m_Importers[i] != null)
                {
                    Editor.CreateCachedEditor(m_Importers[i], m_ModelImporterInspectorType, ref m_Editors[i]);
                    Selection.activeObject = target;
                    EditorGUI.BeginChangeCheck();
                    m_Editors[i].OnInspectorGUI();
                    if(EditorGUI.EndChangeCheck())
                    {
                        //EditorUtility.SetDirty(rule.mesh);
                    }
                }
            }

            GUILayout.EndVertical();

            if (delete)
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_Config.modelRules[i].mesh));
                m_Config.textureRules[i] = null;
                ArrayUtility.RemoveAt(ref m_Config.textureRules, i);
                i--;
            }
        }
    }

    //====================

    void GetNewRulesFromType(ImporterRule.RuleType ruleType, ImporterConfig config)
    {
        string configpath = AssetDatabase.GetAssetPath(target).Replace(".asset", "") + "_Configs";
        System.IO.Directory.CreateDirectory(configpath);

        switch (ruleType)
        {
            case ImporterRule.RuleType.NONE:
                break;
            case ImporterRule.RuleType.TEXTURE:
                {
                    ImporterTextureRule rule = new ImporterTextureRule();
                    rule.Init();
                    ArrayUtility.Add(ref config.textureRules, rule);

                    rule.texture = new Texture2D(32, 32, TextureFormat.RGB24, false);

                    string path = AssetDatabase.GenerateUniqueAssetPath(configpath + "/TextureConfig.jpg");
                    System.IO.File.WriteAllBytes(path, rule.texture.EncodeToPNG());
                    Object.DestroyImmediate(rule.texture);

                    AssetDatabase.ImportAsset(path);

                    rule.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                    EditorUtility.SetDirty(target);
                }
                break;
            case ImporterRule.RuleType.MODELS:
                {
                    ImporterModelRule rule = new ImporterModelRule();
                    rule.Init();
                    ArrayUtility.Add(ref config.modelRules, rule);

                    string path = AssetDatabase.GenerateUniqueAssetPath(configpath + "/MeshConfig.obj");
                    System.IO.File.WriteAllBytes(path, new byte[] { });

                    AssetDatabase.ImportAsset(path);

                    rule.mesh = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    EditorUtility.SetDirty(target);
                }
                break;
            default:
                break;
        }
    }
}
