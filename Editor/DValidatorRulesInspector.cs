using UnityEditor;
using UnityEngine;

namespace TossValidator
{
    [CustomEditor(typeof(DValidatorRules))]
    public class DValidatorRulesInspector : Editor
    {
        private DValidatorRules m_src;

        private readonly string[] m_patternsTypes =
        {
            "ExampleOfNaming",
            "exampleOfNaming",
            "example_Of_Naming",
            "example-Of-Naming",
            "Example_Of_Naming"
        };

        //==========================================================================================
        private void OnEnable()
        {
            m_src = (DValidatorRules) target;
        }

        //==========================================================================================
        public override void OnInspectorGUI()
        {
            GUI.enabled = false;

            EditorGUILayout.LabelField("PATTERN OF NAMING", EditorStyles.boldLabel);
            m_src._patternFolders =
                EditorGUILayout.Popup("Folders:", m_src._patternFolders, m_patternsTypes);
            m_src._patternPrefabs =
                EditorGUILayout.Popup("Prefabs:", m_src._patternPrefabs, m_patternsTypes);
            m_src._patternScripts =
                EditorGUILayout.Popup("Scripts:", m_src._patternScripts, m_patternsTypes);
            m_src._patternTextures =
                EditorGUILayout.Popup("Textures:", m_src._patternTextures, m_patternsTypes);
            m_src._patternScenes =
                EditorGUILayout.Popup("Scenes:", m_src._patternScenes, m_patternsTypes);
            m_src._patternGraphics3D =
                EditorGUILayout.Popup("Graphics 3D:", m_src._patternGraphics3D, m_patternsTypes);
            m_src._patternSounds =
                EditorGUILayout.Popup("Sounds:", m_src._patternSounds, m_patternsTypes);
            m_src._patternMaterials =
                EditorGUILayout.Popup("Materials:", m_src._patternMaterials, m_patternsTypes);
            m_src._patternAnimations =
                EditorGUILayout.Popup("Animations:", m_src._patternAnimations, m_patternsTypes);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("ROOT FOLDERS", EditorStyles.boldLabel);
            m_src._rootFolderPrefabs =
                EditorGUILayout.TextField("Prefabs:", m_src._rootFolderPrefabs);
            m_src._rootFolderScripts =
                EditorGUILayout.TextField("Scripts:", m_src._rootFolderScripts);
            m_src._rootFolderTextures =
                EditorGUILayout.TextField("Textures:", m_src._rootFolderTextures);
            m_src._rootFolderScenes =
                EditorGUILayout.TextField("Scenes:", m_src._rootFolderScenes);
            m_src._rootFolderGraphics3D =
                EditorGUILayout.TextField("Graphics 3D:", m_src._rootFolderGraphics3D);
            m_src._rootFolderSounds =
                EditorGUILayout.TextField("Sounds:", m_src._rootFolderSounds);
            m_src._rootFolderMaterials =
                EditorGUILayout.TextField("Materials:", m_src._rootFolderMaterials);
            m_src._rootFolderAnimations =
                EditorGUILayout.TextField("Animations:", m_src._rootFolderAnimations);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("SPECIAL FOLDERS", EditorStyles.boldLabel);
            for (var i = 0; i < m_src._specialFolders.Count; i++)
            {
                EditorGUILayout.TextField("Folder name:", m_src._specialFolders[i]);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("IGNORE FOLDERS", EditorStyles.boldLabel);
            for (var i = 0; i < m_src._ignoreFolders.Count; i++)
            {
                EditorGUILayout.TextField("Folder name:", m_src._ignoreFolders[i]);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("CONDITIONS", EditorStyles.boldLabel);
            for (var i = 0; i < m_src._conditionFormula.Count; i++)
            {
                m_src._conditionFormula[i] = EditorGUILayout.TextField("Name:",
                    m_src._conditionFormula[i]);
                m_src._conditionSelection[i] = EditorGUILayout.IntField("Selection:",
                    m_src._conditionSelection[i]);
                EditorGUILayout.Space();
            }

            GUI.enabled = true;

            EditorGUILayout.Space();

            EditorUtility.SetDirty(m_src);
        }
    }
}