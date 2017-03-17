using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

[CreateAssetMenu(fileName = "ImporterConfig", menuName = "Importer/Config")]
public class ImporterConfig : ScriptableObject
{
    //All textures import rules (need to differentiate rules in different as ScriptableObject
    //can't serialize polymorphism
    public ImporterTextureRule[] textureRules = new ImporterTextureRule[0];
    public ImporterModelRule[] modelRules = new ImporterModelRule[0];
}


[System.Serializable]
public class ImporterRule
{
    public enum RuleType
    {
        NONE,
        TEXTURE,
        MODELS
    }

    public RuleType ruleType;
    public string nameFilter = "";

    public virtual void Init()
    {
        ruleType = RuleType.NONE;
    }
}

[System.Serializable]
public class ImporterTextureRule : ImporterRule
{
    public Texture2D texture;
    public TextureImporterSettings settings;

    public override void Init()
    {
        ruleType = RuleType.TEXTURE;
        settings = new TextureImporterSettings();
        settings.ApplyTextureType(TextureImporterType.Default);
    }
}

[System.Serializable]
public class ImporterModelRule : ImporterRule
{
    public GameObject mesh;
    public ImporterModelSetting settings;

    public override void Init()
    {
        ruleType = RuleType.MODELS; 
        settings = new ImporterModelSetting();
    }
}

[System.Serializable]
public class ImporterModelSetting
{
    static public PropertyInfo[] s_PropertiesInfos;

    static ImporterModelSetting()
    {
        List<PropertyInfo> prop = new List<PropertyInfo>(typeof(ModelImporter).GetProperties());

        for(int i = 0; i < prop.Count; ++i)
        {
            if (!prop[i].CanWrite)
            {
                prop.RemoveAt(i);
                i--;
            }
        }

        s_PropertiesInfos = prop.ToArray();
    }
}