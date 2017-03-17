using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class CustomImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath.Contains("_Configs"))
            return; //it's a config texture, pass

        string[] assets = AssetDatabase.FindAssets("t:ImporterConfig");
        if(assets.Length > 0)
        {
            ImporterConfig config = AssetDatabase.LoadAssetAtPath<ImporterConfig>(AssetDatabase.GUIDToAssetPath(assets[0]));
            ImporterRule toUse = null;
            for(int i = 0; i < config.textureRules.Length; ++i)
            {
                ImporterTextureRule rule = config.textureRules[i];

                if (rule.nameFilter == "")
                {
                    toUse = rule;
                }
                else
                {
                    string[] filters = rule.nameFilter.Split(';');
                    bool filterFound = false;
                    for (int k = 0; k < filters.Length; ++k)
                    {
                        if (assetImporter.assetPath.Contains(filters[k]))
                        {
                            filterFound = true;
                            toUse = rule;
                            break;
                        }
                    }

                    if (filterFound)
                        break;
                }
            }

            if(toUse != null)
            {
                ImporterTextureRule texRule = toUse as ImporterTextureRule;
                TextureImporter importer = assetImporter as TextureImporter;
                TextureImporterSettings settings = new TextureImporterSettings();

                TextureImporter texImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texRule.texture)) as TextureImporter;

                texImporter.ReadTextureSettings(settings);
                importer.SetTextureSettings(settings);
            }
        }
    }

    void OnPreprocessModel()
    {
        if (assetPath.Contains("_Configs"))
            return; //it's a config models, pass

        string[] assets = AssetDatabase.FindAssets("t:ImporterConfig");
        if (assets.Length > 0)
        {
            ImporterConfig config = AssetDatabase.LoadAssetAtPath<ImporterConfig>(AssetDatabase.GUIDToAssetPath(assets[0]));
            ImporterModelRule current = null;
            
            for (int i = 0; i < config.modelRules.Length; ++i)
            {
                ImporterModelRule rule = config.modelRules[i];

                if (rule.nameFilter == "")
                {
                    current = rule;
                }
                else if (assetImporter.assetPath.Contains(rule.nameFilter))
                {
                    current = rule;
                    break;
                }
            }

            if (current != null)
            {
                ModelImporter meshImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(current.mesh)) as ModelImporter;

                ModelImporter importer = assetImporter as ModelImporter;
                for (int j = 0; j < ImporterModelSetting.s_PropertiesInfos.Length; ++j)
                {
                    if (ImporterModelSetting.s_PropertiesInfos[j].Name.Contains("assetBundle"))
                        continue; 

                    object v = ImporterModelSetting.s_PropertiesInfos[j].GetValue(meshImporter, null);
                    ImporterModelSetting.s_PropertiesInfos[j].SetValue(importer, v, null);
                }
            }
        }
    }
}
 